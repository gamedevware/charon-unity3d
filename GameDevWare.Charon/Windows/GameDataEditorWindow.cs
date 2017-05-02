﻿/*
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using GameDevWare.Charon.Async;
using GameDevWare.Charon.Utils;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

using Debug = UnityEngine.Debug;

// ReSharper disable InconsistentNaming

namespace GameDevWare.Charon.Windows
{
	internal class GameDataEditorWindow : WebViewEditorWindow, IHasCustomMenu
	{
		private static Promise loadEditorTask;

		public GameDataEditorWindow()
		{
			this.titleContent = new GUIContent(Resources.UI_UNITYPLUGIN_WINDOW_EDITOR_TITLE);
			this.minSize = new Vector2(300, 300);
			this.Padding = new Rect(3, 3, 3, 3);
		}

		protected void Awake()
		{
			if (GameDataEditorProcess.IsRunning == false)
			{
				this.titleContent = new GUIContent(Resources.UI_UNITYPLUGIN_WINDOW_EDITOR_TITLE);
				this.Close();
			}
		}

		void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
		{
			menu.AddItem(new GUIContent(Resources.UI_UNITYPLUGIN_WINDOW_RELOAD_BUTTON), false, this.Reload);
			menu.AddItem(new GUIContent(Resources.UI_UNITYPLUGIN_WINDOW_KILL_PROCESS_BUTTON), false, this.KillProcess);
		}

		// ReSharper disable once InconsistentNaming
		// ReSharper disable once UnusedMember.Local
		[OnOpenAsset(0)]
		private static bool OnOpenAsset(int instanceID, int exceptionId)
		{
			var gameDataPath = AssetDatabase.GetAssetPath(instanceID);
			if (GameDataTracker.IsGameDataFile(gameDataPath) == false)
				return false;

			var reference = ValidationException.GetReference(exceptionId);
			loadEditorTask = new Coroutine<bool>(LoadEditor(gameDataPath, reference, loadEditorTask));
			loadEditorTask.ContinueWith(t => EditorUtility.ClearProgressBar());

			return true;
		}
		private static IEnumerable LoadEditor(string gameDataPath, string reference, Promise waitTask)
		{
			if (waitTask != null)
				yield return waitTask.IgnoreFault();

			var title = Path.GetFileName(gameDataPath);
			if (EditorUtility.DisplayCancelableProgressBar(title, Resources.UI_UNITYPLUGIN_WINDOW_EDITOR_CHECKING_RUNTIME, 0.05f))
				throw new InvalidOperationException("Interrupted by user.");

			switch (CharonCli.CheckRequirements())
			{
				case RequirementsCheckResult.MissingRuntime:
					yield return UpdateRuntimeWindow.ShowAsync();
					break;
				case RequirementsCheckResult.MissingExecutable:
					yield return CharonCli.UpdateCharonExecutableAsync(ProgressUtils.ReportToLog(Resources.UI_UNITYPLUGIN_MENU_CHECK_UPDATES));
					break;
				case RequirementsCheckResult.Ok:
					break;
				default:
					throw new InvalidOperationException("Unknown Tools check result.");
			}

			var port = Settings.Current.EditorPort;
			var gameDataEditorUrl = "http://localhost:" + port + "/";

			GameDataEditorProcess.EndGracefully();

			if (Settings.Current.Verbose)
				Debug.Log("Starting gamedata editor at " + gameDataEditorUrl + "...");

			if (EditorUtility.DisplayCancelableProgressBar(title, Resources.UI_UNITYPLUGIN_WINDOW_EDITOR_COPYING_EXECUTABLE, 0.30f))
				throw new InvalidOperationException("Interrupted by user.");
			if (EditorApplication.isCompiling)
				throw new InvalidOperationException("Interrupted by script compiler.");

			if (EditorUtility.DisplayCancelableProgressBar(title, Resources.UI_UNITYPLUGIN_WINDOW_EDITOR_LAUNCHING_EXECUTABLE, 0.60f))
				throw new InvalidOperationException("Interrupted by user.");
			if (EditorApplication.isCompiling)
				throw new InvalidOperationException("Interrupted by script compiler.");

			var charonRunTask = CharonCli.Listen(gameDataPath, port, shadowCopy: true);

			charonRunTask.ContinueWith(t =>
			{
				if (t.HasErrors) return;
				GameDataEditorProcess.Watch(t.GetResult());
			});

			if (Settings.Current.Verbose)
				Debug.Log("Launching gamedata editor process.");

			// wait untill server process start
			var timeoutPromise = Promise.Delayed(TimeSpan.FromSeconds(10));
			var startPromise = charonRunTask.IgnoreFault();
			var startCompletePromise = Promise.WhenAny(timeoutPromise, startPromise);
			var cancelPromise = new Coroutine<bool>(RunCancellableProgress(title, Resources.UI_UNITYPLUGIN_WINDOW_EDITOR_LAUNCHING_EXECUTABLE, 0.65f, 0.80f, TimeSpan.FromSeconds(5), startCompletePromise));

			yield return Promise.WhenAny(timeoutPromise, startPromise, cancelPromise);
			if (timeoutPromise.IsCompleted)
			{
				Debug.LogWarning(Resources.UI_UNITYPLUGIN_WINDOW_FAILED_TO_START_EDITOR_TIMEOUT);
				yield break;
			}
			else if (cancelPromise.IsCompleted)
			{
				if (EditorApplication.isCompiling)
					throw new InvalidOperationException("Interrupted by script compiler.");
				else
					throw new InvalidOperationException("Interrupted by user.");
			}
			else if (charonRunTask.HasErrors)
			{
				throw new InvalidOperationException("Failed to start editor.", charonRunTask.Error.Unwrap());
			}

			if (EditorUtility.DisplayCancelableProgressBar(title, Resources.UI_UNITYPLUGIN_WINDOW_EDITOR_LAUNCHING_EXECUTABLE, 0.80f))
				throw new InvalidOperationException("Interrupted by user.");
			if (EditorApplication.isCompiling)
				throw new InvalidOperationException("Interrupted by script compiler.");

			// wait untill server start to respond
			var downloadStream = new MemoryStream();
			timeoutPromise = Promise.Delayed(TimeSpan.FromSeconds(10));
			startPromise = HttpUtils.DownloadTo(downloadStream, new Uri(gameDataEditorUrl), timeout: TimeSpan.FromSeconds(1));
			startCompletePromise = new Promise();
			cancelPromise = new Coroutine<bool>(RunCancellableProgress(title, Resources.UI_UNITYPLUGIN_WINDOW_EDITOR_LAUNCHING_EXECUTABLE, 0.80f, 0.90f, TimeSpan.FromSeconds(5), startCompletePromise));
			while (!timeoutPromise.IsCompleted)
			{
				yield return startPromise.IgnoreFault();

				if (startPromise.HasErrors == false && downloadStream.Length > 0)
					break;

				if (EditorUtility.DisplayCancelableProgressBar(title, Resources.UI_UNITYPLUGIN_WINDOW_EDITOR_LAUNCHING_EXECUTABLE, 0.93f))
					throw new InvalidOperationException("Interrupted by user.");
				if (EditorApplication.isCompiling)
					throw new InvalidOperationException("Interrupted by script compiler.");

				downloadStream.SetLength(0);
				startPromise = HttpUtils.DownloadTo(downloadStream, new Uri(gameDataEditorUrl), timeout: TimeSpan.FromSeconds(1));
			}
			if (timeoutPromise.IsCompleted && (!startPromise.IsCompleted || startPromise.HasErrors))
			{
				startCompletePromise.TrySetCompleted();
				Debug.LogWarning(Resources.UI_UNITYPLUGIN_WINDOW_FAILED_TO_START_EDITOR_TIMEOUT);
				yield break;
			}
			startCompletePromise.TrySetCompleted();

			if (EditorUtility.DisplayCancelableProgressBar(title, Resources.UI_UNITYPLUGIN_WINDOW_EDITOR_OPENING_BROWSER, 0.95f))
				throw new InvalidOperationException("Interrupted by user.");
			if (EditorApplication.isCompiling)
				throw new InvalidOperationException("Interrupted by script compiler.");

			switch ((BrowserType)Settings.Current.Browser)
			{
				case BrowserType.UnityEmbedded:
					var nearPanels = typeof(SceneView);
					var editorWindow = GetWindow<GameDataEditorWindow>(nearPanels);
					editorWindow.titleContent = new GUIContent(title);
					editorWindow.LoadUrl(gameDataEditorUrl + reference);
					editorWindow.SetWebViewVisibility(true);
					editorWindow.Repaint();
					editorWindow.Focus();
					break;
				case BrowserType.Custom:
					if (string.IsNullOrEmpty(Settings.Current.BrowserPath))
						goto case BrowserType.SystemDefault;
					Process.Start(Settings.Current.BrowserPath, gameDataEditorUrl + reference);
					break;
				case BrowserType.SystemDefault:
					EditorUtility.OpenWithDefaultApp(gameDataEditorUrl + reference);
					break;
			}
		}

		private static IEnumerable RunCancellableProgress(string title, string message, float fromProgress, float toProgress, TimeSpan timeInterpolationWindow, Promise cancellation)
		{
			var startTime = DateTime.UtcNow;
			while (cancellation.IsCompleted == false)
			{
				var timeElapsed = DateTime.UtcNow - startTime;
				var timeElapsedRatio = (float)Math.Min(1.0f, Math.Max(0.0f, timeElapsed.TotalMilliseconds / timeInterpolationWindow.TotalMilliseconds));
				var progress = fromProgress + (toProgress - fromProgress) * timeElapsedRatio;

				if (EditorUtility.DisplayCancelableProgressBar(title, message, progress) || EditorApplication.isCompiling)
					yield return true;

				yield return Promise.Delayed(TimeSpan.FromMilliseconds(100));
			}

			yield return false;
		}

		private void KillProcess()
		{
			GameDataEditorProcess.EndGracefully();
		}
	}
}