using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GameDevWare.Charon.Editor.Cli;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

// ReSharper disable PossibleNullReferenceException, AsyncVoidMethod

namespace Editor.CharonExamples
{
	public class CharonCliExamples
	{
		[MenuItem("Tools/RPG Game/Export Heroes")]
		private static async void ExportHeroes()
		{
			Debug.Log("Exporting heroes to JObject...");

			var gameDataPath = Path.GetFullPath("Assets/StreamingAssets/RpgGameData.gdjs");
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

		[MenuItem("Tools/RPG Game/Export Heroes to File")]
		private static async void ExportHeroesToFile()
		{
			Debug.Log("Exporting heroes to a file...");

			var exportPath = Path.GetTempFileName();
			var gameDataPath = Path.GetFullPath("Assets/StreamingAssets/RpgGameData.gdjs");

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

		[MenuItem("Tools/RPG Game/Export Localizable Data")]
		private static async void ExportLocalizableData()
		{
			Debug.Log("Exporting localizable data to XLSX file...");

			var xlsxFilePath = Path.Combine(Path.GetTempPath(), $"en_US_TO_es_ES_{Guid.NewGuid():N}.xlsx");
			var gameDataPath = Path.GetFullPath("Assets/StreamingAssets/RpgGameData.gdjs");

			await CharonCli.I18NExportToFileAsync(
				gameDataPath,
				apiKey: string.Empty,
				schemaNamesOrIds: new[] { "*" },
				sourceLanguage: "en-US",
				targetLanguage: "es-ES",
				exportedDocumentsFilePath: xlsxFilePath,
				format: ExportFormat.Xslx
			);

			if (File.Exists(xlsxFilePath))
			{
				Debug.Log($"Localizable data successfully exported to the file ({new FileInfo(xlsxFilePath).Length} bytes): {xlsxFilePath}");
				File.Delete(xlsxFilePath);
			}
		}

		[MenuItem("Tools/RPG Game/Import Hero")]
		private static async void ImportHero()
		{
			Debug.Log("Creating Hero document from JObject...");

			var importData = new JObject {
				{
					"Collections", new JObject {
						{
							"Hero", new JArray {

								new JObject
								{
									{ "Id", "SuperBoy" },
									{ "Name", "Super Boy" },
									{ "Religious", false },
								},
								new JObject
								{
									{ "Id", "WounderBobby" },
									{ "Name", "Wounder Boy" },
									{ "Religious", true },
								}
							}
						}
					}
				}
			};

			var gameDataPath = Path.GetFullPath("Assets/StreamingAssets/RpgGameData.gdjs");
			await CharonCli.ImportAsync(
				gameDataPath,
				apiKey: string.Empty,
				schemaNamesOrIds: new []{ "*" },
				importData,
				ImportMode.CreateAndUpdate
			);

			Debug.Log("Successfully imported documents.");

		}

		[MenuItem("Tools/RPG Game/Create Hero")]
		private static async void CreateHero()
		{
			Debug.Log("Creating Hero document from JObject...");

			var heroDocument = new JObject
			{
				{ "Id", "SuperBoy" },
				{ "Name", "Super Boy" },
				{ "Religious", false },

				// many fields omitted here

				// list of references example
				{ "DislikeHeroes", new JArray {
					new JObject { { "Id", "Crossbower" }}
				} },

				// list of embedded documents example
				{ "Armors", new JArray {
					new JObject {
						{ "Id", "SuperBoyArmor1" },
						{ "Name", "Textile Armor" },
						{ "Dodge", 0 },
						{ "HitPoints", 1 }
					},
					new JObject {
						{ "Id", "SuperBoyArmor2" },
						{ "Name", "Kevlar Armor" },
						{ "Dodge", 0.1D },
						{ "HitPoints", 4 }
					}
				}}
			};

			var gameDataPath = Path.GetFullPath("Assets/StreamingAssets/RpgGameData.gdjs");
			var createdHeroDocument = await CharonCli.CreateDocumentAsync(
				gameDataPath,
				apiKey: string.Empty,
				schemaNameOrId: "Hero",
				heroDocument
			);

			Debug.Log("Created hero in game data: " + createdHeroDocument.ToString(Formatting.Indented));

		}

		[MenuItem("Tools/RPG Game/Delete Hero By Id")]
		private static async void DeleteHeroById()
		{
			Debug.Log("Deleting Hero by known Id...");

			var gameDataPath = Path.GetFullPath("Assets/StreamingAssets/RpgGameData.gdjs");
			var deletedHeroDocument = await CharonCli.DeleteDocumentAsync(
				gameDataPath,
				apiKey: string.Empty,
				schemaNameOrId: "Hero",
				id: "SuperBoy"
			);

			Debug.Log("Deleted hero document from game data: " + deletedHeroDocument.ToString(Formatting.Indented));
		}

		[MenuItem("Tools/RPG Game/Delete Hero")]
		private static async void DeleteHero()
		{
			Debug.Log("Deleting Hero document...");

			var heroDocumentToDelete = new JObject {
				{ "Id", "SuperBoy" },
			};

			var gameDataPath = Path.GetFullPath("Assets/StreamingAssets/RpgGameData.gdjs");
			var deletedHeroDocument = await CharonCli.DeleteDocumentAsync(
				gameDataPath,
				apiKey: string.Empty,
				schemaNameOrId: "Hero",
				document: heroDocumentToDelete
			);

			Debug.Log("Deleted hero document from game data: " + deletedHeroDocument.ToString(Formatting.Indented));
		}

		[MenuItem("Tools/RPG Game/Update Hero")]
		private static async void UpdateHero()
		{
			Debug.Log("Update Hero by known Id...");

			var heroDocument = new JObject
			{
				// Id field is mandatory for the update operation.
				{ "Id", "Crossbower" },

				// To update field just specify name and new value.
				{ "Religious", true },

				// When updating collections, you must specify all existing documents in the collection by their ID.
				// Any missing documents will be deleted during the update operation.
				// Sometimes it's easier to find a document through a FindDocumentAsync() operation, update the required field, and UpdateDocumentAsync() the document back.
				{ "Armors", new JArray {
					new JObject { { "Id", "CrossbowerArmor1" } },
					new JObject { { "Id", "CrossbowerArmor2" } },
					new JObject { { "Id", "CrossbowerArmor3" } },
					new JObject { { "Id", "CrossbowerArmor4" } },
					new JObject {
						{ "Id", "CrossbowerArmor5" },
						{ "Dodge", 40 }
					},
				}}
			};

			var gameDataPath = Path.GetFullPath("Assets/StreamingAssets/RpgGameData.gdjs");
			var updatedHeroDocument = await CharonCli.UpdateDocumentAsync(
				gameDataPath,
				apiKey: string.Empty,
				schemaNameOrId: "Hero",
				heroDocument
			);

			Debug.Log("Updated hero document in game data: " + updatedHeroDocument.ToString(Formatting.Indented));
		}

		[MenuItem("Tools/RPG Game/Find Hero")]
		private static async void FindHeroById()
		{
			Debug.Log("Find Hero by known Id...");

			var gameDataPath = Path.GetFullPath("Assets/StreamingAssets/RpgGameData.gdjs");
			var foundHeroDocument = await CharonCli.FindDocumentAsync(
				gameDataPath,
				apiKey: string.Empty,
				schemaNameOrId: "Hero",
				id: "Crossbower"
			);

			Debug.Log("Found hero document in game data: " + foundHeroDocument.ToString(Formatting.Indented));
		}

		[MenuItem("Tools/RPG Game/List Heroes")]
		private static async void ListReligiousHeroes()
		{
			Debug.Log("List Hero by \"Religious\" == true ...");

			var gameDataPath = Path.GetFullPath("Assets/StreamingAssets/RpgGameData.gdjs");
			var listData = await CharonCli.ListDocumentsAsync(
				gameDataPath,
				apiKey: string.Empty,
				schemaNameOrId: "Hero",
				filters: new [] { new ListFilter("Religious", ListFilterOperation.Equal, "true") },
				sorters: new [] { new ListSorter("Name.en-US", ListSorterDirection.Ascending) }
			);

			var heroes = (JArray)listData["Collections"]["Hero"];
			foreach (var hero in heroes)
			{
				var heroName = hero["Name"]["en-US"];

				Debug.Log("Found religious hero in game data: " + heroName);
			}
		}

		[MenuItem("Tools/RPG Game/List Items (Including Embedded)")]
		private static async void ListItems()
		{
			Debug.Log("List Items including embedded ...");

			var gameDataPath = Path.GetFullPath("Assets/StreamingAssets/RpgGameData.gdjs");
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

		[MenuItem("Tools/RPG Game/Validate Game Data")]
		private static async void ValidateGameData()
		{
			Debug.Log("Validating game data ...");

			var gameDataPath = Path.GetFullPath("Assets/StreamingAssets/RpgGameData.gdjs");
			var validationReport = await CharonCli.ValidateAsync(
				gameDataPath,
				apiKey: string.Empty,
				ValidationOptions.AllIntegrityChecks | ValidationOptions.CheckTranslation
			);

			var issues = (
				from validationRecord in validationReport.Records
				from validationError in validationRecord.Errors
				select $"{validationRecord.SchemaName}[id: {validationRecord.Id}] {validationError.Code}: {validationError.Message}"
			).ToList();

			if (issues.Count > 0)
			{
				Debug.LogWarning($"Validation is finished. Issues[{issues.Count}]: {string.Join(", ", issues)}.");
			}
			else
			{
				Debug.Log($"Validation is finished. No issues.");
			}
		}

		[MenuItem("Tools/RPG Game/Transform T4 Template")]
		private static async void RunT4Template()
		{
			Debug.Log("Running T4 template ...");

			var templatePath = Path.GetFullPath("Assets/Editor/CharonExamples/FileList.tt");
			var templateResultPath = Path.GetFullPath("Assets/Editor/CharonExamples/FileList.json");
			var toolRunResult = await CharonCli.RunT4Async(
				templatePath,
				// outputFile: templateResultPath, looks like T4 tool doesn't respect this parameter when running T4 template.
				parameters: new Dictionary<string, string> {
					{ "rootNodeNameParameter", "list" }
				}
			);

			if (toolRunResult.ExitCode != 0)
			{
				Debug.LogWarning($"T4 tool run failed. Captured Output: {toolRunResult.GetOutputData()},\r\n Captured Error: {toolRunResult.GetErrorData()}.");
			}
			else
			{
				Debug.Log($"T4 tool run succeed. Generated Text: {await File.ReadAllTextAsync(templateResultPath)},\r\n Captured Output: {toolRunResult.GetOutputData()},\r\n Captured Error: {toolRunResult.GetErrorData()}.");
			}

			File.Delete(templateResultPath);
		}

		[MenuItem("Tools/RPG Game/Preprocess T4 Template")]
		private static async void GetT4TemplateGenerator()
		{
			Debug.Log("Running T4 tool to get generator's source code ...");

			var outputFilePath = Path.GetTempFileName();
			var templatePath = Path.GetFullPath("Assets/Editor/CharonExamples/FileList.tt");
			var toolRunResult = await CharonCli.RunT4Async(
				templatePath,
				outputFile: outputFilePath,
				templateClassName: "FileList"
			);

			if (toolRunResult.ExitCode != 0)
			{
				Debug.LogWarning($"T4 tool run failed. Captured Output: {toolRunResult.GetOutputData()},\r\n Captured Error: {toolRunResult.GetErrorData()}.");
			}
			else
			{
				Debug.Log($"T4 tool run succeed. Source Code: {await File.ReadAllTextAsync(outputFilePath)},\r\n Captured Output: {toolRunResult.GetOutputData()},\r\n Captured Error: {toolRunResult.GetErrorData()}.");
			}
			File.Delete(outputFilePath);
		}
	}
}
