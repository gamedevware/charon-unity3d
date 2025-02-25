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

using System.Threading;
using GameDevWare.Charon.Editor.Routines;
using GameDevWare.Charon.Editor.Utils;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace GameDevWare.Charon.Editor.Windows
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
				var importTask = ReimportAssetsRoutine.ScheduleAsync(new[] { AssetDatabase.GetAssetPath(assetTarget) }, progressCallback, CancellationToken.None);
				importTask.ContinueWithHideProgressBar();
				importTask.LogFaultAsError();
			}
			EditorGUILayout.EndHorizontal();
			GUI.enabled = true;
		}


	}
}
