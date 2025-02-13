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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GameDevWare.Charon.Editor.Cli;
using GameDevWare.Charon.Editor.Routines;
using GameDevWare.Charon.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

// ReSharper disable UnusedMember.Local
namespace GameDevWare.Charon.Editor.Windows
{
	internal class CreateGameDataWindow : EditorWindow
	{
		private static readonly Rect Padding = new Rect(10, 10, 10, 10);

		[NonSerialized] private CancellationTokenSource closeSource;
		[NonSerialized] private string lastError;
		[NonSerialized] private string progressStatus;
		[NonSerialized] private  float progress;
		[NonSerialized] private Task gameDataCreationTask;
		[NonSerialized] private ILogger logger;

		[SerializeField] private bool autoClose;
		[SerializeField] private string gameDataName;
		[SerializeField] private GameDataFormat format;
		[SerializeField] private UnityObject folder;

		private event EventHandler Done;
		private event EventHandler<ErrorEventArgs> Cancel;

		public CreateGameDataWindow()
		{
			this.titleContent = new GUIContent(Resources.UI_UNITYPLUGIN_CREATE_GAMEDATA_WINDOW_TITLE);
			this.minSize = new Vector2(480, 400);
			this.maxSize = new Vector2(800, 600);
			this.position = new Rect(
				(Screen.width - this.maxSize.x),
				(Screen.height - this.maxSize.y) / 2,
				this.maxSize.x,
				this.maxSize.y
			);
			this.closeSource = new CancellationTokenSource();
			this.autoClose = true;
			this.gameDataName = "GameData";
			this.format = GameDataFormat.Json;
			this.lastError = string.Empty;
			this.progressStatus = string.Empty;
			this.logger = CharonEditorModule.Instance.Logger;
		}

		protected void OnGUI()
		{
			var someOperationPending = this.gameDataCreationTask is { IsCompleted: false };

			EditorLayoutUtils.BeginPaddings(this.position.size, Padding);

			GUI.enabled = !someOperationPending && !CharonEditorModule.Instance.Routines.IsRunning;

			//
			EditorGUILayout.BeginVertical();
			{
				this.gameDataName = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_CREATE_GAMEDATA_NAME_LABEL, this.gameDataName);
				this.format = (GameDataFormat)EditorGUILayout.EnumPopup(Resources.UI_UNITYPLUGIN_CREATE_GAMEDATA_FORMAT_LABEL, this.format);
				this.folder = EditorGUILayout.ObjectField(Resources.UI_UNITYPLUGIN_CREATE_GAMEDATA_FOLDER_LABEL, this.folder, typeof(UnityObject), allowSceneObjects: false);
			}
			EditorGUILayout.EndVertical();

			if (!string.IsNullOrEmpty(this.lastError))
			{
				EditorGUILayout.HelpBox(this.lastError, MessageType.Error);
			}

			GUI.enabled = this.folder != null && !string.IsNullOrEmpty(this.gameDataName) &&
				!someOperationPending && !CharonEditorModule.Instance.Routines.IsRunning;

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			if (GUILayout.Button(Resources.UI_UNITYPLUGIN_CREATE_GAMEDATA_CREATE_BUTTON, GUI.skin.button, GUILayout.Width(80), GUILayout.Height(25)))
			{
				this.lastError = null;
				this.progressStatus = null;

				if (ValidateCreationOptions(this.folder, this.gameDataName, out this.lastError))
				{
					this.gameDataCreationTask = CharonEditorModule.Instance.Routines.Schedule(this.CreateGameDataAsync, this.closeSource.Token);
					this.gameDataCreationTask.LogFaultAsError();
					this.gameDataCreationTask.ContinueWith(t =>
					{
						if (t.Exception.Unwrap() is not OperationCanceledException)
						{
							this.lastError = t.Exception.Unwrap()!.Message;
						}
						this.Repaint();
					}, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Current);
				}

				this.Repaint();
			}

			GUILayout.Space(5);
			EditorGUILayout.EndHorizontal();
			if (this.gameDataCreationTask is { IsCompleted: false } && !string.IsNullOrEmpty(this.progressStatus))
			{
				GUILayout.Label($"[{this.progress * 100:N0}%] {this.progressStatus}");
			}
			GUI.enabled = true;

			EditorLayoutUtils.EndPaddings();

			EditorLayoutUtils.AutoSize(this);
		}

		public static bool ValidateCreationOptions(UnityObject folder, string gameDataName, out string errorMessage)
		{
			var gameDataDirectory = GetBaseDirectory(folder);
			var isStreamingAssetsDirectory = gameDataDirectory.StartsWith(Path.GetFullPath(Path.Combine("Assets", "StreamingAssets")), StringComparison.OrdinalIgnoreCase);
			if (isStreamingAssetsDirectory)
			{
				errorMessage = Resources.UI_UNITYPLUGIN_CREATING_GAME_DATA_NO_STREAMING_ASSETS;
				return false;
			}

			if (!GameDataAssetUtils.IsValidName(gameDataName))
			{
				errorMessage = Resources.UI_UNITYPLUGIN_CREATING_GAME_DATA_INVALID_NAME;
				return false;
			}

			var collidedAssetPath = GameDataAssetUtils.FindNameCollision(gameDataName);
			if (collidedAssetPath != null)
			{
				errorMessage = string.Format(Resources.UI_UNITYPLUGIN_CREATING_GAME_DATA_IS_USED, collidedAssetPath);
			}

			errorMessage = null;
			return true;
		}

