using System.IO;
using GameDevWare.Charon.Editor.Cli;
using UnityEditor;
using UnityEngine;

namespace Editor.CharonExamples
{
	public partial class CharonCliExamples
	{
		[MenuItem("Tools/RPG Game/Preprocess FileList.tt Template")]
		private static async void PreprocessTemplateIntoGenerator()
		{
			Debug.Log("Running T4 tool to get generator's source code ...");

			var outputFilePath = Path.GetTempFileName();
			var templatePath = Path.GetFullPath("Assets/Editor/CharonExamples/FileList.tt");
			var toolRunResult = await CharonCli.PreprocessT4Async(
				templatePath,
				outputFile: outputFilePath,
				templateClassName: "FileList"
			);

			if (toolRunResult.ExitCode != 0)
			{
				Debug.LogWarning($"T4 tool run failed. Captured Output: {toolRunResult.GetOutputData()},\r\n Captured Error: {toolRunResult.GetErrorData()}.");
			}
			else
			{
				Debug.Log(
					$"T4 tool run succeed. Source Code: {await File.ReadAllTextAsync(outputFilePath)},\r\n Captured Output: {toolRunResult.GetOutputData()},\r\n Captured Error: {toolRunResult.GetErrorData()}.");
			}

			File.Delete(outputFilePath);
		}
	}
}
