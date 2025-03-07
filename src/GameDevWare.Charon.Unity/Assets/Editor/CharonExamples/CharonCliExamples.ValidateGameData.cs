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
using System.Linq;
using GameDevWare.Charon.Editor.Cli;
using UnityEditor;
using UnityEngine;

// ReSharper disable PossibleNullReferenceException, AsyncVoidMethod

namespace Editor.CharonExamples
{
	public partial class CharonCliExamples
	{
		[MenuItem("Tools/RPG Game/Validate Game Data")]
		private static async void ValidateGameData()
		{
			Debug.Log("Validating game data...");

			var gameDataPath = Path.GetFullPath("Assets/StreamingAssets/RpgGameData.gdjs");

			//
			// Documentation for the Validate command and its parameters:
			// https://gamedevware.github.io/charon/advanced/commands/data_validate.html
			//
			var validationReport = await CharonCli.ValidateAsync(
				gameDataPath,
				apiKey: string.Empty,
				ValidationOptions.AllIntegrityChecks | ValidationOptions.CheckTranslation
			);

			var issues = (
				from validationRecord in validationReport.Records
				from validationError in validationRecord.Errors
				select $"{validationRecord.SchemaName}[id: {validationRecord.Id}] {validationError.Path}: {validationError.Message}"
			).ToList();

			if (issues.Count > 0)
			{
				Debug.LogWarning($"Validation completed. Found {issues.Count} issues: {string.Join(", ", issues)}.");
			}
			else
			{
				Debug.Log("Validation completed. No issues found.");
			}
		}
	}
}
