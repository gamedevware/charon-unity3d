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
using System.Threading;
using System.Threading.Tasks;
using Editor.GameDevWare.Charon.Cli;
using Editor.GameDevWare.Charon.Routines;
using Editor.GameDevWare.Charon.Utils;
using GameDevWare.Charon;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

// ReSharper disable UnusedMember.Local
namespace Editor.GameDevWare.Charon.Windows
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
			this.minSize = new Vector2(600, 400);
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

			EditorGUILayout.BeginHorizontal(GUI.skin.box);
			{
				EditorGUILayout.BeginVertical();
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.Space();
					GUILayout.Label(EditorGUIUtility.IconContent("cs Script Icon"));
					EditorGUILayout.Space();
					EditorGUILayout.EndHorizontal();

					GUILayout.Label(this.gameDataName + ".cs", GUILayout.MaxWidth((this.position.width - 60) / 4));
				}
				EditorGUILayout.EndVertical();
				GUILayout.FlexibleSpace();
				EditorGUILayout.BeginVertical();
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.Space();
					GUILayout.Label(EditorGUIUtility.IconContent("cs Script Icon"));
					EditorGUILayout.Space();
					EditorGUILayout.EndHorizontal();

					GUILayout.Label(this.gameDataName + "Asset.cs", GUILayout.MaxWidth((this.position.width - 60) / 4));
				}
				EditorGUILayout.EndVertical();
				GUILayout.FlexibleSpace();
				EditorGUILayout.BeginVertical();
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.Space();
					GUILayout.Label(EditorGUIUtility.IconContent("TextAsset Icon"));
					EditorGUILayout.Space();
					EditorGUILayout.EndHorizontal();

					GUILayout.Label(this.gameDataName + this.format.GetExtensionFromGameDataFormat(), GUILayout.MaxWidth((this.position.width - 60) / 4));
				}
				EditorGUILayout.EndVertical();
				GUILayout.FlexibleSpace();
				EditorGUILayout.BeginVertical();
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.Space();
					GUILayout.Label(EditorGUIUtility.IconContent("Prefab Icon"));
					EditorGUILayout.Space();
					EditorGUILayout.EndHorizontal();

					GUILayout.Label(this.gameDataName + ".asset", GUILayout.MaxWidth((this.position.width - 60) / 4));
				}
				EditorGUILayout.EndVertical();
			}
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(5);

			EditorGUILayout.BeginHorizontal();
			{
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
			}
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
