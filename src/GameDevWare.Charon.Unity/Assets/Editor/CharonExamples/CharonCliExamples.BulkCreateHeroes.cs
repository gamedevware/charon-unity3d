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
		[MenuItem("Tools/RPG Game/Bulk Create Heroes")]
		private static async void BulkCreateHeroes()
		{
			Debug.Log("Creating multiple hero documents ...");

			var importData = new JObject {
				{
					"Collections", new JObject {
						{
							"Hero", new JArray {
								new JObject {
									{ "Id", "SuperBoy" },
									{ "Name", "Super Boy" },
									{ "Religious", false },
								},
								new JObject {
									{ "Id", "WounderBobby" },
									{ "Name", "Wounder Boy" },
									{ "Religious", true },
								}
							}
							// You can create/update multiple document types in one ImportAsync command
							// "Items": [
							// ...
							// ]
						}
					}
				}
			};

			var gameDataPath = Path.GetFullPath("Assets/StreamingAssets/RpgGameData.gdjs");

			//
			// Documentation for Import command and its parameters:
			// https://gamedevware.github.io/charon/advanced/commands/data_import.html
			//
			await CharonCli.ImportAsync(
				gameDataPath,
				apiKey: string.Empty,
				schemaNamesOrIds: new[] { "*" },
				importData,
				ImportMode.CreateAndUpdate
			);

			Debug.Log("Successfully created documents.");
		}
	}
}
