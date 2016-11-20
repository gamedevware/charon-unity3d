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
using System.IO;
using System.Reflection;
using Assets.Editor.GameDevWare.Charon.Utils;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;

namespace Assets.Editor.GameDevWare.Charon.Windows
{
	internal class GameDataInspector : UnityEditor.Editor
	{
		private Object lastAsset;
		private GameDataSettings gameDataSettings;

		static GameDataInspector()
		{
			Selection.selectionChanged += OnSelectionChanged;
		}

		public static void OnSelectionChanged()
		{
			if (Selection.activeObject == null || Array.IndexOf(Settings.Current.GameDataPaths, FileUtils.MakeProjectRelative(AssetDatabase.GetAssetPath(Selection.activeObject))) == -1)
				return;

			try
			{
				var templateAsset = Selection.activeObject.GetType();
				var inspectorWindowType = typeof(PopupWindow).Assembly.GetType("UnityEditor.InspectorWindow");
				var inspectorWindow = UnityEditor.EditorWindow.GetWindow(inspectorWindowType);
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
			var gameDataPath = FileUtils.MakeProjectRelative(AssetDatabase.GetAssetPath(gameDataAsset));
			if (Array.IndexOf(Settings.Current.GameDataPaths, FileUtils.MakeProjectRelative(AssetDatabase.GetAssetPath(Selection.activeObject))) == -1)
			{
				this.DrawDefaultInspector();
				return;
			}

			if (this.lastAsset != gameDataAsset || this.gameDataSettings == null)
			{
				this.gameDataSettings = GameDataSettings.Load(gameDataPath);
				this.lastAsset = gameDataAsset;
			}
			GUI.enabled = true;
			GUILayout.Label(Path.GetFileName(gameDataPath), EditorStyles.boldLabel);
			this.gameDataSettings.Generator = (int)(GameDataSettings.CodeGenerator)EditorGUILayout.EnumPopup(Resources.UI_UNITYPLUGIN_WINDOWCODEGENERATOR, (GameDataSettings.CodeGenerator)this.gameDataSettings.Generator);
			if (this.gameDataSettings.Generator != (int)GameDataSettings.CodeGenerator.None)
			{
				this.gameDataSettings.AutoGeneration = EditorGUILayout.Toggle(Resources.UI_UNITYPLUGIN_WINDOWAUTOGENERATION, this.gameDataSettings.AutoGeneration);
				var codeAsset = !string.IsNullOrEmpty(this.gameDataSettings.CodeGenerationPath) && File.Exists(this.gameDataSettings.CodeGenerationPath) ? AssetDatabase.LoadAssetAtPath<TextAsset>(this.gameDataSettings.CodeGenerationPath) : null;
				var assetAsset = !string.IsNullOrEmpty(this.gameDataSettings.AssetGenerationPath) && File.Exists(this.gameDataSettings.AssetGenerationPath) ? AssetDatabase.LoadAssetAtPath<ScriptableObject>(this.gameDataSettings.AssetGenerationPath) : null;

				if (codeAsset != null)
					this.gameDataSettings.CodeGenerationPath = AssetDatabase.GetAssetPath(EditorGUILayout.ObjectField(Resources.UI_UNITYPLUGIN_WINDOWCODEGENERATIONPATH, codeAsset, typeof(TextAsset), false));
				else
					this.gameDataSettings.CodeGenerationPath = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_WINDOWCODEGENERATIONPATH, this.gameDataSettings.CodeGenerationPath);

				if (gameDataSettings.Generator == (int)GameDataSettings.CodeGenerator.CSharpCodeAndAsset)
				{
					if (assetAsset != null)
						this.gameDataSettings.AssetGenerationPath = AssetDatabase.GetAssetPath(EditorGUILayout.ObjectField(Resources.UI_UNITYPLUGIN_WINDOWASSETGENERATIONPATH, assetAsset, typeof(ScriptableObject), false));
					else
						this.gameDataSettings.AssetGenerationPath = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_WINDOWASSETGENERATIONPATH, this.gameDataSettings.AssetGenerationPath);
				}

				if ((this.gameDataSettings.Generator == (int)GameDataSettings.CodeGenerator.CSharp ||
					this.gameDataSettings.Generator == (int)GameDataSettings.CodeGenerator.CSharpCodeAndAsset) &&
					Path.GetExtension(this.gameDataSettings.CodeGenerationPath) != ".cs")
					this.gameDataSettings.CodeGenerationPath = Path.ChangeExtension(this.gameDataSettings.CodeGenerationPath, ".cs");
				if (this.gameDataSettings.Generator == (int)GameDataSettings.CodeGenerator.CSharpCodeAndAsset && Path.GetExtension(this.gameDataSettings.AssetGenerationPath) != ".asset")
					this.gameDataSettings.AssetGenerationPath = Path.ChangeExtension(this.gameDataSettings.AssetGenerationPath, ".asset");

				this.gameDataSettings.Namespace = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_WINDOWCODENAMESPACE, this.gameDataSettings.Namespace);
				this.gameDataSettings.GameDataClassName = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_WINDOWCODEGAMEDATACLASSNAME, this.gameDataSettings.GameDataClassName);
				this.gameDataSettings.DocumentClassName = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_WINDOWCODEENTRYCLASSNAME, this.gameDataSettings.DocumentClassName);
				this.gameDataSettings.LineEnding = (int)(GameDataSettings.LineEndings)EditorGUILayout.EnumPopup(Resources.UI_UNITYPLUGIN_WINDOWCODELINEENDINGS, (GameDataSettings.LineEndings)this.gameDataSettings.LineEnding);
				this.gameDataSettings.Indentation = (int)(GameDataSettings.Indentations)EditorGUILayout.EnumPopup(Resources.UI_UNITYPLUGIN_WINDOWCODEIDENTATION, (GameDataSettings.Indentations)this.gameDataSettings.Indentation);
				this.gameDataSettings.Options = (int)(GameDataSettings.CodeGenerationOptions)EditorGUILayout.EnumMaskField(Resources.UI_UNITYPLUGIN_WINDOWCODEOPTIONS, (GameDataSettings.CodeGenerationOptions)gameDataSettings.Options);
			}

			EditorGUILayout.Space();
			GUILayout.Label(Resources.UI_UNITYPLUGIN_WINDOWACTIONSGROUP, EditorStyles.boldLabel);
			EditorGUILayout.BeginHorizontal();
			GUI.enabled = !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling && string.IsNullOrEmpty(this.gameDataSettings.CodeGenerationPath) == false;
			if (GUILayout.Button(Resources.UI_UNITYPLUGIN_WINDOWEDITBUTTON))
			{
				AssetDatabase.OpenAsset(gameDataAsset);
			}
			if (this.gameDataSettings.Generator != (int)GameDataSettings.CodeGenerator.None)
			{
				if (GUILayout.Button(Resources.UI_UNITYPLUGIN_WINDOWRUNGENERATORBUTTON))
				{
					CoroutineScheduler.Schedule(Menu.GenerateCodeAndAssetsAsync(gameDataPath, ProgressUtils.ReportToLog(Resources.UI_UNITYPLUGIN_WINDOWGENERATIONPREFIX + " ")), "generation::" + gameDataPath);
				}
			}
			GUI.enabled = !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
			if (GUILayout.Button(Resources.UI_UNITYPLUGIN_WINDOWVALIDATEBUTTON))
			{
				CoroutineScheduler.Schedule(Menu.ValidateAsync(gameDataPath, ProgressUtils.ReportToLog(Resources.UI_UNITYPLUGIN_WINDOWVALIDATIONPREFIX + " ")), "validation::" + gameDataPath);
			}
			GUI.enabled = true;
			if (GUILayout.Button(Resources.UI_UNITYPLUGIN_WINDOWUNTRACTBUTTON))
			{
				Settings.Current.RemoveGameDataPath(gameDataPath);
				Settings.Current.Save();
			}
			EditorGUILayout.EndHorizontal();

			if (GUI.changed)
			{
				this.gameDataSettings.Save(gameDataPath);
			}
		}
	}
}
