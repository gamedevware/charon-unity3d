/*
	Copyright (c) 2017 Denis Zykov

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
using GameDevWare.Charon.Unity.Utils;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;

namespace GameDevWare.Charon.Unity.Windows
{
	internal class GameDataInspector : Editor
	{
		private Object lastAsset;
		private GameDataSettings gameDataSettings;
		private Object newScriptingAssembly;
		private string newScriptingAssemblyName;
		[SerializeField]
		private bool scriptingAssembliesFold;
		[SerializeField]
		private bool codeGenerationFold;

		static GameDataInspector()
		{
			Selection.selectionChanged += OnSelectionChanged;
		}

		public static void OnSelectionChanged()
		{
			if (Selection.activeObject == null)
				return;
			var assetPath = FileAndPathUtils.MakeProjectRelative(AssetDatabase.GetAssetPath(Selection.activeObject));
			if (GameDataTracker.IsGameDataFile(assetPath) == false)
				return;

			try
			{
				var selectedAssetType = Selection.activeObject.GetType();
				var inspectorWindowType = typeof(PopupWindow).Assembly.GetType("UnityEditor.InspectorWindow");
				var inspectorWindow = EditorWindow.GetWindow(inspectorWindowType);
				var activeEditorTracker = inspectorWindow.HasProperty("tracker") ?
					inspectorWindow.GetPropertyValue("tracker") : inspectorWindow.GetFieldValue("m_Tracker");
				var customEditorAttributesType = typeof(PopupWindow).Assembly.GetType("UnityEditor.CustomEditorAttributes");
				var customEditorsList = customEditorAttributesType.GetFieldValue("kSCustomEditors") as IList;
				var customEditorsDictionary = customEditorAttributesType.GetFieldValue("kSCustomEditors") as IDictionary;
				var monoEditorType = customEditorAttributesType.GetNestedType("MonoEditorType", BindingFlags.NonPublic);

				// after unity 2018.*
				if (customEditorsDictionary != null)
				{
					var activeEditors = default(IEnumerable);
					foreach (IEnumerable customEditors in customEditorsDictionary.Values)
					{
						foreach (var customEditor in customEditors)
						{
							if (customEditor == null || (Type)customEditor.GetFieldValue("m_InspectedType") != selectedAssetType)
								continue;

							var originalInspectorType = (Type)customEditor.GetFieldValue("m_InspectorType");

							// override inspector
							customEditor.SetFieldValue("m_InspectorType", typeof(GameDataInspector));

							// force rebuild editor list
							activeEditorTracker.Invoke("ForceRebuild");

							activeEditors = (IEnumerable)activeEditorTracker.GetPropertyValue("activeEditors") ?? Enumerable.Empty<object>();
							foreach (Editor activeEditor in activeEditors)
							{
								activeEditor.SetPropertyValue("alwaysAllowExpansion", true);
							}

							// restore original inspector
							customEditor.SetFieldValue("m_InspectorType", originalInspectorType);
							return;
						}
					}

					var newMonoEditorType = Activator.CreateInstance(monoEditorType);
					newMonoEditorType.SetFieldValue("m_InspectedType", selectedAssetType);
					newMonoEditorType.SetFieldValue("m_InspectorType", typeof(GameDataInspector));
					newMonoEditorType.SetFieldValue("m_EditorForChildClasses", false);
					if (monoEditorType.HasField("m_IsFallback"))
						newMonoEditorType.SetFieldValue("m_IsFallback", false);
					var newMonoEditorTypeList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(monoEditorType));
					newMonoEditorTypeList.Add(newMonoEditorType);

					// override inspector
					customEditorsDictionary[selectedAssetType] = newMonoEditorTypeList;

					// force rebuild editor list
					activeEditorTracker.Invoke("ForceRebuild");

					activeEditors = (IEnumerable)activeEditorTracker.GetPropertyValue("activeEditors") ?? Enumerable.Empty<object>();
					foreach (Editor activeEditor in activeEditors)
					{
						activeEditor.SetPropertyValue("alwaysAllowExpansion", true);
					}

					// restore original inspector
					customEditorsDictionary.Remove(selectedAssetType);
				}
				// prior to unity 2018.*
				else if (customEditorsList != null)
				{
					var cachedCustomEditorsByType = customEditorAttributesType.HasField("kCachedEditorForType") ?
						(IDictionary<Type, Type>)customEditorAttributesType.GetFieldValue("kCachedEditorForType") :
						null;

					foreach (var customEditor in customEditorsList)
					{
						if (customEditor == null || (Type)customEditor.GetFieldValue("m_InspectedType") != selectedAssetType)
							continue;

						var originalInspectorType = (Type)customEditor.GetFieldValue("m_InspectorType");

						// override inspector
						customEditor.SetFieldValue("m_InspectorType", typeof(GameDataInspector));
						if (cachedCustomEditorsByType != null)
							cachedCustomEditorsByType[selectedAssetType] = typeof(GameDataInspector);

						// force rebuild editor list
						activeEditorTracker.Invoke("ForceRebuild");
						inspectorWindow.Repaint();

						// restore original inspector
						customEditor.SetFieldValue("m_InspectorType", originalInspectorType);
						if (cachedCustomEditorsByType != null)
							cachedCustomEditorsByType.Remove(selectedAssetType);
						return;
					}

					var newMonoEditorType = Activator.CreateInstance(monoEditorType);
					newMonoEditorType.SetFieldValue("m_InspectedType", selectedAssetType);
					newMonoEditorType.SetFieldValue("m_InspectorType", typeof(GameDataInspector));
					newMonoEditorType.SetFieldValue("m_EditorForChildClasses", false);
					if (monoEditorType.HasField("m_IsFallback"))
						newMonoEditorType.SetFieldValue("m_IsFallback", false);

					// override inspector
					customEditorsList.Insert(0, newMonoEditorType);
					if (cachedCustomEditorsByType != null)
						cachedCustomEditorsByType[selectedAssetType] = typeof(GameDataInspector);
					// force rebuild editor list
					activeEditorTracker.Invoke("ForceRebuild");
					inspectorWindow.Repaint();

					// restore original inspector
					customEditorsList.Remove(newMonoEditorType);
					if (cachedCustomEditorsByType != null)
						cachedCustomEditorsByType.Remove(selectedAssetType);
				}
			}
			catch (Exception updateEditorError)
			{
				if (Settings.Current.Verbose)
					Debug.LogError(updateEditorError);
			}
		}

		/// <inheritdoc />
		public override void OnInspectorGUI()
		{
			this.DrawDefaultInspector();
			
			var gameDataAsset = (Object)this.target;
			var gameDataPath = FileAndPathUtils.MakeProjectRelative(AssetDatabase.GetAssetPath(gameDataAsset));

			var assetPath = FileAndPathUtils.MakeProjectRelative(AssetDatabase.GetAssetPath(Selection.activeObject));
			if (GameDataTracker.IsGameDataFile(assetPath) == false)
			{
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
			this.gameDataSettings.Generator = (int)(GameDataSettings.CodeGenerator)EditorGUILayout.EnumPopup(Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_GENERATOR, (GameDataSettings.CodeGenerator)this.gameDataSettings.Generator);
			if (this.gameDataSettings.Generator != (int)GameDataSettings.CodeGenerator.None)
			{
				this.codeGenerationFold = EditorGUILayout.Foldout(this.codeGenerationFold, Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_GENERATION_LABEL);
				if (this.codeGenerationFold)
				{
					this.gameDataSettings.AutoGeneration = EditorGUILayout.Toggle(Resources.UI_UNITYPLUGIN_INSPECTOR_AUTO_GENERATION,
						this.gameDataSettings.AutoGeneration);
					var codeAsset = !string.IsNullOrEmpty(this.gameDataSettings.CodeGenerationPath) && File.Exists(this.gameDataSettings.CodeGenerationPath) ? AssetDatabase.LoadAssetAtPath<TextAsset>(this.gameDataSettings.CodeGenerationPath) : null;
					var assetAsset = !string.IsNullOrEmpty(this.gameDataSettings.AssetGenerationPath) && File.Exists(this.gameDataSettings.AssetGenerationPath) ? AssetDatabase.LoadAssetAtPath<ScriptableObject>(this.gameDataSettings.AssetGenerationPath) : null;

					if (codeAsset != null)
						this.gameDataSettings.CodeGenerationPath = AssetDatabase.GetAssetPath(EditorGUILayout.ObjectField(Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_GENERATION_PATH, codeAsset, typeof(TextAsset), false));
					else
						this.gameDataSettings.CodeGenerationPath = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_GENERATION_PATH,
							this.gameDataSettings.CodeGenerationPath);

					if (this.gameDataSettings.Generator == (int)GameDataSettings.CodeGenerator.CSharpCodeAndAsset)
					{
						if (assetAsset != null)
							this.gameDataSettings.AssetGenerationPath = AssetDatabase.GetAssetPath(EditorGUILayout.ObjectField(Resources.UI_UNITYPLUGIN_INSPECTOR_ASSET_GENERATION_PATH, assetAsset, typeof(ScriptableObject), false));
						else
							this.gameDataSettings.AssetGenerationPath = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_INSPECTOR_ASSET_GENERATION_PATH, this.gameDataSettings.AssetGenerationPath);
					}

					if ((this.gameDataSettings.Generator == (int)GameDataSettings.CodeGenerator.CSharp ||
						this.gameDataSettings.Generator == (int)GameDataSettings.CodeGenerator.CSharpCodeAndAsset) &&
						Path.GetExtension(this.gameDataSettings.CodeGenerationPath) != ".cs")
						this.gameDataSettings.CodeGenerationPath = Path.ChangeExtension(this.gameDataSettings.CodeGenerationPath, ".cs");
					if (this.gameDataSettings.Generator == (int)GameDataSettings.CodeGenerator.CSharpCodeAndAsset &&
						Path.GetExtension(this.gameDataSettings.AssetGenerationPath) != ".asset")
						this.gameDataSettings.AssetGenerationPath = Path.ChangeExtension(this.gameDataSettings.AssetGenerationPath, ".asset");

					this.gameDataSettings.Namespace = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_NAMESPACE,
						this.gameDataSettings.Namespace);
					this.gameDataSettings.GameDataClassName = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_API_CLASS_NAME,
						this.gameDataSettings.GameDataClassName);
					this.gameDataSettings.DocumentClassName = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_DOCUMENT_CLASS_NAME,
						this.gameDataSettings.DocumentClassName);
					this.gameDataSettings.LineEnding = (int)(GameDataSettings.LineEndings)EditorGUILayout.EnumPopup(
						Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_LINE_ENDINGS, (GameDataSettings.LineEndings)this.gameDataSettings.LineEnding);
					this.gameDataSettings.Indentation = (int)(GameDataSettings.Indentations)EditorGUILayout.EnumPopup(
						Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_INDENTATION, (GameDataSettings.Indentations)this.gameDataSettings.Indentation);
					this.gameDataSettings.Options = (int)(CodeGenerationOptions)EditorGUILayout.EnumMaskField(
						Resources.UI_UNITYPLUGIN_INSPECTOR_CODE_OPTIONS, (CodeGenerationOptions)this.gameDataSettings.Options);
				}
			}

			this.scriptingAssembliesFold = EditorGUILayout.Foldout(this.scriptingAssembliesFold, Resources.UI_UNITYPLUGIN_INSPECTOR_SCRIPTING_ASSEMBLIES_LABEL);
			if (this.scriptingAssembliesFold)
			{
				for (var i = 0; i < this.gameDataSettings.ScriptingAssemblies.Length; i++)
				{
					var watchedAssetPath = this.gameDataSettings.ScriptingAssemblies[i];
					var assetExists = !string.IsNullOrEmpty(watchedAssetPath) && (File.Exists(watchedAssetPath) || Directory.Exists(watchedAssetPath));
					var watchedAsset = assetExists ? AssetDatabase.LoadMainAssetAtPath(watchedAssetPath) : null;
					if (watchedAsset != null)
						this.gameDataSettings.ScriptingAssemblies[i] = AssetDatabase.GetAssetPath(EditorGUILayout.ObjectField(Resources.UI_UNITYPLUGIN_INSPECTOR_ASSET_LABEL, watchedAsset, typeof(Object), false));
					else
						this.gameDataSettings.ScriptingAssemblies[i] = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_INSPECTOR_NAME_LABEL, watchedAssetPath);
				}
				EditorGUILayout.Space();
				this.newScriptingAssembly = EditorGUILayout.ObjectField("<" + Resources.UI_UNITYPLUGIN_INSPECTOR_ADD_ASSET_BUTTON + ">", this.newScriptingAssembly, typeof(Object), false);
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
				if (GUILayout.Button(Resources.UI_UNITYPLUGIN_INSPECTOR_ADD_BUTTON, EditorStyles.toolbarButton, GUILayout.Width(70), GUILayout.Height(18)))
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
			if (this.gameDataSettings.Generator != (int)GameDataSettings.CodeGenerator.None && string.IsNullOrEmpty(this.gameDataSettings.CodeGenerationPath) == false)
			{
				if (GUILayout.Button(Resources.UI_UNITYPLUGIN_INSPECTOR_RUN_GENERATOR_BUTTON))
				{
					CoroutineScheduler.Schedule(Menu.GenerateCodeAndAssetsAsync(gameDataPath, ProgressUtils.ReportToLog(Resources.UI_UNITYPLUGIN_INSPECTOR_GENERATION_PREFIX + " ")), "generation::" + gameDataPath)
						.ContinueWith(_ => this.Repaint());
				}
			}
			GUI.enabled = !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
			if (GUILayout.Button(Resources.UI_UNITYPLUGIN_INSPECTOR_VALIDATE_BUTTON))
			{
				CoroutineScheduler.Schedule(Menu.ValidateAsync(gameDataPath, ProgressUtils.ReportToLog(Resources.UI_UNITYPLUGIN_INSPECTOR_VALIDATION_PREFIX + " ")), "validation::" + gameDataPath)
					.ContinueWith(_ => this.Repaint());
			}
			if (GUILayout.Button(Resources.UI_UNITYPLUGIN_INSPECTOR_BACKUP_BUTTON))
			{
				this.Backup(gameDataPath);
			}
			if (GUILayout.Button(Resources.UI_UNITYPLUGIN_INSPECTOR_RESTORE_BUTTON))
			{
				this.Restore(gameDataPath);
			}
			EditorGUILayout.EndHorizontal();
			GUI.enabled = true;

			if (GUI.changed)
			{
				this.gameDataSettings.Save(gameDataPath);
			}
		}

		private void Restore(string gameDataPath)
		{
			var openFile = EditorUtility.OpenFilePanelWithFilters(Resources.UI_UNITYPLUGIN_INSPECTOR_RESTORE_BUTTON, Path.GetFullPath("./"), GameDataTracker.GameDataExtensionFilters);
			if (string.IsNullOrEmpty(openFile))
				return;

			CharonCli.RestoreAsync(gameDataPath, CommandInput.File(openFile, "auto")).ContinueWith(_ => this.Repaint());
		}
		private void Backup(string gameDataPath)
		{
			var savePath = EditorUtility.SaveFilePanel(Resources.UI_UNITYPLUGIN_INSPECTOR_BACKUP_BUTTON, Path.GetFullPath("./"), Path.GetFileName(gameDataPath), null);
			if (string.IsNullOrEmpty(savePath))
				return;

			var format = default(string);
			switch (Path.GetExtension(savePath))
			{
				case "gdml": format = "xml"; break;
				case "gdmp": format = "msgpack"; break;
				case "gdbs": format = "bson"; break;
				case "gdjs": format = "json"; break;
				default: format = Path.GetExtension(savePath); break;
			}

			CharonCli.BackupAsync(gameDataPath, CommandOutput.File(savePath, format)).ContinueWith(_ => this.Repaint());
		}

	}
}
