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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GameDevWare.Charon.Editor.ServerApi;
using GameDevWare.Charon.Editor.Utils;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

// ReSharper disable Unity.RedundantSerializeFieldAttribute
namespace GameDevWare.Charon.Editor.Windows
{
	internal class ApiKeyPromptWindow : EditorWindow, ISerializationCallbackReceiver
	{
		private static readonly Rect Padding = new Rect(10, 10, 10, 10);

		[NonSerialized] private string lastError;
		[NonSerialized] private string progressStatus;
		[NonSerialized] private Task projectsFetchTask;
		[NonSerialized] private ServerApiClient serverApiClient;
		[NonSerialized] private readonly ILogger logger;

		[SerializeField] private bool autoClose;
		[SerializeField] private string apiKey;
		[SerializeField] private bool? projectFound;
		[SerializeField] private string expectedProjectId;
		[SerializeField] private string expectedProjectName;

		private event EventHandler Done;
		private event EventHandler<ErrorEventArgs> Cancel;

		private ServerApiClient ServerApiClient
		{
			get
			{
				if (this.serverApiClient == null)
				{
					this.serverApiClient = new ServerApiClient(CharonEditorModule.Instance.Settings.GetServerAddressUrl());
				}
				return this.serverApiClient;
			}
		}

		public ApiKeyPromptWindow()
		{
			this.titleContent = new GUIContent("API Key Prompt");
			this.minSize = new Vector2(480, 230);
			this.position = new Rect(
				(Screen.width - this.maxSize.x) / 2,
				(Screen.height - this.maxSize.y) / 2,
				this.maxSize.x,
				this.maxSize.y
			);
			this.apiKey = string.Empty;
			this.projectFound = null;
			this.expectedProjectId = string.Empty;
			this.expectedProjectName = string.Empty;
			this.lastError = string.Empty;
			this.progressStatus = string.Empty;
			this.logger = CharonEditorModule.Instance.Logger;
		}

		[SuppressMessage("ReSharper", "InconsistentNaming"), UsedImplicitly]
		private void OnEnabled()
		{
			if (string.IsNullOrEmpty(this.apiKey) == false)
			{
				this.projectsFetchTask = this.FetchProjectListAsync(this.apiKey);
			}
		}

		[SuppressMessage("ReSharper", "InconsistentNaming"), UsedImplicitly]
		protected void OnGUI()
		{
			var someOperationPending =
				(this.projectsFetchTask != null && !this.projectsFetchTask.IsCompleted);

			EditorLayoutUtils.BeginPaddings(this.position.size, Padding);

			GUI.enabled = !someOperationPending;

			// API Key
			EditorGUILayout.BeginVertical();
			{
				GUILayout.Label("API Key", new GUIStyle(EditorStyles.boldLabel));
				var newApiKey = (EditorGUILayout.TextArea(this.apiKey, new GUIStyle(EditorStyles.textArea) { fixedHeight = 38 }) ?? string.Empty).Trim();
				if (GUILayout.Button(
						$"To generate new API Key go to your <a href=\"{this.ServerApiClient.GetApiKeysUrl().OriginalString}\">Profile -> API Keys</a>.",
						new GUIStyle(EditorStyles.label) { richText = true }))
				{
					EditorUtility.OpenWithDefaultApp(this.ServerApiClient.GetApiKeysUrl().OriginalString);
				}

				if (string.Equals(this.apiKey, newApiKey, StringComparison.OrdinalIgnoreCase) == false)
				{
					if (string.IsNullOrEmpty(newApiKey) == false)
					{
						this.projectsFetchTask = this.FetchProjectListAsync(newApiKey);
					}
				}
				this.apiKey = newApiKey;

				if (!string.IsNullOrEmpty(this.lastError))
				{
					GUILayout.Space(5);
					EditorGUILayout.HelpBox(this.lastError, MessageType.Error);
				}
			}
			EditorGUILayout.EndVertical();

			if (string.IsNullOrEmpty(this.expectedProjectName) == false)
			{
				GUILayout.Space(5);
				this.HorizontalLine(1);
				GUILayout.Space(5);

				// Project Name
				EditorGUILayout.BeginHorizontal();
				{
					EditorGUILayout.LabelField("Project Name", this.expectedProjectName, new GUIStyle(EditorStyles.boldLabel));
					GUILayout.Space(5);
				}
				EditorGUILayout.EndHorizontal();
			}

			GUILayout.Space(5);
			this.HorizontalLine(1);
			GUILayout.Space(5);

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(this.progressStatus, EditorStyles.label);
			EditorGUILayout.Space();

			GUI.enabled = true;

			if (this.autoClose)
			{
				if (GUILayout.Button("Cancel", GUI.skin.button, GUILayout.Width(80), GUILayout.Height(25)))
				{
					this.RaiseCancel();
					this.Close();
				}
			}
			else
			{
				if (GUILayout.Button("Close", GUI.skin.button, GUILayout.Width(80), GUILayout.Height(25)))
				{
					this.RaiseCancel();
					this.Close();
				}
			}

			GUILayout.Space(5);
			EditorGUILayout.EndHorizontal();

			EditorLayoutUtils.EndPaddings();

			EditorLayoutUtils.AutoSize(this);
		}

