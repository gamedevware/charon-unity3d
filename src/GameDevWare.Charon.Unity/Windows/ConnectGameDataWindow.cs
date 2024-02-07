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
using UnityEngine.Serialization;
using Coroutine = GameDevWare.Charon.Unity.Async.Coroutine;

// ReSharper disable UnusedMember.Local
namespace GameDevWare.Charon.Unity.Windows
{
	internal class ConnectGameDataWindow : EditorWindow, ISerializationCallbackReceiver
	{
		private static readonly Rect Padding = new Rect(10, 10, 10, 10);
		private static readonly int NonEmptyProjectThreshold = 10 * 1024; // 10 KiB
		
		[NonSerialized]
		private string lastError;
		[NonSerialized]
		private string progressStatus;
		[NonSerialized]
		private Promise projectsFetchTask;
		[NonSerialized]
		private Promise cloneTask;
		[NonSerialized]
		private ServerApiClient serverApiClient;

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
		private GameDataStoreFormat storeFormat;
		[SerializeField]
		private string folder;
		[SerializeField]
		private bool upload;
		[SerializeField]
		private string fileName;
		[SerializeField]
		private bool advancedFold;
		[SerializeField]
		private string originalAsset;

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
		private string ProjectName
		{
			get
			{
				return this.projectNames != null &&
					this.selectedProjectIndex >= 0 &&
					this.selectedProjectIndex < this.projectNames.Length ? this.projectNames[this.selectedProjectIndex] : null;
			}
		}
		private int SelectedBranchSize
		{
			get
			{
				return this.branches != null &&
					this.selectedBranchIndex >= 0 &&
					this.selectedBranchIndex < this.branches.Length ? this.branches[this.selectedBranchIndex].DataSize : 0;
			}
		}
		
		public ConnectGameDataWindow()
		{
			this.titleContent = new GUIContent("Connect Game Data");
			this.minSize = new Vector2(480, 400);
			this.position = new Rect(
				(Screen.width - this.maxSize.x) / 2,
				(Screen.height - this.maxSize.y) / 2,
				this.maxSize.x,
				this.maxSize.y
			);
			this.apiKey = string.Empty;
			this.projects = new Project[0];
			this.projectNames = new string[0];
			this.selectedProjectIndex = -1;
			this.branches = new Branch[0];
			this.branchNames = new string[0];
			this.selectedBranchIndex = -1;
			this.storeFormat = GameDataStoreFormat.Json;
			this.folder = "Assets";
			this.fileName = "GameData";
			this.lastError = string.Empty;
			this.progressStatus = string.Empty;
		}

		[SuppressMessage("ReSharper", "InconsistentNaming"), UsedImplicitly]
		private void OnEnabled()
		{
			if (string.IsNullOrEmpty(this.apiKey) == false)
			{
				this.ClearProjectList();
				this.projectsFetchTask = this.FetchProjectList(this.apiKey);
			}
		}

