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
		[MenuItem("Tools/RPG Game/Create Hero")]
		private static async void CreateHero()
		{
			Debug.Log("Creating Hero document from JObject...");

			var heroDocument = new JObject {
				{ "Id", "SuperBoy" },
				{ "Name", "Super Boy" },
				{ "Religious", false },

				// many fields omitted here

				// list of references example
				{
					"DislikeHeroes", new JArray {
						new JObject { { "Id", "Crossbower" } }
					}
				},

				// list of embedded documents example
				{
					"Armors", new JArray {
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
					}
				}
			};

			var gameDataPath = Path.GetFullPath("Assets/StreamingAssets/RpgGameData.gdjs");

			//
			// Documentation for CreateDocument command and its parameters:
			// https://gamedevware.github.io/charon/advanced/commands/data_create.html
			//
			var createdHeroDocument = await CharonCli.CreateDocumentAsync(
				gameDataPath,
				apiKey: string.Empty,
				schemaNameOrId: "Hero",
				heroDocument
			);

			Debug.Log("Created hero in game data: " + createdHeroDocument.ToString(Formatting.Indented));
		}
	}
}
