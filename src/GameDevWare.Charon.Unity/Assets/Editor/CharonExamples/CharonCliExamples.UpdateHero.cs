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
		[MenuItem("Tools/RPG Game/Update Hero")]
		private static async void UpdateHero()
		{
			Debug.Log("Update Hero by known Id...");

			var heroDocument = new JObject {
				// Id field is mandatory for the update operation.
				{ "Id", "Crossbower" },

				// To update field just specify name and new value.
				{ "Religious", true },

				// When updating collections, you must specify all existing documents in the collection by their ID.
				// Any missing documents will be deleted during the update operation.
				// Sometimes it's easier to find a document through a FindDocumentAsync() operation, update the required field, and UpdateDocumentAsync() the document back.
				{
					"Armors", new JArray {
						new JObject { { "Id", "CrossbowerArmor1" } },
						new JObject { { "Id", "CrossbowerArmor2" } },
						new JObject { { "Id", "CrossbowerArmor3" } },
						new JObject { { "Id", "CrossbowerArmor4" } },
						new JObject {
							{ "Id", "CrossbowerArmor5" },
							{ "Dodge", 40 }
						},
					}
				}
			};

			var gameDataPath = Path.GetFullPath("Assets/StreamingAssets/RpgGameData.gdjs");

			//
			// Documentation for UpdateDocument command and its parameters:
			// https://gamedevware.github.io/charon/advanced/commands/data_update.html
			//
			var updatedHeroDocument = await CharonCli.UpdateDocumentAsync(
				gameDataPath,
				apiKey: string.Empty,
				schemaNameOrId: "Hero",
				heroDocument
			);

			Debug.Log("Updated hero document in game data: " + updatedHeroDocument.ToString(Formatting.Indented));
		}
	}
}
