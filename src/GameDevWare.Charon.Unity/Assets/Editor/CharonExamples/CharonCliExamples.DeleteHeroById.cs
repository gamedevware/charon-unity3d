using System.IO;
using GameDevWare.Charon.Editor.Cli;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

// ReSharper disable PossibleNullReferenceException, AsyncVoidMethod

namespace Editor.CharonExamples
{
	public partial class CharonCliExamples
	{
		[MenuItem("Tools/RPG Game/Delete Hero By Id")]
		private static async void DeleteHeroById()
		{
			Debug.Log("Deleting Hero by known Id...");

			var gameDataPath = Path.GetFullPath("Assets/StreamingAssets/RpgGameData.gdjs");

			//
			// Documentation for DeleteDocument command and its parameters:
			// https://gamedevware.github.io/charon/advanced/commands/data_delete.html
			//
			var deletedHeroDocument = await CharonCli.DeleteDocumentAsync(
				gameDataPath,
				apiKey: string.Empty,
				schemaNameOrId: "Hero",
				id: "SuperBoy"
			);

			Debug.Log("Deleted hero document from game data: " + deletedHeroDocument.ToString(Formatting.Indented));
		}
	}
}
