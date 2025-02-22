/*
	Copyright (c) 2025 Denis Zykov

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
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
