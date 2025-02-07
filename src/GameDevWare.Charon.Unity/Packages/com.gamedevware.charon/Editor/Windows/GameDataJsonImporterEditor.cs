using System.Threading;
using GameDevWare.Charon;
using GameDevWare.Charon.Editor;
using GameDevWare.Charon.Editor.Utils;
using GameDevWare.Charon.Editor.Windows;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using Resources = GameDevWare.Charon.Resources;
using UnityObject = UnityEngine.Object;

namespace Editor.Windows
{
	[CustomEditor(typeof(GameDataJsonImporter))]
	public class GameDataJsonImporterEditor: ScriptedImporterEditor
	{
		public override void OnInspectorGUI()
		{
			if (this.assetTargets.Length == 1)
			{
				InspectorGUI(this, this.assetTarget);
			}
			this.ApplyRevertGUI();
		}

		public static void InspectorGUI(ScriptedImporterEditor editor, UnityObject assetTarget)
		{
			editor.serializedObject.Update();

			GUI.enabled = !CharonEditorModule.Instance.Routines.IsRunning && !EditorApplication.isCompiling;

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			if (GUILayout.Button(Resources.UI_UNITYPLUGIN_INSPECTOR_REIMPORT_BUTTON, GUI.skin.button, GUILayout.Width(80), GUILayout.Height(25)))
			{
				var progressCallback = ProgressUtils.ShowProgressBar(Resources.UI_UNITYPLUGIN_PROGRESS_IMPORTING);
				var importTask = CharonEditorModule.Instance.Routines.Schedule(() => GameDataAssetInspector.RunImportAsync(assetTarget, progressCallback), CancellationToken.None);
				importTask.ContinueWithHideProgressBar();
				importTask.LogFaultAsError();
			}
			EditorGUILayout.EndHorizontal();
			GUI.enabled = true;
		}


	}
}
