using System.IO;
using GameDevWare.Charon.Editor.Cli;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

// ReSharper disable PossibleNullReferenceException, AsyncVoidMethod

namespace Editor.CharonExamples
{
	public partial class CharonCliExamples
	{
		[MenuItem("Tools/RPG Game/Delete Hero")]
		private static async void DeleteHero()
		{
			Debug.Log("Deleting Hero document...");

			var heroDocumentToDelete = new JObject {
				{ "Id", "SuperBoy" },
			};

			//
			// Documentation for DeleteDocument command and its parameters:
			// https://gamedevware.github.io/charon/advanced/commands/data_delete.html
			//
			var gameDataPath = Path.GetFullPath("Assets/StreamingAssets/RpgGameData.gdjs");
			var deletedHeroDocument = await CharonCli.DeleteDocumentAsync(
				gameDataPath,
				apiKey: string.Empty,
				schemaNameOrId: "Hero",
				document: heroDocumentToDelete
			);

			Debug.Log("Deleted hero document from game data: " + deletedHeroDocument.ToString(Formatting.Indented));
		}
	}
}
