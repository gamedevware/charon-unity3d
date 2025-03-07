/*
	Copyright (c) 2025 GameDevWare, Denis Zykov

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GameDevWare.Charon.Editor.Cli;
using GameDevWare.Charon.Editor.Utils;
using GameDevWare.Charon.Editor.Windows;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.Charon.Editor.Routines
{
	[PublicAPI]
	public static class GenerateSourceCodeRoutine
	{
		public static Task ScheduleAsync(string[] paths = null, Action<string, float> progressCallback = null, CancellationToken cancellationToken = default)
		{
			return CharonEditorModule.Instance.Routines.Schedule(() => RunAsync(paths, progressCallback, cancellationToken), cancellationToken);
		}

		public static Task RunAsync(string[] paths = null, Action<string, float> progressCallback = null, CancellationToken cancellationToken = default)
		{
			var task = RunInternalAsync(paths, progressCallback, cancellationToken);
			task.LogFaultAsError();
			return task;
		}
		public static async Task RunInternalAsync(string[] paths, Action<string, float> progressCallback, CancellationToken cancellationToken)
		{
			paths ??= Array.ConvertAll(AssetDatabase.FindAssets("t:" + nameof(GameDataBase)), AssetDatabase.GUIDToAssetPath);

			var logger = CharonEditorModule.Instance.Logger;

			var total = paths.Length;
			var forceReImportList = new List<string>();

			cancellationToken.ThrowIfCancellationRequested();

			EditorApplication.LockReloadAssemblies();
			try
			{
				for (var i = 0; i < paths.Length; i++)
				{
					var gameDataAssetPath = paths[i];
					if (File.Exists(gameDataAssetPath) == false)
					{
						continue;
					}
					var pathSpecificProgress = progressCallback?.Sub((float)i / total, i + 1.0f / total);
					pathSpecificProgress?.Invoke(string.Format(Resources.UI_UNITYPLUGIN_PROGRESS_PROCESSING_GAMEDATA, CharonFileUtils.GetProjectRelativePath(gameDataAssetPath)), 0);

					var gameDataAsset = AssetDatabase.LoadAssetAtPath<GameDataBase>(gameDataAssetPath);
					if (gameDataAsset == null)
					{
						continue;
					}

					var gameDataSettings = gameDataAsset.settings;
					var gameDataPath = AssetDatabase.GUIDToAssetPath(gameDataSettings.gameDataFileGuid) ?? string.Empty;

					var codeGenerationPath = gameDataSettings.codeGenerationPath;
					if (string.IsNullOrEmpty(codeGenerationPath) && gameDataAsset.GetType() != typeof(GameDataBase))
					{
						var monoScript = MonoScript.FromScriptableObject(gameDataAsset);
						if (monoScript != null)
						{
							codeGenerationPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(monoScript) ?? "");
						}
					}
					if (string.IsNullOrEmpty(codeGenerationPath))
					{
						codeGenerationPath = Path.GetDirectoryName(gameDataAssetPath);
					}

					var optimizations = (SourceCodeGenerationOptimizations)gameDataSettings.optimizations;

					logger.Log(LogType.Assert, $"Preparing C# code generation for game data '{gameDataPath}', code generation path '{codeGenerationPath}'.");

					if (AppDomain.CurrentDomain.GetAssemblies().All(a => a.GetName().Name != "GameDevWare.Dynamic.Expressions")) // no expression library installed
					{
						optimizations |= SourceCodeGenerationOptimizations.DisableFormulaCompilation;
					}

					// trying to touch gamedata file
					await using (var file = await CharonFileUtils.ReadFileAsync(gameDataPath, 5))
					{
						if (file.Length == 0)
						{
							logger.Log(LogType.Assert, $"Code generation was skipped for an empty file '{gameDataPath}'.");
							continue;
						}
					}

					CharonEditorModule.Instance.AssetImporter.ImportOnStart(gameDataAssetPath);

					optimizations &= ~(
						SourceCodeGenerationOptimizations.DisableJsonSerialization |
						SourceCodeGenerationOptimizations.DisableMessagePackSerialization
					);

					var gameDataLocation = default(string);
					var apiKey = string.Empty;
					if (gameDataSettings.IsConnected)
					{
						var serverAddress = new Uri(gameDataSettings.serverAddress);
						var apiKeyPath = new Uri(serverAddress, "/" + gameDataSettings.projectId);
						apiKey = CharonEditorModule.Instance.KeyCryptoStorage.GetKey(apiKeyPath);
						if (string.IsNullOrEmpty(apiKey))
						{
							await ApiKeyPromptWindow.ShowAsync(gameDataSettings.projectId, gameDataSettings.projectName);
							apiKey = CharonEditorModule.Instance.KeyCryptoStorage.GetKey(apiKeyPath);

							if (string.IsNullOrEmpty(apiKey))
							{
								logger.Log(LogType.Warning, $"Unable to generate code for game data at '{gameDataPath}' because there is API Key associated with it. " +
										"Find this asset in Project window and click 'Synchronize' button in Inspector window.");

								continue; // no key
							}
						}

						gameDataLocation = gameDataSettings.MakeDataSourceUrl().OriginalString;
					}
					else
					{
						gameDataLocation = Path.GetFullPath(gameDataPath);
					}

					pathSpecificProgress?.Invoke(Resources.UI_UNITYPLUGIN_PROGRESS_RUNNING_PRE_TASKS, 0.10f);

					cancellationToken.ThrowIfCancellationRequested();
					var taskList = CharonEditorModule.Instance.RaiseOnGameDataPreSourceCodeGeneration(gameDataAsset, codeGenerationPath);
					await taskList.RunAsync(cancellationToken, logger, nameof(GenerateSourceCodeRoutine)).ConfigureAwait(false);
					cancellationToken.ThrowIfCancellationRequested();

					var startTime = Stopwatch.StartNew();
					logger.Log(LogType.Assert, $"Staring C# code generation for game data '{gameDataLocation}'.");

					pathSpecificProgress?.Invoke(string.Format(Resources.UI_UNITYPLUGIN_GENERATE_CODE_FOR, CharonFileUtils.GetProjectRelativePath(gameDataLocation)), 0.30f);

					try
					{
						await CharonCli.GenerateCSharpCodeAsync
						(
							gameDataLocation,
							apiKey,
							outputDirectory: Path.GetFullPath(codeGenerationPath),
							documentClassName: gameDataSettings.gameDataDocumentClassName,
							gameDataClassName: gameDataSettings.gameDataClassName,
							gameDataNamespace: gameDataSettings.gameDataNamespace,
							defineConstants: gameDataSettings.defineConstants ?? string.Empty,
							sourceCodeGenerationOptimizations: optimizations,
							sourceCodeIndentation: (SourceCodeIndentation)gameDataSettings.indentation,
							sourceCodeLineEndings: (SourceCodeLineEndings)gameDataSettings.lineEnding,
							clearOutputDirectory: gameDataSettings.clearOutputDirectory,
							splitFiles: gameDataSettings.splitSourceCodeFiles,
							CharonEditorModule.Instance.Settings.LogLevel
						);
					}
					catch (Exception generationError)
					{
						logger.Log(LogType.Error, string.Format(Resources.UI_UNITYPLUGIN_GENERATE_FAILED_DUE_ERRORS, gameDataPath, generationError.Unwrap().Message));
						logger.Log(LogType.Error, generationError.Unwrap());
					}

					forceReImportList.AddRange(Directory.GetFiles(codeGenerationPath, "*.cs").Select(CharonFileUtils.GetProjectRelativePath));

					var assetCodeGenerationPath = Path.Combine(codeGenerationPath, gameDataSettings.gameDataClassName + "Asset.cs");
					var assetCodeGenerator = new CSharp73GameDataFromAssetGenerator {
						AssetClassName = gameDataSettings.gameDataClassName + "Asset",
						GameDataClassName = gameDataSettings.gameDataNamespace + "." + gameDataSettings.gameDataClassName,
						Namespace = gameDataSettings.gameDataNamespace
					};
					await File.WriteAllTextAsync(assetCodeGenerationPath, assetCodeGenerator.TransformText(), CancellationToken.None);
					forceReImportList.Add(assetCodeGenerationPath);

					logger.Log(LogType.Assert, $"C# code generation for game data '{gameDataPath}' is finished successfully in '{startTime.Elapsed}'.");

					pathSpecificProgress?.Invoke(Resources.UI_UNITYPLUGIN_PROGRESS_RUNNING_POST_TASKS, 0.90f);

					taskList = CharonEditorModule.Instance.RaiseOnGameDataPostSourceCodeGeneration(gameDataAsset, codeGenerationPath);
					await taskList.RunAsync(cancellationToken, logger, nameof(GenerateSourceCodeRoutine)).ConfigureAwait(false);

					pathSpecificProgress?.Invoke(string.Format(Resources.UI_UNITYPLUGIN_GENERATE_CODE_FOR, CharonFileUtils.GetProjectRelativePath(gameDataLocation)), 1.00f);
				}
			}
			finally
			{
				EditorApplication.UnlockReloadAssemblies();
			}

			progressCallback?.Invoke(Resources.UI_UNITYPLUGIN_GENERATE_REFRESHING_ASSETS, 0.99f);
			foreach (var forceReImportPath in forceReImportList)
			{
				AssetDatabase.ImportAsset(forceReImportPath, ImportAssetOptions.ForceUpdate);
			}
			AssetDatabase.Refresh();
			EditorUtility.RequestScriptReload();

			progressCallback?.Invoke(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1.00f);
		}
	}
}
