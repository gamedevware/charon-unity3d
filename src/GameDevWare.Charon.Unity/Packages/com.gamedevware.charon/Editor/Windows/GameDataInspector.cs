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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GameDevWare.Charon.Unity.Async;
using GameDevWare.Charon.Unity.Routines;
using GameDevWare.Charon.Unity.Utils;
using UnityEditor;
using UnityEngine;

using UnityObject = UnityEngine.Object;

namespace GameDevWare.Charon.Unity.Windows
{
	[CustomEditor(typeof(GameDataAsset))]
	internal class GameDataInspector : Editor
	{
		private static UnityObject ActiveObject;
		private static bool SelectionChanged;

		private UnityObject lastAsset;
		private GameDataSettings gameDataSettings;
		private UnityObject newScriptingAssembly;
		private string newScriptingAssemblyName;
		private string lastServerAddress;
		[NonSerialized] private Promise generateCodeTask;
		[NonSerialized] private Promise syncTask;
		[SerializeField] private bool formulaAssembliesFold;
		[SerializeField] private bool codeGenerationFold;
		[SerializeField] private bool connectionFold;

		static GameDataInspector()
		{
			Selection.selectionChanged += OnSelectionChanged;
			EditorApplication.update += OnUpdate;
		}

		public static void OnSelectionChanged()
		{
			SelectionChanged = true;
		}

		private static void OnUpdate()
		{
			if (!SelectionChanged) return;
			SelectionChanged = false;

			if (Selection.activeObject == null)
			{
				ActiveObject = null;
				return;
			}

			var assetPath = FileHelper.MakeProjectRelative(AssetDatabase.GetAssetPath(Selection.activeObject));
			if (GameDataTracker.IsGameDataFile(assetPath) == false)
			{
				return;
			}

			if (Selection.activeObject == ActiveObject)
			{
				return;
			}

			if (!(ActiveObject is GameDataAsset))
			{
				ActiveObject = CreateInstance<GameDataAsset>();
				ActiveObject.hideFlags = HideFlags.DontSave;
			}
			((GameDataAsset)ActiveObject).FilePath = assetPath;

			Selection.activeObject = ActiveObject;
			foreach (var editor in UnityEngine.Resources.FindObjectsOfTypeAll<GameDataInspector>())
			{
				editor.Repaint();
			}

		}

