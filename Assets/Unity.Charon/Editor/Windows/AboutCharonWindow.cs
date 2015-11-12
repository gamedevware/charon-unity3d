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
using Assets.Unity.Charon.Editor.Tasks;
using Assets.Unity.Charon.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Assets.Unity.Charon.Editor.Windows
{
	class AboutCharonWindow : EditorWindow
	{
		private string ToolsVersion = "Checking...";
		[NonSerialized]
		private ExecuteCommandTask checkToolsVersion;

		public AboutCharonWindow()
		{
			this.titleContent = new GUIContent("About Charon");
			this.maxSize = minSize = new Vector2(380, 346);
		}

		protected void OnGUI()
		{
			GUILayout.Box("Charon", new GUIStyle { fontSize = 72, alignment = TextAnchor.MiddleCenter });
			GUILayout.Space(10);
			GUILayout.Label("Info:", EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Tools Version", ToolsVersion);
			EditorGUILayout.LabelField("License Holder", "");
			EditorGUILayout.LabelField("License Key", "");
			GUILayout.Space(10);
			GUILayout.Label("Settings", EditorStyles.boldLabel);
			GUI.enabled = System.IO.File.Exists(Settings.Current.ToolsPath) == false;
			Settings.Current.ToolsPath = EditorGUILayout.TextField("Tools Path", Settings.Current.ToolsPath);
			GUI.enabled = true;
			Settings.Current.ToolsPort = EditorGUILayout.IntField("Tools Port", Settings.Current.ToolsPort);
			Settings.Current.Browser = (Browser)EditorGUILayout.EnumPopup("Browser:", Settings.Current.Browser);
			if (Settings.Current.Browser == Browser.Custom)
			{
				EditorGUILayout.BeginHorizontal();
				{
					Settings.Current.BrowserPath = EditorGUILayout.TextField("Browser Path", Settings.Current.BrowserPath);
					if (GUILayout.Button("Browse...", EditorStyles.toolbarButton, GUILayout.Width(70), GUILayout.Height(18)))
					{
						Settings.Current.BrowserPath = EditorUtility.OpenFilePanel("Path to browser executable", "", "");
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
			if (GUILayout.Button("Ok", GUILayout.Width(80)))
				this.Close();
			GUILayout.EndHorizontal();

			if (GUI.changed)
			{
				Settings.Current.Version++;
				Settings.Current.Save();
			}
		}

		protected void Update()
		{
			switch (ToolsUtils.CheckTools())
			{
				case ToolsCheckResult.MissingMono:
					this.ToolsVersion = "Missing Mono!";
					break;
				case ToolsCheckResult.MissingTools:
					this.ToolsVersion = "Missing Tools!";
					break;
				case ToolsCheckResult.Ok:
					if (this.checkToolsVersion == null)
					{
						this.checkToolsVersion = new ExecuteCommandTask(
							Settings.Current.ToolsPath,
							(s, ea) => { if (!string.IsNullOrEmpty(ea.Data)) this.ToolsVersion = ea.Data; },
							(s, ea) => { if (!string.IsNullOrEmpty(ea.Data)) this.ToolsVersion = ea.Data; },
							"VERSION");
						this.checkToolsVersion.RequireDotNetRuntime();
						this.checkToolsVersion.Start();
					}
					break;
			}
		}
	}
}
