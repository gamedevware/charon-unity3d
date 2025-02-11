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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GameDevWare.Charon.Editor.Routines;
using GameDevWare.Charon.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.Charon.Editor.Services
{
	internal class DeferredAssetImporter
	{
		private const string LIST_SPLITTER = ";";
		private readonly string preferencesPrefix = typeof(DeferredAssetImporter).Assembly.GetName().Name + "::" + nameof(DeferredAssetImporter);

		private static readonly char[] ListSplitterChars = LIST_SPLITTER.ToArray();

		private HashSet<string> assetGenerationList = new HashSet<string>();
		private readonly ILogger logger;

		public DeferredAssetImporter(ILogger logger)
		{
			this.logger = logger;
		}

		public void ImportOnStart(string gameDataAssetPath)
		{
			if (gameDataAssetPath == null) throw new ArgumentNullException(nameof(gameDataAssetPath));

			this.assetGenerationList ??= new HashSet<string>();

			this.assetGenerationList.Add(gameDataAssetPath);
			this.SaveList();
		}

		internal void Initialize()
		{
			var listStr = EditorPrefs.GetString(this.preferencesPrefix + "::List");
			this.assetGenerationList = string.IsNullOrEmpty(listStr) ?
				new HashSet<string>() :
				new HashSet<string>(listStr.Split(ListSplitterChars, StringSplitOptions.RemoveEmptyEntries));

			if (this.assetGenerationList.Count == 0 || EditorApplication.isCompiling)
			{
				return;
			}

			this.logger.Log(LogType.Assert, "Scheduling " + this.assetGenerationList.Count + " asset generating tasks.");

			SynchronizeAssetsRoutine.ScheduleAsync(this.assetGenerationList.ToArray())
				.IgnoreFault()
				.ContinueWith(_ => EditorPrefs.DeleteKey(this.preferencesPrefix + "::List"), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);

			this.assetGenerationList.Clear();
			this.SaveList();
		}

		private void SaveList()
		{
			var listStr = string.Join(LIST_SPLITTER, this.assetGenerationList.ToArray());
			EditorPrefs.SetString(this.preferencesPrefix + "::List", listStr);
		}
	}
}
