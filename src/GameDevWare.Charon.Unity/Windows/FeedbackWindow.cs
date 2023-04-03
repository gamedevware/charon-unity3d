/*
	Copyright (c) 2017 Denis Zykov

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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using GameDevWare.Charon.Unity.Async;
using GameDevWare.Charon.Unity.Utils;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.Charon.Unity.Windows
{
	internal class FeedbackWindow : EditorWindow
	{
		public static readonly string EditorLogPath = string.Empty;
		public static readonly string EditorPrevLogPath = string.Empty;

		public string Name;
		public string Email;
		public string Description;
		public IssueType Type;
		public HashSet<string> Attachments;
		public string LastError;
		public string ThanksMessage;

		private HashSet<string> attachmentsToRemove;
		private HashSet<string> attachmentsToAdd;
		private Promise reportCoroutine;

		static FeedbackWindow()
		{
			if (RuntimeInformation.IsWindows)
			{
				EditorLogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Unity\Editor\Editor.log");
				EditorPrevLogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Unity\Editor\Editor-prev.log");
			}
			else if (RuntimeInformation.IsOsx)
			{
				EditorLogPath = Path.GetFullPath("~/Library/Logs/Unity/Editor.log");
				EditorPrevLogPath = Path.GetFullPath("~/Library/Logs/Unity/Editor-prev.log");
			}
		}
		public FeedbackWindow()
		{
			this.titleContent = new GUIContent(Resources.UI_UNITYPLUGIN_FEEDBACK_WINDOW_TITLE);
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

			if (!string.IsNullOrEmpty(EditorLogPath) && File.Exists(EditorLogPath))
				this.attachmentsToAdd.Add(EditorLogPath);

			if (!string.IsNullOrEmpty(EditorPrevLogPath) && File.Exists(EditorPrevLogPath))
				this.attachmentsToAdd.Add(EditorPrevLogPath);

			var charonLogs = GetCharonLogFilesSortedByCreationTime();
			for (var i = 0; i < charonLogs.Length && i < 3; i++)
			{
				this.attachmentsToAdd.Add(charonLogs[i]);
			}
		}

		[SuppressMessage("ReSharper", "InconsistentNaming")]
		protected void OnGUI()
		{
			GUILayout.Space(5);
			if (string.IsNullOrEmpty(this.ThanksMessage))
			{
				GUI.enabled = this.reportCoroutine == null || this.reportCoroutine.IsCompleted;
				this.Name = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_FEEDBACK_NAME_LABEL, this.Name);
				this.Email = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_FEEDBACK_EMAIL_LABEL, this.Email);
				this.Type = (IssueType)EditorGUILayout.EnumPopup(Resources.UI_UNITYPLUGIN_FEEDBACK_TYPE_LABEL, this.Type);
				this.Description = EditorGUILayout.TextArea(this.Description, GUILayout.Height(120));

				GUILayout.Space(5);

				if (this.Attachments == null) this.Attachments = new HashSet<string>();
				if (this.attachmentsToAdd == null) this.attachmentsToAdd = new HashSet<string>();
				if (this.attachmentsToRemove == null) this.attachmentsToRemove = new HashSet<string>();

				foreach (var attachmentFile in this.Attachments)
				{
					var fileInfo = new FileInfo(attachmentFile);
					if (GUILayout.Toggle(true, string.Format(Resources.UI_UNITYPLUGIN_FEEDBACK_ATTACH_FILE_CHECKBOX, fileInfo.Name, fileInfo.Length / 1024.0 / 1024.0)) == false)
						this.attachmentsToRemove.Add(attachmentFile);
				}

				GUILayout.BeginHorizontal();
				EditorGUILayout.Space();
				if (GUILayout.Button(Resources.UI_UNITYPLUGIN_FEEDBACK_ATTACH_FILE_BUTTON, EditorStyles.toolbarButton, GUILayout.Width(70), GUILayout.Height(18)))
				{
					var file = EditorUtility.OpenFilePanel(Resources.UI_UNITYPLUGIN_SELECT_FILE_TO_ATTACH_TITLE, "", "");
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

				var reporter = string.Empty;
				if (string.IsNullOrEmpty(this.Email) == false && string.IsNullOrEmpty(this.Name) == false)
					reporter = "<" + this.Name + ">" + this.Email;
				else if (string.IsNullOrEmpty(this.Email) == false)
					reporter = this.Email;
				else
					reporter = this.Name;

				GUI.enabled = !string.IsNullOrEmpty(reporter) && !string.IsNullOrEmpty(this.Description) && (this.reportCoroutine == null || this.reportCoroutine.IsCompleted);
				if (GUILayout.Button(Resources.UI_UNITYPLUGIN_FEEDBACK_SEND_BUTTON, GUILayout.Width(80)))
				{

					this.reportCoroutine = this.ReportIssue(reporter, this.Description, this.Type, this.Attachments).ContinueWith(p =>
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

							this.ThanksMessage = Resources.UI_UNITYPLUGIN_FEEDBACK_THANKS_MESSAGE;
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
			if (Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().y > 0)
			{
				var newRect = GUILayoutUtility.GetLastRect();
				this.position = new Rect(this.position.position, new Vector2(this.position.width, newRect.y + 7));
				this.minSize = this.maxSize = this.position.size;
			}
		}

		private Promise ReportIssue(string reporter, string description, IssueType type, HashSet<string> attachments)
		{
			return CoroutineScheduler.Schedule<bool>(this.ReportIssueAsync(reporter, description, type, attachments), "ui::ReportIssue");
		}
		private IEnumerable ReportIssueAsync(string reporter, string description, IssueType type, HashSet<string> attachments)
		{
			if (reporter == null) throw new ArgumentNullException("reporter");
			if (description == null) throw new ArgumentNullException("description");

			if (attachments == null) attachments = new HashSet<string>();

			var reportIssue = CharonCli.ReportIssueAsync(reporter, type, description, attachments.ToArray());
			yield return reportIssue.IgnoreFault();

			var errorOutput = reportIssue.HasErrors ? reportIssue.Error.Message : reportIssue.GetResult().GetErrorData();
			if (string.IsNullOrEmpty(errorOutput) == false)
				throw new InvalidOperationException("Failed to report issue: " + (errorOutput.Length > 0 ? errorOutput : "An unknown error occurred. Please report this issue directly to developer."));
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

		public static string[] GetCharonLogFilesSortedByCreationTime()
		{
			if (string.IsNullOrEmpty(CharonCli.CharonLogsDirectory) || !Directory.Exists(CharonCli.CharonLogsDirectory))
			{
				return new string[0];
			}

			var logFiles = Directory.GetFiles(CharonCli.CharonLogsDirectory);
			Array.Sort(logFiles, (x, y) => File.GetLastWriteTimeUtc(y).CompareTo(File.GetLastWriteTimeUtc(x)));
			return logFiles;
		}
	}
}
