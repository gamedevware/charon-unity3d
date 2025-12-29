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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GameDevWare.Charon.Editor.Cli;
using GameDevWare.Charon.Editor.Services.ServerApi;
using GameDevWare.Charon.Editor.Utils;
using GameDevWare.Charon.Editor.Windows;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace GameDevWare.Charon.Editor.Routines
{
	[PublicAPI]
	public static class SynchronizeAssetsRoutine
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
		private static async Task RunInternalAsync(string[] paths, Action<string, float> progressCallback, CancellationToken cancellationToken)
		{
			paths ??= Array.ConvertAll(AssetDatabase.FindAssets("t:" + nameof(GameDataBase)), AssetDatabase.GUIDToAssetPath);

			var logger = CharonEditorModule.Instance.Logger;

			EditorApplication.LockReloadAssemblies();
			try
			{
				var total = paths.Length;
				for (var i = 0; i < paths.Length; i++)
				{
					var gameDataAssetPath = paths[i];
					if (File.Exists(gameDataAssetPath) == false)
						continue;

					var pathSpecificProgress = progressCallback?.Sub((float)i / total, i + 1.0f / total);
					pathSpecificProgress?.Invoke(string.Format(Resources.UI_UNITYPLUGIN_PROGRESS_PROCESSING_GAMEDATA, gameDataAssetPath), 0.00f);

					var gameDataAsset = AssetDatabase.LoadAssetAtPath<GameDataBase>(gameDataAssetPath);
					if (gameDataAsset == null)
					{
						continue;
					}

					cancellationToken.ThrowIfCancellationRequested();
					var gameDataSettings = gameDataAsset.settings;
					var gameDataPath = AssetDatabase.GUIDToAssetPath(gameDataSettings.gameDataFileGuid) ?? string.Empty;
					var publishFilePath = Path.Combine(CharonFileUtils.TempPath, $"gamedata_{Guid.NewGuid():N}.tmp");


					pathSpecificProgress?.Invoke(Resources.UI_UNITYPLUGIN_PROGRESS_RUNNING_PRE_TASKS, 0.10f);
					var taskList = CharonEditorModule.Instance.RaiseOnGameDataPreSynchronization(gameDataAsset, publishFilePath);
					await taskList.RunAsync(cancellationToken, logger, nameof(SynchronizeAssetsRoutine)).ConfigureAwait(false);
					cancellationToken.ThrowIfCancellationRequested();

					pathSpecificProgress?.Invoke(Resources.UI_UNITYPLUGIN_GENERATING_CODE_AND_ASSETS, 0.30f);

					// synchronize with online service if required
					await SynchronizeAssetIfNeededAsync(gameDataAssetPath, gameDataSettings, pathSpecificProgress?.Sub(0.30f, 0.50f), logger,
						cancellationToken).IgnoreFault();

					pathSpecificProgress?.Invoke(null, 0.50f);

					var startTime = Stopwatch.StartNew();
					var gameDataAssetClassName = gameDataSettings.gameDataNamespace + "." + gameDataSettings.gameDataClassName + "Asset";
					var gameDataAssetType = default(Type);
					foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
					{
						gameDataAssetType = assembly.GetType(gameDataAssetClassName, throwOnError: false);
						if (gameDataAssetType != null)
						{
							break;
						}
					}

					if (gameDataAssetType == null)
					{
						logger.Log(LogType.Error, string.Format(Resources.UI_UNITYPLUGIN_GENERATE_ASSET_CANT_FIND_GAMEDATA_CLASS, gameDataAssetClassName,
							string.Join(", ", AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name))));
						continue;
					}

					var publishFormat = (GameDataFormat)gameDataAsset.settings.publishFormat;
					var exportPublicationFormat = publishFormat switch {
						GameDataFormat.Json => ExportFormat.Json,
						GameDataFormat.MessagePack => ExportFormat.MessagePack,
						_ => throw new ArgumentOutOfRangeException($"Unknown {nameof(GameDataFormat)} value '{publishFormat}' in settings of '{gameDataAssetPath}' asset.")
					};

					logger.Log(LogType.Assert, $"Exporting game data from '{gameDataPath}' into temporary file '{publishFilePath}'[format: {publishFormat}].");

					await CharonCli.ExportToFileAsync(Path.GetFullPath(gameDataPath), apiKey: null, exportedDocumentsFilePath: publishFilePath, format: exportPublicationFormat,
						schemaNamesOrIds: new [] { "*" }, properties: Array.Empty<string>(), languages: gameDataAsset.settings.publishLanguages ?? Array.Empty<string>(),
						exportMode: ExportMode.Publication, logsVerbosity: CharonEditorModule.Instance.Settings.LogLevel).ConfigureAwait(true);

					pathSpecificProgress?.Invoke(null, 0.70f);

					using (var fileStream = await CharonFileUtils.ReadFileAsync(publishFilePath, 5)) // trying to touch gamedata file
					{
						if (gameDataAsset.GetType() != gameDataAssetType)
						{
							logger.Log(LogType.Assert, $"Asset at '{gameDataAssetPath}'({AssetDatabase.AssetPathToGUID(gameDataAssetPath)}) has type " +
								$"'{gameDataAsset.GetType().Name}' while '{gameDataAssetType.Name}' is expected. Recreating it with new type.");

							var newGameDataAsset = (GameDataBase)ScriptableObject.CreateInstance(gameDataAssetType);
							newGameDataAsset.settings = gameDataAsset.settings;
							UnityObject.DestroyImmediate(gameDataAsset, allowDestroyingAssets: true);
							CharonFileUtils.SafeFileDelete(gameDataAssetPath);
							AssetDatabase.CreateAsset(newGameDataAsset, gameDataAssetPath);

							gameDataAsset = AssetDatabase.LoadAssetAtPath<GameDataBase>(gameDataAssetPath);

							logger.Log(LogType.Assert, $"Asset at '{gameDataAssetPath}'({AssetDatabase.AssetPathToGUID(gameDataAssetPath)}) " +
								$"now is '{gameDataAsset.GetType().Name}' type.");
						}

						logger.Log(LogType.Assert, $"Pushing content of '{gameDataPath}' into '{gameDataAssetPath}'({AssetDatabase.AssetPathToGUID(gameDataAssetPath)}) asset with '{publishFormat}' format.");

						gameDataAsset.Save(fileStream, publishFormat);
					}

					pathSpecificProgress?.Invoke(null, 0.80f);

					EditorUtility.SetDirty(gameDataAsset);
					AssetDatabase.SaveAssetIfDirty(gameDataAsset);

					UpdateAssetImporterPaths(gameDataPath, gameDataAssetPath, gameDataAsset);

					logger.Log(LogType.Assert, $"Asset generation of game data at '{gameDataAssetPath}' is finished " +
						$"successfully in '{startTime.Elapsed}'.");

					pathSpecificProgress?.Invoke(Resources.UI_UNITYPLUGIN_PROGRESS_RUNNING_POST_TASKS, 0.90f);

					taskList = CharonEditorModule.Instance.RaiseOnGameDataPostSynchronization(gameDataAsset, publishFilePath);
					await taskList.RunAsync(cancellationToken, logger, nameof(SynchronizeAssetsRoutine)).ConfigureAwait(false);
				}
			}
			catch (Exception reimportError)
			{
				logger.Log(LogType.Error, reimportError.Unwrap());
				throw;
			}
			finally
			{
				EditorApplication.UnlockReloadAssemblies();
			}

			progressCallback?.Invoke(Resources.UI_UNITYPLUGIN_GENERATE_REFRESHING_ASSETS, 0.99f);
			AssetDatabase.Refresh(ImportAssetOptions.Default);
			progressCallback?.Invoke(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1);
		}

		private static void UpdateAssetImporterPaths(string gameDataPath, string gameDataAssetPath, GameDataBase gameDataAsset)
		{
			// update gameDataImporter info in case of compilation error
			var gameDataImporter = AssetImporter.GetAtPath(gameDataPath) as GameDataImporter;
			if (gameDataImporter == null) return;

			gameDataImporter.lastImportAssetPath = AssetDatabase.GetAssetPath(gameDataAsset);
			gameDataImporter.lastImportSettings = new GameDataSettings();
			EditorUtility.CopySerializedManagedFieldsOnly(gameDataAsset.settings, gameDataImporter.lastImportSettings);

			var monoScript = MonoScript.FromScriptableObject(gameDataAsset);
			if (monoScript != null)
			{
				gameDataImporter.lastImportSettings.codeGenerationPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(monoScript) ?? "")?.Replace("\\", "/");
			}

			EditorUtility.SetDirty(gameDataImporter);
			gameDataImporter.SaveAndReimport();
		}

		private static async Task SynchronizeAssetIfNeededAsync
		(
			string gameDataAssetPath,
			GameDataSettings gameDataSettings,
			Action<string, float> progressCallback,
			ILogger logger,
			CancellationToken cancellationToken)
		{
			if (!gameDataSettings.IsConnected)
			{
				return; // no sync required
			}

			var serverAddress = new Uri(gameDataSettings.serverAddress);
			var apiKeyPath = new Uri(serverAddress, "/" + gameDataSettings.projectId);
			var apiKey = CharonEditorModule.Instance.KeyCryptoStorage.GetKey(apiKeyPath);
			if (string.IsNullOrEmpty(apiKey))
			{
				await ApiKeyPromptWindow.ShowAsync(gameDataSettings.projectId, gameDataSettings.projectName);
				apiKey = CharonEditorModule.Instance.KeyCryptoStorage.GetKey(apiKeyPath);

				if (string.IsNullOrEmpty(apiKey))
				{
					logger.Log(LogType.Warning, $"Unable to synchronize game data at '{AssetDatabase.GUIDToAssetPath(gameDataSettings.gameDataFileGuid)}' because there is API Key associated with it. " +
						"Find this asset in Project window and click 'Synchronize' button in Inspector window.");
					return; // no key
				}
			}

			cancellationToken.ThrowIfCancellationRequested();

			progressCallback?.Invoke(string.Format(Resources.UI_UNITYPLUGIN_PROGRESS_PROCESSING_GAMEDATA, gameDataAssetPath), 0.01f);

			if (!gameDataSettings.IsConnected)
			{
				return; // no sync required
			}

			cancellationToken.ThrowIfCancellationRequested();

			// trying to touch gamedata file
			var gameDataPath = AssetDatabase.GUIDToAssetPath(gameDataSettings.gameDataFileGuid);
			{
				await using var readGameDataTask = await CharonFileUtils.ReadFileAsync(gameDataPath, 5);
			}

			var startTime = Stopwatch.StartNew();
			logger.Log(LogType.Assert, $"Starting synchronization of game data at '{gameDataPath}' from server '{serverAddress}'.");

			var downloadTempPath = Path.Combine(CharonFileUtils.GetRandomTempDirectory(), Path.GetFileName(gameDataPath));
			var gameDataPathBak = gameDataPath + ".bak";
			var serverApiClient = new ServerApiClient(serverAddress);
			serverApiClient.UseApiKey(apiKey);

			var downloadFormat = gameDataSettings.publishFormat switch {
				(int) GameDataFormat.Json => GameDataFormat.Json,
				(int) GameDataFormat.MessagePack => GameDataFormat.MessagePack,
				_ => throw new ArgumentOutOfRangeException($"Unknown {nameof(GameDataFormat)} value '{(GameDataFormat)gameDataSettings.publishFormat}' in settings of '{gameDataAssetPath}' asset.")
			};

			await serverApiClient.DownloadDataSourceAsync(gameDataSettings.branchId, downloadFormat, downloadTempPath,
				progressCallback.ToDownloadProgress(Path.GetFileName(downloadTempPath)), cancellation: cancellationToken);

			File.Replace(downloadTempPath, gameDataPath, gameDataPathBak);

			CharonFileUtils.SafeFileDelete(gameDataPathBak);
			CharonFileUtils.SafeDirectoryDelete(Path.GetDirectoryName(downloadTempPath));

			logger.Log(LogType.Assert, $"Synchronization of game data at '{gameDataPath}' from server '{serverAddress}' is finished successfully in '{startTime.Elapsed}'.");

			progressCallback?.Invoke(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1.00f);
		}
	}
}
