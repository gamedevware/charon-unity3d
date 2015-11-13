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
using System.Text;
using System.Text.RegularExpressions;
using Assets.Unity.Charon.Editor.Tasks;
using Assets.Unity.Charon.Editor.Utils;
using UnityEditor;
using UnityEngine;

// ReSharper disable UnusedMember.Local
namespace Assets.Unity.Charon.Editor.Windows
{
	class UpdateRuntimeWindow : EditorWindow
	{
#if UNITY_EDITOR_WIN
		private const string MONO_EXECUTABLE_NAME = "mono.exe";
		private static readonly string MonoDefaultLocation = Path.Combine(GetProgramFilesx86(), @"Mono\bin");
#else
		private const string MONO_EXECUTABLE_NAME = "mono";
		private static readonly string MonoDefaultLocation = @"/usr/bin";
#endif
		private static readonly Version MinimalMonoVersion = new Version(4, 0, 3);

		private string monoPath;
		private string runtimeVersion;
		private Promise checkRuntimeVersionCoroutine;
		private ExecuteCommandTask runMonoTask;

		private event EventHandler Done;
		private event EventHandler<ErrorEventArgs> Cancel;

		public UpdateRuntimeWindow()
		{
			this.titleContent = new GUIContent(Resources.UI_UNITYPLUGIN_WINDOWUPDATERUNTIMETITLE);
			this.maxSize = minSize = new Vector2(480, 220);
		}

		protected void OnGUI()
		{
			EditorGUILayout.HelpBox(Resources.UI_UNITYPLUGIN_WINDOWRUNTIMEREQUIRED + "\r\n\r\n" +
									string.Format(Resources.UI_UNITYPLUGIN_WINDOWFINDMONOMANUALLY) + "\r\n" +
									Resources.UI_UNITYPLUGIN_WINDOWDOWNLOADMONO + "\r\n" +
#if UNITY_EDITOR_WIN
									Resources.UI_UNITYPLUGIN_WINDOWDOWNLOADDOTNET + "\r\n\r\n" +
#endif
									Resources.UI_UNITYPLUGIN_WINDOWPRESSHELP, MessageType.Info);

			var checkIsRunning = this.checkRuntimeVersionCoroutine != null && this.checkRuntimeVersionCoroutine.IsCompleted == false;
			GUI.enabled = !checkIsRunning;
			GUILayout.BeginHorizontal();
			EditorGUILayout.Space();
#if UNITY_EDITOR_WIN
			if (GUILayout.Button(Resources.UI_UNITYPLUGIN_WINDOWDOWNLOADDOTNETBUTTON, GUILayout.Width(140)))
				Application.OpenURL("https://www.microsoft.com/ru-RU/download/details.aspx?id=42643");
#endif
			if (GUILayout.Button(Resources.UI_UNITYPLUGIN_WINDOWDOWNLOADMONOBUTTON, GUILayout.Width(140)))
				Application.OpenURL("http://www.mono-project.com/download/#download-mac");
			if (GUILayout.Button(Resources.UI_UNITYPLUGIN_WINDOWHELPBUTTON, GUILayout.Width(40)))
				Application.OpenURL("https://github.com/deniszykov/charon-unity3d/blob/master/README.md");
			GUILayout.EndHorizontal();

			GUILayout.Space(18);
			EditorGUILayout.BeginHorizontal();
			{
				this.monoPath = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_WINDOWPATHTOMONO, this.monoPath);
				if (GUILayout.Button(Resources.UI_UNITYPLUGIN_WINDOWBROWSEBUTTON, EditorStyles.toolbarButton, GUILayout.Width(70), GUILayout.Height(18)))
				{
					this.monoPath = EditorUtility.OpenFolderPanel(Resources.UI_UNITYPLUGIN_WINDOWPATHTOMONO, "", "");
					GUI.changed = true;
					this.Repaint();
				}
				GUILayout.Space(5);
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.LabelField(Resources.UI_UNITYPLUGIN_WINDOWRUNTIMEVERSION, this.runtimeVersion, new GUIStyle { richText = true });

			GUILayout.Space(18);
			GUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			if (GUILayout.Button(Resources.UI_UNITYPLUGIN_WINDOWRECHECKBUTTON, GUILayout.Width(80)))
				this.RunCheck();

			GUI.enabled = true;
			if (GUILayout.Button(Resources.UI_UNITYPLUGIN_WINDOWCANCELBUTTON, GUILayout.Width(80)))
				this.Close();
			GUILayout.EndHorizontal();

			var canCheckMono = (GUI.changed &&
								string.IsNullOrEmpty(this.monoPath) == false &&
								(File.Exists(Path.Combine(this.monoPath, MONO_EXECUTABLE_NAME)) ||
									File.Exists(this.monoPath)));

			// ReSharper disable once ConditionIsAlwaysTrueOrFalse
			if (!checkIsRunning && canCheckMono)
				this.RunCheck();
		}

