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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using GameDevWare.Charon.Unity.Async;
using GameDevWare.Charon.Unity.Updates;
using GameDevWare.Charon.Unity.Updates.Packages;
using GameDevWare.Charon.Unity.Updates.Packages.Deployment;
using GameDevWare.Charon.Unity.Windows;
using JetBrains.Annotations;
using Debug = UnityEngine.Debug;
using FileMode = System.IO.FileMode;

namespace GameDevWare.Charon.Unity.Utils
{
	[PublicAPI, UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public static class CharonCli
	{
		internal static readonly Regex MonoVersionRegex = new Regex(@"version (?<v>[0-9]+\.[0-9]+\.[0-9]+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
		internal static readonly Version MinimalMonoVersion = new Version(5, 18, 0);
		internal static readonly Version MinimalDotNetVersion = new Version(4, 7, 2);
		internal static readonly SemanticVersion LegacyToolsVersion = new SemanticVersion("2020.1.1");
		internal static readonly SemanticVersion LegacyPluginVersion = new SemanticVersion("2021.3.0");


		internal static Promise<RequirementsCheckResult> CheckRequirementsAsync()
		{
			if (string.IsNullOrEmpty(Settings.CharonExePath) ||
				!File.Exists(Settings.CharonExePath) ||
				string.IsNullOrEmpty(Settings.Current.EditorVersion))
			{
				return Promise.FromResult(RequirementsCheckResult.MissingExecutable);
			}

			var additionalChecks = new List<Promise<RequirementsCheckResult>>();
			if (RuntimeInformation.IsWindows)
			{
				if (DotNetRuntimeInformation.GetVersion() == null &&
					(string.IsNullOrEmpty(MonoRuntimeInformation.MonoPath) || File.Exists(MonoRuntimeInformation.MonoPath) == false))
				{
					return Promise.FromResult(RequirementsCheckResult.MissingRuntime);
				}
				else if (DotNetRuntimeInformation.GetVersion() < MinimalDotNetVersion)
				{
					return Promise.FromResult(RequirementsCheckResult.MissingRuntime);
				}
			}
			else
			{
				if (string.IsNullOrEmpty(MonoRuntimeInformation.MonoPath) || File.Exists(MonoRuntimeInformation.MonoPath) == false)
					return Promise.FromResult(RequirementsCheckResult.MissingRuntime);

				additionalChecks.Add(GetMonoVersionAsync().ContinueWith(getMonoVersion =>
				{
					if (getMonoVersion.HasErrors || getMonoVersion.GetResult() == null || getMonoVersion.GetResult() < MinimalMonoVersion)
						return RequirementsCheckResult.MissingRuntime;
					else
						return RequirementsCheckResult.Ok;
				}));
			}

			additionalChecks.Add(GetVersionAsync().ContinueWith(getCharonVersion =>
			{
				if (getCharonVersion.HasErrors || getCharonVersion.GetResult() == null ||
					getCharonVersion.GetResult().ToString() != Settings.Current.EditorVersion)
					return RequirementsCheckResult.WrongVersion;
				else
					return RequirementsCheckResult.Ok;
			}));

			if (additionalChecks.Count == 0)
				return Promise.FromResult(RequirementsCheckResult.Ok);
			else
				return Promise.WhenAll(additionalChecks.ToArray())
					.ContinueWith(results => results.GetResult().FirstOrDefault(r => r != RequirementsCheckResult.Ok));
		}

		internal static string GetDefaultLockFilePath()
		{
			var charonDir = Path.GetDirectoryName(Settings.CharonExePath);
			var lockFileName = Path.GetFileNameWithoutExtension(Settings.CharonExePath) + ".lock";
			// ReSharper disable once AssignNullToNotNullAttribute
			return Path.GetFullPath(Path.Combine(charonDir, lockFileName));
		}
		internal static Promise<Process> Listen(string gameDataPath, string lockFilePath, int port, bool shadowCopy = true, Action<string, float> progressCallback = null)
		{
			if (string.IsNullOrEmpty(gameDataPath)) throw new ArgumentException("Value cannot be null or empty.", "gameDataPath");
			if (string.IsNullOrEmpty(lockFilePath)) throw new ArgumentException("Value cannot be null or empty.", "lockFilePath");
			if (port <= 0 || port > ushort.MaxValue) throw new ArgumentOutOfRangeException("port");

			var charonPath = Path.GetFullPath(Settings.CharonExePath);
			var charonDirectory = Path.GetDirectoryName(charonPath) ?? Settings.LibraryCharonPath;

			if (File.Exists(gameDataPath) == false) throw new IOException(string.Format("File '{0}' doesn't exists.", gameDataPath));
			if (File.Exists(charonPath) == false) throw new IOException(string.Format("File '{0}' doesn't exists.", charonPath));

			if (shadowCopy)
			{
				if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_WINDOW_EDITOR_COPYING_EXECUTABLE, 0.10f);

				var charonMd5 = FileHelper.ComputeHash(charonPath);
				var shadowDirectory = Path.GetFullPath(Path.Combine(Settings.TempPath, charonMd5));
				if (Directory.Exists(shadowDirectory) == false)
				{
					if (Settings.Current.Verbose)
						Debug.Log("Making shadow copy of '" + Path.GetFileName(charonPath) + "' to '" + shadowDirectory + "'.");

					Directory.CreateDirectory(shadowDirectory);

					var shadowCharonPath = Path.Combine(shadowDirectory, Path.GetFileName(charonPath));
					File.Copy(charonPath, shadowCharonPath, overwrite: true);

					var configPath = charonPath + ".config";
					var configShadowPath = shadowCharonPath + ".config";
					var appSettingsPath = Path.Combine(charonDirectory, "appsettings.json");
					var appSettingsShadowPath = Path.Combine(shadowDirectory, "appsettings.json");
					if (File.Exists(configPath))
					{
						if (Settings.Current.Verbose)
							Debug.Log("Making shadow copy of '" + Path.GetFileName(configPath) + "' to '" + shadowDirectory + "'.");

						File.Copy(configPath, configShadowPath);
					}
					else if (File.Exists(appSettingsPath))
					{
						if (Settings.Current.Verbose)
							Debug.Log("Making shadow copy of '" + Path.GetFileName(appSettingsPath) + "' to '" + shadowDirectory + "'.");

						File.Copy(appSettingsPath, appSettingsShadowPath);
					}
					else
					{
						Debug.LogWarning("Missing required configuration file at '" + configPath + "'.");
					}

					charonPath = shadowCharonPath;
				}
				else
				{
					charonPath = Path.Combine(shadowDirectory, Path.GetFileName(charonPath));
				}
			}
			if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_WINDOW_EDITOR_LAUNCHING_EXECUTABLE, 0.30f);
			var unityPid = Process.GetCurrentProcess().Id;
			var scriptingAssemblies = FindAndLoadScriptingAssemblies(gameDataPath);
			var runTask = CommandLine.Run(
				new RunOptions
				(
					charonPath,

					RunOptions.FlattenArguments(
						IsToolsLegacy() ? new object[] { "SERVE" } : new object[] { "SERVER", "START" },
						"--dataBase", Path.GetFullPath(gameDataPath),
						"--port", port.ToString(),
						"--watchPid", unityPid.ToString(),
						"--lockFile", Path.GetFullPath(lockFilePath),
						"--scriptAssemblies", scriptingAssemblies,

						// v2020.1.1
						IsToolsLegacy() ? new object[] {
							"--environment", "Unity",
							"--extensions", Settings.SupportedExtensions,
						} :
						//
						new object[] {
							"--maxIdleTime", "00:00:30", // auto-close idle editor
							"--log", "out",
						},
						Settings.Current.Verbose ? "--verbose" : ""
					)
				)
				{
					RequireDotNetRuntime = true,
					CaptureStandardError = false,
					CaptureStandardOutput = false,
					ExecutionTimeout = TimeSpan.Zero,
					WaitForExit = false,
					StartInfo =
					{
						EnvironmentVariables =
						{
							// v2020.1.1
							{ "CHARON_APP_DATA", Settings.UserDataPath },
							{ "BASE_DIRECTORY_PATH", Settings.LibraryCharonPath },
							//
							{ "CHARON_API_SERVER", Settings.Current.GetServerAddressUrl().OriginalString },
							{ "CHARON_API_KEY", "" },
							{ "STANDALONE__APPLICATIONDATAPATH", Settings.UserDataPath },
							{ "STANDALONE__APPLICATIONTEMPPATH", Settings.TempPath },
							{ "SERILOG__WRITETO__0__NAME", "File" },
							{ "SERILOG__WRITETO__0__ARGS__PATH", Path.GetFullPath(Path.Combine(Settings.LibraryCharonLogsPath, string.Format("{0:yyyy_MM_dd_hh}.charon.unity.log", DateTime.UtcNow)))  },
						}
					}
				}
			);
			if (progressCallback != null)
				runTask.ContinueWith(_ => progressCallback(Resources.UI_UNITYPLUGIN_WINDOW_EDITOR_LAUNCHING_EXECUTABLE, 1.0f));

			return runTask.ContinueWith(t => t.GetResult().Process);
		}
		internal static void FindAndEndGracefully(string lockFilePath = null)
		{
			if (string.IsNullOrEmpty(lockFilePath))
				lockFilePath = GetDefaultLockFilePath();

			if (File.Exists(lockFilePath) == false)
				return;

			var pidStr = default(string);
			using (var lockFileStream = new FileStream(lockFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 1024, FileOptions.SequentialScan))
				pidStr = new StreamReader(lockFileStream, detectEncodingFromByteOrderMarks: true).ReadToEnd();

			var pid = 0;
			if (string.IsNullOrEmpty(pidStr) || int.TryParse(pidStr, out pid) == false)
				return;

			var stopError = default(Exception);
			try
			{
				if (Settings.Current.Verbose)
					Debug.Log(string.Format("Trying to end process with id {0}.", pidStr));

				using (var process = Process.GetProcessById(pid))
					process.EndGracefully();

				if (Settings.Current.Verbose)
					Debug.Log(string.Format("Successfully ended process with id {0}.", pidStr));
			}
			catch (Exception endError)
			{
				stopError = endError;
				if (Settings.Current.Verbose)
					Debug.LogWarning(string.Format("Failed to get process by id {0}.\r\n{1}", pidStr, endError));
			}

			try
			{
				if (File.Exists(lockFilePath))
					File.Delete(lockFilePath);
			}
			catch (Exception lockDeleteError)
			{
				stopError = stopError ?? lockDeleteError;
				Debug.LogWarning(string.Format("Failed to stop running process with id {0}.\r\n{1}", pidStr, stopError));
				throw stopError;
			}
		}

		internal static Promise DownloadCharon(Action<string, float> progressCallback = null)
		{
			return new Coroutine(DownloadCharonAsync(progressCallback));
		}
		private static IEnumerable DownloadCharonAsync(Action<string, float> progressCallback = null)
		{
			var checkRequirements = CharonCli.CheckRequirementsAsync();
			yield return checkRequirements;

			var checkResult = checkRequirements.GetResult();
			if (checkResult == RequirementsCheckResult.MissingRuntime)
				yield return UpdateRuntimeWindow.ShowAsync();

			var currentVersion = default(SemanticVersion);
			var charonPath = Path.GetFullPath(Settings.CharonExePath);
			var toolName = Path.GetFileNameWithoutExtension(Path.GetFileName(charonPath));

			if (File.Exists(charonPath))
			{
				if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_CHECKING_TOOLS_VERSION, 0.05f);

				var checkToolsVersion = CharonCli.GetVersionAsync();
				yield return checkToolsVersion.IgnoreFault();

				currentVersion = checkToolsVersion.HasErrors ? default(SemanticVersion) : checkToolsVersion.GetResult();
			}

			if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_GETTING_AVAILABLE_BUILDS, 0.10f);

