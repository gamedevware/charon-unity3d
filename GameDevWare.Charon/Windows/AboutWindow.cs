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
using GameDevWare.Charon.Async;
using GameDevWare.Charon.Utils;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.Charon.Windows
{
	internal class AboutWindow : EditorWindow
	{
		private string toolsVersion = Resources.UI_UNITYPLUGIN_WINDOWCHECKINGVERSION;
		private string licenseHolder = Resources.UI_UNITYPLUGIN_WINDOWCHECKINGVERSION;
		private string licenseKey = Resources.UI_UNITYPLUGIN_WINDOWCHECKINGVERSION;
		[NonSerialized]
		private Promise<Version> checkToolsVersion;

		public AboutWindow()
		{
			this.titleContent = new GUIContent(Resources.UI_UNITYPLUGIN_WINDOWABOUTCHARONTITLE);
			this.maxSize = this.minSize = new Vector2(380, 326);
			this.position = new Rect(
				(Screen.width - this.maxSize.x) / 2,
				(Screen.height - this.maxSize.y) / 2,
				this.maxSize.x,
				this.maxSize.y
			);
		}

		protected void OnGui()
		{
			GUILayout.Box("Charon", new GUIStyle { fontSize = 72, alignment = TextAnchor.MiddleCenter });
			GUILayout.Space(10);
			GUILayout.Label(Resources.UI_UNITYPLUGIN_WINDOWINFOGROUP, EditorStyles.boldLabel);
			EditorGUILayout.LabelField(Resources.UI_UNITYPLUGIN_WINDOWTOOLSVERSIONLABEL, this.toolsVersion);
			GUI.enabled = true;
			GUILayout.Space(10);
			GUILayout.Label(Resources.UI_UNITYPLUGIN_WINDOWSETTINGSGROUP, EditorStyles.boldLabel);
			GUI.enabled = System.IO.File.Exists(Settings.Current.ToolsPath) == false;
			Settings.Current.ToolsPath = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_WINDOWTOOLSPATH, Settings.Current.ToolsPath);
			GUI.enabled = true;
			Settings.Current.ToolsPort = EditorGUILayout.IntField(Resources.UI_UNITYPLUGIN_WINDOWTOOLSPORT, Settings.Current.ToolsPort);
			Settings.Current.Browser = Convert.ToInt32(EditorGUILayout.EnumPopup(Resources.UI_UNITYPLUGIN_WINDOWBROWSER, (Browser)Settings.Current.Browser));
			if (Settings.Current.Browser == (int)Browser.Custom)
			{
				EditorGUILayout.BeginHorizontal();
				{
					Settings.Current.BrowserPath = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_WINDOWBROWSERPATH, Settings.Current.BrowserPath);
					if (GUILayout.Button(Resources.UI_UNITYPLUGIN_WINDOWBROWSEBUTTON, EditorStyles.toolbarButton, GUILayout.Width(70), GUILayout.Height(18)))
					{
						Settings.Current.BrowserPath = EditorUtility.OpenFilePanel(Resources.UI_UNITYPLUGIN_WINDOWBROWSERPATHTITLE, "", "");
						GUI.changed = true;
						this.Repaint();
					}
					GUILayout.Space(5);
				}
				EditorGUILayout.EndHorizontal();
			}
			else
				GUILayout.Space(18);

			GUILayout.Space(18);
			GUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			if (GUILayout.Button(Resources.UI_UNITYPLUGIN_WINDOWOKBUTTON, GUILayout.Width(80)))
				this.Close();
			GUILayout.EndHorizontal();

			if (GUI.changed)
				Settings.Current.Save();
		}

		protected void Update()
		{
			switch (CharonCli.CheckCharon())
			{
				case CharonCheckResult.MissingRuntime:
					this.toolsVersion = Resources.UI_UNITYPLUGIN_WINDOWCHECKRESULTMISSINGMONOORDOTNET;
					break;
				case CharonCheckResult.MissingExecutable:
					this.toolsVersion = Resources.UI_UNITYPLUGIN_WINDOWCHECKRESULTMISSINGTOOLS;
					this.licenseHolder = "";
					this.licenseKey = "";
					break;
				case CharonCheckResult.Ok:
					if (this.checkToolsVersion == null)
					{
						this.toolsVersion = Resources.UI_UNITYPLUGIN_WINDOWCHECKINGVERSION;
						this.checkToolsVersion = CharonCli.GetVersionAsync();
						this.checkToolsVersion.ContinueWith(r =>
						{
							if (r.HasErrors)
								this.toolsVersion = r.Error.Unwrap().Message;
							else
								this.toolsVersion = r.GetResult().ToString();
							this.Repaint();
						});
						this.Repaint();
					}
					break;

			}
		}
	}
}
