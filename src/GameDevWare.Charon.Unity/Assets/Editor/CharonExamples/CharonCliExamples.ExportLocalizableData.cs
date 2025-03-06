using System;
using System.IO;
using GameDevWare.Charon.Editor.Cli;
using UnityEditor;
using UnityEngine;

// ReSharper disable PossibleNullReferenceException, AsyncVoidMethod

namespace Editor.CharonExamples
{
	public partial class CharonCliExamples
	{
		[MenuItem("Tools/RPG Game/Export Localizable Data")]
		private static async void ExportLocalizableData()
		{
			Debug.Log("Exporting localizable data to XLSX file...");

			var xlsxFilePath = Path.Combine(Path.GetTempPath(), $"en_US_TO_es_ES_{Guid.NewGuid():N}.xlsx");
			var gameDataPath = Path.GetFullPath("Assets/StreamingAssets/RpgGameData.gdjs");

			//
			// Documentation for I18NExport command and its parameters:
			// https://gamedevware.github.io/charon/advanced/commands/data_i18n_export.html
			//
			await CharonCli.I18NExportToFileAsync(
				gameDataPath,
				apiKey: string.Empty,
				schemaNamesOrIds: new[] { "*" },
				sourceLanguage: "en-US",
				targetLanguage: "es-ES",
				exportedDocumentsFilePath: xlsxFilePath,
				format: ExportFormat.Xslx
			);

			if (File.Exists(xlsxFilePath))
			{
				Debug.Log($"Localizable data successfully exported to the file ({new FileInfo(xlsxFilePath).Length} bytes): {xlsxFilePath}");
				File.Delete(xlsxFilePath);
			}
		}
	}
}
