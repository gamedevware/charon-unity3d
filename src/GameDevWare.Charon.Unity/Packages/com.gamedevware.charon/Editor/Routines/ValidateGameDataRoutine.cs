/*
	Copyright (c) 2025 Denis Zykov

	This is part of "Charon: Game Data Editor" Unity Plugin.

	Charon Game Data Editor Unity Plugin is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see http://www.gnu.org/licenses.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GameDevWare.Charon.Editor.Cli;
using GameDevWare.Charon.Editor.Json;
using GameDevWare.Charon.Editor.ServerApi;
using GameDevWare.Charon.Editor.Utils;
using GameDevWare.Charon.Editor.Windows;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.Charon.Editor.Routines
{
	[PublicAPI]
	public static class ValidateGameDataRoutine
	{
		public static Task<List<GameDataValidationReport>> ScheduleAsync(string[] paths = null, Action<string, float> progressCallback = null, CancellationToken cancellation = default)
		{
			return CharonEditorModule.Instance.Routines.Schedule(() => RunAsync(paths, progressCallback), cancellation);
		}

		public static Task<List<GameDataValidationReport>> RunAsync(string[] paths = null, Action<string, float> progressCallback = null)
		{
			var task = RunInternalAsync(paths, progressCallback);
			task.LogFaultAsError();
			return task;
		}
		private static async Task<List<GameDataValidationReport>> RunInternalAsync(string[] paths = null, Action<string, float> progressCallback = null)
		{
			var reports = new List<GameDataValidationReport>();
			paths ??= Array.ConvertAll(AssetDatabase.FindAssets("t:" + nameof(GameDataBase)), AssetDatabase.GUIDToAssetPath);

			var logger = CharonEditorModule.Instance.Logger;

			var total = paths.Length;
			for (var i = 0; i < paths.Length; i++)
			{
				var gameDataAssetPath = paths[i];
				if (File.Exists(gameDataAssetPath) == false)
					continue;

				progressCallback?.Invoke(string.Format(Resources.UI_UNITYPLUGIN_PROGRESS_PROCESSING_GAMEDATA, gameDataAssetPath), (float)i / total);

				var gameDataAsset = AssetDatabase.LoadAssetAtPath<GameDataBase>(gameDataAssetPath);
				if (gameDataAsset == null)
				{
					continue;
				}

				var gameDataSettings = gameDataAsset.settings;

				var gameDataPath = AssetDatabase.GUIDToAssetPath(gameDataSettings.gameDataFileGuid);
				var apiKey = string.Empty;
				var gameDataLocation = default(string);
				if (gameDataSettings.IsConnected)
				{
					var serverAddress = new Uri(gameDataSettings.serverAddress);
					var apiKeyPath = new Uri(serverAddress, "/" + gameDataSettings.projectId);
					apiKey = CharonEditorModule.Instance.KeyCryptoStorage.GetKey(apiKeyPath);
					if (string.IsNullOrEmpty(apiKey))
					{
						await ApiKeyPromptWindow.ShowAsync(gameDataSettings.projectId, gameDataSettings.projectName);
						apiKey = CharonEditorModule.Instance.KeyCryptoStorage.GetKey(apiKeyPath);

						if (string.IsNullOrEmpty(apiKey))
						{
							logger.Log(LogType.Warning, $"Unable to validate game data at '{gameDataPath}' because there is API Key associated with it. " +
									"Find this asset in Project window and click 'Synchronize' button in Inspector window.");
							continue; // no key
						}
					}

					gameDataLocation = gameDataSettings.MakeDataSourceUrl().OriginalString;
				}
				else
				{
					gameDataLocation = Path.GetFullPath(gameDataPath);
				}

				var startTime = Stopwatch.StartNew();
				logger.Log(LogType.Assert, $"Validating game data '{gameDataLocation}'.");

				progressCallback?.Invoke(string.Format(Resources.UI_UNITYPLUGIN_VALIDATE_RUN_FOR, gameDataLocation), (float)i / total);

				try
				{
					var report = await CharonCli.ValidateAsync(
						gameDataLocation,
						apiKey,
						ValidationOptions.AllIntegrityChecks,
						CharonEditorModule.Instance.Settings.LogLevel
					);
					reports.Add(new GameDataValidationReport(gameDataPath, report));
					PushValidationErrorsToUnityLog(report, gameDataPath, gameDataSettings, logger);
				}
				catch (Exception error)
				{
					reports.Add(new GameDataValidationReport(gameDataPath, ValidationReport.CreateErrorReport(error.Unwrap().Message)));
					logger.Log(LogType.Warning, string.Format(Resources.UI_UNITYPLUGIN_VALIDATE_FAILED_DUE_ERRORS, gameDataLocation, error.Message));
				}

				logger.Log(LogType.Assert, $"Game data validation of '{gameDataPath}' is finished successfully in '{startTime.Elapsed}'.");
			}
			progressCallback?.Invoke(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1);
			return reports;
		}

		private static void PushValidationErrorsToUnityLog(ValidationReport report, string gameDataPath, GameDataSettings gameDataSettings, ILogger logger)
		{
			const int MAX_LOGGED_ERRORS = 20;

			var totalErrors = 0;
			var loggedErrors = 0;
			if (report.HasErrors)
			{
				var projectId = FileHelper.SanitizeFileName(Path.GetFileNameWithoutExtension(gameDataPath));
				var branchId = "development";

				if (gameDataSettings.IsConnected)
				{
					projectId = gameDataSettings.projectId;
					branchId = gameDataSettings.branchId;
				}

				var records = report.Records ?? Enumerable.Empty<ValidationRecord>();
				foreach (var record in records)
				{
					var errors = record.Errors ?? Enumerable.Empty<ServerApi.ValidationError>();
					var errorCount = 0;
					foreach (var error in errors)
					{
						errorCount++;

						if (loggedErrors++ > MAX_LOGGED_ERRORS)
						{
							continue;
						}

						var id = record.Id is JsonPrimitive ? Convert.ToString(((JsonPrimitive)record.Id).Value) : Convert.ToString(record.Id);
#pragma warning disable CS0612
						var schemaName = record.EntityName ?? record.SchemaName;
#pragma warning restore CS0612

						var validationException = new ValidationError(gameDataPath, projectId, branchId, id, schemaName, error.Path, error.Message);

						var log = (Action<Exception>)CharonEditorModule.Instance.Logger.LogException;
						log.BeginInvoke(validationException, null, null);
					}

					totalErrors += errorCount;
				}
			}

			logger.Log(LogType.Log, string.Format(Resources.UI_UNITYPLUGIN_VALIDATE_COMPLETE, gameDataPath, report.HasErrors ? "failure" : "success", totalErrors));
		}
	}
}
