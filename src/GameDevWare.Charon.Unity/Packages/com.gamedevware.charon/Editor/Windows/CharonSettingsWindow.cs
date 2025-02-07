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
using System.Threading.Tasks;
using GameDevWare.Charon.Editor.Utils;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.Charon.Editor.Windows
{
	[Serializable]
	public static class CharonSettingsWindow
	{
		private static string CharonEditorVersion = (typeof(CharonSettingsWindow).Assembly.GetName().Version ?? new Version()).ToString();

		private static Task<SemanticVersion> currentCharonsVersion;
		private static Task<SemanticVersion> lastCharonVersion;

		private static bool HasNewCharonVersion =>
			currentCharonsVersion != null &&
			lastCharonVersion != null &&
			currentCharonsVersion.IsCompleted &&
			lastCharonVersion.IsCompleted;

		[SettingsProvider]
		public static SettingsProvider CreateCharonSettingsProvider()
		{
			var provider = new SettingsProvider("Project/Charon", SettingsScope.Project)
			{
				label = "Charon",
				// Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
				guiHandler = PreferencesGUI,

				// Populate the search keywords to enable smart search filtering and label highlighting:
				keywords = new HashSet<string>(new[] {
					Resources.UI_UNITYPLUGIN_WINDOW_CHARON_EDITOR_VERSION_LABEL,
					Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_AVAILABLE_TITLE,
					Resources.UI_UNITYPLUGIN_CHARON_VERSION_LABEL,
					Resources.UI_UNITYPLUGIN_ABOUT_IDLE_CLOSE_TIMEOUT_LABEL,
					Resources.UI_UNITYPLUGIN_ABOUT_SERVER_ADDRESS_LABEL,
					Resources.UI_UNITYPLUGIN_WINDOW_BROWSER_PATH,
					Resources.UI_UNITYPLUGIN_ABOUT_EDITOR_APPLICATION_TYPE_LABEL,
					Resources.UI_UNITYPLUGIN_WINDOW_BROWSER_PATH_TITLE,
				})
			};
			return provider;
		}

		[UsedImplicitly]
		private static void PreferencesGUI(string searchContext)
		{
			GUILayout.Space(10);
			GUILayout.Label(Resources.UI_UNITYPLUGIN_WINDOW_INFO_GROUP, EditorStyles.boldLabel);
			EditorGUILayout.BeginHorizontal();
			// EditorGUILayout.LabelField(Resources.UI_UNITYPLUGIN_CHARON_VERSION_LABEL, charonVersion);
			if (HasNewCharonVersion)
			{
				if (GUILayout.Button(Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_AVAILABLE_TITLE, EditorStyles.miniButton, GUILayout.Width(120), GUILayout.Height(18)))
				{
					CharonEditorMenu.CheckUpdates();
					GUI.changed = true;
				}
				GUILayout.Space(5);
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.LabelField(Resources.UI_UNITYPLUGIN_WINDOW_CHARON_EDITOR_VERSION_LABEL, CharonEditorVersion);
			GUI.enabled = true;
			GUILayout.Space(10);
			GUILayout.Label(Resources.UI_UNITYPLUGIN_WINDOW_SETTINGS_GROUP, EditorStyles.boldLabel);

			var settings = CharonEditorModule.Instance.Settings;
			var idleCloseTimeoutStr = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_ABOUT_IDLE_CLOSE_TIMEOUT_LABEL, settings.IdleCloseTimeout.ToString());
			if (TimeSpan.TryParse(idleCloseTimeoutStr, out var newIdleCloseTimeout))
				settings.IdleCloseTimeout = newIdleCloseTimeout;

			settings.ServerAddress = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_ABOUT_SERVER_ADDRESS_LABEL, settings.ServerAddress);
			settings.EditorApplication = (CharonEditorApplication)EditorGUILayout.EnumPopup(Resources.UI_UNITYPLUGIN_ABOUT_EDITOR_APPLICATION_TYPE_LABEL, settings.EditorApplication);

			if (settings.EditorApplication == CharonEditorApplication.CustomBrowser)
			{
				EditorGUILayout.BeginHorizontal();
				{
					settings.CustomEditorApplicationPath = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_WINDOW_BROWSER_PATH, settings.CustomEditorApplicationPath);
					if (GUILayout.Button(Resources.UI_UNITYPLUGIN_WINDOW_BROWSE_BUTTON, EditorStyles.miniButton, GUILayout.Width(70), GUILayout.Height(18)))
					{
						settings.CustomEditorApplicationPath = EditorUtility.OpenFilePanel(Resources.UI_UNITYPLUGIN_WINDOW_BROWSER_PATH_TITLE, "", "");
						GUI.changed = true;
					}
					GUILayout.Space(5);
				}
				EditorGUILayout.EndHorizontal();
			}
			else
				GUILayout.Space(18);

			GUILayout.Space(18);
		}
	}
}
