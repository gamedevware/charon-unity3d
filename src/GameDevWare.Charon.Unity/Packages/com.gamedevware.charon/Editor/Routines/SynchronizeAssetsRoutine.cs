/*
	Copyright (c) 2023 Denis Zykov

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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GameDevWare.Charon.Unity.Async;
using GameDevWare.Charon.Unity.ServerApi;
using GameDevWare.Charon.Unity.ServerApi.KeyStorage;
using GameDevWare.Charon.Unity.Utils;
using GameDevWare.Charon.Unity.Windows;
using JetBrains.Annotations;
using UnityEditor;

namespace GameDevWare.Charon.Unity.Routines
{
	[PublicAPI, UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal class SynchronizeAssetsRoutine
	{
		public static Promise Run(bool force, string[] paths = null, Action<string, float> progressCallback = null, Promise cancellation = null)
		{
			return new Async.Coroutine(SynchronizeAssets(force, paths, progressCallback, cancellation));
		}
		public static Promise Schedule(bool force, string[] paths = null, Action<string, float> progressCallback = null, string coroutineId = null, Promise cancellation = null)
		{
			return CoroutineScheduler.Schedule<Dictionary<string, object>>(SynchronizeAssets(force, paths, progressCallback, cancellation), coroutineId);
		}

		private static IEnumerable SynchronizeAssets(bool force, string[] paths = null, Action<string, float> progressCallback = null, Promise cancellation = null)
		{
			if (paths == null) paths = GameDataTracker.All.ToArray();

			var total = paths.Length;
			for (var i = 0; i < paths.Length; i++)
			{
				cancellation.ThrowIfCancellationRequested();

				var gameDataPath = paths[i];

				if (File.Exists(gameDataPath) == false)
					continue;


				if (progressCallback != null) progressCallback(string.Format(Resources.UI_UNITYPLUGIN_PROGRESS_PROCESSING_GAMEDATA, gameDataPath), (float)i / total);


				var gameDataObj = AssetDatabase.LoadAssetAtPath(gameDataPath, typeof(UnityEngine.Object));
				var assetImport = AssetImporter.GetAtPath(gameDataPath);
				if (assetImport == null)
					continue;

				var gameDataSettings = GameDataSettings.Load(gameDataObj);

				if (!gameDataSettings.IsConnected)
				{
					continue; // no sync required
				}

				var isSyncTooSoon = (DateTime.UtcNow - File.GetLastWriteTimeUtc(gameDataPath)) < TimeSpan.FromMinutes(2);
				if (isSyncTooSoon && !force)
				{
					if (Settings.Current.Verbose)
					{
						UnityEngine.Debug.Log(string.Format("Skipping synchronization game data at '{0}' because there not much time passes since last one.", gameDataPath));
					}
					continue;
				}

				var storeFormat = StorageFormats.GetStoreFormat(gameDataPath);
				if (storeFormat == null)
				{
					UnityEngine.Debug.LogWarning(string.Format("Skipping synchronization game data at '{0}' because storage format '{1}' is not supported.", gameDataPath, Path.GetExtension(gameDataPath)));
					continue;
				}

				var serverAddress = new Uri(gameDataSettings.ServerAddress);
				var apiKeyPath = new Uri(serverAddress, "/" + gameDataSettings.ProjectId);
				var apiKey = KeyCryptoStorage.GetKey(apiKeyPath);
				if (string.IsNullOrEmpty(apiKey))
				{
					if (force)
					{
						var apiKeyPromptTask = ApiKeyPromptWindow.ShowAsync(gameDataSettings.ProjectId, gameDataSettings.ProjectName);
						yield return apiKeyPromptTask;
						apiKey = KeyCryptoStorage.GetKey(apiKeyPath);
					}

					if (string.IsNullOrEmpty(apiKey))
					{
						if (Settings.Current.Verbose)
						{
							UnityEngine.Debug.LogWarning(string.Format("Unable to synchronize game data at '{0}' because there is API Key associated with it. ", gameDataPath) +
								"Find this asset in Project window and click 'Synchronize' button in Inspector window.");
						}
						continue; // no key
					}
				}

				cancellation.ThrowIfCancellationRequested();

				// trying to touch gamedata file
				var readGameDataTask = FileHelper.ReadFileAsync(gameDataPath, 5);
				yield return readGameDataTask;
				readGameDataTask.GetResult().Dispose();

				var startTime = Stopwatch.StartNew();
				if (Settings.Current.Verbose)
				{
					UnityEngine.Debug.Log(string.Format("Starting synchronization of game data at '{0}' from server '{1}'.", gameDataPath, serverAddress));
				}

				var downloadTempPath = Path.Combine(FileHelper.GetRandomTempDirectory(), Path.GetFileName(gameDataPath));
				var gameDataPathBak = gameDataPath + ".bak";
				var serverApiClient = new ServerApiClient(serverAddress);
				serverApiClient.UseApiKey(apiKey);

				var subProgressCallback = progressCallback != null ? progressCallback.Sub(i + 0.0f / total, i + 1.0f / total) : null;
				var downloadDataSourceAsync = serverApiClient.DownloadDataSourceAsync(gameDataSettings.BranchId, storeFormat.Value, downloadTempPath,
					subProgressCallback.ToDownloadProgress(Path.GetFileName(downloadTempPath)), cancellation: cancellation);
				yield return downloadDataSourceAsync;

				File.Replace(downloadTempPath, gameDataPath, gameDataPathBak);

				FileHelper.SafeFileDelete(gameDataPathBak);
				FileHelper.SafeDirectoryDelete(Path.GetDirectoryName(downloadTempPath));

				if (Settings.Current.Verbose)
				{
					UnityEngine.Debug.Log(string.Format("Synchronization of game data at '{0}' from server '{1}' is finished successfully in '{2}'.", gameDataPath, serverAddress, startTime.Elapsed));
				}
			}

			if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_GENERATE_REFRESHING_ASSETS, 0.99f);

			AssetDatabase.Refresh(ImportAssetOptions.Default);

			if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1);
		}
	}
}
