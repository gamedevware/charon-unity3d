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
			Debug.Log("Validating game data ...");

			var gameDataPath = Path.GetFullPath("Assets/StreamingAssets/RpgGameData.gdjs");

			//
			// Documentation for Validate command and its parameters:
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
				select $"{validationRecord.SchemaName}[id: {validationRecord.Id}] {validationError.Code}: {validationError.Message}"
			).ToList();

			if (issues.Count > 0)
			{
				Debug.LogWarning($"Validation is finished. Issues[{issues.Count}]: {string.Join(", ", issues)}.");
			}
			else
			{
				Debug.Log("Validation is finished. No issues.");
			}
		}
	}
}