		private void RunCheck()
		{
			this.checkRuntimeVersionCoroutine = new Tasks.Coroutine(this.CheckRuntimeVersionAsync());
		}
		private IEnumerable CheckRuntimeVersionAsync()
		{

#if UNITY_EDITOR_WIN
			var dotNetRuntimeVersion = ToolsUtils.Get45or451FromRegistry();
			if (dotNetRuntimeVersion != null)
			{
				UpdateRuntimeVersionLabel(dotNetRuntimeVersion, ".NET", true);
				this.RaiseDone(monoRuntimePath: null);
				yield break;
			}
#endif

			if (string.IsNullOrEmpty(this.monoPath))
				yield break;

			this.runtimeVersion = Resources.UI_UNITYPLUGIN_WINDOWCHECKINGMONO;
			var monoRuntimePath = File.Exists(this.monoPath) ? this.monoPath : Path.Combine(this.monoPath, MONO_EXECUTABLE_NAME);
			var output = new StringBuilder();
			this.runMonoTask = new ExecuteCommandTask(
				processPath: monoRuntimePath,
				outputDataReceived: ((sender, args) => output.Append(args.Data ?? string.Empty)),
				errorDataReceived: ((sender, args) => output.Append(args.Data ?? string.Empty)),
				arguments: new[] { "--version" });
			this.runMonoTask.Start();

			yield return this.runMonoTask.IgnoreFault();

			var monoRuntimeVersionMatch = new Regex(@"version (?<v>[0-9]+\.[0-9]+\.[0-9]+)", RegexOptions.Multiline | RegexOptions.IgnoreCase).Match(output.ToString());
			if (!monoRuntimeVersionMatch.Success)
			{
				if (Settings.Current.Verbose)
					Debug.LogWarning(output.Length > 0 ? output.ToString() : string.Format(Resources.UI_UNITYPLUGIN_WINDOWCHECKINGMONOFAILED, this.runMonoTask.ExitCode));

				this.UpdateRuntimeVersionLabel(Resources.UI_UNITYPLUGIN_WINDOWRUNTIMEVERSIONUNKNOWN, "Mono", isValid: false);
			}
			else
			{
				try
				{
					var monoRuntimeVersion = new Version(monoRuntimeVersionMatch.Groups["v"].Value);
					this.runtimeVersion = monoRuntimeVersion + " (Mono)";

					if (monoRuntimeVersion >= MinimalMonoVersion)
						this.RaiseDone(monoRuntimePath);
					else
						this.UpdateRuntimeVersionLabel(monoRuntimeVersion.ToString(), "Mono", isValid: false);
				}
				catch (Exception e)
				{
					if (Settings.Current.Verbose)
						Debug.LogError(e);
					this.UpdateRuntimeVersionLabel(Resources.UI_UNITYPLUGIN_WINDOWRUNTIMEVERSIONERROR, "Mono", isValid: false);
				}
			}
		}

		private void RaiseDone(string monoRuntimePath)
		{
			ToolsUtils.MonoPath = monoRuntimePath;
			if (this.Done != null)
				this.Done(this, EventArgs.Empty);
			this.Done = null;
			this.Cancel = null;

			if (Settings.Current.Verbose)
				Debug.Log(string.Format("'{0}' window is closed with selected Mono Runtime path: {1}. Runtime version: {2}.", this.titleContent.text, ToolsUtils.MonoPath, this.runtimeVersion));
			//this.Close();
		}
		private void RaiseCancel()
		{
			if (this.Cancel != null)
			{
				this.Cancel(this, new ErrorEventArgs(new InvalidOperationException(Resources.UI_UNITYPLUGIN_OPERATIONCANCELLED)));
				if (Settings.Current.Verbose)
					Debug.Log(string.Format("'{0}' window is closed by user.", this.titleContent.text));
			}
			this.Cancel = null;
			this.Done = null;
		}
		private void UpdateRuntimeVersionLabel(string version, string runtime, bool isValid)
		{
			if (isValid)
				this.runtimeVersion = string.Format("{0} ({1})", version, runtime);
			else
				this.runtimeVersion = string.Format("<color=#ff0000ff>{0} ({1})</color>", version, runtime);

			this.Repaint();
		}
		private void OnDestroy()
		{
			if (this.runMonoTask != null)
				this.runMonoTask.Close();

			this.RaiseCancel();
		}

		public static IAsyncResult ShowAsync()
		{
			var promise = new Promise();
			var window = EditorWindow.GetWindow<UpdateRuntimeWindow>(utility: true);


			window.Done += (sender, args) => promise.TrySetCompleted();
			window.Cancel += (sender, args) => promise.TrySetFailed(args.GetException());

			window.monoPath = string.IsNullOrEmpty(ToolsUtils.MonoPath) ? MonoDefaultLocation : ToolsUtils.MonoPath;
#if UNITY_EDITOR_WIN
			window.runtimeVersion = ToolsUtils.Get45or451FromRegistry();
			window.RunCheck();
#endif
			if (Settings.Current.Verbose)
				Debug.Log(string.Format("Showing '{0}' window. Where current mono location: '{1}'.", window.titleContent.text, window.monoPath));

			return promise;
		}

#if UNITY_EDITOR_WIN
		// ReSharper disable once IdentifierTypo
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
