using System.IO;
using GameDevWare.Charon.Editor.Cli;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

// ReSharper disable PossibleNullReferenceException, AsyncVoidMethod

namespace Editor.CharonExamples
{
	public partial class CharonCliExamples
	{
		[MenuItem("Tools/RPG Game/List Heroes")]
		private static async void ListReligiousHeroes()
		{
			Debug.Log("List Hero by \"Religious\" == true ...");

			var gameDataPath = Path.GetFullPath("Assets/StreamingAssets/RpgGameData.gdjs");

			//
			// Documentation for ListDocuments command and its parameters:
			// https://gamedevware.github.io/charon/advanced/commands/data_list.html
			//
			var listData = await CharonCli.ListDocumentsAsync(
				gameDataPath,
				apiKey: string.Empty,
				schemaNameOrId: "Hero",
				filters: new[] { new ListFilter("Religious", ListFilterOperation.Equal, "true") },
				sorters: new[] { new ListSorter("Name.en-US", ListSorterDirection.Ascending) }
			);

			var heroes = (JArray)listData["Collections"]["Hero"];
			foreach (var hero in heroes)
			{
				var heroName = hero["Name"]["en-US"];

				Debug.Log("Found religious hero in game data: " + heroName);
			}
		}
	}
}
