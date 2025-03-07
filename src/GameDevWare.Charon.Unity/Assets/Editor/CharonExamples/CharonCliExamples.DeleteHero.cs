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
		[MenuItem("Tools/RPG Game/Delete Hero")]
		private static async void DeleteHero()
		{
			Debug.Log("Deleting hero document...");

			var heroDocumentToDelete = new JObject {
				{ "Id", "SuperBoy" },
			};

			var gameDataPath = Path.GetFullPath("Assets/StreamingAssets/RpgGameData.gdjs");

			//
			// Documentation for the DeleteDocument command and its parameters:
			// https://gamedevware.github.io/charon/advanced/commands/data_delete.html
			//
			var deletedHeroDocument = await CharonCli.DeleteDocumentAsync(
				gameDataPath,
				apiKey: string.Empty,
				schemaNameOrId: "Hero",
				document: heroDocumentToDelete
			);

			Debug.Log("Successfully deleted hero document from game data: " + deletedHeroDocument.ToString(Formatting.Indented));
		}
	}
}
