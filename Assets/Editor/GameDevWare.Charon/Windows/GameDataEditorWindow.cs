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
		public enum EditorStatus { Unloaded, Loading, Ready, Crashed }

		[SerializeField]
		private string gameDataPath;
		[SerializeField]
		private int toolsProcessId;

		private Promise loadEditorTask;

		public EditorStatus Status { get; private set; }

		public GameDataEditorWindow()
		{
			this.titleContent = new GUIContent(Resources.UI_UNITYPLUGIN_WINDOWEDITORTITLE);
			this.minSize = new Vector2(300, 300);
			this.Paddings = new Rect(3, 3, 3, 3);
		}

		protected override void OnGUI()
		{
			switch (this.Status)
			{
				case EditorStatus.Loading:
					EditorGUILayout.HelpBox(Resources.UI_UNITYPLUGIN_WINDOWEDITORLOADING, MessageType.Info);
					break;
				case EditorStatus.Ready:
					EditorGUILayout.HelpBox(string.Format(Resources.UI_UNITYPLUGIN_WINDOWEDITORISOPENED, this.gameDataPath), MessageType.Info);
					break;
				case EditorStatus.Crashed:
					if (this.loadEditorTask != null && this.loadEditorTask.HasErrors)
						EditorGUILayout.HelpBox(string.Format(Resources.UI_UNITYPLUGIN_WINDOWLOADINGFAILEDWITHERROR, this.loadEditorTask.Error.Unwrap()), MessageType.Error);
					else
						EditorGUILayout.HelpBox(Resources.UI_UNITYPLUGIN_WINDOWEDITORWASCRASHED, MessageType.Warning);
					this.SetWebViewVisibility(false);

					if (string.IsNullOrEmpty(this.gameDataPath) == false)
					{
						EditorGUILayout.Space();
						GUILayout.BeginHorizontal();
						GUILayout.Space(10);
						if (GUILayout.Button(Resources.UI_UNITYPLUGIN_WINDOWRELOADBUTTON, GUILayout.Width(60)))
							this.Load(this.gameDataPath, null);
						GUILayout.EndHorizontal();
					}
					break;
			}
			base.OnGUI();
		}

		protected override void OnDestroy()
		{

			this.gameDataPath = null;
			this.Status = EditorStatus.Unloaded;

			base.OnDestroy();
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
			if (!Settings.Current.GameDataPaths.Contains(gameDataPath))
				return false;

			var reference = ValidationException.GetReference(exceptionId);
			var nearPanels = typeof(SceneView);
			var editorWindow = EditorWindow.GetWindow<GameDataEditorWindow>(nearPanels);
			editorWindow.Load(gameDataPath, reference);
			editorWindow.Focus();

			return true;
		}

		private void Load(string gameDataPath, string reference)
		{
			this.gameDataPath = gameDataPath;

			var prepareAndLoadAsync = new Tasks.Coroutine(PrepareEditor(reference));
			prepareAndLoadAsync.ContinueWith(_ =>
			{
				if (prepareAndLoadAsync.HasErrors == false)
					return;

				this.gameDataPath = null;
				this.Close();
			});
		}

		private IEnumerable PrepareEditor(string reference)
		{
			switch (Utils.ToolsRunner.CheckCharon())
			{
				case CharonCheckResult.MissingRuntime:
					yield return UpdateRuntimeWindow.ShowAsync();
					break;
				case CharonCheckResult.MissingExecutable:
					yield return Utils.ToolsRunner.UpdateCharonExecutable(ProgressUtils.ReportToLog(Resources.UI_UNITYPLUGIN_MENUCHECKUPDATES));
					break;
				case CharonCheckResult.Ok:
					break;
				default:
					throw new InvalidOperationException("Unknown Tools check result.");
			}

			var license = default(LicenseInfo);
			while (license == null && this)
			{
				var getLicense = Licenses.GetLicense(scheduleCoroutine: true);
				yield return getLicense;
				license = getLicense.GetResult();
				if (license == null)
					yield return LicenseActivationWindow.ShowAsync();
			}

			if (this.loadEditorTask != null)
				yield return this.loadEditorTask.IgnoreFault();

			var loadEditor = this.loadEditorTask = new Coroutine<object>(this.LoadEditor(reference));
			yield return loadEditor.IgnoreFault();
			if (loadEditor.HasErrors && this.loadEditorTask == loadEditor)
				this.Status = EditorStatus.Crashed;
		}

		private IEnumerable LoadEditor(string reference)
		{
			if (this.toolsProcessId != 0)
				this.KillToolsProcess(this.toolsProcessId);
			this.toolsProcessId = 0;
			this.Status = EditorStatus.Loading;

			this.titleContent = new GUIContent(Path.GetFileName(this.gameDataPath));

			var toolsPath = Settings.Current.ToolsPath;
			var port = Settings.Current.ToolsPort;
			var gameDataEditorUrl = "http://localhost:" + port + "/";
			var listenPromise = new Promise<string>();
			var errorPromise = new Promise<string>();
			var timeoutPromise = Promise.Delayed(TimeSpan.FromSeconds(10));

			if (Settings.Current.Verbose)
				Debug.Log("Starting gamedata editor at " + gameDataEditorUrl + "...");

			// ReSharper disable once AssignNullToNotNullAttribute
			var shadowCopyOfTools = Path.GetFullPath(Path.Combine(Utils.ToolsRunner.ToolShadowCopyPath, Path.GetFileName(toolsPath)));
			if (File.Exists(shadowCopyOfTools) == false)
			{
				if (Directory.Exists(Utils.ToolsRunner.ToolShadowCopyPath) == false)
					Directory.CreateDirectory(Utils.ToolsRunner.ToolShadowCopyPath);

				if (Settings.Current.Verbose)
					Debug.Log("Shadow copying tools to " + shadowCopyOfTools + ".");

				File.Copy(Settings.Current.ToolsPath, shadowCopyOfTools, overwrite: true);

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

			var toolsProcessTask = ToolsRunner.Run(
				new ToolExecutionOptions
				(
					shadowCopyOfTools,

					"LISTEN", Path.GetFullPath(this.gameDataPath),
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
			// re-paint on exit or crash
			toolsProcessTask.IgnoreFault().ContinueWith(_ => this.Repaint());

			if (Settings.Current.Verbose)
				Debug.Log("Launching gamedata editor process.");

			var startPromise = toolsProcessTask.IgnoreFault();

			yield return Promise.WhenAny(listenPromise, errorPromise, timeoutPromise, startPromise);

			if (errorPromise.IsCompleted)
			{
				Debug.LogError(Resources.UI_UNITYPLUGIN_WINDOWFAILEDTOSTARTEDITOR + errorPromise.GetResult());
				this.Status = EditorStatus.Crashed;
				yield break;
			}
			//else if (startPromise.IsCompleted)
			//{
			//	Debug.LogError(Resources.UI_UNITYPLUGIN_WINDOWFAILEDTOSTARTEDITOR + " Process has exited with code: " + toolsProcessTask.GetResult().ExitCode);
			//	this.Status = EditorStatus.Crashed;
			//	yield break;
			//}
			else if (timeoutPromise.IsCompleted)
			{
				Debug.LogWarning(Resources.UI_UNITYPLUGIN_WINDOWFAILEDTOSTARTEDITORTIMEOUT);
				this.Status = EditorStatus.Crashed;
				yield break;
			}

			if (!this)
			{
				this.Status = EditorStatus.Unloaded;
				yield break;
			}

			this.toolsProcessId = toolsProcessTask.GetResult().ProcessId;
			switch (Settings.Current.Browser)
			{
				case Browser.UnityEmbedded:
					this.LoadUrl(gameDataEditorUrl + reference);
					this.SetWebViewVisibility(true);
					this.Repaint();
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

			this.Status = EditorStatus.Ready;
		}

		private void KillToolsProcess(int processId)
		{
			if (Settings.Current.Verbose)
				Debug.Log(string.Format("Trying to kill process with id {0}.", processId));

			try
			{
				using (var process = Process.GetProcessById(processId))
				{
					process.CloseMainWindow();
					process.WaitForExit(500);
					process.Kill();
					process.WaitForExit(500);

				}
			}
			catch (Exception error)
			{
				if (Settings.Current.Verbose)
					Debug.Log(string.Format("Failed to kill process with id {0} because of error: {1}{2}", processId, Environment.NewLine, error.Message));
			}
		}
	}
}
