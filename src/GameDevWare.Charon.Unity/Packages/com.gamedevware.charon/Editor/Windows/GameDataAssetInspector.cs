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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
		private static readonly KeyValuePair<string, string>[] AllLanguagesWithId = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
			.OrderBy(c => c.Name).Select(c => new KeyValuePair<string, string>($"{{{c.Name}}}", $"[{c.Name}] {c.EnglishName}")).ToArray();

		[NonSerialized]
		private GameDataBase lastGameDataAsset;
		[NonSerialized]
		private UnityObject gameDataFile;
		[NonSerialized]
		private GameDataSettings gameDataSettings;
		[NonSerialized]
		private Task reimportAndGenerateCodeTask;
		[NonSerialized]
		private Task syncTask;
		[NonSerialized]
		private string lastServerAddress;
		[NonSerialized]
		private string lastGameDataFileRevisionHash;
		[NonSerialized]
		private DateTime lastGameDataFileRevisionHashCheckTime;
		[SerializeField]
		private bool codeGenerationFold = true;
		[SerializeField]
		private bool connectionFold = true;
		[SerializeField]
		private bool publicationFold = true;
		[SerializeField]
		private bool publicationLanguagesFold = true;
		[SerializeField]
		private Vector2 scrollPosition;

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

			GUI.enabled = true;

			this.codeGenerationFold = EditorGUILayout.BeginFoldoutHeaderGroup(this.codeGenerationFold, Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_GENERATION_LABEL);
			if (this.codeGenerationFold)
			{
				this.OnCodeGenerationSettingsGUI(gameDataAsset);
			}
			EditorGUILayout.EndFoldoutHeaderGroup();

			this.publicationFold = EditorGUILayout.BeginFoldoutHeaderGroup(this.publicationFold, Resources.UI_UNITYPLUGIN_INSPECTOR_ASSET_IMPORT_SETTINGS_LABEL);
			if (this.publicationFold)
			{
				this.OnAssetImportSettingsGUI(gameDataAsset);
			}
			EditorGUILayout.EndFoldoutHeaderGroup();


			this.connectionFold = EditorGUILayout.BeginFoldoutHeaderGroup(this.connectionFold, this.lastServerAddress);
			if (this.connectionFold)
			{
				this.OnConnectionGUI(gameDataAsset);
			}
			EditorGUILayout.EndFoldoutHeaderGroup();

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

			var hasDoneImport = this.reimportAndGenerateCodeTask != null && this.reimportAndGenerateCodeTask.IsCompleted;
			var hasRunningImport = this.reimportAndGenerateCodeTask != null && !this.reimportAndGenerateCodeTask.IsCompleted;
			var importStatus = (hasDoneImport ? " " + Resources.UI_UNITYPLUGIN_INSPECTOR_OPERATION_DONE :
				hasRunningImport ? " " + Resources.UI_UNITYPLUGIN_INSPECTOR_OPERATION_RUNNING : "");

			if (GUILayout.Button(Resources.UI_UNITYPLUGIN_INSPECTOR_SYNCHRONIZE_BUTTON + importStatus))
			{
				var progressCallback = ProgressUtils.ShowProgressBar(Resources.UI_UNITYPLUGIN_PROGRESS_IMPORTING);
				this.reimportAndGenerateCodeTask = ReimportAssetsRoutine.ScheduleAsync(new[] { AssetDatabase.GetAssetPath(gameDataAsset) }, progressCallback, CancellationToken.None);
				this.reimportAndGenerateCodeTask.ContinueWithHideProgressBar();
				this.reimportAndGenerateCodeTask.LogFaultAsError();
			}

			EditorGUILayout.EndHorizontal();

			GUI.enabled = true;

			if (GUI.changed)
			{
				EditorUtility.SetDirty(this.target);
				AssetDatabase.SaveAssetIfDirty(this.target);
			}
		}
		private void OnConnectionGUI(GameDataBase gameDataAsset)
		{
			EditorGUI.indentLevel++;

			if (gameDataAsset.settings.IsConnected)
			{
				EditorGUILayout.LabelField(Resources.UI_UNITYPLUGIN_INSPECTOR_PROJECT_LABEL,
					gameDataAsset.settings.projectName ?? gameDataAsset.settings.projectId);
				EditorGUILayout.LabelField(Resources.UI_UNITYPLUGIN_INSPECTOR_BRANCH_LABEL, gameDataAsset.settings.branchName ?? gameDataAsset.settings.branchId);

				EditorGUILayout.Space();

				EditorGUILayout.BeginHorizontal();

				if (GUILayout.Button(Resources.UI_UNITYPLUGIN_INSPECTOR_DISCONNECT_BUTTON))
				{
					gameDataAsset.settings.serverAddress = null;
					gameDataAsset.settings.branchId = null;
					gameDataAsset.settings.branchName = null;
					gameDataAsset.settings.projectId = null;
					gameDataAsset.settings.projectName = null;
					this.lastServerAddress = Resources.UI_UNITYPLUGIN_INSPECTOR_NOT_CONNECTED_LABEL;

					GUI.changed = true;
				}

				if (GUILayout.Button(Resources.UI_UNITYPLUGIN_INSPECTOR_SET_API_KEY_BUTTON))
				{
					ApiKeyPromptWindow.ShowAsync(gameDataAsset.settings.projectId, gameDataAsset.settings.projectName);
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

			EditorGUI.indentLevel--;
		}
		private void OnCodeGenerationSettingsGUI(GameDataBase gameDataAsset)
		{
			EditorGUI.indentLevel++;
			GUI.enabled = !CharonEditorModule.Instance.Routines.IsRunning && !EditorApplication.isCompiling;

			var folderAsset = !string.IsNullOrEmpty(gameDataAsset.settings.codeGenerationPath) && Directory.Exists(gameDataAsset.settings.codeGenerationPath) ?
				AssetDatabase.LoadAssetAtPath<DefaultAsset>(gameDataAsset.settings.codeGenerationPath) : null;

			if (folderAsset != null)
			{
				gameDataAsset.settings.codeGenerationPath = AssetDatabase.GetAssetPath(
					EditorGUILayout.ObjectField(Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_GENERATION_PATH, folderAsset, typeof(DefaultAsset), false));
			}
			else
			{
				gameDataAsset.settings.codeGenerationPath = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_GENERATION_PATH,
					gameDataAsset.settings.codeGenerationPath);
			}

			gameDataAsset.settings.gameDataNamespace = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_NAMESPACE,
				gameDataAsset.settings.gameDataNamespace);
			gameDataAsset.settings.gameDataClassName = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_GAMEDATA_CLASS_NAME,
				gameDataAsset.settings.gameDataClassName);
			gameDataAsset.settings.gameDataDocumentClassName = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_DOCUMENT_CLASS_NAME,
				gameDataAsset.settings.gameDataDocumentClassName);
			gameDataAsset.settings.defineConstants = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_DEFINE_CONSTANTS,
				gameDataAsset.settings.defineConstants);
			gameDataAsset.settings.lineEnding = (int)(SourceCodeLineEndings)EditorGUILayout.EnumPopup(
				Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_LINE_ENDINGS, (SourceCodeLineEndings)gameDataAsset.settings.lineEnding);
			gameDataAsset.settings.indentation = (int)(SourceCodeIndentation)EditorGUILayout.EnumPopup(
				Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_INDENTATION, (SourceCodeIndentation)gameDataAsset.settings.indentation);
			gameDataAsset.settings.optimizations = (int)(SourceCodeGenerationOptimizations)EditorGUILayout.EnumFlagsField(
				Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_OPTIMIZATIONS, (SourceCodeGenerationOptimizations)gameDataAsset.settings.optimizations);
			gameDataAsset.settings.clearOutputDirectory = EditorGUILayout.Toggle(Resources.UI_UNITYPLUGIN_INSPECTOR_CLEAR_OUTPUT_DIRECTORY,
				gameDataAsset.settings.clearOutputDirectory);
			gameDataAsset.settings.splitSourceCodeFiles = EditorGUILayout.Toggle(Resources.UI_UNITYPLUGIN_INSPECTOR_SPLIT_FILES,
				gameDataAsset.settings.splitSourceCodeFiles);

			GUI.enabled = true;
			EditorGUI.indentLevel--;
		}
		private void OnAssetImportSettingsGUI(GameDataBase gameDataAsset)
		{
			EditorGUI.indentLevel++;

			gameDataAsset.settings.publishFormat = Convert.ToInt32(EditorGUILayout.EnumPopup(Resources.UI_UNITYPLUGIN_INSPECTOR_PUBLICATION_FORMAT_LABEL,
				(GameDataFormat)gameDataAsset.settings.publishFormat));

			var publishLanguages = gameDataAsset.settings.publishLanguages ?? Array.Empty<string>();
			this.publicationLanguagesFold = EditorGUI.Foldout(EditorGUILayout.GetControlRect(), this.publicationLanguagesFold, Resources.UI_UNITYPLUGIN_INSPECTOR_PUBLICATION_LANGUAGES_LABEL + $" [{publishLanguages.Length}]", toggleOnLabelClick: true);
			if (!this.publicationLanguagesFold)
			{
				EditorGUI.indentLevel--;
				return;
			}

			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.Space(20);

				this.scrollPosition = GUILayout.BeginScrollView(this.scrollPosition, GUI.skin.box, GUILayout.Height(120));
				{
					foreach (var languageWithId in AllLanguagesWithId)
					{
						var hasThisLanguage = publishLanguages.Contains(languageWithId.Key, StringComparer.OrdinalIgnoreCase);
						var languageToggled = GUILayout.Toggle(hasThisLanguage, languageWithId.Value);
						if (!hasThisLanguage && languageToggled)
						{
							gameDataAsset.settings.publishLanguages = new List<string>(publishLanguages) { languageWithId.Key }.ToArray();
						}
						else if (hasThisLanguage && !languageToggled)
						{
							gameDataAsset.settings.publishLanguages = new List<string>(publishLanguages.Where(languageId => !string.Equals(languageId,
								languageWithId.Key, StringComparison.OrdinalIgnoreCase))).ToArray();
						}
					}
				}
				GUILayout.EndScrollView();
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			{
				EditorGUILayout.Space();
				if (GUILayout.Button(Resources.UI_UNITYPLUGIN_INSPECTOR_RESET_BUTTON, GUILayout.Height(20), GUILayout.Width(50)))
				{
					gameDataAsset.settings.publishLanguages = Array.Empty<string>();
				}
			}
			EditorGUILayout.EndHorizontal();

			EditorGUI.indentLevel--;

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
	}
}
