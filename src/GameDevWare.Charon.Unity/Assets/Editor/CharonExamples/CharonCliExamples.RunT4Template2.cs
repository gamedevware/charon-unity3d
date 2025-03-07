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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Scripts;
using GameDevWare.Charon.Editor.Cli;
using UnityEditor;
using UnityEngine;

// ReSharper disable PossibleNullReferenceException, AsyncVoidMethod

namespace Editor.CharonExamples
{
	public partial class CharonCliExamples
	{
		[MenuItem("Tools/RPG Game/Transform TypedDocumentReferences.tt Template")]
		private static async void RunTypedDocumentReferencesTemplate()
		{
			Debug.Log("Running T4 template...");

			var templatePath = Path.GetFullPath("Assets/Editor/CharonExamples/TypedDocumentReferences.tt");
			var templateResultPath = Path.GetFullPath("Assets/Editor/CharonExamples/TypedDocumentReferences.cs");
			try
			{
				var documentTypes = typeof(Document).Assembly.GetTypes().Where(type => type.BaseType == typeof(Document)).Select(type => type.FullName);
				var toolRunResult = await CharonCli.RunT4Async(
					templatePath,
					parameters: new Dictionary<string, string> {
						{ "documentsTypes", string.Join(",", documentTypes) },
					}
				);

				if (toolRunResult.ExitCode != 0)
				{
					Debug.LogWarning($"T4 tool run failed. Captured Output: {toolRunResult.GetOutputData()},\r\nCaptured Error: {toolRunResult.GetErrorData()}.");
				}
				else
				{
					Debug.Log(
						$"T4 tool run succeeded. Generated Text: {await File.ReadAllTextAsync(templateResultPath)},\r\nCaptured Output: {toolRunResult.GetOutputData()},\r\nCaptured Error: {toolRunResult.GetErrorData()}.");
				}

				var targetFilePath = Path.GetFullPath("Assets/Scripts/TypedDocumentReferences.cs");
				if (File.Exists(targetFilePath))
				{
					File.Delete(targetFilePath);
				}

				File.Move(templateResultPath, targetFilePath);
			}
			finally
			{
				if (File.Exists(templateResultPath))
				{
					File.Delete(templateResultPath);
				}
			}
		}
	}
}