		[SuppressMessage("ReSharper", "InconsistentNaming"), UsedImplicitly]
		protected void OnGUI()
		{
			var someOperationPending =
				(this.projectsFetchTask != null && !this.projectsFetchTask.IsCompleted) ||
				(this.cloneTask != null && !this.cloneTask.IsCompleted);

			EditorLayoutUtils.BeginPaddings(this.position.size, Padding);

			GUI.enabled = !someOperationPending;

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

			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(10);
			EditorGUILayout.BeginVertical();
			{
				this.advancedFold = EditorGUILayout.Foldout(this.advancedFold, Resources.UI_UNITYPLUGIN_GENERATE_ADVANCED_OPTIONS_LABEL);
				if (this.advancedFold)
				{
					// Branch
					EditorGUILayout.BeginHorizontal();
					{
						this.selectedBranchIndex = EditorGUILayout.Popup(Resources.UI_UNITYPLUGIN_GENERATE_BRANCH_LABEL, this.selectedBranchIndex, this.branchNames,
							new GUIStyle(EditorStyles.popup));
						GUILayout.Space(5);
					}
					EditorGUILayout.EndHorizontal();

					// Format
					EditorGUILayout.BeginHorizontal();
					{
						this.storeFormat = (GameDataStoreFormat)EditorGUILayout.EnumPopup(Resources.UI_UNITYPLUGIN_GENERATE_FORMAT_LABEL, this.storeFormat);
						GUILayout.Space(5);
					}
					EditorGUILayout.EndHorizontal();

					// Folder
					EditorGUILayout.BeginHorizontal();
					{
						var folderAsset = !string.IsNullOrEmpty(this.folder) && Directory.Exists(this.folder) ?
							AssetDatabase.LoadAssetAtPath<DefaultAsset>(this.folder) : null;
						this.folder = AssetDatabase.GetAssetPath(EditorGUILayout.ObjectField(Resources.UI_UNITYPLUGIN_GENERATE_FOLDER_LABEL, folderAsset,
							typeof(DefaultAsset), false));
						GUILayout.Space(5);
					}
					EditorGUILayout.EndHorizontal();

					// Name
					EditorGUILayout.BeginHorizontal();
					{
						this.fileName = EditorGUILayout.TextField("Name", this.fileName);
						GUILayout.Space(5);
					}
					EditorGUILayout.EndHorizontal();
				}
			}
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();

			GUI.enabled = true;

			var assetPath = "";
			var assetSize = 0L;
			EditorGUILayout.BeginHorizontal();
			{
				if (string.IsNullOrEmpty(this.folder) == false &&
					string.IsNullOrEmpty(this.fileName) == false)
				{
					var extension = StorageFormats.GetStoreFormatExtension(this.storeFormat);

					assetPath = Path.Combine(this.folder, this.fileName + extension).Replace('\\', '/');
				}

				var fileInfo = new FileInfo(assetPath);
				if (fileInfo.Exists)
				{
					assetSize = fileInfo.Length;
				}
				
				EditorGUILayout.LabelField(Resources.UI_UNITYPLUGIN_GENERATE_TARGET_PATH_LABEL, assetPath, EditorStyles.boldLabel);
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
					EditorGUILayout.HelpBox(string.Format(Resources.UI_UNITYPLUGIN_GENERATE_LOCAL_ERASED_WARNING, Path.GetFileName(assetPath)), MessageType.Warning);
				}
			}

			GUILayout.Space(5);
			this.HorizontalLine(1);
			GUILayout.Space(5);
			
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(this.progressStatus, EditorStyles.label, GUILayout.MaxWidth(360));
			EditorGUILayout.Space();

			GUI.enabled = this.selectedProjectIndex >= 0 &&
				this.selectedProjectIndex >= 0 &&
				!string.IsNullOrEmpty(assetPath) &&
				!someOperationPending;

			var syncButtonText = this.upload ? Resources.UI_UNITYPLUGIN_GENERATE_UPLOAD_BUTTON :
				Resources.UI_UNITYPLUGIN_GENERATE_DOWNLOAD_BUTTON;
			if (GUILayout.Button(syncButtonText, GUI.skin.button, GUILayout.Width(80), GUILayout.Height(25)))
			{
				var downloadPath = Path.GetFullPath(assetPath);
				this.cloneTask = new Coroutine(this.CloneProjectAsync(assetPath, downloadPath));

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
			this.projects = new Project[0];
			this.projectNames = new string[0];
			this.upload = false;
			this.selectedProjectIndex = -1;
			this.branches = new Branch[0];
			this.branchNames = new string[0];
			this.selectedBranchIndex = -1;
		}
		private Promise FetchProjectList(string apiKey)
		{
			if (apiKey == null) throw new ArgumentNullException("apiKey");

			this.lastError = string.Empty;
			this.projects = new Project[0];
			this.projectNames = new string[0];
			this.upload = false;
			this.selectedProjectIndex = -1;
			this.branches = new Branch[0];
			this.branchNames = new string[0];
			this.selectedBranchIndex = -1;
			
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

			this.projects = getMyProjectsAsync.GetResult();
			this.projectNames = this.projects.Select(p => p.Name).ToArray();
			this.selectedProjectIndex = this.projects.Length > 0 ? 0 : -1;

			this.UpdateBranchList();

			this.Repaint();
		}

