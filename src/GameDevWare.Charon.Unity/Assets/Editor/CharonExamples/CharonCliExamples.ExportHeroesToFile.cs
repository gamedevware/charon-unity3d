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
			// Documentation for the Export command and its parameters:
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
