/*
	Copyright (c) 2016 Denis Zykov

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
using Assets.Editor.GameDevWare.Charon.Utils;
using Assets.Editor.GameDevWare.Charon.Windows;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor.GameDevWare.Charon
{
	[InitializeOnLoad, Serializable]
	class AssetGenerator : ScriptableObject
	{
		private const string PREFS_KEY = Settings.PREF_PREFIX + "AssetGenerationList";
		public const string LIST_SPLITTER = ";";
		public static readonly char[] ListSplitterChars = LIST_SPLITTER.ToArray();

		public static AssetGenerator Instance;
		private HashSet<string> AssetGenerationList;

		static AssetGenerator()
		{
			Instance = ScriptableObject.CreateInstance<AssetGenerator>();
		}

		public void AddPath(string path)
		{
			if (path == null) throw new ArgumentNullException("path");

			if (this.AssetGenerationList == null) this.AssetGenerationList = new HashSet<string>();

			this.AssetGenerationList.Add(path);
			this.OnDisable();
		}

		protected void Awake()
		{
			System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(GameDataInspector).TypeHandle);


			var listStr = EditorPrefs.GetString(PREFS_KEY);
			if (string.IsNullOrEmpty(listStr))
				this.AssetGenerationList = new HashSet<string>();
			else
				this.AssetGenerationList = new HashSet<string>(listStr.Split(ListSplitterChars, StringSplitOptions.RemoveEmptyEntries));
		}

		protected void OnEnable()
		{
			if (this.AssetGenerationList == null || this.AssetGenerationList.Count <= 0)
				return;

			if (Settings.Current.Verbose)
				Debug.Log("Sheduling " + this.AssetGenerationList.Count + " asset generating tasks.");

			CoroutineScheduler.Schedule(Menu.GenerateAssetsAsync(this.AssetGenerationList.ToArray()))
				.IgnoreFault()
				.ContinueWith(_ => EditorPrefs.DeleteKey(PREFS_KEY));

			this.AssetGenerationList.Clear();
		}

		protected void OnDisable()
		{
			if (this.AssetGenerationList == null || this.AssetGenerationList.Count == 0)
				return;

			var listStr = string.Join(LIST_SPLITTER, this.AssetGenerationList.ToArray());
			EditorPrefs.SetString(PREFS_KEY, listStr);
		}
	}
}
