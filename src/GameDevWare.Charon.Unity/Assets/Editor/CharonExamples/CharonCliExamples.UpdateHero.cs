/*
	Copyright (c) 2025 GameDevWare, Denis Zykov

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

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
			Debug.Log("Updating hero by known ID...");

			var heroDocument = new JObject {
				// The "Id" field is mandatory for the update operation.
				{ "Id", "Crossbower" },

				// To update a field, specify its name and new value.
				{ "Religious", true },

				// When updating collections, you must specify all existing documents in the collection by their ID.
				// Any missing documents will be deleted during the update operation.
				// Sometimes it's easier to find a document using FindDocumentAsync(), update the required field, and then use UpdateDocumentAsync() to save the changes.
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
			// Documentation for the UpdateDocument command and its parameters:
			// https://gamedevware.github.io/charon/advanced/commands/data_update.html
			//
			var updatedHeroDocument = await CharonCli.UpdateDocumentAsync(
				gameDataPath,
				apiKey: string.Empty,
				schemaNameOrId: "Hero",
				heroDocument
			);

			Debug.Log("Successfully updated hero document in game data: " + updatedHeroDocument.ToString(Formatting.Indented));
		}
	}
}
