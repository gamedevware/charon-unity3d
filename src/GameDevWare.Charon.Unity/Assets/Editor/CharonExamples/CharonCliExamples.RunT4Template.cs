using System.Collections.Generic;
using System.IO;
using GameDevWare.Charon.Editor.Cli;
using UnityEditor;
using UnityEngine;

// ReSharper disable PossibleNullReferenceException, AsyncVoidMethod

namespace Editor.CharonExamples
{
	public partial class CharonCliExamples
	{
		[MenuItem("Tools/RPG Game/Transform FileList.tt Template")]
		private static async void RunFileListTemplate()
		{
			Debug.Log("Running T4 template ...");

			var templatePath = Path.GetFullPath("Assets/Editor/CharonExamples/FileList.tt");
			var templateResultPath = Path.GetFullPath("Assets/Editor/CharonExamples/FileList.json");
			var toolRunResult = await CharonCli.RunT4Async(
				templatePath,

				// outputFile: templateResultPath, looks like T4 tool doesn't respect this parameter when running T4 template.
				parameters: new Dictionary<string, string> {
					{ "rootNodeNameParameter", "list" }
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

			File.Delete(templateResultPath);
		}
	}
}
