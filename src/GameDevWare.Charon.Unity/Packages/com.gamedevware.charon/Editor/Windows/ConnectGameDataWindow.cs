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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GameDevWare.Charon.Editor.Services.ServerApi;
using GameDevWare.Charon.Editor.Utils;
using UnityEditor;
using UnityEngine;

// ReSharper disable UnusedMember.Local
namespace GameDevWare.Charon.Editor.Windows
{
	internal class ConnectGameDataWindow : EditorWindow, ISerializationCallbackReceiver
	{
		private static readonly Rect Padding = new Rect(10, 10, 10, 10);
		private static readonly int NonEmptyProjectThreshold = 10 * 1024; // 10 KiB

		[NonSerialized] private string lastError;
		[NonSerialized]	private string progressStatus;
		[NonSerialized] private Task projectsFetchTask;
		[NonSerialized] private Task cloneTask;
		[NonSerialized] private ServerApiClient serverApiClient;
		[NonSerialized] private ILogger logger;

		[SerializeField]
		private bool autoClose;
		[SerializeField]
		private string apiKey;
		[SerializeField]
		private int selectedProjectIndex;
		[SerializeField]
		private Project[] projects;
		[SerializeField]
		private string[] projectNames;
		[SerializeField]
		private int selectedBranchIndex;
		[SerializeField]
		private Branch[] branches;
		[SerializeField]
		private string[] branchNames;
		[SerializeField]
		private GameDataFormat storeFormat;
		[SerializeField]
		private bool upload;
		[SerializeField]
		private bool advancedFold;
		[SerializeField]
		private GameDataBase gameDataAsset;

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
		private string ProjectName =>
			this.projectNames != null &&
			this.selectedProjectIndex >= 0 &&
			this.selectedProjectIndex < this.projectNames.Length ? this.projectNames[this.selectedProjectIndex] : null;
		private int SelectedBranchSize =>
			this.branches != null &&
			this.selectedBranchIndex >= 0 &&
			this.selectedBranchIndex < this.branches.Length ? this.branches[this.selectedBranchIndex].DataSize : 0;

		public ConnectGameDataWindow()
		{
			this.titleContent = new GUIContent(Resources.UI_UNITYPLUGIN_CONNECT_WINDOW_TITLE);
			this.minSize = new Vector2(480, 400);
			this.position = new Rect(
				(Screen.width - this.minSize.x) / 2,
				(Screen.height - this.minSize.y) / 2,
				this.minSize.x,
				this.minSize.y
			);
			this.apiKey = string.Empty;
			this.projects = Array.Empty<Project>();
			this.projectNames = Array.Empty<string>();
			this.selectedProjectIndex = -1;
			this.branches = Array.Empty<Branch>();
			this.branchNames = Array.Empty<string>();
			this.selectedBranchIndex = -1;
			this.storeFormat = GameDataFormat.Json;
			this.lastError = string.Empty;
			this.progressStatus = string.Empty;
			this.logger = CharonEditorModule.Instance.Logger;
		}

		private void OnEnabled()
		{
			if (string.IsNullOrEmpty(this.apiKey) == false)
			{
				this.ClearProjectList();
				this.projectsFetchTask = this.FetchProjectListAsync(this.apiKey);
			}
		}

