/*
	Copyright (c) 2015 Denis Zykov

	This is part of Charon Game Data Editor Unity Plugin.

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
using System.IO;
using Assets.Unity.Charon.Editor.Tasks;
using Assets.Unity.Charon.Editor.Utils;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
// ReSharper disable InconsistentNaming

namespace Assets.Unity.Charon.Editor.Windows
{
	class GameDataEditorWindow : WebViewEditorWindow, IHasCustomMenu
	{
		public readonly static string ToolShadowCopyPath = FileUtil.GetUniqueTempPathInProject();
		private static bool assemblyReloadLocked;

		[SerializeField]
		private string gameDataPath;

		private Promise loadingTask;
		private ExecuteCommandTask editorProcess;

		public bool IsLoaded { get { return this.editorProcess != null && this.loadingTask != null && this.loadingTask.IsCompleted; } }
		public bool IsLoading { get { return this.loadingTask != null && this.editorProcess != null && !this.loadingTask.IsCompleted; } }
		public bool IsCrashed { get { return this.loadingTask != null && this.editorProcess != null && this.loadingTask.IsCompleted && !this.editorProcess.IsRunning; } }


		public GameDataEditorWindow()
		{
			this.titleContent = new GUIContent(Resources.UI_UNITYPLUGIN_WINDOWEDITORTITLE);
			this.minSize = new Vector2(300, 300);
			this.Paddings = new Rect(3, 3, 3, 19);
		}

		protected override void OnGUI()
		{
			if (this.IsCrashed)
			{
				if (this.loadingTask.HasErrors)
					EditorGUILayout.HelpBox(string.Format(Resources.UI_UNITYPLUGIN_WINDOWLOADINGFAILEDWITHERROR, this.loadingTask.Error.Unwrap()), MessageType.Error);
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
			}
			else if (this.IsLoading)
			{
				EditorGUILayout.HelpBox(Resources.UI_UNITYPLUGIN_WINDOWEDITORLOADING, MessageType.Info);
			}
			else
			{
				EditorGUILayout.HelpBox(string.Format(Resources.UI_UNITYPLUGIN_WINDOWEDITORISOPENED, this.gameDataPath), MessageType.Info);
				base.OnGUI();
			}

			GUILayout.BeginVertical();
			GUILayout.FlexibleSpace();
			GUILayout.BeginHorizontal();
			var codeRecpmpilationEnabled = EditorGUILayout.ToggleLeft(Resources.UI_UNITYPLUGIN_WINDOWRESUMECODERECOMPILATION, !assemblyReloadLocked);
			if (!codeRecpmpilationEnabled && !assemblyReloadLocked)
			{
				this.LockCodeReload();
			}
			else if (codeRecpmpilationEnabled && assemblyReloadLocked)
			{
				this.UnlockCodeReload();
			}
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
		}
		protected override void OnDestroy()
		{
			this.gameDataPath = null;
			this.CleanUp();
			base.OnDestroy();
		}
		protected override void OnBeforeUnload()
		{
			if (this.editorProcess != null)
				this.editorProcess.Kill();
			this.CleanUp();
		}

		void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
		{
			menu.AddItem(new GUIContent(Resources.UI_UNITYPLUGIN_WINDOWRELOADBUTTON), false, this.Reload);
		}

		protected void Update()
		{
			if (this.loadingTask == null && string.IsNullOrEmpty(this.gameDataPath) == false)
				this.Load(gameDataPath, null);
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
			var loadAsync = CoroutineScheduler.Schedule(this.LoadEditor(gameDataPath, reference));
			loadAsync.ContinueWith(ar => { if (loadAsync.HasErrors) this.CleanUp(); }, null);
			this.loadingTask = loadAsync;
		}
		private void CleanUp()
		{
			this.UnlockCodeReload();
			this.loadingTask = null;
			if (this.editorProcess != null)
				this.editorProcess.Close();
			this.editorProcess = null;

		}

		private IEnumerable LoadEditor(string gameDataPath, string reference)
		{
			switch (ToolsUtils.CheckTools())
			{
				case ToolsCheckResult.MissingRuntime: yield return UpdateRuntimeWindow.ShowAsync(); break;
				case ToolsCheckResult.MissingTools: yield return UpdateToolsWindow.ShowAsync(); break;
				case ToolsCheckResult.Ok: break;
				default: throw new InvalidOperationException("Unknown Tools check result.");
			}

			if (this.editorProcess != null)
				yield return this.editorProcess.Close();

			this.gameDataPath = gameDataPath;

			this.titleContent = new GUIContent(Path.GetFileNameWithoutExtension(gameDataPath));

			var toolsPath = Settings.Current.ToolsPath;
			var port = Settings.Current.ToolsPort;
			var gameDataEditorUrl = "http://localhost:" + port + "/";
			var listenPromise = new Promise<string>();
			var errorPromise = new Promise<string>();
			var timeout = Promise.Delayed(TimeSpan.FromSeconds(10));

			if (Settings.Current.Verbose)
				Debug.Log("Starting gamedata editor at " + gameDataEditorUrl + "...");

			// ReSharper disable once AssignNullToNotNullAttribute
			var shadowCopyOfTools = Path.GetFullPath(Path.Combine(ToolShadowCopyPath, Path.GetFileName(toolsPath)));
			if (File.Exists(shadowCopyOfTools) == false)
			{
				if (Directory.Exists(ToolShadowCopyPath) == false)
					Directory.CreateDirectory(ToolShadowCopyPath);

				if (Settings.Current.Verbose)
					Debug.Log("Shadow copying tools to " + shadowCopyOfTools + ".");

				File.Copy(Settings.Current.ToolsPath, shadowCopyOfTools);

				var configPath = toolsPath + ".config";
				var configShadowPath = shadowCopyOfTools + ".config";
				if (File.Exists(configPath))
				{
					var configText = File.ReadAllText(configPath);
					configText = configText.Replace("<!--add key=\"AppData\" value=\"#\"/-->", "<add key=\"AppData\" value=\"" + Path.GetFullPath("./Library") + "\"/>");
					if (Settings.Current.Verbose)
						configText = configText.Replace("<!--appender-ref ref=\"FileAppender\"/-->", "<appender-ref ref=\"FileAppender\"/>");
					else
						configText = configText.Replace("<appender-ref ref=\"FileAppender\"/>", "<!--appender-ref ref=\"FileAppender\"/-->");

					if (Settings.Current.Verbose)
						Debug.Log("Shadow copying tools configuration to " + configShadowPath + ".");
					File.WriteAllText(configShadowPath, configText);
				}
				else
				{
					Debug.LogWarning("Missing required configuration file at '" + configPath + "'.");
				}
			}

			this.editorProcess = new ExecuteCommandTask
			(
				shadowCopyOfTools,
				(sender, args) =>
				{
					if (string.IsNullOrEmpty(args.Data)) return;
					listenPromise.TrySetResult(args.Data);
				},
				(sender, args) =>
				{
					if (string.IsNullOrEmpty(args.Data)) return;
					errorPromise.TrySetResult(args.Data);
					if (Settings.Current.Verbose)
						Debug.LogWarning(Path.GetFileName(Settings.Current.ToolsPath) + ": " + args.Data);
				},
				"LISTEN",
				Path.GetFullPath(gameDataPath),
				"--port",
				port.ToString(),
				"--parentPid",
				System.Diagnostics.Process.GetCurrentProcess().Id.ToString(),
				Settings.Current.Verbose ? "--verbose" : ""
			);
			this.editorProcess.RequireDotNetRuntime();
			this.editorProcess.Start();

			// re-paint on exit or crash
			this.editorProcess.IgnoreFault().ContinueWith(_ => this.Repaint(), null);

			if (Settings.Current.Verbose)
				Debug.Log("Launching gamedata editor process.");

			yield return Promise.WhenAny(listenPromise, errorPromise, timeout);

			if (errorPromise.IsCompleted)
			{
				Debug.LogError(string.Format(Resources.UI_UNITYPLUGIN_WINDOWFAILEDTOSTARTEDITOR, errorPromise.GetResult()));
				this.editorProcess.Close();
				yield break;
			}
			else if (timeout.IsCompleted)
			{
				Debug.LogWarning(Resources.UI_UNITYPLUGIN_WINDOWFAILEDTOSTARTEDITORTIMEOUT);
				this.editorProcess.Kill();
				yield break;
			}

			switch ((Browser)Settings.Current.Browser)
			{
				case Browser.UnityEmbedded:
					this.LoadUrl(gameDataEditorUrl + reference);
					this.SetWebViewVisibility(true);
					this.Repaint();
					break;
				case Browser.Custom:
					if (string.IsNullOrEmpty(Settings.Current.BrowserPath))
						goto SystemDefault;

					var startBrowser = new ExecuteCommandTask(Settings.Current.BrowserPath, gameDataEditorUrl + reference);
					startBrowser.Start();
					break;
				case Browser.SystemDefault:
					SystemDefault:
					EditorUtility.OpenWithDefaultApp(gameDataEditorUrl + reference);
					break;

			}

			if (!assemblyReloadLocked)
				this.LockCodeReload();
		}

		private void LockCodeReload()
		{
			if (assemblyReloadLocked)
				return;

			this.Paddings = new Rect(3, 3, 3, 19);
			assemblyReloadLocked = true;
			EditorApplication.LockReloadAssemblies();
		}
		private void UnlockCodeReload()
		{
			if (!assemblyReloadLocked)
				return;

			this.Paddings = new Rect(3, 3, 3, 3);
			assemblyReloadLocked = false;
			EditorApplication.UnlockReloadAssemblies();
		}
	}
}
