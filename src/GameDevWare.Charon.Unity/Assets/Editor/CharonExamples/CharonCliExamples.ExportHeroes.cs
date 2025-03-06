using System;
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
		[MenuItem("Tools/RPG Game/Export Heroes")]
		private static async void ExportHeroes()
		{
			Debug.Log("Exporting heroes to JObject...");

			var gameDataPath = Path.GetFullPath("Assets/StreamingAssets/RpgGameData.gdjs");

			//
			// Documentation for Export command and its parameters:
			// https://gamedevware.github.io/charon/advanced/commands/data_export.html
			//
			var exportData = await CharonCli.ExportAsync(
				gameDataPath,
				apiKey: string.Empty,
				schemaNamesOrIds: new[] { "Hero" },
				properties: Array.Empty<string>(),
				languages: Array.Empty<string>(),
				exportMode: ExportMode.Normal
			);

			var heroes = (JArray)exportData["Collections"]["Hero"];
			foreach (var hero in heroes)
			{
				var heroName = hero["Name"]["en-US"];

				Debug.Log("Found hero in game data: " + heroName);
			}
		}
	}
}
