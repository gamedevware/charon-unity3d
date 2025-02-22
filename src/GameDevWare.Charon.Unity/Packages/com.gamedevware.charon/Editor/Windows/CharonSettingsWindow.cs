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
			{
				EditorGUILayout.LabelField(Resources.UI_UNITYPLUGIN_WINDOW_CHARON_EDITOR_VERSION_LABEL, CharonEditorVersion);
				GUILayout.Space(5);
				if (GUILayout.Button(Resources.UI_UNITYPLUGIN_WINDOW_CHECK_UPDATES_BUTTON, EditorStyles.miniButton, GUILayout.Width(120), GUILayout.Height(18)))
				{
					CharonEditorMenu.CheckUpdates();
					GUI.changed = true;
				}

				GUILayout.Space(5);
			}
			EditorGUILayout.EndHorizontal();

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
