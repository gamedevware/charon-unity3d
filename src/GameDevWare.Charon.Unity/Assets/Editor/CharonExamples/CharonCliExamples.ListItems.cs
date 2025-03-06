using System.IO;
using System.Linq;
using GameDevWare.Charon.Editor.Cli;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

// ReSharper disable PossibleNullReferenceException, AsyncVoidMethod

namespace Editor.CharonExamples
{
	public partial class CharonCliExamples
	{
		[MenuItem("Tools/RPG Game/List Items (Including Embedded)")]
		private static async void ListItems()
		{
			Debug.Log("List Items including embedded ...");

			var gameDataPath = Path.GetFullPath("Assets/StreamingAssets/RpgGameData.gdjs");

			//
			// Documentation for ListDocuments command and its parameters:
			// https://gamedevware.github.io/charon/advanced/commands/data_list.html
			//
			var listData = await CharonCli.ListDocumentsAsync(
				gameDataPath,
				apiKey: string.Empty,
				schemaNameOrId: "Item",
				path: "*" // This option causes the query to return all documents, including embedded ones.
			);

			var items = (JArray)listData["Collections"]["Item"];

			var foundItemsNames = items!.Select(item => item["Name"]["en-US"].ToString()).ToArray();
			Debug.Log($"Found items in game data [{foundItemsNames.Length}]: " + string.Join(", ", foundItemsNames));
		}
	}
}
