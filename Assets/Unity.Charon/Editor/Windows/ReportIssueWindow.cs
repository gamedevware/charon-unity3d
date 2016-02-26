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
using System.Collections.Generic;
using System.IO;
using System.Text;
using Assets.Unity.Charon.Editor.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.Unity.Charon.Editor.Windows
{
	class ReportIssueWindow : EditorWindow
	{
		public enum IssueType
		{
			Bug = 0,
			NewFeature,
			Improvement,
			Question
		}

		public string Reporter;
		public string Description;
		public IssueType Type;
		public HashSet<string> Attachments;
		public string LastError;
		public string ThanksMessage;

		private HashSet<string> attachmentsToRemove;
		private HashSet<string> attachmentsToAdd;
		private Promise reportCoroutine;

		public ReportIssueWindow()
		{
			this.titleContent = new GUIContent(Resources.UI_UNITYPLUGIN_WINDOWREPORTISSUETITLE);
			this.position = new Rect(
				(Screen.width - this.minSize.x) / 2,
				(Screen.height - this.minSize.y) / 2,
				380,
				200
			);
		}

		protected void OnEnable()
		{
			this.LastError = null;
			this.ThanksMessage = null;
			this.Description = null;
			this.Type = IssueType.Bug;
			this.Attachments = new HashSet<string>();
			this.attachmentsToAdd = new HashSet<string>();
			this.attachmentsToRemove = new HashSet<string>();

			var editorLogPath = string.Empty;
			var editorPrevLogPath = string.Empty;
			var charonLogPath = Path.Combine(GameDataEditorWindow.ToolShadowCopyPath, "charon.log");

#if UNITY_EDITOR_WIN
			editorLogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Unity\Editor\Editor.log");
			editorPrevLogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Unity\Editor\Editor-prev.log");
#elif UNITY_EDITOR_OSX
			editorLogPath = Path.GetFullPath("~/Library/Logs/Unity/Editor.log");
			editorLogPath = Path.GetFullPath("~/Library/Logs/Unity/Editor-prev.log");
#else
#endif
			if (!string.IsNullOrEmpty(editorLogPath) && File.Exists(editorLogPath))
				this.attachmentsToAdd.Add(editorLogPath);

			if (!string.IsNullOrEmpty(editorPrevLogPath) && File.Exists(editorPrevLogPath))
				this.attachmentsToAdd.Add(editorPrevLogPath);

			if (!string.IsNullOrEmpty(charonLogPath) && File.Exists(charonLogPath))
				this.attachmentsToAdd.Add(charonLogPath);
		}

		protected void OnGUI()
		{
			GUILayout.Space(5);
			if (string.IsNullOrEmpty(this.ThanksMessage))
			{
				GUI.enabled = this.reportCoroutine == null || this.reportCoroutine.IsCompleted;
				this.Reporter = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_WINDOWREPORTERLABEL, this.Reporter);
				this.Type = (IssueType)EditorGUILayout.EnumPopup(Resources.UI_UNITYPLUGIN_WINDOWTYPELABEL, this.Type);
				this.Description = EditorGUILayout.TextArea(this.Description, GUILayout.Height(120));

				GUILayout.Space(5);

				if (this.Attachments == null) this.Attachments = new HashSet<string>();
				if (this.attachmentsToAdd == null) this.attachmentsToAdd = new HashSet<string>();
				if (this.attachmentsToRemove == null) this.attachmentsToRemove = new HashSet<string>();

				foreach (var attachmentFile in this.Attachments)
				{
					var fileInfo = new FileInfo(attachmentFile);
					if (GUILayout.Toggle(true, string.Format(Resources.UI_UNITYPLUGIN_WINDOWATTACHFILECHECKBOX, fileInfo.Name, fileInfo.Length / 1024.0 / 1024.0)) == false)
						this.attachmentsToRemove.Add(attachmentFile);
				}

				GUILayout.BeginHorizontal();
				EditorGUILayout.Space();
				if (GUILayout.Button(Resources.UI_UNITYPLUGIN_ATTACHFILEBUTTON, EditorStyles.toolbarButton, GUILayout.Width(70), GUILayout.Height(18)))
				{
					var file = EditorUtility.OpenFilePanel(Resources.UI_UNITYPLUGIN_SELECTFILETOATTACHTITLE, "", "");
					if (!string.IsNullOrEmpty(file) && File.Exists(file))
						this.attachmentsToAdd.Add(Path.GetFullPath(file));
				}
				GUILayout.Space(5);
				GUILayout.EndHorizontal();

				GUILayout.Space(5);
				if (!string.IsNullOrEmpty(this.LastError))
					GUILayout.Box(this.LastError);

				GUILayout.Space(18);
				GUILayout.BeginHorizontal();
				EditorGUILayout.Space();
				GUI.enabled = !string.IsNullOrEmpty(this.Reporter) && !string.IsNullOrEmpty(this.Description) && (this.reportCoroutine == null || this.reportCoroutine.IsCompleted);
				if (GUILayout.Button(Resources.UI_UNITYPLUGIN_REPORTBUTTON, GUILayout.Width(80)))
				{
					this.reportCoroutine = this.ReportIssue(this.Reporter, this.Description, this.Type, this.Attachments).ContinueWith(p =>
					{
						if (p.HasErrors)
						{
							this.LastError = p.Error.Unwrap().Message;
						}
						else
						{
							this.LastError = null;
							this.Description = null;
							this.Type = IssueType.Bug;
							this.Attachments = null;
							this.attachmentsToAdd = null;
							this.attachmentsToRemove = null;

							this.ThanksMessage = Resources.UI_UNITYPLUGIN_WINDOWREPORTTHANKSMESSAGE;
							Promise.Delayed(TimeSpan.FromSeconds(3)).ContinueWith(_ => this.Close());
						}
						this.Repaint();
					});
				}
				GUI.enabled = true;
				GUILayout.EndHorizontal();
			}
			else
			{
				GUILayout.Box(this.ThanksMessage, new GUIStyle { fontSize = 24, alignment = TextAnchor.MiddleCenter });
			}

			GUILayoutUtility.GetRect(1, 1, 1, 1);
			if (Event.current.type == EventType.repaint && GUILayoutUtility.GetLastRect().y > 0)
			{
				var newRect = GUILayoutUtility.GetLastRect();
				this.position = new Rect(this.position.position, new Vector2(this.position.width, newRect.y + 7));
				this.minSize = this.maxSize = this.position.size;
			}
		}

		private Promise ReportIssue(string reporter, string description, IssueType type, HashSet<string> attachments)
		{
			return CoroutineScheduler.Schedule<bool>(ReportIssueAsync(reporter, description, type, attachments));
		}
		private IEnumerable ReportIssueAsync(string reporter, string description, IssueType type, HashSet<string> attachments)
		{
			var errorOutput = new StringBuilder();
			var arguments = new List<string>
			{
				"SERVER", "REPORTISSUE",
				"--reporter", reporter,
				"--description", description,
				"--type", type.ToString()
			};
			if (attachments != null && attachments.Count > 0)
			{
				arguments.Add("--attachments");
				foreach (var attachment in attachments)
					arguments.Add(attachment);
			}
			if (Settings.Current.Verbose)
				arguments.Add("--verbose");

			var reportIssue = new ExecuteCommandTask(
				Settings.Current.ToolsPath,
				null,
				(s, ea) => { lock (errorOutput) errorOutput.Append(ea.Data ?? ""); },
				arguments.ToArray()
			);

			reportIssue.StartInfo.EnvironmentVariables["CHARON_APP_DATA"] = Path.GetFullPath("./Library/Charon");
			if (string.IsNullOrEmpty(Settings.Current.LicenseServerAddress) == false)
				reportIssue.StartInfo.EnvironmentVariables["CHARON_LICENSE_SERVER"] = Settings.Current.LicenseServerAddress;
			reportIssue.RequireDotNetRuntime();
			reportIssue.Start();

			yield return reportIssue.IgnoreFault();

			if (reportIssue.ExitCode != 0)
				throw new InvalidOperationException("Failed to report issue: " + (errorOutput.Length > 0 ? errorOutput.ToString() : "An unknown error occured. Please report this issue directly to developer."));
		}

		protected void Update()
		{
			if (this.Attachments == null) this.Attachments = new HashSet<string>();

			if (this.attachmentsToRemove != null && this.attachmentsToRemove.Count > 0)
			{
				this.Attachments.RemoveWhere(this.attachmentsToRemove.Contains);
				this.attachmentsToRemove.Clear();
			}
			if (this.attachmentsToAdd != null && this.attachmentsToAdd.Count > 0)
			{
				foreach (var toAdd in this.attachmentsToAdd)
					this.Attachments.Add(toAdd);
				this.attachmentsToAdd.Clear();
			}
		}
	}
}