		private void HorizontalLine(int height = 1)
		{
			var rect = EditorGUILayout.GetControlRect(false, height);

			rect.height = height;

			EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
		}
		private async Task FetchProjectListAsync(string apiKey)
		{
			if (apiKey == null) throw new ArgumentNullException(nameof(apiKey));


			this.lastError = string.Empty;
			this.projectFound = null;

			var updateTask = this.UpdateProjectListAsync(apiKey);
			await updateTask.IgnoreFault().ConfigureAwait(true);

			this.Repaint();

			if (updateTask.IsFaulted)
			{
				this.lastError = updateTask.Exception.Unwrap().Message;
				this.logger.Log(LogType.Error, updateTask.Exception.Unwrap());
			}
		}

		private async Task UpdateProjectListAsync(string apiKey)
		{
			if (apiKey == null) throw new ArgumentNullException(nameof(apiKey));

			this.ServerApiClient.UseApiKey(apiKey);

			var projects = await this.ServerApiClient.GetMyProjectsAsync().ConfigureAwait(true);

			if (string.IsNullOrEmpty(this.expectedProjectId))
			{
				this.projectFound = true;

				// save keys to all projects
				foreach (var project in projects)
				{
					var apiKeyPath = new Uri(this.ServerApiClient.BaseAddress, "/" + project.Id);
					CharonEditorModule.Instance.KeyCryptoStorage.StoreKey(apiKeyPath, apiKey);
				}
			}
			else
			{
				this.projectFound = projects.Any(p => string.Equals(p.Id, this.expectedProjectId));
				if (this.projectFound == false)
				{
					throw new InvalidOperationException($"API Key is valid but has no access to project '{this.expectedProjectName}'.");
				}

				if (this.projectFound.GetValueOrDefault())
				{
					var apiKeyPath = new Uri(this.ServerApiClient.BaseAddress, "/" + this.expectedProjectId);
					CharonEditorModule.Instance.KeyCryptoStorage.StoreKey(apiKeyPath, apiKey);
				}
			}


			this.progressStatus = Resources.UI_UNITYPLUGIN_PROGRESS_DONE;

			this.Repaint();

			await Task.Delay(1000).ConfigureAwait(true);

			this.RaiseDone();
		}

		private void RaiseDone()
		{
			if (this.Done != null)
				this.Done(this, EventArgs.Empty);
			this.Done = null;
			this.Cancel = null;

			if (!this.autoClose)
				return;

			this.Close();
		}
		private void RaiseCancel()
		{
			if (this.Cancel != null)
			{
				this.Cancel(this, new ErrorEventArgs(new InvalidOperationException(Resources.UI_UNITYPLUGIN_OPERATION_CANCELLED)));
			}
			this.Cancel = null;
			this.Done = null;
		}
		private void OnDestroy()
		{
			this.RaiseCancel();
		}

		private Action<string, float> GetProgressReport()
		{
			return (msg, progress) =>
			{
				this.progressStatus = $"{msg} ({progress * 100:F2}%)";
				this.Repaint();
			};
		}

		public static Task ShowAsync(string expectedProjectId, string expectedProjectName, bool autoClose = true)
		{
			var taskCompletionSource = new TaskCompletionSource<object>();
			var window = GetWindow<ApiKeyPromptWindow>(utility: true);

			window.Done += (_, _) => taskCompletionSource.TrySetResult(null);
			window.Cancel += (_, args) => taskCompletionSource.TrySetException(args.GetException());

			window.autoClose = autoClose;
			window.expectedProjectId = expectedProjectId;
			window.expectedProjectName = expectedProjectName;

			window.Focus();

			return taskCompletionSource.Task;
		}

		/// <inheritdoc />
		public void OnBeforeSerialize()
		{

		}
		/// <inheritdoc />
		public void OnAfterDeserialize()
		{
			if (string.IsNullOrEmpty(this.apiKey))
				return;

			this.projectsFetchTask = this.FetchProjectListAsync(this.apiKey);
		}
	}
}
