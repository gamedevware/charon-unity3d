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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameDevWare.Charon.Unity.Routines;
using GameDevWare.Charon.Unity.Utils;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.Charon.Unity
{
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal class ChangeHooks : AssetPostprocessor
	{
		private static readonly Dictionary<string, FileSystemWatcher> Watchers = new Dictionary<string, FileSystemWatcher>(StringComparer.Ordinal);
		private static readonly Dictionary<string, string> GameDataHashByPath = new Dictionary<string, string>(StringComparer.Ordinal);
		private static readonly HashSet<string> ChangedAssetPaths = new HashSet<string>(StringComparer.Ordinal);
		private static int LastWatchedGameDataTrackerVersion;

		static ChangeHooks()
		{
			EditorApplication.update += Update;
		}

		private static void Update()
		{
			if (Settings.Current == null)
				return;

			if (LastWatchedGameDataTrackerVersion != GameDataTracker.Version)
			{
				var gameDataPaths = new HashSet<string>(GameDataTracker.All);
				foreach (var gameDataPath in gameDataPaths)
				{
					if (Watchers.ContainsKey(gameDataPath) || File.Exists(gameDataPath) == false)
						continue;

					try
					{
						var fullPath = Path.GetFullPath(gameDataPath);
						var directoryName = Path.GetDirectoryName(fullPath);
						var watcher = new FileSystemWatcher(directoryName)
						{
							NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
						};
						watcher.Filter = Path.GetFileName(fullPath);
						watcher.Changed += GameDataChanged;
						Watchers.Add(gameDataPath, watcher);

						try { GameDataHashByPath[gameDataPath] = FileHelper.ComputeHash(gameDataPath); }
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

				foreach (var gameDataPath in Watchers.Keys.ToArray())
				{
					if (gameDataPaths.Contains(gameDataPath))
						continue;

					var watcher = Watchers[gameDataPath];
					Watchers.Remove(gameDataPath);
					try { watcher.Dispose(); }
					catch (Exception e) { Debug.LogError("Failed to dispose FileSystemWatcher of GameData: " + e); }
				}
				LastWatchedGameDataTrackerVersion = GameDataTracker.Version;
			}

			var changedAssetsCopy = default(string[]);
			lock (ChangedAssetPaths)
			{
				if (ChangedAssetPaths.Count > 0)
				{
					changedAssetsCopy = ChangedAssetPaths.ToArray();
					ChangedAssetPaths.Clear();
				}
			}

			if (changedAssetsCopy != null)
			{
				foreach (var changedAsset in changedAssetsCopy)
				{
					if (Settings.Current.Verbose)
						Debug.Log("Changed Asset: " + changedAsset);

					if (!File.Exists(changedAsset) || GameDataTracker.IsTracked(changedAsset) == false)
						continue;
					var gameDataSettings = GameDataSettings.Load(changedAsset);
					if (!gameDataSettings.AutoGeneration)
						continue;

					var assetHash = default(string);
					try
					{
						assetHash = FileHelper.ComputeHash(changedAsset);
					}
					catch (Exception e)
					{
						Debug.LogWarning("Failed to compute hash of " + changedAsset + ": " + e);
						continue;
					}

					var oldAssetHash = default(string);
					if (GameDataHashByPath.TryGetValue(changedAsset, out oldAssetHash) && assetHash == oldAssetHash)
						continue; // not changed

					if (EditorApplication.isUpdating)
						continue;

					if (Settings.Current.Verbose)
						Debug.Log("Asset's " + changedAsset + " hash has changed from " + (oldAssetHash ?? "<none>") + " to " + assetHash);

					GameDataHashByPath[changedAsset] = assetHash;

					GenerateCodeAndAssetsRoutine.Schedule(
						path: changedAsset,
						progressCallback: ProgressUtils.ReportToLog("Generation (Auto): "),
						coroutineId: "generation::" + changedAsset
					);
				}
			}

		}

		// ReSharper disable once UnusedMember.Local
		// ReSharper disable once IdentifierTypo
		private static void OnPostprocessAllAssets(string[] _, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			if (Settings.Current == null)
				return;

			foreach (var deletedPath in deletedAssets)
			{
				if (!GameDataTracker.Untrack(deletedPath))
					continue;

				if (Settings.Current.Verbose)
					Debug.Log("GameData deleted: " + deletedPath);

				GameDataHashByPath.Remove(deletedPath);
			}
			for (var i = 0; i < movedAssets.Length; i++)
			{
				var fromPath = FileHelper.MakeProjectRelative(movedFromAssetPaths[i]);
				var toPath = FileHelper.MakeProjectRelative(movedAssets[i]);
				if (fromPath == null || toPath == null) continue;

				if (!GameDataTracker.IsTracked(fromPath))
					continue;

				GameDataTracker.Untrack(fromPath);
				GameDataTracker.Track(toPath);

				if (Settings.Current.Verbose)
					Debug.Log("GameData moved: " + toPath + " from: " + fromPath);

				GameDataHashByPath.Remove(fromPath);
				GameDataHashByPath.Remove(toPath);
			}
		}

		private static void GameDataChanged(object sender, FileSystemEventArgs e)
		{
			var path = FileHelper.MakeProjectRelative(e.FullPath);
			lock (ChangedAssetPaths)
				ChangedAssetPaths.Add(path);
		}
	}
}