			var getBuildsAsync = PackageManager.GetVersions(ProductInformation.PRODUCT_CHARON);
			yield return getBuildsAsync.IgnoreFault();
			if (getBuildsAsync.HasErrors)
			{
				if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1.0f);

				Debug.LogError(string.Format("Failed to get builds list from server. Error: {0}", getBuildsAsync.Error.Unwrap().Message));
				yield break;
			}

			var builds = getBuildsAsync.GetResult();
			var buildsByVersion = builds.ToDictionary(b => b.Version);
			var lastBuild = builds.OrderByDescending(b => b.Version).FirstOrDefault();

			if (lastBuild == null)
			{
				if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1.0f);

				if (Settings.Current.Verbose)
					Debug.Log(string.Format("No builds of {0} are available.", toolName));
				yield break;
			}

			var currentBuild = currentVersion != null ? builds.FirstOrDefault(b => b.Version == currentVersion) : null;
			var lastVersion = lastBuild.Version;
			var isMissing = File.Exists(charonPath);
			var hashFileName = currentBuild == null ? null : Path.Combine(Path.Combine(Settings.LibraryCharonPath, currentVersion.ToString()), Path.GetFileName(charonPath) + ".sha1");
			var actualHash = isMissing || currentBuild == null ? null : FileHelper.ComputeHash(charonPath, "SHA1");
			var expectedHash = isMissing || hashFileName == null || File.Exists(hashFileName) == false ? null : File.ReadAllText(hashFileName);
			var isCorrupted = currentBuild != null && string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase) == false;
			var expectedVersion = string.IsNullOrEmpty(Settings.Current.EditorVersion) ? default(SemanticVersion) : new SemanticVersion(Settings.Current.EditorVersion);

			if (!isMissing && !isCorrupted && expectedVersion == currentVersion && currentVersion != null)
			{
				if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1.0f);

				if (Settings.Current.Verbose)
					Debug.Log(string.Format("{0} version '{1}' is expected and file hash is matching.", toolName, currentVersion));
				yield break;
			}

			var versionToDeploy = expectedVersion ?? lastVersion;
			if (buildsByVersion.ContainsKey(versionToDeploy) == false)
			{
				if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1.0f);

				Debug.LogError(string.Format("Package of {0} with version '{1}' is not available to download.", toolName, versionToDeploy));
				yield break;
			}

			if (progressCallback != null) progressCallback(string.Format(Resources.UI_UNITYPLUGIN_PROGRESS_DOWNLOADING, 0, 0, ProductInformation.PRODUCT_CHARON), 0.10f);

			var deployAction = new CharonDeploymentAction(versionToDeploy, progressCallback);
			var prepareAsync = deployAction.Prepare();
			yield return prepareAsync.IgnoreFault();

			if (prepareAsync.HasErrors)
			{
				if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1.0f);

				Debug.LogError(string.Format("Failed to download package of {0} with version '{1}'.{2}{3}", toolName, versionToDeploy, Environment.NewLine, prepareAsync.Error.Unwrap()));

				deployAction.Complete();
				yield break;
			}

			var deployAsync = deployAction.Complete();
			yield return deployAsync.IgnoreFault();

			if (deployAsync.HasErrors)
			{
				if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1.0f);

				Debug.LogError(string.Format("Failed to deploy package of {0} with version '{1}'.{2}{3}", toolName, versionToDeploy, Environment.NewLine, deployAsync.Error.Unwrap()));

				deployAction.Complete();
				yield break;
			}

			// cleanup resources
			deployAction.Complete();

			if (File.Exists(charonPath))
			{
				if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_CHECKING_TOOLS_VERSION, 0.95f);

				var checkToolsVersion = CharonCli.GetVersionAsync();
				yield return checkToolsVersion.IgnoreFault();
				currentVersion = checkToolsVersion.HasErrors ? default(SemanticVersion) : checkToolsVersion.GetResult();
			}

			if (currentVersion != null)
			{
				Settings.Current.EditorVersion = currentVersion.ToString();
				Settings.Current.Save();
			}

			Debug.Log(string.Format("{1} version is '{0}'. Update is completed.", currentVersion, Path.GetFileName(charonPath)));

			if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1.0f);
		}

		public static Promise<RunResult> CreateDocumentAsync(GameDataLocation gameDataLocation, string schema, CommandInput input, CommandOutput output)
		{
			if (gameDataLocation.Location == null) throw new ArgumentNullException("gameDataLocation");
			if (schema == null) throw new ArgumentNullException("schema");
			if (input == null) throw new ArgumentNullException("input");
			if (output == null) throw new ArgumentNullException("output");

			gameDataLocation.ThrowIfFileNotExists();

			var runTask = RunInternal
			(
				gameDataLocation.ApiKey,
				RunOptions.FlattenArguments(
					"DATA", "CREATE", gameDataLocation,
					IsToolsLegacy() ? "--entity" : "--schema", schema,
					"--input", input.Source,
					"--inputFormat", input.Format,
					"--inputFormattingOptions", input.FormattingOptions,
					"--output", output.Target,
					"--outputFormat", output.Format,
					"--outputFormattingOptions", output.FormattingOptions
				)
			);
			input.StickWith(runTask);
			return output.Capture(runTask);
		}

		public static Promise<RunResult> UpdateDocumentAsync(GameDataLocation gameDataLocation, string schema, CommandInput input, CommandOutput output)
		{
			return UpdateDocumentAsync(gameDataLocation, schema, string.Empty, input, output);
		}
		public static Promise<RunResult> UpdateDocumentAsync(GameDataLocation gameDataLocation, string schema, string id, CommandInput input, CommandOutput output)
		{
			if (gameDataLocation.Location == null) throw new ArgumentNullException("gameDataLocation");
			if (schema == null) throw new ArgumentNullException("schema");
			if (id == null) throw new ArgumentNullException("id");
			if (input == null) throw new ArgumentNullException("input");
			if (output == null) throw new ArgumentNullException("output");

			gameDataLocation.ThrowIfFileNotExists();

			var runTask = RunInternal
			(
				gameDataLocation.ApiKey,
				RunOptions.FlattenArguments(
					"DATA", "UPDATE", gameDataLocation,
					IsToolsLegacy() ? "--entity" : "--schema", schema,
					"--id", id,
					"--input", input.Source,
					"--inputFormat", input.Format,
					"--inputFormattingOptions", input.FormattingOptions,
					"--output", output.Target,
					"--outputFormat", output.Format,
					"--outputFormattingOptions", output.FormattingOptions
				)
			);

			input.StickWith(runTask);
			return output.Capture(runTask);
		}
		
		public static Promise<RunResult> DeleteDocumentAsync(GameDataLocation gameDataLocation, string schema, string id, CommandOutput output)
		{
			if (gameDataLocation.Location == null) throw new ArgumentNullException("gameDataLocation");
			if (schema == null) throw new ArgumentNullException("schema");
			if (id == null) throw new ArgumentNullException("id");
			if (output == null) throw new ArgumentNullException("output");

			gameDataLocation.ThrowIfFileNotExists();

			var runTask = RunInternal
			(
				gameDataLocation.ApiKey,
				RunOptions.FlattenArguments(
					"DATA", "DELETE", gameDataLocation,
					IsToolsLegacy() ? "--entity" : "--schema", schema,
					"--id", id,
					"--output", output.Target,
					"--outputFormat", output.Format,
					"--outputFormattingOptions", output.FormattingOptions
				)
			);
			return output.Capture(runTask);
		}

		public static Promise<RunResult> FindDocumentAsync(GameDataLocation gameDataLocation, string schema, string id, CommandOutput output)
		{
			if (gameDataLocation.Location == null) throw new ArgumentNullException("gameDataLocation");
			if (schema == null) throw new ArgumentNullException("schema");
			if (id == null) throw new ArgumentNullException("id");
			if (output == null) throw new ArgumentNullException("output");

			gameDataLocation.ThrowIfFileNotExists();

			var runTask = RunInternal
			(
				gameDataLocation.ApiKey,
				RunOptions.FlattenArguments(
					"DATA", "FIND", gameDataLocation,
					IsToolsLegacy() ? "--entity" : "--schema", schema,
					"--id", id,
					"--output", output.Target,
					"--outputFormat", output.Format,
					"--outputFormattingOptions", output.FormattingOptions
				)
			);
			return output.Capture(runTask);
		}

		public static Promise<RunResult> ImportAsync(GameDataLocation gameDataLocation, CommandInput input, ImportMode mode)
		{
			return ImportAsync(gameDataLocation, new string[0], input, mode);
		}
		public static Promise<RunResult> ImportAsync(GameDataLocation gameDataLocation, string[] schemas, CommandInput input, ImportMode mode)
		{
			if (gameDataLocation.Location == null) throw new ArgumentNullException("gameDataLocation");
			if (schemas == null) throw new ArgumentNullException("schemas");
			if (input == null) throw new ArgumentNullException("input");

			if (Enum.IsDefined(typeof(ImportMode), mode) == false) throw new ArgumentException("Unknown import mode.", "mode");
			gameDataLocation.ThrowIfFileNotExists();

			var runTask = RunInternal
			(
				gameDataLocation.ApiKey,
				RunOptions.FlattenArguments(
					"DATA", "IMPORT", gameDataLocation,
					IsToolsLegacy() ? "--entities" : "--schemas", schemas,
					"--mode", mode,
					"--input", input.Source,
					"--inputFormat", input.Format,
					"--inputFormattingOptions", input.FormattingOptions
				)
			);
			input.StickWith(runTask);
			return runTask;
		}

		public static Promise<RunResult> ExportAsync(GameDataLocation gameDataLocation, CommandOutput output, ExportMode mode)
		{
			return ExportAsync(gameDataLocation, new string[0], output, mode);
		}
		public static Promise<RunResult> ExportAsync(GameDataLocation gameDataLocation, string[] schemas, CommandOutput output, ExportMode mode)
		{
			return ExportAsync(gameDataLocation, schemas, new string[0], output, mode);
		}
		public static Promise<RunResult> ExportAsync(GameDataLocation gameDataLocation, string[] schemas, string[] properties, CommandOutput output, ExportMode mode)
		{
			return ExportAsync(gameDataLocation, schemas, properties, new CultureInfo[0], output, mode);
		}
		public static Promise<RunResult> ExportAsync(GameDataLocation gameDataLocation, string[] schemas, string[] properties, CultureInfo[] languages, CommandOutput output, ExportMode mode)
		{
			if (gameDataLocation.Location == null) throw new ArgumentNullException("gameDataLocation");
			if (schemas == null) throw new ArgumentNullException("schemas");
			if (properties == null) throw new ArgumentNullException("properties");
			if (languages == null) throw new ArgumentNullException("languages");
			if (output == null) throw new ArgumentNullException("output");

			if (Enum.IsDefined(typeof(ExportMode), mode) == false) throw new ArgumentException("Unknown export mode.", "mode");
			gameDataLocation.ThrowIfFileNotExists();

			var runTask = RunInternal
			(
				gameDataLocation.ApiKey,
				RunOptions.FlattenArguments(
					"DATA", "EXPORT", gameDataLocation,
					IsToolsLegacy() ? "--entities" : "--schemas", schemas,
					IsToolsLegacy() ? "--attributes" : "--properties", properties,
					"--languages", Array.ConvertAll(languages, l => l.Name),
					"--mode", mode,
					"--output", output.Target,
					"--outputFormat", output.Format,
					"--outputFormattingOptions", output.FormattingOptions
				)
			);
			return runTask;
		}

		public static Promise<RunResult> CreatePatchAsync(GameDataLocation gameDataLocation, GameDataLocation gameDataPath2, CommandOutput output)
		{
			if (gameDataLocation.Location == null) throw new ArgumentNullException("gameDataPath");
			if (gameDataPath2.Location == null) throw new ArgumentNullException("gameDataPath");
			if (output == null) throw new ArgumentNullException("output");

			gameDataLocation.ThrowIfRemote();
			gameDataPath2.ThrowIfRemote();
			gameDataLocation.ThrowIfFileNotExists();
			gameDataPath2.ThrowIfFileNotExists();

			var runTask = RunInternal
			(
				apiKey: null,
				arguments: RunOptions.FlattenArguments(
					"DATA", "CREATEPATCH", gameDataLocation, gameDataPath2,
					"--output", output.Target,
					"--outputFormat", output.Format,
					"--outputFormattingOptions", output.FormattingOptions
				)
			);
			return runTask;
		}
		public static Promise<RunResult> ApplyPatchAsync(GameDataLocation gameDataLocation, CommandInput input)
		{
			if (gameDataLocation.Location == null) throw new ArgumentNullException("gameDataLocation");
			if (input == null) throw new ArgumentNullException("input");

			gameDataLocation.ThrowIfFileNotExists();

			var runTask = RunInternal
			(
				gameDataLocation.ApiKey,
				RunOptions.FlattenArguments(
					"DATA", "APPLYPATCH", gameDataLocation,
					"--input", input.Source,
					"--inputFormat", input.Format,
					"--inputFormattingOptions", input.FormattingOptions
				)
			);
			input.StickWith(runTask);
			return runTask;
		}

		public static Promise<RunResult> BackupAsync(GameDataLocation gameDataLocation, CommandOutput output)
		{
			if (gameDataLocation.Location == null) throw new ArgumentNullException("gameDataLocation");
			if (output == null) throw new ArgumentNullException("output");

			gameDataLocation.ThrowIfFileNotExists();

			var runTask = RunInternal
			(
				gameDataLocation.ApiKey,
				RunOptions.FlattenArguments(
					"DATA", "Backup", gameDataLocation,
					"--output", output.Target,
					"--outputFormat", output.Format,
					"--outputFormattingOptions", output.FormattingOptions
				)
			);
			return output.Capture(runTask);
		}
		public static Promise<RunResult> RestoreAsync(GameDataLocation gameDataLocation, CommandInput input)
		{
			if (gameDataLocation.Location == null) throw new ArgumentNullException("gameDataLocation");
			if (input == null) throw new ArgumentNullException("input");

			var runTask = RunInternal
			(
				gameDataLocation.ApiKey,
				RunOptions.FlattenArguments(
					"DATA", "RESTORE", gameDataLocation,
					"--input", input.Source,
					"--inputFormat", input.Format,
					"--inputFormattingOptions", input.FormattingOptions
				)
			);
			input.StickWith(runTask);
			return runTask;
		}

		public static Promise<RunResult> ValidateAsync(GameDataLocation gameDataLocation, ValidationOptions validationOptions, CommandOutput output)
		{
			if (gameDataLocation.Location == null) throw new ArgumentNullException("gameDataLocation");
			if (output == null) throw new ArgumentNullException("output");

			gameDataLocation.ThrowIfFileNotExists();

			var runTask = RunInternal
			(
				gameDataLocation.ApiKey,
				RunOptions.FlattenArguments(
					"DATA", "VALIDATE", gameDataLocation,
					"--validationOptions", ((int)validationOptions).ToString(),
					"--output", output.Target,
					"--outputFormat", output.Format,
					"--outputFormattingOptions", output.FormattingOptions
				)
			);
			return output.Capture(runTask);
		}

		public static Promise<RunResult> GenerateCSharpCodeAsync(GameDataLocation gameDataLocation, string outputDirectory,
			CSharpLanguageVersion languageVersion, SourceCodeGenerationOptimizations optimizations = 0,
			string documentClassName = "Document", string gameDataClassName = "GameData",
			string @namespace = "GameParameters", string defineConstants = "", SourceCodeIndentation sourceCodeIndentation = SourceCodeIndentation.Tabs,
			SourceCodeLineEndings sourceCodeLineEndings = SourceCodeLineEndings.Windows, bool splitFiles = false)
		{
			if (gameDataLocation.Location == null) throw new ArgumentNullException("gameDataLocation");
			if (outputDirectory == null) throw new ArgumentNullException("outputDirectory");
			if (documentClassName == null) throw new ArgumentNullException("documentClassName");
			if (gameDataClassName == null) throw new ArgumentNullException("gameDataClassName");
			if (@namespace == null) throw new ArgumentNullException("namespace");

			gameDataLocation.ThrowIfFileNotExists();

			if (true)
			{
				optimizations |= SourceCodeGenerationOptimizations.DisableFormulaCompilation;
			}

			var optimizationsList = new List<string>();
			foreach (SourceCodeGenerationOptimizations optimization in Enum.GetValues(typeof(SourceCodeGenerationOptimizations)))
			{
				if ((optimizations & optimization) != 0)
				{
					optimizationsList.Add(optimization.ToString());
				}
			}

			if (IsToolsLegacy())
			{
				var options = default(LegacySourceCodeGenerationOptimizations);
				if ((optimizations & SourceCodeGenerationOptimizations.DisableJsonSerialization) != 0)
				{
					options |= LegacySourceCodeGenerationOptimizations.DisableJsonSerialization;
				}
				if ((optimizations & SourceCodeGenerationOptimizations.DisableMessagePackSerialization) != 0)
				{
					options |= LegacySourceCodeGenerationOptimizations.DisableMessagePackSerialization;
				}
				if ((optimizations & SourceCodeGenerationOptimizations.DisablePatching) != 0)
				{
					options |= LegacySourceCodeGenerationOptimizations.DisablePatching;
				}
				if ((optimizations & SourceCodeGenerationOptimizations.EagerReferenceResolution) == 0)
				{
					options |= LegacySourceCodeGenerationOptimizations.LazyReferences;
				}
				if ((optimizations & SourceCodeGenerationOptimizations.RawLocalizedStrings) == 0)
				{
					options |= LegacySourceCodeGenerationOptimizations.HideLocalizedStrings;
				}
				if ((optimizations & SourceCodeGenerationOptimizations.RawReferences) == 0)
				{
					options |= LegacySourceCodeGenerationOptimizations.HideReferences;
				}
				if ((optimizations & SourceCodeGenerationOptimizations.DisableFormulaCompilation) == 0)
				{
					options |= LegacySourceCodeGenerationOptimizations.DisableFormulas;
				}

				var generatedCodeFileName = Path.ChangeExtension(Path.GetFileName(gameDataLocation.Location.LocalPath), ".cs");
				
				var runTask = RunInternal
				(
					gameDataLocation.ApiKey,
					RunOptions.FlattenArguments
					(
						"GENERATE", "CSHARPCODE", gameDataLocation,
						"--documentClassName", documentClassName,
						"--apiClassName", gameDataClassName,
						"--namespace", @namespace,
						"--options", ((int)options).ToString(),
						"--output", Path.Combine(outputDirectory, generatedCodeFileName),
						"--outputEncoding", "utf-8"
					)
				);
				return runTask;
			}
			else
			{
				var runTask = RunInternal
				(
					gameDataLocation.ApiKey,
					RunOptions.FlattenArguments
					(
						"GENERATE", "CSHARPCODE", gameDataLocation,
						"--outputDirectory", outputDirectory,
						"--documentClassName", documentClassName,
						"--gameDataClassName", gameDataClassName,
						"--namespace", @namespace,
						"--defineConstants", defineConstants,
						"--languageVersion", languageVersion.ToString(),
						"--indentation", sourceCodeIndentation.ToString(),
						"--lineEndings", sourceCodeLineEndings.ToString(),
						"--optimizations", optimizationsList,
						splitFiles ? "--splitFiles" : null
					)
				);
				return runTask;
			}
		}

		internal static Promise<RunResult> DumpTemplatesAsync(string outputDirectory)
		{
			if (string.IsNullOrEmpty(outputDirectory)) throw new ArgumentException("Value cannot be null or empty.", "outputDirectory");

			if (Directory.Exists(outputDirectory) == false) throw new IOException(string.Format("Directory '{0}' doesn't exists.", outputDirectory));

			var runTask = RunInternal
			(
				apiKey: default(string),
				arguments: RunOptions.FlattenArguments
				(
					"GENERATE", "TEMPLATES",
					"--outputDirectory", outputDirectory
				)
			);
			return runTask;

		}

		public static Promise<Version> GetGameDataVersionAsync(GameDataLocation gameDataLocation)
		{
			if (gameDataLocation.Location == null) throw new ArgumentNullException("gameDataLocation");

			gameDataLocation.ThrowIfFileNotExists();

			var runTask = RunInternal(
				apiKey: gameDataLocation.ApiKey,
				arguments: RunOptions.FlattenArguments
				(
					"DATA", "VERSION", gameDataLocation
				));

			return runTask.ContinueWith(r =>
			{
				using (var result = r.GetResult())
					return new Version(result.GetOutputData());
			});
		}

		internal static Promise<SemanticVersion> GetVersionAsync()
		{
			var runTask = RunInternal(null, new[] { "VERSION" });
			return runTask.ContinueWith(r =>
			{
				using (var result = r.GetResult())
				{
					var versionString = result.GetOutputData();
					if (string.IsNullOrEmpty(versionString))
						return new SemanticVersion(0, 0, 0, 0);
					else
						return new SemanticVersion(versionString);

				}
			});
		}

		internal static Promise<Version> GetMonoVersionAsync()
		{
			var checkMonoRuntimeVersion = CommandLine.Run(new RunOptions(MonoRuntimeInformation.MonoPath, "--version")
			{
				CaptureStandardOutput = true,
				CaptureStandardError = true,
				ExecutionTimeout = TimeSpan.FromSeconds(5),
			});
			return checkMonoRuntimeVersion.ContinueWith(runTask =>
			{
				var checkMonoRuntimeVersionOutput = string.Empty;
				if (runTask.HasErrors == false)
					checkMonoRuntimeVersionOutput = runTask.GetResult().GetOutputData() ?? "";
				if (runTask.HasErrors || MonoVersionRegex.IsMatch(checkMonoRuntimeVersionOutput) == false)
					return default(Version);

				var monoVersionMatch = MonoVersionRegex.Match(checkMonoRuntimeVersionOutput);
				var monoVersion = default(Version);
				try
				{
					monoVersion = new Version(monoVersionMatch.Groups["v"].Value);
				}
				catch
				{
					/*ignore*/
				}
				return monoVersion;
			});
		}

		internal static void CleanUpLogsDirectory()
		{
			if (string.IsNullOrEmpty(Settings.LibraryCharonLogsPath) || Directory.Exists(Settings.LibraryCharonLogsPath) == false)
			{
				return;
			}

			var logsRetentionTime = TimeSpan.FromDays(2);
			foreach (var logFile in Directory.GetFiles(Settings.LibraryCharonLogsPath))
			{
				if (DateTime.UtcNow - File.GetLastWriteTimeUtc(logFile) <= logsRetentionTime)
				{
					continue; // not old enough
				}

				try
				{
					if (Settings.Current.Verbose)
					{
						Debug.Log(string.Format("Deleting old log file at '{0}'.", logFile));
					}
					File.Delete(logFile);
				}
				catch (Exception deleteError)
				{
					if (Settings.Current.Verbose)
					{
						Debug.LogWarning(string.Format("Failed to delete log file at '{0}'.", logFile));
						Debug.LogWarning(deleteError);
					}
				}
			}
		}

		private static Promise<RunResult> RunInternal(string apiKey, string[] arguments)
		{
			try
			{
				var charonPath = Settings.CharonExePath;

				if (File.Exists(charonPath) == false) throw new IOException(string.Format("File '{0}' doesn't exists.", charonPath));

				arguments = arguments.Concat(new[] { Settings.Current.Verbose ? "--verbose" : "" }).ToArray();

				var runTask = CommandLine.Run(new RunOptions(charonPath, arguments)
				{
					CaptureStandardOutput = true,
					CaptureStandardError = true,
					ExecutionTimeout = TimeSpan.FromSeconds(30),
					RequireDotNetRuntime = true,
					WaitForExit = true,
					StartInfo = {
						EnvironmentVariables = {
							// v2020.1.1
							{ "CHARON_APP_DATA", Settings.UserDataPath },
							{ "BASE_DIRECTORY_PATH", Settings.LibraryCharonPath },
							//
							{ "CHARON_API_SERVER", Settings.Current.GetServerAddressUrl().OriginalString },
							{ "CHARON_API_KEY", apiKey ?? string.Empty },
							{ "STANDALONE__APPLICATIONDATAPATH", Settings.UserDataPath },
							{ "STANDALONE__APPLICATIONTEMPPATH", Settings.TempPath },
							{ "SERILOG__WRITETO__0__NAME", "File" },
							{ "SERILOG__WRITETO__0__ARGS__PATH", Path.GetFullPath(Path.Combine(Settings.LibraryCharonLogsPath, string.Format("{0:yyyy_MM_dd_hh}.charon.unity.log", DateTime.UtcNow)))  },
						}
					}
				});

				runTask.ContinueWith(t =>
				{
					var result = t.GetResult();
					if (result.ExitCode != 0)
						throw new InvalidOperationException(result.GetErrorData() ?? string.Format("An error occurred. Process exited with code: {0}.", result.ExitCode));
					else
						return result;
				});

				return runTask;
			}
			catch (Exception runError)
			{
				var failed = new Promise<RunResult>();
				failed.TrySetFailed(runError.Unwrap());
				return failed;
			}
		}
		private static string[] FindAndLoadScriptingAssemblies(string gameDataPath)
		{
			var gameDataSettings = GameDataSettings.Load(gameDataPath);
			// enumerate loaded assemblies by name and fullname
			var loadedAssemblies = (
				from assembly in AppDomain.CurrentDomain.GetAssemblies()
				where assembly is AssemblyBuilder == false && string.IsNullOrEmpty(assembly.Location) == false
				let assemblyName = assembly.GetName(false)
				from name in new[] { assemblyName.Name, assemblyName.FullName }
				select new { name, assembly.Location }
			).ToLookup(a => a.name, a => a.Location);
			// get scripting assemblies from gamedata settings and add all internal scripting assemblies with prefix "Assembly-"
			var scriptingAssemblies = (gameDataSettings.ScriptingAssemblies ?? Enumerable.Empty<string>())
				.Union(loadedAssemblies.Where(k => k.Key.StartsWith("Assembly-", StringComparison.Ordinal)).Select(k => k.Key).Distinct());

			var foundScriptingAssemblies = new HashSet<string>();
			foreach (var assemblyName in scriptingAssemblies)
			{
				var path = assemblyName;
				// found as path
				if (File.Exists(path))
				{
					foundScriptingAssemblies.Add(Path.GetFullPath(path));
					continue;
				}
				path = loadedAssemblies[assemblyName].FirstOrDefault();
				if (path != null && File.Exists(path))
				{
					foundScriptingAssemblies.Add(Path.GetFullPath(path));
					continue;
				}

				try
				{
					var assembly = AppDomain.CurrentDomain.Load(assemblyName);
					if (File.Exists(assembly.Location))
						foundScriptingAssemblies.Add(Path.GetFullPath(assembly.Location));
					continue;
				}
				catch (Exception loadError)
				{
					if (Settings.Current.Verbose)
						Debug.LogWarning("Failed to load assembly with name '" + assemblyName + "':\r\n" + loadError);
				}

				Debug.LogWarning("Failed to find scripting assembly with name '" + assemblyName + "'.");
			}

			return foundScriptingAssemblies.ToArray();
		}
		internal static bool IsToolsLegacy()
		{
			var toolsVersion = default(SemanticVersion);
			try
			{
				toolsVersion = new SemanticVersion(Settings.Current.EditorVersion);
			}
			catch
			{
				/* ignore parsing errors */
			}

			return toolsVersion == null || toolsVersion <= LegacyToolsVersion;
		}
	}
}
