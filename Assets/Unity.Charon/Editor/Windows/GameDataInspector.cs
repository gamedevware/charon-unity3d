/*
	Copyright (c) 2015 Denis Zykov

	This is part of Charon Game Data Editor Unity Plugin.

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
using UnityEditor;
using UnityEngine;

namespace Assets.Unity.Charon.Editor.Windows
{
	[CustomEditor(typeof(TextAsset))]
	class GameDataInspector : UnityEditor.Editor
	{
		private static readonly UnityEditor.Editor DefaultEditor = (UnityEditor.Editor)ScriptableObject.CreateInstance(typeof(EditorWindow).Assembly.GetType("UnityEditor.TextAssetInspector", true));

		private TextAsset lastAsset;
		private GameDataSettings gameDataSettings;

		public override void OnInspectorGUI()
		{
			var gameDataAsset = (TextAsset)this.target;
			var gameDataPath = FileUtils.MakeProjectRelative(AssetDatabase.GetAssetPath(gameDataAsset));
			if (Settings.Current.GameDataPaths.Contains(gameDataPath) == false)
			{
				DefaultEditor.Invoke("InternalSetTargets", new object[] { this.targets });
				DefaultEditor.OnInspectorGUI();
				return;
			}

			if (this.lastAsset != gameDataAsset || this.gameDataSettings == null)
			{
				this.gameDataSettings = GameDataSettings.Load(gameDataPath);
				this.lastAsset = gameDataAsset;
			}
			GUI.enabled = true;
			GUILayout.Label(Path.GetFileName(gameDataPath), EditorStyles.boldLabel);
			this.gameDataSettings.Generator = (int)(GameDataSettings.CodeGenerator)EditorGUILayout.EnumPopup("Code Generator", (GameDataSettings.CodeGenerator)this.gameDataSettings.Generator);
			if (this.gameDataSettings.Generator != (int)GameDataSettings.CodeGenerator.None)
			{
				this.gameDataSettings.AutoGeneration = EditorGUILayout.Toggle("Auto-Generation", this.gameDataSettings.AutoGeneration);
				var codeAsset = !string.IsNullOrEmpty(this.gameDataSettings.CodeGenerationPath) && File.Exists(this.gameDataSettings.CodeGenerationPath) ? AssetDatabase.LoadAssetAtPath<TextAsset>(this.gameDataSettings.CodeGenerationPath) : null;
				var assetAsset = !string.IsNullOrEmpty(this.gameDataSettings.AssetGenerationPath) && File.Exists(this.gameDataSettings.AssetGenerationPath) ? AssetDatabase.LoadAssetAtPath<ScriptableObject>(this.gameDataSettings.AssetGenerationPath) : null;

				if (codeAsset != null)
					this.gameDataSettings.CodeGenerationPath = AssetDatabase.GetAssetPath(EditorGUILayout.ObjectField("Code Generation Path", codeAsset, typeof(TextAsset), false));
				else
					this.gameDataSettings.CodeGenerationPath = EditorGUILayout.TextField("Code Generation Path", this.gameDataSettings.CodeGenerationPath);

				if (gameDataSettings.Generator == (int) GameDataSettings.CodeGenerator.CSharpCodeAndAsset)
				{
					if (assetAsset != null)
						this.gameDataSettings.AssetGenerationPath = AssetDatabase.GetAssetPath(EditorGUILayout.ObjectField("Asset Generation Path", assetAsset, typeof (ScriptableObject), false));
					else
						this.gameDataSettings.AssetGenerationPath = EditorGUILayout.TextField("Asset Generation Path", this.gameDataSettings.AssetGenerationPath);
				}

				if ((this.gameDataSettings.Generator == (int)GameDataSettings.CodeGenerator.CSharp ||
					this.gameDataSettings.Generator == (int)GameDataSettings.CodeGenerator.CSharpCodeAndAsset) &&
					Path.GetExtension(this.gameDataSettings.CodeGenerationPath) != ".cs")
					this.gameDataSettings.CodeGenerationPath = Path.ChangeExtension(this.gameDataSettings.CodeGenerationPath, ".cs");
				if (this.gameDataSettings.Generator == (int)GameDataSettings.CodeGenerator.CSharpCodeAndAsset && Path.GetExtension(this.gameDataSettings.AssetGenerationPath) != ".asset")
					this.gameDataSettings.AssetGenerationPath = Path.ChangeExtension(this.gameDataSettings.AssetGenerationPath, ".asset");

				this.gameDataSettings.Namespace = EditorGUILayout.TextField("Code Namespace", this.gameDataSettings.Namespace);
				this.gameDataSettings.GameDataClassName = EditorGUILayout.TextField("Code GameData class", this.gameDataSettings.GameDataClassName);
				this.gameDataSettings.EntryClassName = EditorGUILayout.TextField("Code Entry class", this.gameDataSettings.EntryClassName);
				this.gameDataSettings.LineEnding = (int)(GameDataSettings.LineEndings)EditorGUILayout.EnumPopup("Line endings", (GameDataSettings.LineEndings)this.gameDataSettings.LineEnding);
				this.gameDataSettings.Identation = (int)(GameDataSettings.Identations)EditorGUILayout.EnumPopup("Identation", (GameDataSettings.Identations)this.gameDataSettings.Identation);
				this.gameDataSettings.Options = (int)(GameDataSettings.CodeGenerationOptions)EditorGUILayout.EnumMaskField("Options", (GameDataSettings.CodeGenerationOptions)gameDataSettings.Options);
			}

			EditorGUILayout.Space();
			GUILayout.Label("Actions", EditorStyles.boldLabel);
			EditorGUILayout.BeginHorizontal();
			GUI.enabled = !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling && string.IsNullOrEmpty(this.gameDataSettings.CodeGenerationPath) == false;
			if (GUILayout.Button("Edit"))
			{
				AssetDatabase.OpenAsset(gameDataAsset);
			}
			if (GUILayout.Button("Run Generator"))
			{
				CoroutineScheduler.Schedule(Menu.GenerateCodeAndAssetsAsync(gameDataPath, ProgressUtils.ReportToLog("Generation: ")), "generation::" + gameDataPath);
			}
			GUI.enabled = !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
			if (GUILayout.Button("Validate"))
			{
				CoroutineScheduler.Schedule(Menu.ValidateAsync(gameDataPath, ProgressUtils.ReportToLog("Validation: ")), "validation::" + gameDataPath);
			}
			if (GUILayout.Button("Migrate"))
			{
				CoroutineScheduler.Schedule(Menu.MigrateAsync(gameDataPath, ProgressUtils.ReportToLog("Migration: ")), "migration::" + gameDataPath);
			}
			GUI.enabled = true;
			if (GUILayout.Button("Untrack"))
			{
				Settings.Current.GameDataPaths.Remove(gameDataPath);
				Settings.Current.Version++;
			}
			EditorGUILayout.EndHorizontal();

			if (GUI.changed)
			{
				this.gameDataSettings.Save(gameDataPath);
			}
		}

		//private void ValidateAndTrack(Object assetToValidate)
		//{
		//	var path = FileUtils.MakeProjectRelative(AssetDatabase.GetAssetPath(assetToValidate));
		//	var validationCoroutine = CoroutineScheduler.Schedule<Dictionary<string, object>>(
		//		Menu.ValidateAsync(path, ProgressUtils.ReportToLog("Validation: ")
		//	));
		//	validationCoroutine.ContinueWith(s =>
		//	{
		//		var report = default(object);
		//		var errorMsg = default(string);
		//		if (validationCoroutine.HasErrors)
		//			errorMsg = "Validation failed: " + validationCoroutine.Error.Unwrap().ToString();
		//		else if (validationCoroutine.GetResult() == null || validationCoroutine.GetResult().TryGetValue(path, out report) == false)
		//			errorMsg = "Validation failed: " + "No report return from tool.";
		//		else if (report is string)
		//			errorMsg = "Validation failed: " + report;
		//		else if (!Settings.Current.GameDataPaths.Contains(path))
		//		{
		//			Settings.Current.GameDataPaths.Add(path);
		//			Settings.Current.Version++;
		//		}

		//		if (this.lastCheckedObject == assetToValidate)
		//		{
		//			this.infoMessageType = MessageType.Error;
		//			if (string.IsNullOrEmpty(errorMsg) == false)
		//				this.infoMessage = errorMsg;
		//			else
		//				this.lastCheckedObject = null;
		//		}
		//	}, null);
		//}
	}
}
