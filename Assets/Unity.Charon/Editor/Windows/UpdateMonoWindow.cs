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
using System.IO;
using Assets.Unity.Charon.Editor.Tasks;
using Assets.Unity.Charon.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Assets.Unity.Charon.Editor.Windows
{
	class UpdateMonoWindow : EditorWindow
	{
#if UNITY_EDITOR_WIN
		private const string MONO_EXECUTABLE_NAME = "mono.exe";
		private static readonly string MonoDefaultLocation = Path.Combine(GetProgramFilesx86(), @"Mono\bin");
#else
		private const string MONO_EXECUTABLE_NAME = "mono";
		private static readonly string MonoDefaultLocation = @"/usr/bin";
#endif

		private string monoPath;

		private event EventHandler Done;
		private event EventHandler<ErrorEventArgs> Cancel;

		public UpdateMonoWindow()
		{
			this.titleContent = new GUIContent("Mono Update");
			this.maxSize = minSize = new Vector2(420, 148);
		}

		protected void OnGUI()
		{
			EditorGUILayout.HelpBox("Please provide path to Mono binaries.\r\n" +
									"Default location is: " + MonoDefaultLocation + "\r\n" +
									"If it doesn't exists you can press `Download Mono` button below.", MessageType.Info);
			GUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			if (GUILayout.Button("Download Mono", GUILayout.Width(140)))
				Application.OpenURL("http://www.mono-project.com/download/#download-mac");
			GUILayout.EndHorizontal();

			GUILayout.Space(18);
			EditorGUILayout.BeginHorizontal();
			{
				this.monoPath = EditorGUILayout.TextField("Mono Path", this.monoPath);
				if (GUILayout.Button("Browse...", EditorStyles.toolbarButton, GUILayout.Width(70), GUILayout.Height(18)))
				{
					this.monoPath = EditorUtility.OpenFolderPanel("Path to Mono binaries", "", "bin");
					GUI.changed = true;
					this.Repaint();
				}
				GUILayout.Space(5);
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(18);
			GUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			if (GUILayout.Button("Cancel", GUILayout.Width(80)))
				this.Close();
			GUILayout.EndHorizontal();

			if (string.IsNullOrEmpty(this.monoPath) == false &&
				Directory.Exists(this.monoPath) &&
				File.Exists(Path.Combine(this.monoPath, MONO_EXECUTABLE_NAME)))
			{
				ToolsUtils.MonoPath = Path.Combine(this.monoPath, MONO_EXECUTABLE_NAME);
				Debug.Log("Mono location is set to '" + ToolsUtils.MonoPath + "'. Closing window.");

				if (this.Done != null)
					this.Done(this, EventArgs.Empty);
				this.Close();
			}
		}
		protected void OnDestroy()
		{
			if (File.Exists(ToolsUtils.MonoPath))
				return;

			if (this.Cancel != null)
				this.Cancel(this, new ErrorEventArgs(new InvalidOperationException("Operation was cancelled by user.")));

			Debug.Log("Mono Update window is closed by user.");
		}

		public static IAsyncResult ShowAsync()
		{
			var promise = new Promise();
			var window = EditorWindow.GetWindow<UpdateMonoWindow>(utility: true);

			window.monoPath = string.IsNullOrEmpty(ToolsUtils.MonoPath) ? MonoDefaultLocation : ToolsUtils.MonoPath;

			Debug.Log("Showing 'Update Mono' window. With current mono location: '" + window.monoPath + "'.");

			window.Done += (sender, args) => promise.TrySetCompleted();
			window.Cancel += (sender, args) => promise.TrySetFailed(args.GetException());

			return promise;
		}

#if UNITY_EDITOR_WIN
		private static string GetProgramFilesx86()
		{
			if (8 == IntPtr.Size
				|| (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
			{
				return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
			}

			return Environment.GetEnvironmentVariable("ProgramFiles");
		}
#endif
	}
}