		/// <inheritdoc />
		// ReSharper disable once FunctionComplexityOverflow
		public override void OnInspectorGUI()
		{
			this.DrawDefaultInspector();

			var gameDataAsset = this.target;
			if (gameDataAsset is GameDataAsset)
			{
				gameDataAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(((GameDataAsset)gameDataAsset).FilePath);
			}

			var gameDataPath = FileHelper.MakeProjectRelative(AssetDatabase.GetAssetPath(gameDataAsset));
			if (GameDataTracker.IsGameDataFile(gameDataPath) == false)
			{
				return;
			}

			if (this.lastAsset != gameDataAsset || this.gameDataSettings == null)
			{
				this.gameDataSettings = GameDataSettings.Load(gameDataPath);
				this.gameDataSettings.ScriptingAssemblies = this.gameDataSettings.ScriptingAssemblies ?? new string[0];
				this.lastServerAddress = string.IsNullOrEmpty(this.gameDataSettings.ServerAddress) == false ?
					Resources.UI_UNITYPLUGIN_INSPECTOR_CONNECTION_LABEL + ": " + new Uri(this.gameDataSettings.ServerAddress).GetComponents(UriComponents.HostAndPort, UriFormat.SafeUnescaped) :
					Resources.UI_UNITYPLUGIN_INSPECTOR_NOT_CONNECTED_LABEL;
				this.lastAsset = gameDataAsset;
				this.newScriptingAssembly = null;
				this.newScriptingAssemblyName = null;
			}
			GUI.enabled = true;
			GUILayout.Label(Path.GetFileName(gameDataPath), EditorStyles.boldLabel);

			this.codeGenerationFold = EditorGUILayout.Foldout(this.codeGenerationFold, Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_GENERATION_LABEL);
			if (this.codeGenerationFold)
			{
				GUI.enabled = !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;

				this.gameDataSettings.Generator = (int)(GameDataSettings.CodeGenerator)EditorGUILayout.EnumPopup(Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_GENERATOR,
					(GameDataSettings.CodeGenerator)this.gameDataSettings.Generator);

				this.gameDataSettings.LanguageVersion = (int)(CSharpLanguageVersion)EditorGUILayout.EnumPopup(
					Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_LANGUAGE_VERSION, (CSharpLanguageVersion)this.gameDataSettings.LanguageVersion);
				
				if (this.gameDataSettings.Generator != (int)GameDataSettings.CodeGenerator.None)
				{

					this.gameDataSettings.AutoGeneration = EditorGUILayout.Toggle(Resources.UI_UNITYPLUGIN_INSPECTOR_AUTO_GENERATION,
						this.gameDataSettings.AutoGeneration);

					var folderAsset = !string.IsNullOrEmpty(this.gameDataSettings.CodeGenerationPath) && Directory.Exists(this.gameDataSettings.CodeGenerationPath) ?
						AssetDatabase.LoadAssetAtPath<DefaultAsset>(this.gameDataSettings.CodeGenerationPath) : null;
					var assetAsset = !string.IsNullOrEmpty(this.gameDataSettings.AssetGenerationPath) && File.Exists(this.gameDataSettings.AssetGenerationPath) ?
						AssetDatabase.LoadAssetAtPath<ScriptableObject>(this.gameDataSettings.AssetGenerationPath) : null;

					if (folderAsset != null)
					{
						this.gameDataSettings.CodeGenerationPath = AssetDatabase.GetAssetPath(
							EditorGUILayout.ObjectField(Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_GENERATION_PATH, folderAsset, typeof(DefaultAsset), false));
					}
					else
					{
						this.gameDataSettings.CodeGenerationPath = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_GENERATION_PATH,
							this.gameDataSettings.CodeGenerationPath);
					}

					if (this.gameDataSettings.Generator == (int)GameDataSettings.CodeGenerator.CSharpCodeAndAsset)
					{
						if (assetAsset != null)
						{
							this.gameDataSettings.AssetGenerationPath = AssetDatabase.GetAssetPath(EditorGUILayout.ObjectField(Resources.UI_UNITYPLUGIN_INSPECTOR_ASSET_GENERATION_PATH, assetAsset, typeof(ScriptableObject), false));
						}
						else
						{
							this.gameDataSettings.AssetGenerationPath = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_INSPECTOR_ASSET_GENERATION_PATH, this.gameDataSettings.AssetGenerationPath);
						}
					}

					if (this.gameDataSettings.Generator == (int)GameDataSettings.CodeGenerator.CSharpCodeAndAsset &&
						Path.GetExtension(this.gameDataSettings.AssetGenerationPath) != ".asset")
						this.gameDataSettings.AssetGenerationPath = Path.ChangeExtension(this.gameDataSettings.AssetGenerationPath, ".asset");

					this.gameDataSettings.Namespace = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_NAMESPACE,
						this.gameDataSettings.Namespace);
					this.gameDataSettings.GameDataClassName = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_GAMEDATA_CLASS_NAME,
						this.gameDataSettings.GameDataClassName);
					this.gameDataSettings.DocumentClassName = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_DOCUMENT_CLASS_NAME,
						this.gameDataSettings.DocumentClassName);
					this.gameDataSettings.LineEnding = (int)(SourceCodeLineEndings)EditorGUILayout.EnumPopup(
						Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_LINE_ENDINGS, (SourceCodeLineEndings)this.gameDataSettings.LineEnding);
					this.gameDataSettings.Indentation = (int)(SourceCodeIndentation)EditorGUILayout.EnumPopup(
						Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_INDENTATION, (SourceCodeIndentation)this.gameDataSettings.Indentation);
					this.gameDataSettings.Optimizations = (int)(SourceCodeGenerationOptimizations)EditorGUILayout.EnumFlagsField(
						Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_OPTIMIZATIONS, (SourceCodeGenerationOptimizations)this.gameDataSettings.Optimizations);

					if (!CharonCli.IsToolsLegacy())
					{
						this.gameDataSettings.SplitSourceCodeFiles = EditorGUILayout.Toggle(Resources.UI_UNITYPLUGIN_INSPECTOR_SPLIT_FILES,
							this.gameDataSettings.SplitSourceCodeFiles);
					}
					GUI.enabled = true;
				}
			}

			GUI.enabled = !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling && 
				string.IsNullOrEmpty(this.gameDataSettings.CodeGenerationPath) == false && 
				this.gameDataSettings.Generator != (int)GameDataSettings.CodeGenerator.None;

			EditorGUILayout.Space();

			var hasDoneGeneration = this.generateCodeTask != null && this.generateCodeTask.IsCompleted;
			var hasRunningGeneration = this.generateCodeTask != null && !this.generateCodeTask.IsCompleted;
			var generationStatus = (hasDoneGeneration ? " " + Resources.UI_UNITYPLUGIN_INSPECTOR_OPERATION_DONE : 
				hasRunningGeneration ? " " + Resources.UI_UNITYPLUGIN_INSPECTOR_OPERATION_RUNNING : "");
			if (GUILayout.Button(Resources.UI_UNITYPLUGIN_INSPECTOR_RUN_GENERATOR_BUTTON + generationStatus))
			{
				this.generateCodeTask = GenerateCodeAndAssetsRoutine.Schedule(
					path: gameDataPath,
					progressCallback: ProgressUtils.ReportToLog(Resources.UI_UNITYPLUGIN_INSPECTOR_GENERATION_PREFIX + " "),
					coroutineId: "generation::" + gameDataPath
				).ContinueWith(_ => this.Repaint());
			}

			this.connectionFold = EditorGUILayout.Foldout(this.connectionFold, this.lastServerAddress);
			if (this.connectionFold)
			{
				if (this.gameDataSettings.IsConnected)
				{
					this.gameDataSettings.AutoSynchronization = EditorGUILayout.Toggle(Resources.UI_UNITYPLUGIN_INSPECTOR_AUTO_SYNC,
						this.gameDataSettings.AutoSynchronization);
					EditorGUILayout.LabelField(Resources.UI_UNITYPLUGIN_INSPECTOR_PROJECT_LABEL, this.gameDataSettings.ProjectName ?? this.gameDataSettings.ProjectId);
					EditorGUILayout.LabelField(Resources.UI_UNITYPLUGIN_INSPECTOR_BRANCH_LABEL, this.gameDataSettings.BranchName ?? this.gameDataSettings.BranchId);

					EditorGUILayout.Space();

					EditorGUILayout.BeginHorizontal();
					var hasDoneSync = this.syncTask != null && this.syncTask.IsCompleted;
					var hasRunningSync = this.syncTask != null && !this.syncTask.IsCompleted;
					var syncStatus = (hasDoneSync ? " " + Resources.UI_UNITYPLUGIN_INSPECTOR_OPERATION_DONE : 
						hasRunningSync ? " " + Resources.UI_UNITYPLUGIN_INSPECTOR_OPERATION_RUNNING : "");
					
					if (GUILayout.Button(Resources.UI_UNITYPLUGIN_INSPECTOR_SYNCHRONIZE_BUTTON + syncStatus))
					{
						var cancellation = new Promise();
						this.syncTask = SynchronizeAssetsRoutine.Schedule(
							force: true,
							paths: new[] { gameDataPath },
							progressCallback: ProgressUtils.ShowCancellableProgressBar(Resources.UI_UNITYPLUGIN_INSPECTOR_SYNCHONIZATION_PREFIX + " ", cancellation: cancellation),
							cancellation: cancellation,
							coroutineId: "synchronization::" + gameDataPath
						).ContinueWith(_ =>
						{
							ProgressUtils.HideProgressBar();
							this.Repaint();
						});
					}
					if (GUILayout.Button(Resources.UI_UNITYPLUGIN_INSPECTOR_DISCONNECT_BUTTON))
					{
						this.gameDataSettings.ServerAddress = null;
						this.gameDataSettings.BranchId = null;
						this.gameDataSettings.BranchName = null;
						this.gameDataSettings.ProjectId = null;
						this.gameDataSettings.ProjectName = null;
						this.lastServerAddress = Resources.UI_UNITYPLUGIN_INSPECTOR_NOT_CONNECTED_LABEL;

						GUI.changed = true;
					}
					EditorGUILayout.EndHorizontal();
				}
			}
			if (!this.gameDataSettings.IsConnected)
			{
				EditorGUILayout.Space();

				if (GUILayout.Button(Resources.UI_UNITYPLUGIN_INSPECTOR_CONNECT_BUTTON))
				{
					ConnectGameDataWindow.ShowAsync(
						folder: Path.GetDirectoryName(gameDataPath) ?? "./",
						name: Path.GetFileNameWithoutExtension(Path.GetFileName(gameDataPath)),
						autoClose: true
					);
				}
				
				this.formulaAssembliesFold = EditorGUILayout.Foldout(this.formulaAssembliesFold, Resources.UI_UNITYPLUGIN_INSPECTOR_FORMULA_ASSEMBLIES_LABEL);
				if (this.formulaAssembliesFold)
				{
					for (var i = 0; i < this.gameDataSettings.ScriptingAssemblies.Length; i++)
					{
						var watchedAssetPath = this.gameDataSettings.ScriptingAssemblies[i];
						var assetExists = !string.IsNullOrEmpty(watchedAssetPath) && (File.Exists(watchedAssetPath) || Directory.Exists(watchedAssetPath));
						var watchedAsset = assetExists ? AssetDatabase.LoadMainAssetAtPath(watchedAssetPath) : null;
						if (watchedAsset != null)
							this.gameDataSettings.ScriptingAssemblies[i] = AssetDatabase.GetAssetPath(EditorGUILayout.ObjectField(Resources.UI_UNITYPLUGIN_INSPECTOR_ASSET_LABEL, watchedAsset, typeof(UnityObject), false));
						else
							this.gameDataSettings.ScriptingAssemblies[i] = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_INSPECTOR_NAME_LABEL, watchedAssetPath);
					}
					EditorGUILayout.Space();
					this.newScriptingAssembly = EditorGUILayout.ObjectField("<" + Resources.UI_UNITYPLUGIN_INSPECTOR_ADD_ASSET_BUTTON + ">", this.newScriptingAssembly, typeof(UnityObject), false);
					if (Event.current.type == EventType.Repaint && this.newScriptingAssembly != null)
					{
						var assemblies = new HashSet<string>(this.gameDataSettings.ScriptingAssemblies);
						assemblies.Remove("");
						assemblies.Add(AssetDatabase.GetAssetPath(this.newScriptingAssembly));
						this.gameDataSettings.ScriptingAssemblies = assemblies.ToArray();
						this.newScriptingAssembly = null;
						GUI.changed = true;
					}
					EditorGUILayout.BeginHorizontal();
					this.newScriptingAssemblyName = EditorGUILayout.TextField("<" + Resources.UI_UNITYPLUGIN_INSPECTOR_ADD_NAME_BUTTON + ">", this.newScriptingAssemblyName);
					if (GUILayout.Button(Resources.UI_UNITYPLUGIN_INSPECTOR_ADD_BUTTON, EditorStyles.miniButton, GUILayout.Width(50), GUILayout.Height(18)))
					{
						var assemblies = new HashSet<string>(this.gameDataSettings.ScriptingAssemblies);
						assemblies.Remove("");
						assemblies.Add(this.newScriptingAssemblyName);
						this.gameDataSettings.ScriptingAssemblies = assemblies.ToArray();
						this.newScriptingAssemblyName = null;
						GUI.changed = true;
						this.Repaint();
					}
					GUILayout.Space(5);
					EditorGUILayout.EndHorizontal();
				}
			}

			EditorGUILayout.Space();
			GUILayout.Label(Resources.UI_UNITYPLUGIN_INSPECTOR_ACTIONS_GROUP, EditorStyles.boldLabel);

			if (EditorApplication.isCompiling)
				EditorGUILayout.HelpBox(Resources.UI_UNITYPLUGIN_COMPILING_WARNING, MessageType.Warning);
			else if (CoroutineScheduler.IsRunning)
				EditorGUILayout.HelpBox(Resources.UI_UNITYPLUGIN_COROUTINE_IS_RUNNIG_WARNING, MessageType.Warning);

			EditorGUILayout.BeginHorizontal();
			GUI.enabled = !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
			if (GUILayout.Button(Resources.UI_UNITYPLUGIN_INSPECTOR_EDIT_BUTTON))
			{
				AssetDatabase.OpenAsset(gameDataAsset);
				this.Repaint();
			}

			GUI.enabled = !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
			if (GUILayout.Button(Resources.UI_UNITYPLUGIN_INSPECTOR_VALIDATE_BUTTON))
			{
				ValidateGameDataRoutine.Schedule(
					path: gameDataPath,
					progressCallback: ProgressUtils.ReportToLog(Resources.UI_UNITYPLUGIN_INSPECTOR_VALIDATION_PREFIX + " "),
					coroutineId: "validation::" + gameDataPath
				).ContinueWith(_ => this.Repaint());
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button(Resources.UI_UNITYPLUGIN_INSPECTOR_SELECT_BUTTON))
			{
				Selection.activeObject = gameDataAsset;
				ActiveObject = gameDataAsset;
				EditorGUIUtility.PingObject(gameDataAsset);
			}
			EditorGUILayout.EndHorizontal();

			GUI.enabled = true;

			if (GUI.changed)
			{
				this.gameDataSettings.Save(gameDataPath);
			}
		}
	}

	/// <summary>
	/// Asset surrogate for game data files
	/// </summary>
	public class GameDataAsset : ScriptableObject
	{
		[System.NonSerialized] public string FilePath;
	}
}
