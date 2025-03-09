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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GameDevWare.Charon.Editor.Cli;
using GameDevWare.Charon.Editor.Services.ServerApi;
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

				var pathSpecificProgress = progressCallback?.Sub((float)i / total, i + 1.0f / total);
				pathSpecificProgress?.Invoke(string.Format(Resources.UI_UNITYPLUGIN_PROGRESS_PROCESSING_GAMEDATA, gameDataAssetPath), 0.00f);

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

				pathSpecificProgress?.Invoke(string.Format(Resources.UI_UNITYPLUGIN_VALIDATE_RUN_FOR, gameDataLocation), 0.10f);

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
				var projectId = CharonFileUtils.SanitizeFileName(Path.GetFileNameWithoutExtension(gameDataPath));
				var branchId = "development";

				if (gameDataSettings.IsConnected)
				{
					projectId = gameDataSettings.projectId;
					branchId = gameDataSettings.branchId;
				}

				var records = report.Records ?? Enumerable.Empty<ValidationRecord>();
				foreach (var record in records)
				{
					var errors = record.Errors ?? Enumerable.Empty<Services.ServerApi.ValidationError>();
					var errorCount = 0;
					foreach (var error in errors)
					{
						errorCount++;

						if (loggedErrors++ > MAX_LOGGED_ERRORS)
						{
							continue;
						}

						var id =record.Id;
						var schemaName = record.SchemaName;

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
