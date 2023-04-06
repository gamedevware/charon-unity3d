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
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using GameDevWare.Charon.Unity.Async;
using GameDevWare.Charon.Unity.ServerApi;
using GameDevWare.Charon.Unity.ServerApi.KeyStorage;
using GameDevWare.Charon.Unity.Utils;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

using Coroutine = GameDevWare.Charon.Unity.Async.Coroutine;

// ReSharper disable UnusedMember.Local
namespace GameDevWare.Charon.Unity.Windows
{
	internal class ApiKeyPromptWindow : EditorWindow, ISerializationCallbackReceiver
	{
		private static readonly Rect Padding = new Rect(10, 10, 10, 10);

		[NonSerialized] private string lastError;
		[NonSerialized] private string progressStatus;
		[NonSerialized] private Promise projectsFetchTask;
		[NonSerialized] private ServerApiClient serverApiClient;

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
					this.serverApiClient = new ServerApiClient(Settings.Current.GetServerAddressUrl());
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
		}

		[SuppressMessage("ReSharper", "InconsistentNaming"), UsedImplicitly]
		private void OnEnabled()
		{
			if (string.IsNullOrEmpty(this.apiKey) == false)
			{
				this.projectsFetchTask = this.FetchProjectList(this.apiKey);
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
				if (GUILayout.Button(string.Format("To generate new API Key go to your <a href=\"{0}\">Profile -> API Keys</a>.", this.ServerApiClient.GetApiKeysUrl().OriginalString),
						new GUIStyle(EditorStyles.label) { richText = true }))
				{
					EditorUtility.OpenWithDefaultApp(this.ServerApiClient.GetApiKeysUrl().OriginalString);
				}

				if (string.Equals(this.apiKey, newApiKey, StringComparison.OrdinalIgnoreCase) == false)
				{
					if (string.IsNullOrEmpty(newApiKey) == false)
					{
						this.projectsFetchTask = this.FetchProjectList(newApiKey);
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
			GUILayout.Label(this.progressStatus, EditorStyles.label, GUILayout.MaxWidth(360));
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
		private Promise FetchProjectList(string apiKey)
		{
			if (apiKey == null) throw new ArgumentNullException("apiKey");


			this.lastError = string.Empty;
			this.projectFound = null;

			return new Coroutine(this.UpdateProjectListAsync(apiKey)).ContinueWith(promise =>
			{
				this.Repaint();

				if (!promise.HasErrors) return;

				this.lastError = promise.Error.Unwrap().Message;
				Debug.LogWarning(promise.Error.Unwrap());
			});

		}
		private IEnumerable UpdateProjectListAsync(string apiKey)
		{
			if (apiKey == null) throw new ArgumentNullException("apiKey");

			this.ServerApiClient.UseApiKey(apiKey);

			var getMyProjectsAsync = this.ServerApiClient.GetMyProjectsAsync();
			yield return getMyProjectsAsync;

			var projects = getMyProjectsAsync.GetResult();

			if (string.IsNullOrEmpty(this.expectedProjectId))
			{
				this.projectFound = true;

				// save keys to all projects
				foreach (var project in projects)
				{
					var apiKeyPath = new Uri(this.ServerApiClient.BaseAddress, "/" + project.Id);
					KeyCryptoStorage.StoreKey(apiKeyPath, apiKey);
				}
			}
			else
			{
				this.projectFound = projects.Any(p => string.Equals(p.Id, this.expectedProjectId));
				if (this.projectFound == false)
				{
					throw new InvalidOperationException(string.Format("API Key is valid but has no access to project '{0}'.", this.expectedProjectName));
				}

				if (this.projectFound.GetValueOrDefault())
				{
					var apiKeyPath = new Uri(this.ServerApiClient.BaseAddress, "/" + this.expectedProjectId);
					KeyCryptoStorage.StoreKey(apiKeyPath, apiKey);
				}
			}

			this.RaiseDone();
			this.Repaint();
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
				if (Settings.Current.Verbose)
					Debug.Log(string.Format("'{0}' window is closed by user.", this.titleContent.text));
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
				this.progressStatus = string.Format("{0} ({1:F2}%)", msg, progress * 100);
				this.Repaint();
			};
		}

		public static Promise ShowAsync(string expectedProjectId, string expectedProjectName, bool autoClose = true)
		{
			var promise = new Promise();
			var window = GetWindow<ApiKeyPromptWindow>(utility: true);

			window.Done += (sender, args) => promise.TrySetCompleted();
			window.Cancel += (sender, args) => promise.TrySetFailed(args.GetException());

			window.autoClose = autoClose;
			window.expectedProjectId = expectedProjectId;
			window.expectedProjectName = expectedProjectName;

			window.Focus();

			if (Settings.Current.Verbose)
				Debug.Log(string.Format("Showing '{0}' window.", window.titleContent.text));

			return promise;
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

			this.projectsFetchTask = this.FetchProjectList(this.apiKey);
		}
	}
}
