using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using GameDevWare.Charon.Unity.Utils;
using UnityEngine;

namespace Assets.Scripts.Editor
{
	public static class Tools
	{
		[MenuItem("Tools/RPG Game/Export Heroes")]
		private static async void ExportHeroes()
		{
			var output = CommandOutput.CaptureJson();
			
			var gameDataPath = new GameDataLocation("Assets/StreamingAssets/RpgGameData.gdjs"); // relative to Project root directory
			var exportResult = await CharonCli.ExportAsync(gameDataPath, new[] { "Hero" }, output, ExportMode.Normal);

			if (exportResult.ExitCode != 0)
			{
				Debug.LogError("Failed to export heroes from game data: " + exportResult.GetErrorData());
				return;
			}
			
			var exportData = output.ReadJsonObject();
			// or use your own deserializer
			// var jsonData = exportResult.GetOutputData();
			
			var document = exportData["document"] as IDictionary<string, object>; // WARN: 'document' item will be replaced with 'Collections' in 2025
			var collections = document["Collections"] as IDictionary<string, object>;
			var heroes = collections["Hero"] as IList<object>;
			foreach (IDictionary<string, object> hero in heroes)
			{
				var heroNameLocString = hero["Name"] as IDictionary<string, object>;
				Debug.Log("Found hero in game data: "  + heroNameLocString["en-US"]);
			}
		}
		
		[MenuItem("Tools/RPG Game/List Items")]
		private static async void ListItems()
		{
			var output = CommandOutput.CaptureJson();
			
			var gameDataPath = new GameDataLocation("Assets/StreamingAssets/RpgGameData.gdjs"); // relative to Project root directory
			var listResult = await CharonCli.ListAsync(gameDataPath, "Item", output, path: "*" /* list all items including embedded */);

			if (listResult.ExitCode != 0)
			{
				Debug.LogError("Failed to list items from game data: " + listResult.GetErrorData());
				return;
			}
			
			var listData = output.ReadJsonObject();
			// or use your own deserializer
			// var jsonData = exportResult.GetOutputData();
			
			Debug.Log(listResult.GetOutputData());
			
			var items = listData["documents"] as IList<object>; // WARN: 'document' item will be replaced with 'Collections' in 2025
			var itemNames = new List<string>();
			foreach (IDictionary<string, object> item in items)
			{
				var itemNameLocString = item["Name"] as IDictionary<string, object>;
				itemNames.Add(itemNameLocString["en-US"] as string);
			}
			Debug.Log($"Found {itemNames.Count} items in game data: "  + string.Join(", ", itemNames));

		}
	}
}