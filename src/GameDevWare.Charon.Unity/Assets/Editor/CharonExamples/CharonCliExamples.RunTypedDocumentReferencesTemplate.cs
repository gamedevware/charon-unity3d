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
			Debug.Log("Running T4 template ...");

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
					Debug.LogWarning($"T4 tool run failed. Captured Output: {toolRunResult.GetOutputData()},\r\n Captured Error: {toolRunResult.GetErrorData()}.");
				}
				else
				{
					Debug.Log(
						$"T4 tool run succeed. Generated Text: {await File.ReadAllTextAsync(templateResultPath)},\r\n Captured Output: {toolRunResult.GetOutputData()},\r\n Captured Error: {toolRunResult.GetErrorData()}.");
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
