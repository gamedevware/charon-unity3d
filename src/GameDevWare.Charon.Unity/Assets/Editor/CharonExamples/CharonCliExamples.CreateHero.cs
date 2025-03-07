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
		[MenuItem("Tools/RPG Game/Create Hero")]
		private static async void CreateHero()
		{
			Debug.Log("Creating hero document from JObject...");

			var heroDocument = new JObject {
				{ "Id", "SuperBoy" },
				{ "Name", "Super Boy" },
				{ "Religious", false },

				// Many fields omitted here for brevity.

				// Example of a list of references
				{
					"DislikeHeroes", new JArray {
						new JObject { { "Id", "Crossbower" } }
					}
				},

				// Example of a list of embedded documents
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
			// Documentation for the CreateDocument command and its parameters:
			// https://gamedevware.github.io/charon/advanced/commands/data_create.html
			//
			var createdHeroDocument = await CharonCli.CreateDocumentAsync(
				gameDataPath,
				apiKey: string.Empty,
				schemaNameOrId: "Hero",
				heroDocument
			);

			Debug.Log("Successfully created hero in game data: " + createdHeroDocument.ToString(Formatting.Indented));
		}
	}
}
