using UnityEditor.AssetImporters;
using UnityEngine;

namespace GameDevWare.Charon.Editor
{
	public abstract class GameDataImporter : ScriptedImporter
	{
		[SerializeField]
		public GameDataSettings lastImportSettings;
		[SerializeField]
		public string lastImportAssetPath;
	}
}
