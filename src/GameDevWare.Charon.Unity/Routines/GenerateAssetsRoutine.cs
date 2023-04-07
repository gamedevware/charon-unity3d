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
using System.Diagnostics;
using System.IO;
using GameDevWare.Charon.Unity.Async;
using GameDevWare.Charon.Unity.ServerApi.KeyStorage;
using GameDevWare.Charon.Unity.Utils;
using JetBrains.Annotations;
using UnityEditor;

namespace GameDevWare.Charon.Unity.Routines
{
	[PublicAPI, UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public static class GenerateAssetsRoutine
	{
		public static Promise Run(string[] paths, Action<string, float> progressCallback = null)
		{
			return new Async.Coroutine(GenerateAssets(paths, progressCallback));
		}
		public static Promise Schedule(string[] paths, Action<string, float> progressCallback = null)
		{
			return CoroutineScheduler.Schedule(GenerateAssets(paths, progressCallback));
		}

		private static IEnumerable GenerateAssets(string[] paths, Action<string, float> progressCallback)
		{
			var total = paths.Length;
			for (var i = 0; i < paths.Length; i++)
			{
				var gameDataPath = paths[i];
				if (File.Exists(gameDataPath) == false)
					continue;

				if (progressCallback != null) progressCallback(string.Format(Resources.UI_UNITYPLUGIN_PROGRESS_PROCESSING_GAMEDATA, gameDataPath), (float)i / total);


				var gameDataObj = AssetDatabase.LoadAssetAtPath(gameDataPath, typeof(UnityEngine.Object));
				var assetImport = AssetImporter.GetAtPath(gameDataPath);
				if (assetImport == null)
					continue;

				var gameDataSettings = GameDataSettings.Load(gameDataObj);
				var assetGenerationPath = FileHelper.MakeProjectRelative(gameDataSettings.AssetGenerationPath);
				if (string.IsNullOrEmpty(assetGenerationPath))
					continue;

				// synchronize with online service if required
				var synchronizeAssetTask = new Async.Coroutine(SynchronizeAssetIfNeeded(gameDataPath, gameDataSettings, progressCallback.Sub((float)i / total, i + 1.0f / total)));
				yield return synchronizeAssetTask.IgnoreFault();

				// trying to touch gamedata file
				var readGameDataTask = FileHelper.ReadFileAsync(gameDataPath, 5);
				yield return readGameDataTask;

				var startTime = Stopwatch.StartNew();
				var gameDataBytes = default(byte[]);
				using (var file = readGameDataTask.GetResult())
				{
					if (file.Length == 0)
						continue;

					if (Settings.Current.Verbose)
					{
						UnityEngine.Debug.Log(string.Format("Starting asset generation of game data at '{0}'.", gameDataPath));
					}

					gameDataBytes = new byte[file.Length];
					int read, offset = 0;
					while ((read = file.Read(gameDataBytes, offset, gameDataBytes.Length - offset)) > 0)
						offset += read;
				}

				var gameDataAssetType = Type.GetType(gameDataSettings.Namespace + "." + gameDataSettings.GameDataClassName + "Asset, Assembly-CSharp", throwOnError: false) ??
					Type.GetType(gameDataSettings.Namespace + "." + gameDataSettings.GameDataClassName + "Asset, Assembly-CSharp-firstpass", throwOnError: false) ??
					Type.GetType(gameDataSettings.Namespace + "." + gameDataSettings.GameDataClassName + "Asset, Assembly-CSharp-Editor", throwOnError: false);
				if (gameDataAssetType == null)
				{
					UnityEngine.Debug.LogError(Resources.UI_UNITYPLUGIN_GENERATE_ASSET_CANT_FIND_GAMEDATA_CLASS);
					continue;
				}

				var assetDirectory = Path.GetDirectoryName(assetGenerationPath);
				if (assetDirectory != null && !Directory.Exists(assetDirectory))
				{
					Directory.CreateDirectory(assetDirectory);
				}

				var gameDataAsset = UnityEngine.ScriptableObject.CreateInstance(gameDataAssetType);
				gameDataAsset.SetFieldValue("dataBytes", gameDataBytes);
				gameDataAsset.SetFieldValue("extension", Path.GetExtension(gameDataPath));
				AssetDatabase.CreateAsset(gameDataAsset, assetGenerationPath);
				AssetDatabase.SaveAssets();

				if (Settings.Current.Verbose)
				{
					UnityEngine.Debug.Log(string.Format("Asset generation of game data at '{0}' is finished successfully in '{1}'.", gameDataPath, startTime.Elapsed));
				}
			}
			if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_GENERATE_REFRESHING_ASSETS, 0.99f);
			AssetDatabase.Refresh(ImportAssetOptions.Default);
			if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1);
		}
		private static IEnumerable SynchronizeAssetIfNeeded(string gameDataPath, GameDataSettings gameDataSettings, Action<string, float> progressCallback)
		{
			if (!gameDataSettings.AutoSynchronization ||
				!gameDataSettings.IsConnected)
			{
				yield break; // no sync required
			}

			var serverAddress = new Uri(gameDataSettings.ServerAddress);
			var apiKeyPath = new Uri(serverAddress, "/" + gameDataSettings.ProjectId);
			var apiKey = KeyCryptoStorage.GetKey(apiKeyPath);
			if (apiKey == null)
			{
				if (Settings.Current.Verbose)
				{
					UnityEngine.Debug.LogWarning(string.Format("Unable to synchronize game data at '{0}' because there is API Key associated with it. ", gameDataPath) +
						"Find this asset in Project window and click 'Synchronize' button in Inspector window.");
				}
				yield break; // no key
			}

			var synchronizeAssetTask = SynchronizeAssetsRoutine.Run(false, new[] { gameDataPath }, progressCallback);
			yield return synchronizeAssetTask;
		}
	}
}
