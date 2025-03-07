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
		[MenuItem("Tools/RPG Game/Bulk Delete Heroes")]
		private static async void BulkDeleteHeroes()
		{
			Debug.Log("Bulk deleting hero documents...");

			var importData = new JObject {
				{
					"Collections", new JObject {
						{
							"Hero", new JArray {
								// only Ids are required for mass delete
								new JObject { { "Id", "Crossbower" } },
								new JObject { { "Id", "Monstrosity" } },
								new JObject { { "Id", "Zealot" } },
							}

							// You can delete multiple document types in one ImportAsync command
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
				ImportMode.Delete
			);

			Debug.Log("Successfully bulk deleted documents.");
		}
	}
}
