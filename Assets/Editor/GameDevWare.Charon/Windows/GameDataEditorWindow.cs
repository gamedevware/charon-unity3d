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
using Assets.Editor.GameDevWare.Charon.Models;
using Assets.Editor.GameDevWare.Charon.Tasks;
using Assets.Editor.GameDevWare.Charon.Utils;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

using Debug = UnityEngine.Debug;

// ReSharper disable InconsistentNaming

namespace Assets.Editor.GameDevWare.Charon.Windows
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
			EditorUtility.DisplayProgressBar(title, Resources.UI_UNITYPLUGIN_WINDOWEDITORCHECKINGRUNTIME, 0.05f);

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

			EditorUtility.DisplayProgressBar(title, Resources.UI_UNITYPLUGIN_WINDOWEDITORCHECKINGLICENSE, 0.10f);

			var license = default(LicenseInfo);
			while (license == null)
			{
				var getLicense = Licenses.GetLicense(scheduleCoroutine: true);
				yield return getLicense;
				license = getLicense.GetResult();
				if (license == null)
					yield return LicenseActivationWindow.ShowAsync();
			}

			var toolsPath = Settings.Current.ToolsPath;
			var port = Settings.Current.ToolsPort;
			var gameDataEditorUrl = "http://localhost:" + port + "/";
			var timeoutPromise = Promise.Delayed(TimeSpan.FromSeconds(10));

			if (string.IsNullOrEmpty(toolsPath) || File.Exists(toolsPath) == false)
				throw new InvalidOperationException("Unable to launch Charon.exe tool because path to it is null or empty.");

			toolsPath = Path.GetFullPath(toolsPath);

			if (GameDataEditorProcess.IsRunning)
				GameDataEditorProcess.Kill();

			if (Settings.Current.Verbose)
				Debug.Log("Starting gamedata editor at " + gameDataEditorUrl + "...");

			EditorUtility.DisplayProgressBar(title, Resources.UI_UNITYPLUGIN_WINDOWEDITORCOPYINGEXECUTABLE, 0.30f);

			// ReSharper disable once AssignNullToNotNullAttribute
			var shadowCopyOfTools = Path.GetFullPath(Path.Combine(Utils.ToolsRunner.ToolShadowCopyPath, Path.GetFileName(toolsPath)));
			if (File.Exists(shadowCopyOfTools) == false)
			{
				if (Directory.Exists(Utils.ToolsRunner.ToolShadowCopyPath) == false)
					Directory.CreateDirectory(Utils.ToolsRunner.ToolShadowCopyPath);

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

			EditorUtility.DisplayProgressBar(title, Resources.UI_UNITYPLUGIN_WINDOWEDITORLAUNCHINGEXECUTABLE, 0.60f);

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
					CaptureStandartError = true,
					CaptureStandartOutput = true,
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

			if (Settings.Current.Verbose)
				Debug.Log("Launching gamedata editor process.");

			var startPromise = toolsProcessTask.IgnoreFault();

			yield return Promise.WhenAny(timeoutPromise, startPromise);

			if (timeoutPromise.IsCompleted)
			{
				Debug.LogWarning(Resources.UI_UNITYPLUGIN_WINDOWFAILEDTOSTARTEDITORTIMEOUT);
				toolsProcessTask.ContinueWith(t =>
				{
					if (t.HasErrors) return;
					GameDataEditorProcess.Kill(t.GetResult().ProcessId);
				});
				yield break;
			}

			GameDataEditorProcess.Watch(toolsProcessTask.GetResult().ProcessId, shadowCopyOfTools);

			EditorUtility.DisplayProgressBar(title, Resources.UI_UNITYPLUGIN_WINDOWEDITOROPENINGBROWSER, 0.90f);

			switch ((Browser)Settings.Current.Browser)
			{
				case Browser.UnityEmbedded:
					var nearPanels = typeof(SceneView);
					var editorWindow = EditorWindow.GetWindow<GameDataEditorWindow>(nearPanels);
					editorWindow.titleContent = new GUIContent(title);
					editorWindow.LoadUrl(gameDataEditorUrl + reference);
					editorWindow.SetWebViewVisibility(true);
					editorWindow.Repaint();
					editorWindow.Focus();
					break;
				case Browser.Custom:
					if (string.IsNullOrEmpty(Settings.Current.BrowserPath))
						goto SystemDefault;

					var startBrowser = Process.Start(Settings.Current.BrowserPath, gameDataEditorUrl + reference);
					startBrowser.Start();
					break;
				case Browser.SystemDefault:
					SystemDefault:
					EditorUtility.OpenWithDefaultApp(gameDataEditorUrl + reference);
					break;
			}
		}

	}
}
