/*
	Copyright (c) 2023 Denis Zykov

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

using GameDevWare.Charon.Unity.Json;
using GameDevWare.Charon.Unity.Utils;
using GameDevWare.Charon.Unity.Windows;
using System.Collections.Generic;
using System.IO;
using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using GameDevWare.Charon.Unity.Async;
using GameDevWare.Charon.Unity.ServerApi;
using GameDevWare.Charon.Unity.ServerApi.KeyStorage;
using JetBrains.Annotations;
using UnityEditor;

namespace GameDevWare.Charon.Unity.Routines
{
	[PublicAPI, UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public static class ValidateGameDataRoutine
	{
		public static Promise<List<GameDataValidationReport>> Run(string path = null, Action<string, float> progressCallback = null)
		{
			return new Coroutine<List<GameDataValidationReport>>(ValidateGameData(path, progressCallback));
		}
		public static Promise<List<GameDataValidationReport>> Schedule(string path = null, Action<string, float> progressCallback = null, string coroutineId = null)
		{
			return CoroutineScheduler.Schedule<List<GameDataValidationReport>>(ValidateGameData(path, progressCallback), coroutineId);
		}

		private static IEnumerable ValidateGameData(string path = null, Action<string, float> progressCallback = null)
		{
			var checkRequirements = CharonCli.CheckRequirementsAsync();
			yield return checkRequirements;

			switch (checkRequirements.GetResult())
			{
				case RequirementsCheckResult.MissingRuntime: yield return UpdateRuntimeWindow.ShowAsync(); break;
				case RequirementsCheckResult.WrongVersion:
				case RequirementsCheckResult.MissingExecutable: yield return CharonCli.DownloadCharon(progressCallback); break;
				case RequirementsCheckResult.Ok: break;
				default: throw new InvalidOperationException("Unknown Tools check result.");
			}

			var reports = new List<GameDataValidationReport>();
			var paths = !string.IsNullOrEmpty(path) ? new[] { path } : GameDataTracker.All.ToArray();
			var total = paths.Length;
			for (var i = 0; i < paths.Length; i++)
			{
				var gameDataPath = paths[i];
				if (File.Exists(gameDataPath) == false)
					continue;

				if (progressCallback != null) progressCallback(string.Format(Resources.UI_UNITYPLUGIN_PROGRESS_PROCESSING_GAMEDATA, gameDataPath), (float)i / total);

				var gameDataObj = AssetDatabase.LoadAssetAtPath(gameDataPath, typeof(UnityEngine.Object));
				var assetImport = AssetImporter.GetAtPath(gameDataPath);
				if (assetImport == null)
				{
					continue;
				}

				var gameDataSettings = GameDataSettings.Load(gameDataObj);

				var apiKey = default(string);
				var gameDataLocation = default(GameDataLocation);
				if (gameDataSettings.IsConnected)
				{
					var serverAddress = new Uri(gameDataSettings.ServerAddress);
					var apiKeyPath = new Uri(serverAddress, "/" + gameDataSettings.ProjectId);
					apiKey = KeyCryptoStorage.GetKey(apiKeyPath);
					if (string.IsNullOrEmpty(apiKey))
					{
						var apiKeyPromptTask = ApiKeyPromptWindow.ShowAsync(gameDataSettings.ProjectId, gameDataSettings.ProjectName);
						yield return apiKeyPromptTask;
						apiKey = KeyCryptoStorage.GetKey(apiKeyPath);

						if (string.IsNullOrEmpty(apiKey))
						{
							if (Settings.Current.Verbose)
							{
								UnityEngine.Debug.LogWarning(string.Format("Unable to validate game data at '{0}' because there is API Key associated with it. ", gameDataPath) +
									"Find this asset in Project window and click 'Synchronize' button in Inspector window.");
							}
							continue; // no key
						}
					}

					gameDataLocation = new GameDataLocation(gameDataSettings.MakeDataSourceUrl(), apiKey);
				}
				else
				{
					gameDataLocation = new GameDataLocation(gameDataPath);
				}

				var startTime = Stopwatch.StartNew();
				if (Settings.Current.Verbose)
					UnityEngine.Debug.Log(string.Format("Validating game data '{0}'.", gameDataLocation));

				if (progressCallback != null) progressCallback(string.Format(Resources.UI_UNITYPLUGIN_VALIDATE_RUN_FOR, gameDataLocation), (float)i / total);

				var output = CommandOutput.CaptureJson();
				var validateProcess = CharonCli.ValidateAsync(gameDataLocation, ValidationOptions.AllIntegrityChecks, output);
				yield return validateProcess;

				using (var validateResult = validateProcess.GetResult())
				{
					if (Settings.Current.Verbose) UnityEngine.Debug.Log(string.Format("Validation complete, exit code: '{0}'", validateResult.ExitCode));

					if (validateResult.ExitCode != 0)
					{
						reports.Add(new GameDataValidationReport(gameDataPath, ValidationReport.CreateErrorReport(validateProcess.GetResult().GetErrorData())));

						UnityEngine.Debug.LogWarning(string.Format(Resources.UI_UNITYPLUGIN_VALIDATE_FAILED_DUE_ERRORS, gameDataLocation, validateResult.GetErrorData()));
					}
					else
					{
						try
						{
							var report = output.ReadJsonAs<ValidationReport>();
							reports.Add(new GameDataValidationReport(gameDataPath, report));
							PushValidationErrorsToUnityLog(report, gameDataPath, gameDataSettings);
						}
						catch (Exception reportProcessingError)
						{
							UnityEngine.Debug.LogWarning(string.Format(Resources.UI_UNITYPLUGIN_VALIDATE_FAILED_DUE_ERRORS, gameDataPath, reportProcessingError));
						}
					}
				}

				UnityEngine.Debug.Log(string.Format("Game data validation of '{0}' is finished successfully in '{1}'.", gameDataPath, startTime.Elapsed));
			}
			if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1);
			yield return reports;
		}

		private static void PushValidationErrorsToUnityLog(ValidationReport report, string gameDataPath, GameDataSettings gameDataSettings)
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
					projectId = gameDataSettings.ProjectId;
					branchId = gameDataSettings.BranchId;
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

						var log = (Action<Exception>)UnityEngine.Debug.LogException;
						log.BeginInvoke(validationException, null, null);
					}

					totalErrors += errorCount;
				}
			}

			UnityEngine.Debug.Log(string.Format(Resources.UI_UNITYPLUGIN_VALIDATE_COMPLETE, gameDataPath, report.HasErrors ? "failure" : "success", totalErrors));
		}
	}
}
