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
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GameDevWare.Charon.Editor.ServerApi;
using GameDevWare.Charon.Editor.Utils;
using GameDevWare.Charon.Editor.Windows;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.Charon.Editor.Routines
{
	[PublicAPI]
	internal class SynchronizeAssetsRoutine
	{

		public static Task ScheduleAsync(bool force, string[] paths = null, Action<string, float> progressCallback = null, CancellationToken cancellation = default)
		{
			return CharonEditorModule.Instance.Routines.Schedule(() => RunAsync(force, paths, progressCallback, cancellation), cancellation);
		}

		public static Task RunAsync(bool force, string[] paths = null, Action<string, float> progressCallback = null, CancellationToken cancellation = default)
		{
			var task = RunInternalAsync(force, paths, progressCallback);
			task.LogFaultAsError();
			return task;
		}
		public static async Task RunInternalAsync(bool force, string[] paths = null, Action<string, float> progressCallback = null, CancellationToken cancellation = default)
		{
			paths ??= Array.ConvertAll(AssetDatabase.FindAssets("t:" + nameof(GameDataBase)), AssetDatabase.GUIDToAssetPath);

			var logger = CharonEditorModule.Instance.Logger;

			var total = paths.Length;
			for (var i = 0; i < paths.Length; i++)
			{
				cancellation.ThrowIfCancellationRequested();

				var gameDataAssetPath = paths[i];

				if (File.Exists(gameDataAssetPath) == false)
					continue;


				progressCallback?.Invoke(string.Format(Resources.UI_UNITYPLUGIN_PROGRESS_PROCESSING_GAMEDATA, gameDataAssetPath), (float)i / total);

				var gameDataAsset = AssetDatabase.LoadAssetAtPath<GameDataBase>(gameDataAssetPath);
				if (gameDataAsset == null)
				{
					continue;
				}

				var gameDataSettings = gameDataAsset.settings;
				if (!gameDataSettings.IsConnected)
				{
					continue; // no sync required
				}

				var isSyncTooSoon = (DateTime.UtcNow - File.GetLastWriteTimeUtc(gameDataAssetPath)) < TimeSpan.FromMinutes(2);
				if (isSyncTooSoon && !force)
				{
					logger.Log(LogType.Assert, $"Skipping synchronization game data at '{gameDataAssetPath}' because there not much time passes since last one.");
					continue;
				}

				var gameDataFormat = FormatsExtensions.GetGameDataFormatForExtension(gameDataAssetPath);
				if (gameDataFormat == null)
				{
					logger.Log(LogType.Warning, $"Skipping synchronization game data at '{gameDataAssetPath}' because storage format '{Path.GetExtension(gameDataAssetPath)}' is not supported.");
					continue;
				}

				var serverAddress = new Uri(gameDataSettings.serverAddress);
				var apiKeyPath = new Uri(serverAddress, "/" + gameDataSettings.projectId);
				var apiKey = CharonEditorModule.Instance.KeyCryptoStorage.GetKey(apiKeyPath);
				if (string.IsNullOrEmpty(apiKey))
				{
					if (force)
					{
						await ApiKeyPromptWindow.ShowAsync(gameDataSettings.projectId, gameDataSettings.projectName);
						apiKey = CharonEditorModule.Instance.KeyCryptoStorage.GetKey(apiKeyPath);
					}

					if (string.IsNullOrEmpty(apiKey))
					{
						logger.Log(LogType.Warning, $"Unable to synchronize game data at '{gameDataAssetPath}' because there is API Key associated with it. " +
							"Find this asset in Project window and click 'Synchronize' button in Inspector window.");
						continue; // no key
					}
				}

				cancellation.ThrowIfCancellationRequested();

				// trying to touch gamedata file
				var gameDataPath = AssetDatabase.GUIDToAssetPath(gameDataSettings.gameDataFileGuid);
				{
					await using var readGameDataTask = await FileHelper.ReadFileAsync(gameDataPath, 5);
				}

				var startTime = Stopwatch.StartNew();
				logger.Log(LogType.Assert, $"Starting synchronization of game data at '{gameDataPath}' from server '{serverAddress}'.");

				var downloadTempPath = Path.Combine(FileHelper.GetRandomTempDirectory(), Path.GetFileName(gameDataPath));
				var gameDataPathBak = gameDataPath + ".bak";
				var serverApiClient = new ServerApiClient(serverAddress);
				serverApiClient.UseApiKey(apiKey);

				var subProgressCallback = progressCallback?.Sub(i + 0.0f / total, i + 1.0f / total);
				await serverApiClient.DownloadDataSourceAsync(gameDataSettings.branchId, gameDataFormat.Value, downloadTempPath,
					subProgressCallback.ToDownloadProgress(Path.GetFileName(downloadTempPath)), cancellation: cancellation);

				File.Replace(downloadTempPath, gameDataPath, gameDataPathBak);

				FileHelper.SafeFileDelete(gameDataPathBak);
				FileHelper.SafeDirectoryDelete(Path.GetDirectoryName(downloadTempPath));

				logger.Log(LogType.Assert,$"Synchronization of game data at '{gameDataPath}' from server '{serverAddress}' is finished successfully in '{startTime.Elapsed}'.");
			}

			progressCallback?.Invoke(Resources.UI_UNITYPLUGIN_GENERATE_REFRESHING_ASSETS, 0.99f);

			AssetDatabase.Refresh(ImportAssetOptions.Default);

			progressCallback?.Invoke(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1);
		}
	}
}
