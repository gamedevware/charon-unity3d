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
using System.Linq;
using UnityEditor;

namespace GameDevWare.Charon.Unity.Utils
{
	internal static class GameDataTracker
	{
		public static readonly string[] GameDataExtensions = new[] { ".gdjs", ".gdbs", ".gdmp", ".gdml" };
		public static readonly string[] GameDataExtensionFilters = new[] { "Json", "gdjs,json", "Bson", "gdbs,bson", "Message Pack", "msgpack,gdmp", "XML", "xml,gdml" };

		private static readonly HashSet<string> TrackedFiles;
		private static int TrackedFilesVersion;

		public static IEnumerable<string> All
		{
			get
			{
				if (TrackedFiles.Count == 0)
				{
					foreach (var file in ScanForGameDataFiles())
						TrackedFiles.Add(file);
					TrackedFilesVersion++;
				}
				return TrackedFiles;
			}
		}
		
		public static int Version { get { return TrackedFilesVersion; } }

		static GameDataTracker()
		{
			TrackedFiles = new HashSet<string>(StringComparer.Ordinal);
		}

		public static bool Untrack(string gameDataPath)
		{
			if (gameDataPath == null) throw new ArgumentNullException("gameDataPath");

			var removed = TrackedFiles.Remove(gameDataPath);
			if (removed)
				TrackedFilesVersion++;
			return removed;

		}
		public static void Track(string gameDataPath)
		{
			if (gameDataPath == null) throw new ArgumentNullException("gameDataPath");

			if (TrackedFiles.Add(gameDataPath))
				TrackedFilesVersion++;
		}
		public static bool IsTracked(string gameDataPath)
		{
			if (gameDataPath == null) throw new ArgumentNullException("gameDataPath");

			return TrackedFiles.Contains(gameDataPath);
		}
		public static bool IsGameDataFile(string gameDataPath)
		{
			if (string.IsNullOrEmpty(gameDataPath)) return false;

			foreach (var gameDataExtension in GameDataExtensions)
			{
				if (gameDataPath.EndsWith(gameDataExtension, StringComparison.OrdinalIgnoreCase))
					return true;
			}
			return false;
		}


		private static IEnumerable<string> ScanForGameDataFiles()
		{
			return (from id in AssetDatabase.FindAssets("t:DefaultAsset")
					let path = FileHelper.MakeProjectRelative(AssetDatabase.GUIDToAssetPath(id))
					where path != null && IsGameDataFile(path)
					select path);
		}
	}
}
