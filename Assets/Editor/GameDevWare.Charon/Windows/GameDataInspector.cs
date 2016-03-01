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

using System.IO;
using Assets.Editor.GameDevWare.Charon.Utils;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor.GameDevWare.Charon.Windows
{
	[CustomEditor(typeof(Object), editorForChildClasses: true)]
	class GameDataInspector : UnityEditor.Editor
	{

		private Object lastAsset;
		private GameDataSettings gameDataSettings;

		public override void OnInspectorGUI()
		{
			var gameDataAsset = (Object)this.target;
			var gameDataPath = FileUtils.MakeProjectRelative(AssetDatabase.GetAssetPath(gameDataAsset));
			if (Settings.Current.GameDataPaths.Contains(gameDataPath) == false)
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
				this.gameDataSettings.EntryClassName = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_WINDOWCODEENTRYCLASSNAME, this.gameDataSettings.EntryClassName);
				this.gameDataSettings.LineEnding = (int)(GameDataSettings.LineEndings)EditorGUILayout.EnumPopup(Resources.UI_UNITYPLUGIN_WINDOWCODELINEENDINGS, (GameDataSettings.LineEndings)this.gameDataSettings.LineEnding);
				this.gameDataSettings.Identation = (int)(GameDataSettings.Identations)EditorGUILayout.EnumPopup(Resources.UI_UNITYPLUGIN_WINDOWCODEIDENTATION, (GameDataSettings.Identations)this.gameDataSettings.Identation);
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
			if (GUILayout.Button(Resources.UI_UNITYPLUGIN_WINDOWRUNGENERATORBUTTON))
			{
				CoroutineScheduler.Schedule(Menu.GenerateCodeAndAssetsAsync(gameDataPath, ProgressUtils.ReportToLog(Resources.UI_UNITYPLUGIN_WINDOWGENERATIONPREFIX + " ")), "generation::" + gameDataPath);
			}
			GUI.enabled = !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
			if (GUILayout.Button(Resources.UI_UNITYPLUGIN_WINDOWVALIDATEBUTTON))
			{
				CoroutineScheduler.Schedule(Menu.ValidateAsync(gameDataPath, ProgressUtils.ReportToLog(Resources.UI_UNITYPLUGIN_WINDOWVALIDATIONPREFIX + " ")), "validation::" + gameDataPath);
			}
			GUI.enabled = true;
			if (GUILayout.Button(Resources.UI_UNITYPLUGIN_WINDOWUNTRACTBUTTON))
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
	}
}
