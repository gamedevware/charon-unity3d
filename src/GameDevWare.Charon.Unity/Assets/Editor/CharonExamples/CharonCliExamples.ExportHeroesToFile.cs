using System;
using System.IO;
using System.Text;
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
		[MenuItem("Tools/RPG Game/Export Heroes to File")]
		private static async void ExportHeroesToFile()
		{
			Debug.Log("Exporting heroes to a file...");

			var exportPath = Path.GetTempFileName();
			var gameDataPath = Path.GetFullPath("Assets/StreamingAssets/RpgGameData.gdjs");

			//
			// Documentation for Export command and its parameters:
			// https://gamedevware.github.io/charon/advanced/commands/data_export.html
			//
			await CharonCli.ExportToFileAsync(
				gameDataPath,
				apiKey: string.Empty,
				schemaNamesOrIds: new[] { "Hero" },
				properties: Array.Empty<string>(),
				languages: Array.Empty<string>(),
				exportMode: ExportMode.Normal,
				exportedDocumentsFilePath: exportPath,
				ExportFormat.Json
			);

			JObject exportData;
			using (JsonReader jsonReader = new JsonTextReader(new StreamReader(File.OpenRead(exportPath), Encoding.UTF8)) { CloseInput = true })
			{
				exportData = await JObject.LoadAsync(jsonReader);
			}

			File.Delete(exportPath);

			var heroes = (JArray)exportData["Collections"]["Hero"];
			foreach (var hero in heroes)
			{
				var heroName = hero["Name"]["en-US"];

				Debug.Log("Found hero in game data: " + heroName);
			}
		}
	}
}
