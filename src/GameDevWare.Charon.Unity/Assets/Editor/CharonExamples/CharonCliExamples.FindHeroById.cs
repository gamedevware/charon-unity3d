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
		[MenuItem("Tools/RPG Game/Find Hero")]
		private static async void FindHeroById()
		{
			Debug.Log("Find Hero by known Id...");

			var gameDataPath = Path.GetFullPath("Assets/StreamingAssets/RpgGameData.gdjs");

			//
			// Documentation for FindDocument command and its parameters:
			// https://gamedevware.github.io/charon/advanced/commands/data_find.html
			//
			var foundHeroDocument = await CharonCli.FindDocumentAsync(
				gameDataPath,
				apiKey: string.Empty,
				schemaNameOrId: "Hero",
				id: "Crossbower"
			);

			Debug.Log("Found hero document in game data: " + foundHeroDocument.ToString(Formatting.Indented));
		}
	}
}