		private async Task CreateGameDataAsync()
		{
			var progressCallback = new Action<string, float>((message, progress) =>
			{
				this.progressStatus = message ?? this.progressStatus;
				this.progress = progress;
				this.Repaint();
			});

			EditorApplication.LockReloadAssemblies();
			try
			{
				this.closeSource.Token.ThrowIfCancellationRequested();
				this.closeSource.Token.ThrowIfScriptsCompiling();

				var gameDataDirectory = GetBaseDirectory(this.folder);

				progressCallback(Resources.UI_UNITYPLUGIN_CREATING_PROGRESS_INIT_GAMEDATA, 0.01f);

				var extension = this.format.GetExtensionFromGameDataFormat();
				var gameDataPath = Path.Combine(gameDataDirectory, this.gameDataName + extension);

				this.logger.Log(LogType.Assert, $"Initializing blank game data file at {gameDataPath}...");

				progressCallback(null, 0.05f);

				await CharonCli.InitGameDataAsync(Path.GetFullPath(gameDataPath), CharonEditorModule.Instance.Settings.LogLevel).ConfigureAwait(true);
				AssetDatabase.Refresh();

				this.closeSource.Token.ThrowIfCancellationRequested();

				var gameDataFile = AssetDatabase.LoadAssetAtPath<UnityObject>(gameDataPath);
				var gameDataFileGuid = AssetDatabase.AssetPathToGUID(gameDataPath, AssetPathToGUIDOptions.OnlyExistingAssets);
				if (gameDataFile == null || string.IsNullOrEmpty(gameDataFileGuid))
				{
					this.logger.Log(LogType.Error, $"Failed to get data file as asset at {gameDataPath}.");
					CharonFileUtils.SafeFileDelete(gameDataPath);
					return;
				}

				progressCallback(Resources.UI_UNITYPLUGIN_CREATING_GAMEDATA_ASSET, 0.30f);
				this.closeSource.Token.ThrowIfCancellationRequested();

				var gameDataAssetPath = CharonFileUtils.GetProjectRelativePath(Path.Combine(gameDataDirectory, Path.GetFileNameWithoutExtension(gameDataPath) + ".asset"));

				this.logger.Log(LogType.Assert, $"Creating blank game data asset at {gameDataPath}.");

				progressCallback(null, 0.40f);

				var gameDataAsset = CreateInstance<GameDataBase>();
				gameDataAsset.settings = GameDataSettingsUtils.CreateDefault(gameDataPath, gameDataFileGuid);
				AssetDatabase.CreateAsset(gameDataAsset, gameDataAssetPath);
				EditorGUIUtility.PingObject(gameDataAsset);

				progressCallback(null, 0.50f);
				this.closeSource.Token.ThrowIfCancellationRequested();

				CharonEditorModule.Instance.AssetImporter.ImportOnStart(gameDataAssetPath);

				this.logger.Log(LogType.Assert, $"Generating initial source for for game data asset at {gameDataAssetPath}.");

				progressCallback(Resources.UI_UNITYPLUGIN_GENERATING_SOURCE_CODE, 0.50f);

				await GenerateSourceCodeRoutine.RunAsync(
					paths: new[] { gameDataAssetPath },
					progressCallback: progressCallback.Sub(0.50f, 0.99f),
					cancellationToken: CancellationToken.None
				).ConfigureAwait(true);

				progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1.00f);

				this.logger.Log(LogType.Assert, $"Game data creation at '{gameDataPath}' is finished successfully. Scheduling compilation and after it " +
					$"import of game data file '{Path.GetFileName(gameDataPath)}' into game data asset '{Path.GetFileName(gameDataAssetPath)}'.");

				this.RaiseDone();
			}
			finally
			{
				EditorApplication.UnlockReloadAssemblies();
			}
		}
		private static string GetBaseDirectory(UnityObject folder)
		{
			var gameDataDirectory = "Assets";
			if (folder != null)
			{
				gameDataDirectory = AssetDatabase.GetAssetPath(folder);
				if (File.Exists(gameDataDirectory))
				{
					gameDataDirectory = Path.GetDirectoryName(gameDataDirectory) ?? gameDataDirectory;
				}
			}

			return gameDataDirectory;
		}

		private void RaiseDone()
		{
			if (this.Done != null)
				this.Done(this, EventArgs.Empty);
			this.Done = null;
			this.Cancel = null;
			this.closeSource.Cancel();

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
			this.closeSource.Cancel();
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

		public static Task ShowAsync(UnityObject folder, bool autoClose = true)
		{
			var taskCompletionSource = new TaskCompletionSource<object>();
			var window = GetWindow<CreateGameDataWindow>(utility: true);

			window.Done += (_, _) => taskCompletionSource.TrySetResult(null);
			window.Cancel += (_, args) => taskCompletionSource.TrySetException(args.GetException());

			window.autoClose = autoClose;
			window.folder = folder;
			window.Focus();

			return taskCompletionSource.Task;
		}
	}
}
