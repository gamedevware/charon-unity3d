/*
	Copyright (c) 2015 Denis Zykov

	This is part of Charon Game Data Editor Unity Plugin.

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
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Assets.Unity.Charon.Editor
{
	class ChangeHooks : AssetPostprocessor
	{
		private static Dictionary<string, FileSystemWatcher> watchers = new Dictionary<string, FileSystemWatcher>(StringComparer.Ordinal);
		private static Dictionary<string, string> hashes = new Dictionary<string, string>(StringComparer.Ordinal);
		private static HashSet<string> changedAssets = new HashSet<string>(StringComparer.Ordinal);
		private static int settingsVersion = int.MinValue;

		static ChangeHooks()
		{
			EditorApplication.update += Update;
		}

		private static void Update()
		{
			if (Settings.Current == null)
				return;

			if (Settings.Current.Version != settingsVersion)
			{
				foreach (var gameDataPath in Settings.Current.GameDataPaths)
				{
					if (watchers.ContainsKey(gameDataPath) || File.Exists(gameDataPath) == false)
						continue;

					try
					{
						var fullPath = Path.GetFullPath(gameDataPath);
						var watcher = new FileSystemWatcher(Path.GetDirectoryName(fullPath))
						{
							NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
						};
						watcher.Filter = Path.GetFileName(fullPath);
						watcher.Changed += GameDataChanged;
						watchers.Add(gameDataPath, watcher);

						try { hashes[gameDataPath] = FileUtils.ComputeMd5Hash(gameDataPath); }
						catch
						{
							// ignored
						}
						watcher.EnableRaisingEvents = true;
					}
					catch (Exception e)
					{
						Debug.LogError("Failed to create FileSystemWatcher for GameData " + gameDataPath + ": " + e);
					}
				}

				foreach (var gameDataPath in watchers.Keys.ToArray())
				{
					if (Settings.Current.GameDataPaths.Contains(gameDataPath))
						continue;

					var watcher = watchers[gameDataPath];
					watchers.Remove(gameDataPath);
					try { watcher.Dispose(); }
					catch (Exception e) { Debug.LogError("Failed to dispose FileSystemWatcher of GameData: " + e); }
				}
				settingsVersion = Settings.Current.Version;
			}

			var changedAssetsCopy = default(string[]);
			lock (changedAssets)
			{
				if (changedAssets.Count > 0)
				{
					changedAssetsCopy = changedAssets.ToArray();
					changedAssets.Clear();
				}
			}

			if (changedAssetsCopy != null)
			{
				foreach (var changedAsset in changedAssetsCopy)
				{
					if (Settings.Current.Verbose)
						Debug.Log("Changed Asset: " + changedAsset);

					if (!File.Exists(changedAsset) || Settings.Current.GameDataPaths.Contains(changedAsset) == false)
						continue;
					var gameDataSettings = GameDataSettings.Load(changedAsset);
					if (!gameDataSettings.AutoGeneration)
						continue;

					var assetHash = default(string);
					try
					{
						assetHash = FileUtils.ComputeMd5Hash(changedAsset);
					}
					catch (Exception e)
					{
						Debug.LogWarning("Failed to compute hash of " + changedAsset + ": " + e);
						continue;
					}

					var oldAssetHash = default(string);
					if (hashes.TryGetValue(changedAsset, out oldAssetHash) && assetHash == oldAssetHash)
						continue; // not changed

					if (EditorApplication.isUpdating)
						continue;

					if (Settings.Current.Verbose)
						Debug.Log("Asset's " + changedAsset + " hash has changed from " + (oldAssetHash ?? "<none>") + " to " + assetHash);

					hashes[changedAsset] = assetHash;
					CoroutineScheduler.Schedule
					(
						Menu.GenerateCodeAndAssetsAsync(
							path: changedAsset,
							progressCallback: ProgressUtils.ReportToLog("Generation(Auto): ")),
						"generation::" + changedAsset
					);
				}
			}

		}

		// ReSharper disable once UnusedMember.Local
		private static void OnPostprocessAllAssets(string[] _, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			if (Settings.Current == null)
				return;

			foreach (var deletedPath in deletedAssets)
			{
				if (!Settings.Current.GameDataPaths.Remove(deletedPath))
					continue;

				Settings.Current.Version++;
				if (Settings.Current.Verbose)
					Debug.Log("GameData deleted: " + deletedPath);
				hashes.Remove(deletedPath);
			}
			for (var i = 0; i < movedAssets.Length; i++)
			{
				var fromPath = FileUtils.MakeProjectRelative(movedFromAssetPaths[i]);
				var toPath = FileUtils.MakeProjectRelative(movedAssets[i]);
				if (fromPath == null || toPath == null) continue;

				if (Path.GetFullPath(Settings.Current.ToolsPath) == fromPath)
					Settings.Current.ToolsPath = toPath;

				if (!Settings.Current.GameDataPaths.Contains(fromPath))
					continue;

				Settings.Current.GameDataPaths.Remove(fromPath);
				Settings.Current.GameDataPaths.Add(toPath);
				if (Settings.Current.Verbose)
					Debug.Log("GameData moved: " + toPath + " from: " + fromPath);

				hashes.Remove(fromPath);
				hashes.Remove(toPath);
			}
		}

		private static void GameDataChanged(object sender, FileSystemEventArgs e)
		{
			var path = FileUtils.MakeProjectRelative(e.FullPath);
			lock (changedAssets)
				changedAssets.Add(path);
		}
	}
}
