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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GameDevWare.Charon.Editor.Cli;
using GameDevWare.Charon.Editor.Routines;
using GameDevWare.Charon.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace GameDevWare.Charon.Editor.Windows
{
	[CustomEditor(typeof(GameDataBase), editorForChildClasses: true)]
	internal class GameDataAssetInspector : UnityEditor.Editor
	{
		private static readonly Regex RevisionHashRegex = new Regex("\"RevisionHash\"\\s*:\\s*\"([a-fA-F0-9\\-]+)\"");

		[NonSerialized]
		private GameDataBase lastGameDataAsset;
		[NonSerialized]
		private UnityObject gameDataFile;
		[NonSerialized]
		private GameDataSettings gameDataSettings;
		[NonSerialized]
		private Task generateCodeTask;
		[NonSerialized]
		private Task syncTask;
		[NonSerialized]
		private string lastServerAddress;
		[NonSerialized]
		private string lastGameDataFileRevisionHash;
		[NonSerialized]
		private DateTime lastGameDataFileRevisionHashCheckTime;
		[SerializeField]
		private bool codeGenerationFold;
		[SerializeField]
		private bool connectionFold;

		/// <inheritdoc />

		// ReSharper disable once FunctionComplexityOverflow
		public override void OnInspectorGUI()
		{
			this.DrawDefaultInspector();

			var gameDataAsset = this.target as GameDataBase;
			if (gameDataAsset == null)
			{
				return;
			}

			var gameDataAssetPath = CharonFileUtils.GetProjectRelativePath(AssetDatabase.GetAssetPath(this.target));
			if (this.lastGameDataAsset != gameDataAsset || this.gameDataSettings == null)
			{
				this.gameDataSettings = gameDataAsset.settings;
				this.gameDataFile = AssetDatabase.LoadAssetAtPath<UnityObject>(AssetDatabase.GUIDToAssetPath(this.gameDataSettings.gameDataFileGuid) ?? "");
				this.lastServerAddress = string.IsNullOrEmpty(this.gameDataSettings.serverAddress) ? Resources.UI_UNITYPLUGIN_INSPECTOR_NOT_CONNECTED_LABEL :
					string.Concat(Resources.UI_UNITYPLUGIN_INSPECTOR_CONNECTION_LABEL, ": ",
						new Uri(this.gameDataSettings.serverAddress).GetComponents(UriComponents.HostAndPort, UriFormat.SafeUnescaped));
				this.lastGameDataAsset = gameDataAsset;
			}

			GUI.enabled = this.gameDataFile == null;
			var newGameDataFile = EditorGUILayout.ObjectField(Resources.UI_UNITYPLUGIN_INSPECTOR_GAME_DATA_FILE, this.gameDataFile, typeof(UnityObject), false);
			if (newGameDataFile != null && newGameDataFile != this.gameDataFile)
			{
				this.gameDataSettings.gameDataFileGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(newGameDataFile));
				this.gameDataFile = newGameDataFile;
			}

			GUI.enabled = true;

			if (this.gameDataFile == null)
			{
				EditorGUILayout.HelpBox(Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_MISSING_GAMEDATA_FILE, MessageType.Error);
			}

			this.RecheckGamedataHash();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(Resources.UI_UNITYPLUGIN_INSPECTOR_REVISION_HASH_LABEL, gameDataAsset.RevisionHash);
			if (!string.Equals(this.lastGameDataFileRevisionHash, gameDataAsset.RevisionHash, StringComparison.OrdinalIgnoreCase))
			{
				GUILayout.Label("\u26a0\ufe0f", GUILayout.Width(20));
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.LabelField(Resources.UI_UNITYPLUGIN_INSPECTOR_GAME_DATA_VERSION_LABEL, gameDataAsset.GameDataVersion);

			this.codeGenerationFold = EditorGUILayout.Foldout(this.codeGenerationFold, Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_GENERATION_LABEL);
			if (this.codeGenerationFold)
			{
				GUI.enabled = !CharonEditorModule.Instance.Routines.IsRunning && !EditorApplication.isCompiling;

				var folderAsset = !string.IsNullOrEmpty(this.gameDataSettings.codeGenerationPath) && Directory.Exists(this.gameDataSettings.codeGenerationPath) ?
					AssetDatabase.LoadAssetAtPath<DefaultAsset>(this.gameDataSettings.codeGenerationPath) : null;

				if (folderAsset != null)
				{
					this.gameDataSettings.codeGenerationPath = AssetDatabase.GetAssetPath(
						EditorGUILayout.ObjectField(Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_GENERATION_PATH, folderAsset, typeof(DefaultAsset), false));
				}
				else
				{
					this.gameDataSettings.codeGenerationPath = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_GENERATION_PATH,
						this.gameDataSettings.codeGenerationPath);
				}

				this.gameDataSettings.gameDataNamespace = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_NAMESPACE,
					this.gameDataSettings.gameDataNamespace);
				this.gameDataSettings.gameDataClassName = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_GAMEDATA_CLASS_NAME,
					this.gameDataSettings.gameDataClassName);
				this.gameDataSettings.gameDataDocumentClassName = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_DOCUMENT_CLASS_NAME,
					this.gameDataSettings.gameDataDocumentClassName);
				this.gameDataSettings.defineConstants = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_DEFINE_CONSTANTS,
					this.gameDataSettings.defineConstants);
				this.gameDataSettings.lineEnding = (int)(SourceCodeLineEndings)EditorGUILayout.EnumPopup(
					Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_LINE_ENDINGS, (SourceCodeLineEndings)this.gameDataSettings.lineEnding);
				this.gameDataSettings.indentation = (int)(SourceCodeIndentation)EditorGUILayout.EnumPopup(
					Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_INDENTATION, (SourceCodeIndentation)this.gameDataSettings.indentation);
				this.gameDataSettings.optimizations = (int)(SourceCodeGenerationOptimizations)EditorGUILayout.EnumFlagsField(
					Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_OPTIMIZATIONS, (SourceCodeGenerationOptimizations)this.gameDataSettings.optimizations);
				this.gameDataSettings.clearOutputDirectory = EditorGUILayout.Toggle(Resources.UI_UNITYPLUGIN_INSPECTOR_CLEAR_OUTPUT_DIRECTORY,
					this.gameDataSettings.clearOutputDirectory);
				this.gameDataSettings.splitSourceCodeFiles = EditorGUILayout.Toggle(Resources.UI_UNITYPLUGIN_INSPECTOR_SPLIT_FILES,
					this.gameDataSettings.splitSourceCodeFiles);

				GUI.enabled = true;
			}

			GUI.enabled = !CharonEditorModule.Instance.Routines.IsRunning &&
				!EditorApplication.isCompiling;

			EditorGUILayout.Space();

			this.connectionFold = EditorGUILayout.Foldout(this.connectionFold, this.lastServerAddress);
			if (this.connectionFold)
			{
				if (this.gameDataSettings.IsConnected)
				{
					EditorGUILayout.LabelField(Resources.UI_UNITYPLUGIN_INSPECTOR_PROJECT_LABEL,
						this.gameDataSettings.projectName ?? this.gameDataSettings.projectId);
					EditorGUILayout.LabelField(Resources.UI_UNITYPLUGIN_INSPECTOR_BRANCH_LABEL, this.gameDataSettings.branchName ?? this.gameDataSettings.branchId);

					EditorGUILayout.Space();

					EditorGUILayout.BeginHorizontal();

					if (GUILayout.Button(Resources.UI_UNITYPLUGIN_INSPECTOR_DISCONNECT_BUTTON))
					{
						this.gameDataSettings.serverAddress = null;
						this.gameDataSettings.branchId = null;
						this.gameDataSettings.branchName = null;
						this.gameDataSettings.projectId = null;
						this.gameDataSettings.projectName = null;
						this.lastServerAddress = Resources.UI_UNITYPLUGIN_INSPECTOR_NOT_CONNECTED_LABEL;

						GUI.changed = true;
					}

					if (GUILayout.Button(Resources.UI_UNITYPLUGIN_INSPECTOR_SET_API_KEY_BUTTON))
					{
						ApiKeyPromptWindow.ShowAsync(this.gameDataSettings.projectId, this.gameDataSettings.projectName);
					}

					EditorGUILayout.EndHorizontal();
				}
				else
				{
					EditorGUILayout.HelpBox(Resources.UI_UNITYPLUGIN_INSPECTOR_NOT_CONNECTED, MessageType.Info);
					EditorGUILayout.Space();

					if (GUILayout.Button(Resources.UI_UNITYPLUGIN_INSPECTOR_CONNECT_BUTTON))
					{
						ConnectGameDataWindow.ShowAsync(
							gameDataBase: gameDataAsset,
							autoClose: true
						);
					}
				}
			}

			EditorGUILayout.Space();
			GUILayout.Label(Resources.UI_UNITYPLUGIN_INSPECTOR_ACTIONS_GROUP, EditorStyles.boldLabel);

			if (EditorApplication.isCompiling)
				EditorGUILayout.HelpBox(Resources.UI_UNITYPLUGIN_COMPILING_WARNING, MessageType.Warning);
			else if (CharonEditorModule.Instance.Routines.IsRunning)
				EditorGUILayout.HelpBox(Resources.UI_UNITYPLUGIN_COROUTINE_IS_RUNNING_WARNING, MessageType.Warning);

			EditorGUILayout.BeginHorizontal();
			GUI.enabled = !CharonEditorModule.Instance.Routines.IsRunning && !EditorApplication.isCompiling;
			if (GUILayout.Button(Resources.UI_UNITYPLUGIN_INSPECTOR_EDIT_BUTTON))
			{
				AssetDatabase.OpenAsset(this.target);
				this.Repaint();
			}

			GUI.enabled = !CharonEditorModule.Instance.Routines.IsRunning && !EditorApplication.isCompiling;

			var hasDoneImport = this.generateCodeTask != null && this.generateCodeTask.IsCompleted;
			var hasRunningImport = this.generateCodeTask != null && !this.generateCodeTask.IsCompleted;
			var importStatus = (hasDoneImport ? " " + Resources.UI_UNITYPLUGIN_INSPECTOR_OPERATION_DONE :
				hasRunningImport ? " " + Resources.UI_UNITYPLUGIN_INSPECTOR_OPERATION_RUNNING : "");

			if (GUILayout.Button(Resources.UI_UNITYPLUGIN_INSPECTOR_SYNCHRONIZE_BUTTON + importStatus))
			{
				var progressCallback = ProgressUtils.ShowProgressBar(Resources.UI_UNITYPLUGIN_PROGRESS_IMPORTING);
				this.generateCodeTask = CharonEditorModule.Instance.Routines.Schedule(() => RunImportAsync(gameDataAsset, progressCallback), CancellationToken.None);
				this.generateCodeTask.ContinueWithHideProgressBar();
				this.generateCodeTask.LogFaultAsError();
			}

			EditorGUILayout.EndHorizontal();

			GUI.enabled = true;

			if (GUI.changed)
			{
				EditorUtility.SetDirty(this.target);
				AssetDatabase.SaveAssetIfDirty(this.target);
			}
		}

		private void RecheckGamedataHash()
		{
			if (this.lastGameDataFileRevisionHash != null &&
				this.lastGameDataFileRevisionHashCheckTime != default &&
				DateTime.UtcNow - this.lastGameDataFileRevisionHashCheckTime < TimeSpan.FromSeconds(5))
			{
				return; // recently updated
			}

			this.lastGameDataFileRevisionHash = new string('0', 24);
			this.lastGameDataFileRevisionHashCheckTime = DateTime.UtcNow;

			try
			{
				if (this.gameDataFile == null)
				{
					return;
				}

				var gameDataPath = AssetDatabase.GetAssetPath(this.gameDataFile);
				if (!File.Exists(gameDataPath))
				{
					return;
				}

				// read first 8 KiB to the buffer
				var buffer = new byte[1024 * 8];
				var offset = 0;
				var read = 0;
				using var gameDataFileStream = File.OpenRead(gameDataPath);
				while ((read = gameDataFileStream.Read(buffer, offset, buffer.Length - offset)) > 0)
				{
					offset += read;
				}

				// clear non-ASCII characters
				for (var i = 0; i < buffer.Length; i++)
				{
					if (buffer[i] > 127) buffer[i] = (byte)'_';
				}

				var text = Encoding.ASCII.GetString(buffer, 0, offset);
				var match = RevisionHashRegex.Match(text);
				if (match.Success)
				{
					this.lastGameDataFileRevisionHash = match.Groups[1].Value;
				}
			}
			catch
			{
				/* ignore hash compute errors */
			}
		}

		public static async Task RunImportAsync(UnityObject gameDataFile, Action<string, float> progressCallback)
		{
			if (gameDataFile == null) throw new ArgumentNullException(nameof(gameDataFile));

			var gameDataAsset = GameDataAssetUtils.GetAssociatedGameDataAsset(gameDataFile);
			var gameDataPath = AssetDatabase.GetAssetPath(gameDataFile);
			var gameDataFileGuid = AssetDatabase.AssetPathToGUID(gameDataPath);

			var gameDataAssetPath = string.Empty;
			if (gameDataAsset == null) // crate new asset and code
			{
				progressCallback?.Invoke(Resources.UI_UNITYPLUGIN_CREATING_GAMEDATA_ASSET, 0.05f);
				gameDataAssetPath = CreateNewGameDataAsset(gameDataFile, gameDataPath, gameDataFileGuid);
			}
			else
			{
				gameDataAssetPath = AssetDatabase.GetAssetPath(gameDataAsset);
			}

			CharonEditorModule.Instance.AssetImporter.ImportOnStart(gameDataAssetPath);

			progressCallback?.Invoke(Resources.UI_UNITYPLUGIN_GENERATING_SOURCE_CODE, 0.30f);

			await GenerateCodeRoutine.RunAsync(
				paths: new[] { gameDataAssetPath },
				progressCallback: progressCallback?.Sub(0.30f, 1.00f),
				cancellationToken: CancellationToken.None
			).ConfigureAwait(true);
		}
		private static string CreateNewGameDataAsset(UnityObject gameDataFile, string gameDataPath, string gameDataFileGuid)
		{
			if (!CreateGameDataWindow.ValidateCreationOptions(gameDataFile, Path.GetFileNameWithoutExtension(gameDataPath), out var errorMessage))
			{
				throw new InvalidOperationException(errorMessage);
			}

			var newGameDataAssetPath = CharonFileUtils.GetProjectRelativePath(Path.Combine(Path.GetDirectoryName(gameDataPath) ?? "",
				Path.GetFileNameWithoutExtension(gameDataPath) + ".asset"));
			var gameDataAsset = CreateInstance<GameDataBase>();
			gameDataAsset.settings = GameDataSettingsUtils.CreateDefault(gameDataPath, gameDataFileGuid);
			AssetDatabase.CreateAsset(gameDataAsset, newGameDataAssetPath);
			return newGameDataAssetPath;
		}
	}
}