		private IEnumerable CloneProjectAsync(string assetPath, string downloadPath)
		{
			if (assetPath == null) throw new ArgumentNullException("assetPath");
			if (downloadPath == null) throw new ArgumentNullException("downloadPath");

			// de-select updated asset to prevent inspector from overwriting settings
			if (Selection.activeObject != null &&
				string.Equals(AssetDatabase.GetAssetPath(Selection.activeObject), assetPath))
			{
				Selection.activeObject = null;
			}

			// this is case when extensions between original and new asset doesn't match
			if (string.IsNullOrEmpty(this.originalAsset) == false)
			{
				AssetDatabase.MoveAsset(this.originalAsset, assetPath);
				this.originalAsset = null;
			}

			var branch = this.branches[this.selectedBranchIndex];
			var project = this.projects[this.selectedProjectIndex];
			var storeFormat = this.storeFormat;

			var apiKeyPath = new Uri(this.ServerApiClient.BaseAddress, "/" + project.Id);
			KeyCryptoStorage.StoreKey(apiKeyPath, this.apiKey);
			this.ServerApiClient.UseApiKey(this.apiKey);

			if (this.upload)
			{
				var uploadDataSourceAsync = this.ServerApiClient.UploadDataSourceAsync(branch.Id, storeFormat, Path.GetFullPath(assetPath),
					this.GetProgressReport().ToUploadProgress(Path.GetFileName(assetPath)));
				yield return uploadDataSourceAsync;
			}
			
			var downloadDataSourceAsync = this.ServerApiClient.DownloadDataSourceAsync(branch.Id, storeFormat, Path.GetFullPath(downloadPath),
				this.GetProgressReport().ToDownloadProgress(Path.GetFileName(downloadPath)));
			yield return downloadDataSourceAsync;

			this.RaiseDone();

			AssetDatabase.Refresh();

			var gameDataAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
			if (gameDataAsset == null)
			{
				throw new InvalidOperationException(string.Format("Unable to load game data asset at '{0}'.", assetPath));
			}

			var gameDataSettings = GameDataSettings.Load(gameDataAsset);
			gameDataSettings.ServerAddress = this.ServerApiClient.BaseAddress.OriginalString;
			gameDataSettings.ProjectId = project.Id;
			gameDataSettings.ProjectName = project.Name;
			gameDataSettings.BranchName = branch.Name;
			gameDataSettings.BranchId = branch.Id;
			gameDataSettings.AutoSynchronization = true;
			gameDataSettings.Save(assetPath);

			this.progressStatus = string.Empty;

			EditorUtility.FocusProjectWindow();

			Selection.activeObject = gameDataAsset;
			EditorGUIUtility.PingObject(gameDataAsset);
			ProjectWindowUtil.ShowCreatedAsset(gameDataAsset);

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

		public static Promise ShowAsync(string folder, string name, bool autoClose = true)
		{
			var promise = new Promise();
			var window = GetWindow<ConnectGameDataWindow>(utility: true);

			window.Done += (sender, args) => promise.TrySetCompleted();
			window.Cancel += (sender, args) => promise.TrySetFailed(args.GetException());

			window.autoClose = autoClose;

			if (string.IsNullOrEmpty(name) == false &&
				string.IsNullOrEmpty(folder) == false)
			{
				window.folder = folder;
				window.fileName = name;
				window.originalAsset = Path.Combine(folder, name);
				window.storeFormat = StorageFormats.GetStoreFormat(name) ?? GameDataStoreFormat.Json;
			}

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