		protected void OnGUI()
		{
			var someOperationPending =
				(this.projectsFetchTask != null && !this.projectsFetchTask.IsCompleted) ||
				(this.cloneTask != null && !this.cloneTask.IsCompleted);

			EditorLayoutUtils.BeginPaddings(this.position.size, Padding);

			GUI.enabled = !someOperationPending;

			if (this.gameDataAsset == null)
			{
				EditorGUILayout.HelpBox(Resources.UI_UNITYPLUGIN_CONNECT_MISSING_OBJECT_WARNING, MessageType.Warning);
				return;
			}

			// API Key
			EditorGUILayout.BeginVertical();
			{
				GUILayout.Label(Resources.UI_UNITYPLUGIN_GENERATE_API_KEY_TITLE, new GUIStyle(EditorStyles.boldLabel));
				var newApiKey = (EditorGUILayout.TextArea(this.apiKey, new GUIStyle(EditorStyles.textArea) { fixedHeight = 38 }) ?? string.Empty).Trim();
				if (GUILayout.Button(Resources.UI_UNITYPLUGIN_GENERATE_API_KEY_MESSAGE,
						new GUIStyle(EditorStyles.label)))
				{
					EditorUtility.OpenWithDefaultApp(this.ServerApiClient.GetApiKeysUrl().OriginalString);
				}

				if (string.Equals(this.apiKey, newApiKey, StringComparison.OrdinalIgnoreCase) == false)
				{
					if (string.IsNullOrEmpty(newApiKey))
					{
						this.ClearProjectList();
					}
					else
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

			GUILayout.Space(5);
			this.HorizontalLine(1);
			GUILayout.Space(5);

			// Project
			EditorGUILayout.BeginHorizontal();
			{
				var newSelectedProjectIndex = EditorGUILayout.Popup(Resources.UI_UNITYPLUGIN_GENERATE_PROJECT_LABEL, this.selectedProjectIndex, this.projectNames,
					new GUIStyle(EditorStyles.popup));
				if (newSelectedProjectIndex != this.selectedProjectIndex)
				{
					this.selectedProjectIndex = newSelectedProjectIndex;
					this.UpdateBranchList();
				}

				GUILayout.Space(5);
			}
			EditorGUILayout.EndHorizontal();

			// Branch
			EditorGUILayout.BeginHorizontal();
			{
				this.selectedBranchIndex = EditorGUILayout.Popup(Resources.UI_UNITYPLUGIN_GENERATE_BRANCH_LABEL, this.selectedBranchIndex, this.branchNames,
					new GUIStyle(EditorStyles.popup));
				GUILayout.Space(5);
			}
			EditorGUILayout.EndHorizontal();

			GUI.enabled = true;

			var gameDataPath = AssetDatabase.GUIDToAssetPath(this.gameDataAsset.settings.gameDataFileGuid);
			var assetSize = 0L;
			EditorGUILayout.BeginHorizontal();
			{
				var gameDataFileInfo = new FileInfo(gameDataPath);
				if (gameDataFileInfo.Exists)
				{
					assetSize = gameDataFileInfo.Length;
				}

				EditorGUILayout.LabelField(Resources.UI_UNITYPLUGIN_GENERATE_TARGET_PATH_LABEL, gameDataPath, EditorStyles.boldLabel);
				GUILayout.Space(5);
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(5);

			if (assetSize > 0 && this.projects != null && this.projects.Length > 0)
			{
				GUI.enabled = this.SelectedBranchSize < NonEmptyProjectThreshold;
				EditorGUILayout.BeginHorizontal();
				var originalWidth = EditorGUIUtility.labelWidth;
				EditorGUIUtility.labelWidth = this.position.width - (Padding.width + Padding.x) - 30;
				this.upload = EditorGUILayout.Toggle(string.Format(Resources.UI_UNITYPLUGIN_GENERATE_UPLOAD_LOCAL_GAME_DATA, assetSize / 1024.0), this.upload);
				EditorGUIUtility.labelWidth = originalWidth;
				EditorGUILayout.EndHorizontal();
				GUILayout.Space(5);
				GUI.enabled = true;

				if (!this.upload)
				{
					EditorGUILayout.HelpBox(string.Format(Resources.UI_UNITYPLUGIN_GENERATE_LOCAL_ERASED_WARNING, Path.GetFileName(gameDataPath)), MessageType.Warning);
				}
			}

			GUILayout.Space(5);
			this.HorizontalLine(1);
			GUILayout.Space(5);

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(this.progressStatus, EditorStyles.label, GUILayout.MaxWidth(Math.Max(this.position.width - 60, 60)));
			EditorGUILayout.Space();

			GUI.enabled = this.selectedProjectIndex >= 0 &&
				this.selectedProjectIndex >= 0 &&
				!string.IsNullOrEmpty(gameDataPath) &&
				!someOperationPending;

			var syncButtonText = this.upload ? Resources.UI_UNITYPLUGIN_GENERATE_UPLOAD_BUTTON :
				Resources.UI_UNITYPLUGIN_GENERATE_DOWNLOAD_BUTTON;
			if (GUILayout.Button(syncButtonText, GUI.skin.button, GUILayout.Width(80), GUILayout.Height(25)))
			{
				this.cloneTask = this.CloneProjectAsync();

				this.Repaint();
			}

			GUILayout.Space(5);
			EditorGUILayout.EndHorizontal();

			GUI.enabled = true;

			EditorLayoutUtils.EndPaddings();

			EditorLayoutUtils.AutoSize(this);
		}

		private void HorizontalLine(int height = 1)
		{
			var rect = EditorGUILayout.GetControlRect(false, height);

			rect.height = height;

			EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
		}

		private void ClearProjectList()
		{
			this.projects = Array.Empty<Project>();
			this.projectNames = Array.Empty<string>();
			this.upload = false;
			this.selectedProjectIndex = -1;
			this.branches = Array.Empty<Branch>();
			this.branchNames = Array.Empty<string>();
			this.selectedBranchIndex = -1;
		}
		private async Task FetchProjectListAsync(string apiKey)
		{
			if (apiKey == null) throw new ArgumentNullException(nameof(apiKey));

			this.lastError = string.Empty;
			this.projects = Array.Empty<Project>();
			this.projectNames = Array.Empty<string>();
			this.upload = false;
			this.selectedProjectIndex = -1;
			this.branches = Array.Empty<Branch>();
			this.branchNames = Array.Empty<string>();
			this.selectedBranchIndex = -1;

			var updateProjectsTask = this.UpdateProjectListAsync(apiKey);
			await updateProjectsTask.IgnoreFault().ConfigureAwait(true);

			this.Repaint();

			if (updateProjectsTask.IsFaulted)
			{
				this.lastError = updateProjectsTask.Exception.Unwrap().Message;
				this.logger.Log(LogType.Error, updateProjectsTask.Exception.Unwrap());
			}
		}
		private async Task UpdateProjectListAsync(string apiKey)
		{
			if (apiKey == null) throw new ArgumentNullException(nameof(apiKey));

			this.ServerApiClient.UseApiKey(apiKey);

			this.projects = await this.ServerApiClient.GetMyProjectsAsync().ConfigureAwait(true);
			this.projectNames = this.projects.Select(p => p.Name).ToArray();
			this.selectedProjectIndex = this.projects.Length > 0 ? 0 : -1;

			this.UpdateBranchList();

			this.Repaint();
		}

		private async Task CloneProjectAsync()
		{
			var gameDataAsset = this.gameDataAsset;
			var gameDataPath = AssetDatabase.GUIDToAssetPath(gameDataAsset.settings.gameDataFileGuid);

			// de-select updated asset to prevent inspector from overwriting settings
			if (Selection.activeObject != null &&
				string.Equals(AssetDatabase.GetAssetPath(Selection.activeObject), gameDataPath))
			{
				Selection.activeObject = null;
			}

			var branch = this.branches[this.selectedBranchIndex];
			var project = this.projects[this.selectedProjectIndex];
			var storeFormat = this.storeFormat;

			var apiKeyPath = new Uri(this.ServerApiClient.BaseAddress, "/" + project.Id);
			CharonEditorModule.Instance.KeyCryptoStorage.StoreKey(apiKeyPath, this.apiKey);
			this.ServerApiClient.UseApiKey(this.apiKey);

			if (this.upload)
			{
				await this.ServerApiClient.UploadDataSourceAsync(branch.Id, storeFormat, Path.GetFullPath(gameDataPath),
					this.GetProgressReport().ToUploadProgress(Path.GetFileName(gameDataPath))).ConfigureAwait(true);
			}

			var downloadPath = gameDataPath + ".tmp";
			await this.ServerApiClient.DownloadDataSourceAsync(branch.Id, storeFormat, Path.GetFullPath(downloadPath),
				this.GetProgressReport().ToDownloadProgress(Path.GetFileName(downloadPath))).ConfigureAwait(true);

			CharonFileUtils.SafeFileDelete(gameDataPath);
			File.Move(downloadPath, gameDataPath);

			this.RaiseDone();

			AssetDatabase.Refresh();

			var gameDataSettings = gameDataAsset.settings;
			gameDataSettings.serverAddress = this.ServerApiClient.BaseAddress.OriginalString;
			gameDataSettings.projectId = project.Id;
			gameDataSettings.projectName = project.Name;
			gameDataSettings.branchName = branch.Name;
			gameDataSettings.branchId = branch.Id;

			EditorUtility.SetDirty(this.gameDataAsset);
			AssetDatabase.SaveAssetIfDirty(this.gameDataAsset);

			this.progressStatus = string.Empty;

			EditorUtility.FocusProjectWindow();

			Selection.activeObject = this.gameDataAsset;
			EditorGUIUtility.PingObject(this.gameDataAsset);
			ProjectWindowUtil.ShowCreatedAsset(this.gameDataAsset);

			this.Repaint();
		}

		private void UpdateBranchList()
		{
			if (this.selectedProjectIndex < 0 || this.selectedProjectIndex >= this.projects.Length)
			{
				return;
			}

			var project = this.projects[this.selectedProjectIndex];

			this.branches = project.Branches;
			this.branchNames = project.Branches.Select(b => b.Name).ToArray();

			// select primary branch
			this.selectedBranchIndex = Array.FindIndex(project.Branches, b => b.IsPrimary);

			// or select first available branch
			if (this.selectedBranchIndex < 0 && this.branchNames.Length > 0)
			{
				this.selectedBranchIndex = 0;
			}
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

		public static Task ShowAsync(GameDataBase gameDataBase, bool autoClose = true)
		{
			if (gameDataBase == null) throw new ArgumentNullException(nameof(gameDataBase));

			var taskCompletionSource = new TaskCompletionSource<object>();
			var window = GetWindow<ConnectGameDataWindow>(utility: true);

			window.Done += (_, _) => taskCompletionSource.TrySetResult(null);
			window.Cancel += (_, args) => taskCompletionSource.TrySetException(args.GetException());

			window.autoClose = autoClose;

			// ReSharper disable once SuspiciousTypeConversion.Global
			window.gameDataAsset = gameDataBase;
			window.storeFormat = FormatsExtensions.GetGameDataFormatForExtension(AssetDatabase.GUIDToAssetPath(gameDataBase.settings.gameDataFileGuid)) ?? GameDataFormat.Json;

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
