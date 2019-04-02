/*
	Copyright (c) 2017 Denis Zykov

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
using GameDevWare.Charon.Unity.Async;
using UnityEditor;
using UnityEngine;

// ReSharper disable UnusedMember.Local

namespace GameDevWare.Charon.Unity
{
	[InitializeOnLoad]
	internal static class AssetGenerator
	{
		private const string PREFS_KEY = Settings.PREF_PREFIX + "AssetGenerationList";
		private const string LIST_SPLITTER = ";";
		private static readonly char[] ListSplitterChars = LIST_SPLITTER.ToArray();
		private static HashSet<string> AssetGenerationList = new HashSet<string>();
		private static readonly EditorApplication.CallbackFunction InitializeCallback = Initialize;

		static AssetGenerator()
		{
			EditorApplication.update += InitializeCallback;
		}

		private static void Initialize()
		{
			if (EditorApplication.isCompiling)
				return;

			EditorApplication.update -= InitializeCallback;
			
			var listStr = EditorPrefs.GetString(PREFS_KEY);
			if (string.IsNullOrEmpty(listStr))
				AssetGenerationList = new HashSet<string>();
			else
				AssetGenerationList = new HashSet<string>(listStr.Split(ListSplitterChars, StringSplitOptions.RemoveEmptyEntries));

			if (Settings.Current.Verbose)
				Debug.Log("Scheduling " + AssetGenerationList.Count + " asset generating tasks.");

			CoroutineScheduler.Schedule(Menu.GenerateAssetsAsync(AssetGenerationList.ToArray()))
				.IgnoreFault()
				.ContinueWith(_ => EditorPrefs.DeleteKey(PREFS_KEY));

			AssetGenerationList.Clear();
			SaveList();
		}

		public static void AddPath(string path)
		{
			if (path == null) throw new ArgumentNullException("path");

			if (AssetGenerationList == null) AssetGenerationList = new HashSet<string>();

			AssetGenerationList.Add(path);
			SaveList();
		}

		private static void SaveList()
		{
			var listStr = string.Join(LIST_SPLITTER, AssetGenerationList.ToArray());
			EditorPrefs.SetString(PREFS_KEY, listStr);
		}
	}
}
