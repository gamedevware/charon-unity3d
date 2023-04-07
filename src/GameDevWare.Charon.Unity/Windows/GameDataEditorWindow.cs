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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using GameDevWare.Charon.Unity.Utils;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

using Debug = UnityEngine.Debug;

// ReSharper disable InconsistentNaming

namespace GameDevWare.Charon.Unity.Windows
{
	internal class GameDataEditorWindow : WebViewEditorWindow, IHasCustomMenu
	{
		public static readonly bool IsWebViewAvailable = typeof(SceneView).Assembly.GetType("UnityEditor.WebView", throwOnError: false) != null;

		private bool watchLocalProcess;

		public GameDataEditorWindow()
		{
			this.titleContent = new GUIContent(Resources.UI_UNITYPLUGIN_WINDOW_EDITOR_TITLE);
			this.minSize = new Vector2(300, 300);
			this.Padding = new Rect(3, 3, 3, 3);
		}

		[UsedImplicitly]
		protected void CreateGUI()
		{
			this.OnLoad();
		}
		[UsedImplicitly]
		protected void OnEnabled()
		{
			this.OnLoad();
		}

		private void OnLoad()
		{
			if (!this.watchLocalProcess)
				return;

			if (File.Exists(CharonCli.GetDefaultLockFilePath()) == false && this)
			{
				this.titleContent = new GUIContent(Resources.UI_UNITYPLUGIN_WINDOW_EDITOR_TITLE);
				this.Close();
			}
		}

		[SuppressMessage("ReSharper", "InconsistentNaming")]
		protected override void OnGUI()
		{
			base.OnGUI();
		}

		/// <inheritdoc />
		protected override void OnDestroy()
		{
			base.OnDestroy();

			if (Settings.Current.Verbose)
				Debug.Log("Ending game data editor process because window is closed.");

			if (this.watchLocalProcess)
				CharonCli.FindAndEndGracefully();
		}

		private void KillProcess()
		{
			if (this.watchLocalProcess)
				CharonCli.FindAndEndGracefully();

			if (this)
				this.Close();
		}

		void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
		{
			menu.AddItem(new GUIContent(Resources.UI_UNITYPLUGIN_WINDOW_RELOAD_BUTTON), false, this.Reload);
		}

		public static void FindAllAndClose()
		{
			foreach (var window in UnityEngine.Resources.FindObjectsOfTypeAll<GameDataEditorWindow>())
				window.KillProcess();
		}

		public static void ShowWebView(string gameDataPath, Uri gameDataEditorUrl, Uri navigateUrl)
		{
			if (gameDataPath == null) throw new ArgumentNullException("gameDataPath");
			if (gameDataEditorUrl == null) throw new ArgumentNullException("gameDataEditorUrl");
			if (navigateUrl == null) throw new ArgumentNullException("navigateUrl");

			var nearPanels = typeof(SceneView);
			var editorWindow = GetWindow<GameDataEditorWindow>(nearPanels);
			editorWindow.titleContent = new GUIContent(Path.GetFileName(gameDataPath));
			editorWindow.LoadUrl(navigateUrl.OriginalString);
			editorWindow.SetWebViewVisibility(true);
			editorWindow.Repaint();
			editorWindow.Focus();
			editorWindow.watchLocalProcess = string.Equals(gameDataEditorUrl.Host, "localhost");
		}
	}
}
