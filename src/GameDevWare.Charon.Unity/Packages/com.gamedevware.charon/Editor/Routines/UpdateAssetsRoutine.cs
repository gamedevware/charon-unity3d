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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GameDevWare.Charon.Editor.Utils;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace GameDevWare.Charon.Editor.Routines
{
	[PublicAPI]
	public static class UpdateAssetsRoutine
	{
		public static Task ScheduleAsync(string[] paths, CancellationToken cancellation = default)
		{
			return CharonEditorModule.Instance.Routines.Schedule(() => RunAsync(paths, null), cancellation);
		}

		public static Task RunAsync(string[] paths = null, Action<string, float> progressCallback = null)
		{
			var task = RunInternalAsync(paths, progressCallback);
			task.LogFaultAsError();
			return task;
		}
		private static async Task RunInternalAsync(string[] paths, Action<string, float> progressCallback)
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

					progressCallback?.Invoke(string.Format(Resources.UI_UNITYPLUGIN_PROGRESS_PROCESSING_GAMEDATA, gameDataAssetPath), (float)i / total);

					var gameDataAsset = AssetDatabase.LoadAssetAtPath<GameDataBase>(gameDataAssetPath);
					if (gameDataAsset == null)
					{
						continue;
					}

					var gameDataSettings = gameDataAsset.settings;
					var gameDataPath = AssetDatabase.GUIDToAssetPath(gameDataSettings.gameDataFileGuid) ?? string.Empty;

					// synchronize with online service if required
					await SynchronizeAssetIfNeededAsync(gameDataAssetPath, gameDataSettings, progressCallback.Sub((float)i / total, i + 1.0f / total), logger).IgnoreFault();

					// trying to touch gamedata file
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

					using (var file = await FileHelper.ReadFileAsync(gameDataPath, 5))
					{
						var gameDataFormat = FormatsExtensions.GetGameDataFormatForExtension(gameDataPath);
						if (gameDataFormat == null)
						{
							throw new InvalidOperationException($"Unable to generate game data asset at '{gameDataPath}' " +
								$"because storage format '{Path.GetExtension(gameDataPath)}' is not supported.");
						}

						if (gameDataAsset.GetType() != gameDataAssetType)
						{
							logger.Log(LogType.Assert, $"Asset at '{gameDataAssetPath}'({AssetDatabase.AssetPathToGUID(gameDataAssetPath)}) has type " +
									$"'{gameDataAsset.GetType().Name}' while '{gameDataAssetType.Name}' is expected. Recreating it with new type.");

							var newGameDataAsset = (GameDataBase)ScriptableObject.CreateInstance(gameDataAssetType);
							newGameDataAsset.settings = gameDataAsset.settings;
							UnityObject.DestroyImmediate(gameDataAsset, allowDestroyingAssets: true);
							FileHelper.SafeFileDelete(gameDataAssetPath);
							AssetDatabase.CreateAsset(newGameDataAsset, gameDataAssetPath);

							gameDataAsset = AssetDatabase.LoadAssetAtPath<GameDataBase>(gameDataAssetPath);

							logger.Log(LogType.Assert, $"Asset at '{gameDataAssetPath}'({AssetDatabase.AssetPathToGUID(gameDataAssetPath)}) " +
								$"now is '{gameDataAsset.GetType().Name}' type.");
						}

						gameDataAsset.Save(file, gameDataFormat.Value);
					}

					EditorUtility.SetDirty(gameDataAsset);
					AssetDatabase.SaveAssetIfDirty(gameDataAsset);

					logger.Log(LogType.Assert, $"Asset generation of game data at '{gameDataAssetPath}' is finished " +
						$"successfully in '{startTime.Elapsed}'.");
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

		private static async Task SynchronizeAssetIfNeededAsync(string gameDataAssetPath, GameDataSettings gameDataSettings, Action<string, float> progressCallback, ILogger logger)
		{
			if (!gameDataSettings.IsConnected)
			{
				return; // no sync required
			}

			var serverAddress = new Uri(gameDataSettings.serverAddress);
			var apiKeyPath = new Uri(serverAddress, "/" + gameDataSettings.projectId);
			var apiKey = CharonEditorModule.Instance.KeyCryptoStorage.GetKey(apiKeyPath);
			if (apiKey == null)
			{
				logger.Log(LogType.Warning, $"Unable to synchronize game data at '{AssetDatabase.GUIDToAssetPath(gameDataSettings.gameDataFileGuid)}' because there is API Key associated with it. " +
						"Find this asset in Project window and click 'Synchronize' button in Inspector window.");
				return; // no key
			}

			await SynchronizeAssetsRoutine.RunAsync(force: false, new[] { gameDataAssetPath }, progressCallback);
		}
	}
}
