/*
	Copyright (c) 2025 Denis Zykov

	This is part of "Charon: Game Data Editor" Unity Plugin.

	Charon Game Data Editor Unity Plugin is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see http://www.gnu.org/licenses.
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
	public static class GenerateCodeRoutine
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

					progressCallback?.Invoke(string.Format(Resources.UI_UNITYPLUGIN_PROGRESS_PROCESSING_GAMEDATA, CharonFileUtils.GetProjectRelativePath(gameDataAssetPath)), (float)i / total);

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
						codeGenerationPath = "Assets" + Path.DirectorySeparatorChar;
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

					cancellationToken.ThrowIfCancellationRequested();
					var taskList = CharonEditorModule.Instance.RaiseOnGameDataPreSourceCodeGeneration(gameDataAsset, codeGenerationPath);
					await taskList.RunAsync(cancellationToken, logger, nameof(GenerateCodeRoutine)).ConfigureAwait(false);
					cancellationToken.ThrowIfCancellationRequested();

					var startTime = Stopwatch.StartNew();
					logger.Log(LogType.Assert, $"Staring C# code generation for game data '{gameDataLocation}'.");

					progressCallback?.Invoke(string.Format(Resources.UI_UNITYPLUGIN_GENERATE_CODE_FOR, CharonFileUtils.GetProjectRelativePath(gameDataLocation)), (float)i / total);

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
					await File.WriteAllTextAsync(assetCodeGenerationPath, assetCodeGenerator.TransformText());
					forceReImportList.Add(assetCodeGenerationPath);

					logger.Log(LogType.Assert, $"C# code generation for game data '{gameDataPath}' is finished successfully in '{startTime.Elapsed}'.");

					taskList = CharonEditorModule.Instance.RaiseOnGameDataPostSourceCodeGeneration(gameDataAsset, codeGenerationPath);
					await taskList.RunAsync(cancellationToken, logger, nameof(GenerateCodeRoutine)).ConfigureAwait(false);
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

			progressCallback?.Invoke(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1);
		}
	}
}
