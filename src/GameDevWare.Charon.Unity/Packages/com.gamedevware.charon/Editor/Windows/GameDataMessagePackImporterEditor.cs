using GameDevWare.Charon;
using UnityEditor;
using UnityEditor.AssetImporters;

namespace Editor.Windows
{
	[CustomEditor(typeof(GameDataMessagePackImporter))]
	public class GameDataMessagePackImporterEditor: ScriptedImporterEditor
	{
		public override void OnInspectorGUI()
		{
			if (this.assetTargets.Length == 1)
			{
				GameDataJsonImporterEditor.InspectorGUI(this, this.assetTarget);
			}
			this.ApplyRevertGUI();
		}
	}
}
