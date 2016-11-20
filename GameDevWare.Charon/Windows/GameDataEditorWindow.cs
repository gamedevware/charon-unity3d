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
using System.Collections;
using System.Diagnostics;
using System.IO;
using GameDevWare.Charon.Tasks;
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
			this.titleContent = new GUIContent(Resources.UI_UNITYPLUGIN_WINDOWEDITORTITLE);
			this.minSize = new Vector2(300, 300);
			this.Paddings = new Rect(3, 3, 3, 3);
		}

		protected void Awake()
		{
			if (GameDataEditorProcess.IsRunning == false)
			{
				this.titleContent = new GUIContent(Resources.UI_UNITYPLUGIN_WINDOWEDITORTITLE);
				this.Close();
			}
		}

		void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
		{
			menu.AddItem(new GUIContent(Resources.UI_UNITYPLUGIN_WINDOWRELOADBUTTON), false, this.Reload);
		}

		// ReSharper disable once InconsistentNaming
		// ReSharper disable once UnusedMember.Local
		[OnOpenAsset(0)]
		private static bool OnOpenAsset(int instanceID, int exceptionId)
		{
			var gameDataPath = AssetDatabase.GetAssetPath(instanceID);
			if (Array.IndexOf(Settings.Current.GameDataPaths, gameDataPath) == -1)
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
			if (EditorUtility.DisplayCancelableProgressBar(title, Resources.UI_UNITYPLUGIN_WINDOWEDITORCHECKINGRUNTIME, 0.05f))
				throw new InvalidOperationException("Interrupted by user.");

			switch (ToolsRunner.CheckCharon())
			{
				case CharonCheckResult.MissingRuntime:
					yield return UpdateRuntimeWindow.ShowAsync();
					break;
				case CharonCheckResult.MissingExecutable:
					yield return ToolsRunner.UpdateCharonExecutable(ProgressUtils.ReportToLog(Resources.UI_UNITYPLUGIN_MENUCHECKUPDATES));
					break;
				case CharonCheckResult.Ok:
					break;
				default:
					throw new InvalidOperationException("Unknown Tools check result.");
			}

			// EditorUtility.DisplayProgressBar(title, Resources.UI_UNITYPLUGIN_WINDOWEDITORCHECKINGLICENSE, 0.10f);
			//var license = default(LicenseInfo);
			//while (license == null)
			//{
			//	var getLicense = Licenses.GetLicense(scheduleCoroutine: true);
			//	yield return getLicense;
			//	license = getLicense.GetResult();
			//	if (license == null)
			//		yield return LicenseActivationWindow.ShowAsync();
			//}

			var toolsPath = Settings.Current.ToolsPath;
			var port = Settings.Current.ToolsPort;
			var gameDataEditorUrl = "http://localhost:" + port + "/";

			if (string.IsNullOrEmpty(toolsPath) || File.Exists(toolsPath) == false)
				throw new InvalidOperationException("Unable to launch Charon.exe tool because path to it is null or empty.");

			toolsPath = Path.GetFullPath(toolsPath);

			GameDataEditorProcess.EndGracefully();

			if (Settings.Current.Verbose)
				Debug.Log("Starting gamedata editor at " + gameDataEditorUrl + "...");

			if (EditorUtility.DisplayCancelableProgressBar(title, Resources.UI_UNITYPLUGIN_WINDOWEDITORCOPYINGEXECUTABLE, 0.30f))
				throw new InvalidOperationException("Interrupted by user.");
			if (EditorApplication.isCompiling)
				throw new InvalidOperationException("Interrupted by script compiler.");

			// ReSharper disable once AssignNullToNotNullAttribute
			var shadowCopyOfTools = Path.GetFullPath(Path.Combine(ToolsRunner.ToolShadowCopyPath, Path.GetFileName(toolsPath)));
			if (File.Exists(shadowCopyOfTools) == false)
			{
				if (Directory.Exists(ToolsRunner.ToolShadowCopyPath) == false)
					Directory.CreateDirectory(ToolsRunner.ToolShadowCopyPath);

				if (Settings.Current.Verbose)
					Debug.Log("Shadow copying tools to " + shadowCopyOfTools + ".");

				File.Copy(toolsPath, shadowCopyOfTools, overwrite: true);

				var configPath = toolsPath + ".config";
				var configShadowPath = shadowCopyOfTools + ".config";
				if (File.Exists(configPath))
				{
					if (Settings.Current.Verbose)
						Debug.Log("Shadow copying tools configuration to " + configShadowPath + ".");
					File.Copy(configPath, configShadowPath);
				}
				else
				{
					Debug.LogWarning("Missing required configuration file at '" + configPath + "'.");
				}
			}

			if (EditorUtility.DisplayCancelableProgressBar(title, Resources.UI_UNITYPLUGIN_WINDOWEDITORLAUNCHINGEXECUTABLE, 0.60f))
				throw new InvalidOperationException("Interrupted by user.");
			if (EditorApplication.isCompiling)
				throw new InvalidOperationException("Interrupted by script compiler.");

			var toolsProcessTask = ToolsRunner.Run(
				new ToolExecutionOptions
				(
					shadowCopyOfTools,

					"LISTEN", Path.GetFullPath(gameDataPath),
					"--port", port.ToString(),
					"--parentPid", Process.GetCurrentProcess().Id.ToString(),
					Settings.Current.Verbose ? "--verbose" : ""
				)
				{
					RequireDotNetRuntime = true,
					CaptureStandartError = false,
					CaptureStandartOutput = false,
					ExecutionTimeout = TimeSpan.Zero,
					WaitForExit = false,
					StartInfo =
					{
						EnvironmentVariables =
						{
							{ "CHARON_APP_DATA", Settings.GetAppDataPath() },
							{ "CHARON_LICENSE_SERVER", Settings.Current.LicenseServerAddress },
							{ "CHARON_SELECTED_LICENSE", Settings.Current.SelectedLicense },
						}
					}
				}
			);

			toolsProcessTask.ContinueWith(t =>
			{
				if (t.HasErrors) return;
				using (var result = toolsProcessTask.GetResult())
					GameDataEditorProcess.Watch(result.Process.Id, result.Process.ProcessName);
			});

			if (Settings.Current.Verbose)
				Debug.Log("Launching gamedata editor process.");

			// wait untill server process start
			var timeoutPromise = Promise.Delayed(TimeSpan.FromSeconds(10));
			var startPromise = toolsProcessTask.IgnoreFault();
			var startCompletePromise = Promise.WhenAny(timeoutPromise, startPromise);
			var cancelPromise = new Coroutine<bool>(RunCancellableProgress(title, Resources.UI_UNITYPLUGIN_WINDOWEDITORLAUNCHINGEXECUTABLE, 0.65f, 0.80f, TimeSpan.FromSeconds(5), startCompletePromise));

			yield return Promise.WhenAny(timeoutPromise, startPromise, cancelPromise);
			if (timeoutPromise.IsCompleted)
			{
				Debug.LogWarning(Resources.UI_UNITYPLUGIN_WINDOWFAILEDTOSTARTEDITORTIMEOUT);
				yield break;
			}
			else if (cancelPromise.IsCompleted)
			{
				if (EditorApplication.isCompiling)
					throw new InvalidOperationException("Interrupted by script compiler.");
				else
					throw new InvalidOperationException("Interrupted by user.");
			}

			if (EditorUtility.DisplayCancelableProgressBar(title, Resources.UI_UNITYPLUGIN_WINDOWEDITORLAUNCHINGEXECUTABLE, 0.80f))
				throw new InvalidOperationException("Interrupted by user.");
			if (EditorApplication.isCompiling)
				throw new InvalidOperationException("Interrupted by script compiler.");

			// wait untill server start to respond
			timeoutPromise = Promise.Delayed(TimeSpan.FromSeconds(10));
			startPromise = HttpUtils.Download(new Uri(gameDataEditorUrl), timeout: TimeSpan.FromSeconds(1));
			startCompletePromise = new Promise();
			cancelPromise = new Coroutine<bool>(RunCancellableProgress(title, Resources.UI_UNITYPLUGIN_WINDOWEDITORLAUNCHINGEXECUTABLE, 0.80f, 0.90f, TimeSpan.FromSeconds(5), startCompletePromise));
			while (!timeoutPromise.IsCompleted)
			{
				yield return startPromise.IgnoreFault();
				if (startPromise.HasErrors == false)
					break;
				else
					startPromise = HttpUtils.Download(new Uri(gameDataEditorUrl), timeout: TimeSpan.FromSeconds(1));
			}
			if (timeoutPromise.IsCompleted && (!startPromise.IsCompleted || startPromise.HasErrors))
			{
				startCompletePromise.TrySetCompleted();
				Debug.LogWarning(Resources.UI_UNITYPLUGIN_WINDOWFAILEDTOSTARTEDITORTIMEOUT);
				yield break;
			}
			startCompletePromise.TrySetCompleted();

			if (EditorUtility.DisplayCancelableProgressBar(title, Resources.UI_UNITYPLUGIN_WINDOWEDITOROPENINGBROWSER, 0.95f))
				throw new InvalidOperationException("Interrupted by user.");
			if (EditorApplication.isCompiling)
				throw new InvalidOperationException("Interrupted by script compiler.");

			switch ((Browser)Settings.Current.Browser)
			{
				case Browser.UnityEmbedded:
					var nearPanels = typeof(SceneView);
					var editorWindow = GetWindow<GameDataEditorWindow>(nearPanels);
					editorWindow.titleContent = new GUIContent(title);
					editorWindow.LoadUrl(gameDataEditorUrl + reference);
					editorWindow.SetWebViewVisibility(true);
					editorWindow.Repaint();
					editorWindow.Focus();
					break;
				case Browser.Custom:
					if (string.IsNullOrEmpty(Settings.Current.BrowserPath))
						goto case Browser.SystemDefault;
					Process.Start(Settings.Current.BrowserPath, gameDataEditorUrl + reference);
					break;
				case Browser.SystemDefault:
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
	}
}
