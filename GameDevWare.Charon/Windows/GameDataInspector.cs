/*
	Copyright (c) 2016 Denis Zykov

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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameDevWare.Charon.Async;
using GameDevWare.Charon.Utils;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;

namespace GameDevWare.Charon.Windows
{
	internal class GameDataInspector : Editor
	{
		private Object lastAsset;
		private GameDataSettings gameDataSettings;
		private Object newScriptingAssembly;
		private string newScriptingAssemblyName;
		private bool scriptingAssembliesFold;

		static GameDataInspector()
		{
			Selection.selectionChanged += OnSelectionChanged;
		}

		public static void OnSelectionChanged()
		{
			if (Selection.activeObject == null)
				return;
			var assetPath = PathUtils.MakeProjectRelative(AssetDatabase.GetAssetPath(Selection.activeObject));
			if (GameDataTracker.IsGameDataFile(assetPath) == false)
				return;

			try
			{
				var templateAsset = Selection.activeObject.GetType();
				var inspectorWindowType = typeof(PopupWindow).Assembly.GetType("UnityEditor.InspectorWindow");
				var inspectorWindow = EditorWindow.GetWindow(inspectorWindowType);
				var activeEditorTracker = inspectorWindow.GetFieldValue("m_Tracker");
				var customEditorAttributesType = typeof(PopupWindow).Assembly.GetType("UnityEditor.CustomEditorAttributes");
				var customEditorsList = (System.Collections.IList)customEditorAttributesType.GetFieldValue("kSCustomEditors");
				foreach (var customEditor in customEditorsList)
				{
					if ((Type)customEditor.GetFieldValue("m_InspectedType") != templateAsset) continue;

					var originalInspectorType = (Type)customEditor.GetFieldValue("m_InspectorType");
					// override inspector
					customEditor.SetFieldValue("m_InspectorType", typeof(GameDataInspector));
					// force rebuild editor list
					activeEditorTracker.Invoke("ForceRebuild");
					inspectorWindow.Invoke("Repaint");
					// restore original inspector
					customEditor.SetFieldValue("m_InspectorType", originalInspectorType);
				}

			}
			catch (Exception e)
			{
				Debug.LogError(e);
			}
		}

		public override void OnInspectorGUI()
		{
			var gameDataAsset = (Object)this.target;
			var gameDataPath = PathUtils.MakeProjectRelative(AssetDatabase.GetAssetPath(gameDataAsset));

			var assetPath = PathUtils.MakeProjectRelative(AssetDatabase.GetAssetPath(Selection.activeObject));
			if (GameDataTracker.IsGameDataFile(assetPath) == false)
			{
				this.DrawDefaultInspector();
				return;
			}

			if (this.lastAsset != gameDataAsset || this.gameDataSettings == null)
			{
				this.gameDataSettings = GameDataSettings.Load(gameDataPath);
				this.gameDataSettings.ScriptingAssemblies = this.gameDataSettings.ScriptingAssemblies ?? new string[0];
				this.lastAsset = gameDataAsset;
				this.newScriptingAssembly = null;
				this.newScriptingAssemblyName = null;
			}
			GUI.enabled = true;
			GUILayout.Label(Path.GetFileName(gameDataPath), EditorStyles.boldLabel);
			this.gameDataSettings.Generator = (int)(GameDataSettings.CodeGenerator)EditorGUILayout.EnumPopup(Resources.UI_UNITYPLUGIN_WINDOW_CODE_GENERATOR, (GameDataSettings.CodeGenerator)this.gameDataSettings.Generator);
			if (this.gameDataSettings.Generator != (int)GameDataSettings.CodeGenerator.None)
			{
				this.gameDataSettings.AutoGeneration = EditorGUILayout.Toggle(Resources.UI_UNITYPLUGIN_WINDOW_AUTO_GENERATION, this.gameDataSettings.AutoGeneration);
				var codeAsset = !string.IsNullOrEmpty(this.gameDataSettings.CodeGenerationPath) && File.Exists(this.gameDataSettings.CodeGenerationPath) ? AssetDatabase.LoadAssetAtPath<TextAsset>(this.gameDataSettings.CodeGenerationPath) : null;
				var assetAsset = !string.IsNullOrEmpty(this.gameDataSettings.AssetGenerationPath) && File.Exists(this.gameDataSettings.AssetGenerationPath) ? AssetDatabase.LoadAssetAtPath<ScriptableObject>(this.gameDataSettings.AssetGenerationPath) : null;

				if (codeAsset != null)
					this.gameDataSettings.CodeGenerationPath = AssetDatabase.GetAssetPath(EditorGUILayout.ObjectField(Resources.UI_UNITYPLUGIN_WINDOW_CODE_GENERATION_PATH, codeAsset, typeof(TextAsset), false));
				else
					this.gameDataSettings.CodeGenerationPath = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_WINDOW_CODE_GENERATION_PATH, this.gameDataSettings.CodeGenerationPath);

				if (this.gameDataSettings.Generator == (int)GameDataSettings.CodeGenerator.CSharpCodeAndAsset)
				{
					if (assetAsset != null)
						this.gameDataSettings.AssetGenerationPath = AssetDatabase.GetAssetPath(EditorGUILayout.ObjectField(Resources.UI_UNITYPLUGIN_WINDOW_ASSET_GENERATION_PATH, assetAsset, typeof(ScriptableObject), false));
					else
						this.gameDataSettings.AssetGenerationPath = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_WINDOW_ASSET_GENERATION_PATH, this.gameDataSettings.AssetGenerationPath);
				}

				if ((this.gameDataSettings.Generator == (int)GameDataSettings.CodeGenerator.CSharp ||
					this.gameDataSettings.Generator == (int)GameDataSettings.CodeGenerator.CSharpCodeAndAsset) &&
					Path.GetExtension(this.gameDataSettings.CodeGenerationPath) != ".cs")
					this.gameDataSettings.CodeGenerationPath = Path.ChangeExtension(this.gameDataSettings.CodeGenerationPath, ".cs");
				if (this.gameDataSettings.Generator == (int)GameDataSettings.CodeGenerator.CSharpCodeAndAsset && Path.GetExtension(this.gameDataSettings.AssetGenerationPath) != ".asset")
					this.gameDataSettings.AssetGenerationPath = Path.ChangeExtension(this.gameDataSettings.AssetGenerationPath, ".asset");

				this.gameDataSettings.Namespace = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_WINDOW_CODE_NAMESPACE, this.gameDataSettings.Namespace);
				this.gameDataSettings.GameDataClassName = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_WINDOW_CODE_API_CLASS_NAME, this.gameDataSettings.GameDataClassName);
				this.gameDataSettings.DocumentClassName = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_WINDOW_CODE_DOCUMENT_CLASS_NAME, this.gameDataSettings.DocumentClassName);
				this.gameDataSettings.LineEnding = (int)(GameDataSettings.LineEndings)EditorGUILayout.EnumPopup(Resources.UI_UNITYPLUGIN_WINDOW_CODE_LINE_ENDINGS, (GameDataSettings.LineEndings)this.gameDataSettings.LineEnding);
				this.gameDataSettings.Indentation = (int)(GameDataSettings.Indentations)EditorGUILayout.EnumPopup(Resources.UI_UNITYPLUGIN_WINDOW_CODE_INDENTATION, (GameDataSettings.Indentations)this.gameDataSettings.Indentation);
				this.gameDataSettings.Options = (int)(CodeGenerationOptions)EditorGUILayout.EnumMaskField(Resources.UI_UNITYPLUGIN_WINDOW_CODE_OPTIONS, (CodeGenerationOptions)this.gameDataSettings.Options);
			}
			this.scriptingAssembliesFold = EditorGUILayout.Foldout(this.scriptingAssembliesFold, "Scripting Assemblies");
			if (this.scriptingAssembliesFold)
			{
				for (var i = 0; i < this.gameDataSettings.ScriptingAssemblies.Length; i++)
				{
					var watchedAssetPath = this.gameDataSettings.ScriptingAssemblies[i];
					var assetExists = !string.IsNullOrEmpty(watchedAssetPath) && (File.Exists(watchedAssetPath) || Directory.Exists(watchedAssetPath));
					var watchedAsset = assetExists ? AssetDatabase.LoadMainAssetAtPath(watchedAssetPath) : null;
					if (watchedAsset != null)
						this.gameDataSettings.ScriptingAssemblies[i] = AssetDatabase.GetAssetPath(EditorGUILayout.ObjectField("Asset", watchedAsset, typeof(Object), false));
					else
						this.gameDataSettings.ScriptingAssemblies[i] = EditorGUILayout.TextField("Name", watchedAssetPath);
				}
				EditorGUILayout.Space();
				this.newScriptingAssembly = EditorGUILayout.ObjectField("<Add Asset>", this.newScriptingAssembly, typeof(Object), false);
				if (Event.current.type == EventType.repaint && this.newScriptingAssembly != null)
				{
					var assemblies = new HashSet<string>(this.gameDataSettings.ScriptingAssemblies);
					assemblies.Remove("");
					assemblies.Add(AssetDatabase.GetAssetPath(this.newScriptingAssembly));
					this.gameDataSettings.ScriptingAssemblies = assemblies.ToArray();
					this.newScriptingAssembly = null;
					GUI.changed = true;
				}
				EditorGUILayout.BeginHorizontal();
				this.newScriptingAssemblyName = EditorGUILayout.TextField("<Add Name>", this.newScriptingAssemblyName);
				if (GUILayout.Button("Add", EditorStyles.toolbarButton, GUILayout.Width(70), GUILayout.Height(18)))
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

			EditorGUILayout.Space();
			GUILayout.Label(Resources.UI_UNITYPLUGIN_WINDOW_ACTIONS_GROUP, EditorStyles.boldLabel);
			EditorGUILayout.BeginHorizontal();
			GUI.enabled = !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling && string.IsNullOrEmpty(this.gameDataSettings.CodeGenerationPath) == false;
			if (GUILayout.Button(Resources.UI_UNITYPLUGIN_WINDOW_EDIT_BUTTON))
			{
				AssetDatabase.OpenAsset(gameDataAsset);
			}
			if (this.gameDataSettings.Generator != (int)GameDataSettings.CodeGenerator.None)
			{
				if (GUILayout.Button(Resources.UI_UNITYPLUGIN_WINDOW_RUN_GENERATOR_BUTTON))
				{
					CoroutineScheduler.Schedule(Menu.GenerateCodeAndAssetsAsync(gameDataPath, ProgressUtils.ReportToLog(Resources.UI_UNITYPLUGIN_WINDOW_GENERATION_PREFIX + " ")), "generation::" + gameDataPath);
				}
			}
			GUI.enabled = !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
			if (GUILayout.Button(Resources.UI_UNITYPLUGIN_WINDOW_VALIDATE_BUTTON))
			{
				CoroutineScheduler.Schedule(Menu.ValidateAsync(gameDataPath, ProgressUtils.ReportToLog(Resources.UI_UNITYPLUGIN_WINDOW_VALIDATION_PREFIX + " ")), "validation::" + gameDataPath);
			}
			GUI.enabled = true;
			EditorGUILayout.EndHorizontal();

			if (GUI.changed)
			{
				this.gameDataSettings.Save(gameDataPath);
			}
		}
	}
}
