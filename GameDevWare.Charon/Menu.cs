/*
	Copyright (c) 2016 Denis Zykov

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
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using GameDevWare.Charon.Async;
using GameDevWare.Charon.Json;
using GameDevWare.Charon.Utils;
using GameDevWare.Charon.Windows;
using UnityEditor;
using UnityEngine;

// ReSharper disable UnusedMember.Local
namespace GameDevWare.Charon
{
	internal static class Menu
	{
		public const string TOOLS_PREFIX = "Tools/Charon/";
		public const string TROUBLESHOOTING_PREFIX = TOOLS_PREFIX + Resources.UI_UNITYPLUGIN_MENU_TROUBLESHOOTING + "/";
		public static bool WillingToUpdate;

		[MenuItem(TOOLS_PREFIX + Resources.UI_UNITYPLUGIN_MENU_GENERATE_CODE_AND_ASSETS, false, 2)]
		private static void GenerateCodeAndAssets()
		{
			if (!GenerateCodeAndAssetsCheck()) return;

			var generateCoroutine = CoroutineScheduler.Schedule
			(
				GenerateCodeAndAssetsAsync(
					progressCallback: ProgressUtils.ShowProgressBar(Resources.UI_UNITYPLUGIN_GENERATING_CODE_AND_ASSETS))
			);
			generateCoroutine.ContinueWith(ProgressUtils.HideProgressBar);
			FocusConsoleWindow();
		}
		[MenuItem(TOOLS_PREFIX + Resources.UI_UNITYPLUGIN_MENU_GENERATE_CODE_AND_ASSETS, true, 2)]
		private static bool GenerateCodeAndAssetsCheck()
		{
			return !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
		}

		[MenuItem(TOOLS_PREFIX + Resources.UI_UNITYPLUGIN_MENU_VALIDATE_ASSETS, false, 3)]
		private static void ValidateAll()
		{
			if (!ValidateAllCheck()) return;

			var validateCoroutine = CoroutineScheduler.Schedule<Dictionary<string, object>>(
				ValidateAsync(
					progressCallback: ProgressUtils.ShowProgressBar(Resources.UI_UNITYPLUGIN_VALIDATING_ASSETS)
				)
			);
			validateCoroutine.ContinueWith(ProgressUtils.HideProgressBar);
			FocusConsoleWindow();
		}
		[MenuItem(TOOLS_PREFIX + Resources.UI_UNITYPLUGIN_MENU_VALIDATE_ASSETS, true, 3)]
		private static bool ValidateAllCheck()
		{
			return !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
		}

		[MenuItem(TOOLS_PREFIX + Resources.UI_UNITYPLUGIN_MENU_EXTRACT_T4_TEMPLATES, false, 5)]
		private static void ExtractT4Templates()
		{
			if (!ExtractT4TemplatesCheck())
				return;

			var extractionPath = EditorUtility.OpenFolderPanel(Resources.UI_UNITYPLUGIN_SPECIFY_EXTRACTION_LOC_TITLE, "", "");
			if (string.IsNullOrEmpty(extractionPath))
				return;

			CoroutineScheduler.Schedule
			(
				ExtractT4Templates(extractionPath)
			);
			FocusConsoleWindow();
		}
		[MenuItem(TOOLS_PREFIX + Resources.UI_UNITYPLUGIN_MENU_EXTRACT_T4_TEMPLATES, true, 5)]
		private static bool ExtractT4TemplatesCheck()
		{
			return !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
		}

		[MenuItem(TROUBLESHOOTING_PREFIX + Resources.UI_UNITYPLUGIN_MENU_VERBOSE_LOGS, false, 9)]
		private static void VerboseLogs()
		{
			Settings.Current.Verbose = !Settings.Current.Verbose;
			UnityEditor.Menu.SetChecked(TROUBLESHOOTING_PREFIX + Resources.UI_UNITYPLUGIN_MENU_VERBOSE_LOGS, Settings.Current.Verbose);
			Settings.Current.Save();
		}

		[MenuItem(TROUBLESHOOTING_PREFIX + Resources.UI_UNITYPLUGIN_MENU_REPORT_ISSUE, false, 7)]
		private static void ReportIssue()
		{
			EditorWindow.GetWindow<ReportIssueWindow>(utility: true);
		}

		[MenuItem(TROUBLESHOOTING_PREFIX + Resources.UI_UNITYPLUGIN_MENU_OPEN_LOGS, false, 8)]
		private static void OpenLogs()
		{
			if (string.IsNullOrEmpty(ReportIssueWindow.CharonLogPath) == false && File.Exists(ReportIssueWindow.CharonLogPath))
				EditorUtility.OpenWithDefaultApp(ReportIssueWindow.CharonLogPath);
		}

		[MenuItem(TROUBLESHOOTING_PREFIX + Resources.UI_UNITYPLUGIN_MENU_OPEN_LOGS, true, 8)]
		private static bool OpenLogsCheck()
		{
			return string.IsNullOrEmpty(ReportIssueWindow.CharonLogPath) == false && File.Exists(ReportIssueWindow.CharonLogPath);
		}

		[MenuItem(TROUBLESHOOTING_PREFIX + Resources.UI_UNITYPLUGIN_MENU_CHECK_RUNTIME, false, 6)]
		private static void CheckRuntime()
		{
			UpdateRuntimeWindow.ShowAsync(autoClose: false);
		}

		[MenuItem(TOOLS_PREFIX + Resources.UI_UNITYPLUGIN_MENU_DOCUMENTATION, false, 10)]
		private static void ShowDocumentation()
		{
			Application.OpenURL("https://gamedevware.com/docs/");
		}

		[MenuItem(TOOLS_PREFIX + Resources.UI_UNITYPLUGIN_MENU_CHECK_UPDATES, false, 11)]
		private static void CheckUpdates()
		{
			if (!CheckUpdatesCheck()) return;

			var checkUpdatesCheck = CoroutineScheduler.Schedule(
				CheckForUpdatesAsync(
					progressCallback: ProgressUtils.ShowProgressBar(Resources.UI_UNITYPLUGIN_MENU_CHECK_UPDATES)
				)
			);
			checkUpdatesCheck.ContinueWith(ProgressUtils.HideProgressBar);
			FocusConsoleWindow();
		}

		[MenuItem(TOOLS_PREFIX + Resources.UI_UNITYPLUGIN_MENU_CHECK_UPDATES, true, 11)]
		private static bool CheckUpdatesCheck()
		{
			return !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
		}

		[MenuItem(TOOLS_PREFIX + Resources.UI_UNITYPLUGIN_MENU_ABOUT, false, 12)]
		private static void About()
		{
			EditorWindow.GetWindow<AboutWindow>(utility: true);
		}

		[MenuItem("Assets/Create/" + Resources.UI_UNITYPLUGIN_MENU_CREATE_GAMEDATA_JSON)]
		private static void CreateGameDataAsset()
		{
			if (!CreateGameDataAssetCheck()) return;

			var location = Path.GetFullPath(AssetDatabase.GetAssetPath(Selection.activeObject));
			if (File.Exists(location))
				location = Path.GetDirectoryName(location);

			System.Diagnostics.Debug.Assert(location != null, "location != null");

			var i = 1;
			var gameDataPath = Path.Combine(location, "GameData.json");

			while (File.Exists(gameDataPath))
				gameDataPath = Path.Combine(location, "GameData" + (i++) + ".gdjs");


			gameDataPath = PathUtils.MakeProjectRelative(gameDataPath);

			File.WriteAllText(gameDataPath, "");
			AssetDatabase.Refresh();
		}
		[MenuItem("Assets/Create/" + Resources.UI_UNITYPLUGIN_MENU_CREATE_GAMEDATA_JSON, true)]
		private static bool CreateGameDataAssetCheck()
		{
			if (Selection.activeObject == null)
				return false;

			return !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
		}

		private static void FocusConsoleWindow()
		{
			var consoleWindowType = typeof(SceneView).Assembly.GetType("UnityEditor.ConsoleWindow", throwOnError: false);
			if (consoleWindowType == null)
				return;

			var consoleWindow = EditorWindow.GetWindow(consoleWindowType);
			consoleWindow.Focus();
		}

		public static IEnumerable GenerateCodeAndAssetsAsync(string path = null, Action<string, float> progressCallback = null)
		{
			switch (CharonCli.CheckRequirements())
			{
				case RequirementsCheckResult.MissingRuntime: yield return UpdateRuntimeWindow.ShowAsync(); break;
				case RequirementsCheckResult.MissingExecutable: yield return CharonCli.UpdateCharonExecutableAsync(progressCallback); break;
				case RequirementsCheckResult.Ok: break;
				default: throw new InvalidOperationException("Unknown Tools check result.");
			}

			var paths = !string.IsNullOrEmpty(path) ? new[] { path } : GameDataTracker.All.ToArray();
			var total = paths.Length;
			var forceReImportList = new List<string>();
			for (var i = 0; i < paths.Length; i++)
			{
				var gameDataPath = paths[i];
				if (File.Exists(gameDataPath) == false)
					continue;
				if (progressCallback != null) progressCallback(string.Format(Resources.UI_UNITYPLUGIN_PROGRESS_CURRENT_TARGET_IS, gameDataPath), (float)i / total);

				var gameDataObj = AssetDatabase.LoadAssetAtPath(gameDataPath, typeof(UnityEngine.Object));
				var assetImport = AssetImporter.GetAtPath(gameDataPath);
				if (assetImport == null)
					continue;

				var gameDataSettings = GameDataSettings.Load(gameDataObj);
				var codeGenerationPath = PathUtils.MakeProjectRelative(gameDataSettings.CodeGenerationPath);
				if (gameDataSettings.Generator == (int)GameDataSettings.CodeGenerator.None)
					continue;

				var generationOptions = gameDataSettings.Options;
				if (Array.IndexOf(Settings.SupportedExtensions, Settings.EXTENSION_EXPRESSIONS) == -1) // no expression library installed
					generationOptions |= (int)CodeGenerationOptions.DisableExpressions;

				// trying to touch gamedata file
				var touchGamedata = new Coroutine<FileStream>(TouchGameDataFile(gameDataPath));
				yield return touchGamedata;
				touchGamedata.GetResult().Dispose(); // release touched file

				var generator = (GameDataSettings.CodeGenerator)gameDataSettings.Generator;
				switch (generator)
				{
					case GameDataSettings.CodeGenerator.CSharpCodeAndAsset:
						if (!string.IsNullOrEmpty(gameDataSettings.AssetGenerationPath))
						{
							AssetGenerator.AddPath(gameDataPath);
							generationOptions &= ~(int)(CodeGenerationOptions.DisableJsonSerialization |
								CodeGenerationOptions.DisableBsonSerialization |
								CodeGenerationOptions.DisableMessagePackSerialization |
								CodeGenerationOptions.DisableXmlSerialization
							);
						}
						goto generateCSharpCode;
					case GameDataSettings.CodeGenerator.CSharp:
						generateCSharpCode:
						if (Settings.Current.Verbose)
							Debug.Log(string.Format("Generating C# code for '{0}'...", gameDataPath));
						if (progressCallback != null)
							progressCallback(string.Format(Resources.UI_UNITYPLUGIN_GENERATE_CODE_FOR, gameDataPath), (float)i / total);

						var generateProcess = generator == GameDataSettings.CodeGenerator.CSharp
							? CharonCli.GenerateCSharpCodeAsync
							(
								gameDataPath,
								Path.GetFullPath(codeGenerationPath),
								(CodeGenerationOptions)generationOptions,
								gameDataSettings.DocumentClassName,
								gameDataSettings.GameDataClassName,
								gameDataSettings.Namespace
							)
							: CharonCli.GenerateUnityCSharpCodeAsync
							(
								gameDataPath,
								Path.GetFullPath(codeGenerationPath),
								(CodeGenerationOptions)generationOptions,
								gameDataSettings.DocumentClassName,
								gameDataSettings.GameDataClassName,
								gameDataSettings.Namespace
							);
						yield return generateProcess;

						if (Settings.Current.Verbose)
							Debug.Log(string.Format("Generation complete, exit code: '{0}'", generateProcess.GetResult().ExitCode));
						using (var generateResult = generateProcess.GetResult())
						{
							if (generateResult.ExitCode != 0)
							{
								Debug.LogWarning(string.Format(Resources.UI_UNITYPLUGIN_GENERATE_FAILED_DUE_ERRORS, gameDataPath, generateResult.GetErrorData()));
							}
							else
							{
								if (Settings.Current.Verbose)
									Debug.Log(string.Format("Code generation for '{0}' is complete.", gameDataPath));

								forceReImportList.Add(codeGenerationPath);

								if (gameDataSettings.LineEnding != 0 ||
									gameDataSettings.Indentation != 0)
								{
									if (progressCallback != null)
										progressCallback(string.Format(Resources.UI_UNITYPLUGIN_GENERATE_REFORMAT_CODE, gameDataPath), (float)i / total);

									var code = new StringBuilder(File.ReadAllText(codeGenerationPath));
									switch ((GameDataSettings.LineEndings)gameDataSettings.LineEnding)
									{
										case GameDataSettings.LineEndings.Windows:
											// already windows
											break;
										case GameDataSettings.LineEndings.Unix:
											code.Replace("\r\n", "\n");
											break;
										default:
											throw new InvalidOperationException(string.Format("Unknown LineEnding value '{0}' is set for {1}", gameDataSettings.LineEnding, gameDataPath));
									}
									switch ((GameDataSettings.Indentations)gameDataSettings.Indentation)
									{
										case GameDataSettings.Indentations.Tab:
											// already tabs
											break;
										case GameDataSettings.Indentations.FourSpaces:
											code.Replace("\t", "    ");
											break;
										case GameDataSettings.Indentations.TwoSpaces:
											code.Replace("\t", "  ");
											break;
										default:
											throw new InvalidOperationException(string.Format("Unknown indentation value '{0}' is set for {1}", gameDataSettings.Indentation, gameDataPath));
									}
									File.WriteAllText(codeGenerationPath, code.ToString());
								}
							}
						}
						break;
					default:
						Debug.LogError("Unknown code/asset generator type " + (GameDataSettings.CodeGenerator)gameDataSettings.Generator + ".");
						break;
				}
			}
			if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_GENERATE_REFRESHING_ASSETS, 0.99f);
			foreach (var forceReImportPath in forceReImportList)
				AssetDatabase.ImportAsset(forceReImportPath, ImportAssetOptions.ForceUpdate);
			if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1);
		}
		public static IEnumerable GenerateAssetsAsync(string[] paths, Action<string, float> progressCallback = null)
		{
			var total = paths.Length;
			for (var i = 0; i < paths.Length; i++)
			{
				var gameDataPath = paths[i];
				if (File.Exists(gameDataPath) == false)
					continue;
				if (progressCallback != null) progressCallback(string.Format(Resources.UI_UNITYPLUGIN_PROGRESS_CURRENT_TARGET_IS, gameDataPath), (float)i / total);


				var gameDataObj = AssetDatabase.LoadAssetAtPath(gameDataPath, typeof(UnityEngine.Object));
				var assetImport = AssetImporter.GetAtPath(gameDataPath);
				if (assetImport == null)
					continue;

				var gameDataSettings = GameDataSettings.Load(gameDataObj);
				var assetGenerationPath = PathUtils.MakeProjectRelative(gameDataSettings.AssetGenerationPath);
				if (string.IsNullOrEmpty(assetGenerationPath))
					continue;

				// trying to touch gamedata file
				var touchGamedata = new Coroutine<FileStream>(TouchGameDataFile(gameDataPath));
				yield return touchGamedata;

				using (var file = touchGamedata.GetResult())
				{
					var gameDataBytes = new byte[file.Length];
					int read, offset = 0;
					while ((read = file.Read(gameDataBytes, offset, gameDataBytes.Length - offset)) > 0)
						offset += read;

					var gameDataType = Type.GetType(gameDataSettings.Namespace + "." + gameDataSettings.GameDataClassName + ", Assembly-CSharp", throwOnError: false) ??
									   Type.GetType(gameDataSettings.Namespace + "." + gameDataSettings.GameDataClassName + ", Assembly-CSharp-firstpass", throwOnError: false) ??
									   Type.GetType(gameDataSettings.Namespace + "." + gameDataSettings.GameDataClassName + ", Assembly-CSharp-Editor", throwOnError: false);
					if (gameDataType == null)
					{
						Debug.LogError(Resources.UI_UNITYPLUGIN_GENERATE_ASSET_CANT_FIND_GAMEDATA_CLASS);
						continue;
					}

					var gameDataAsset = ScriptableObject.CreateInstance(gameDataType);
					gameDataAsset.SetFieldValue("dataBytes", gameDataBytes);
					gameDataAsset.SetFieldValue("format", 0);
					AssetDatabase.CreateAsset(gameDataAsset, assetGenerationPath);
					AssetDatabase.SaveAssets();
				}
			}
			if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_GENERATE_REFRESHING_ASSETS, 0.99f);
			AssetDatabase.Refresh(ImportAssetOptions.Default);
			if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1);
		}
		public static IEnumerable ValidateAsync(string path = null, Action<string, float> progressCallback = null)
		{
			switch (CharonCli.CheckRequirements())
			{
				case RequirementsCheckResult.MissingRuntime: yield return UpdateRuntimeWindow.ShowAsync(); break;
				case RequirementsCheckResult.MissingExecutable: yield return CharonCli.UpdateCharonExecutableAsync(progressCallback); break;
				case RequirementsCheckResult.Ok: break;
				default: throw new InvalidOperationException("Unknown Tools check result.");
			}

			var reports = new Dictionary<string, object>();
			var paths = !string.IsNullOrEmpty(path) ? new[] { path } : GameDataTracker.All.ToArray();
			var total = paths.Length;
			for (var i = 0; i < paths.Length; i++)
			{
				var gameDataPath = paths[i];
				if (File.Exists(gameDataPath) == false)
					continue;
				if (progressCallback != null) progressCallback(string.Format(Resources.UI_UNITYPLUGIN_PROGRESS_CURRENT_TARGET_IS, gameDataPath), (float)i / total);

				if (Settings.Current.Verbose) Debug.Log(string.Format("Validating GameData at '{0}'...", gameDataPath));
				if (progressCallback != null) progressCallback(string.Format(Resources.UI_UNITYPLUGIN_VALIDATE_RUN_FOR, gameDataPath), (float)i / total);

				var output = CommandOutput.CaptureJson();
				var validateProcess = CharonCli.ValidateAsync(Path.GetFullPath(gameDataPath), ValidationOptions.None, output);
				yield return validateProcess;

				using (var validateResult = validateProcess.GetResult())
				{
					if (Settings.Current.Verbose) Debug.Log(string.Format("Validation complete, exit code: '{0}'", validateResult.ExitCode));

					if (validateResult.ExitCode != 0)
					{
						reports.Add(gameDataPath, validateProcess.GetResult().GetErrorData());
						Debug.LogWarning(string.Format(Resources.UI_UNITYPLUGIN_VALIDATE_FAILED_DUE_ERRORS, gameDataPath, validateResult.GetErrorData()));
					}
					else
					{
						try
						{
							var report = output.ReadJsonAs<JsonObject>();
							reports.Add(gameDataPath, report);
							var success = (bool)report["success"];
							var totalErrors = 0;
							if (!success)
							{
								var items = (JsonArray)report["items"];
								System.Diagnostics.Debug.Assert(items != null, "items != null");
								foreach (var record in items.Cast<JsonObject>())
								{
									var errors = record.ContainsKey("errors") ? ((JsonArray)record["errors"]) : null;
									if (errors != null)
									{
										foreach (var error in errors.Cast<JsonObject>())
										{
											var id = record["id"] is JsonPrimitive ? Convert.ToString(((JsonPrimitive)record["id"]).Value) : record["id"].Stringify();
											var entityName = (string)record["entityName"];
											var msg = (string)error["msg"];
											var errorPath = (string)error["path"];

											var validationException = new ValidationException(gameDataPath, id, entityName, errorPath, msg);

											var log = (Action<Exception>)Debug.LogException;
											log.BeginInvoke(validationException, null, null);
										}
										totalErrors += errors.Count;
									}
								}
							}

							Debug.Log(string.Format(Resources.UI_UNITYPLUGIN_VALIDATE_COMPLETE, gameDataPath, success ? "success" : "failure", totalErrors));

						}
						catch (Exception e)
						{
							Debug.LogWarning(string.Format(Resources.UI_UNITYPLUGIN_VALIDATE_FAILED_DUE_ERRORS, gameDataPath, e));
							reports[gameDataPath] = e.Unwrap().ToString();
						}
					}
				}
			}
			if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1);
			yield return reports;
		}
		public static IEnumerable ExtractT4Templates(string extractionPath)
		{
			switch (CharonCli.CheckRequirements())
			{
				case RequirementsCheckResult.MissingRuntime: yield return UpdateRuntimeWindow.ShowAsync(); break;
				case RequirementsCheckResult.MissingExecutable: yield return CharonCli.UpdateCharonExecutableAsync(ProgressUtils.ReportToLog(Resources.UI_UNITYPLUGIN_MENU_CHECK_UPDATES)); break;
				case RequirementsCheckResult.Ok: break;
				default: throw new InvalidOperationException("Unknown Tools check result.");
			}

			if (Settings.Current.Verbose) Debug.Log(string.Format("Extracting T4 Templates to '{0}'...", extractionPath));
			var dumpProcess = CharonCli.DumpTemplatesAsync(extractionPath);
			yield return dumpProcess;

			using (var dumpResult = dumpProcess.GetResult())
			{
				if (string.IsNullOrEmpty(dumpResult.GetErrorData()) == false)
					Debug.LogWarning(string.Format(Resources.UI_UNITYPLUGIN_T4_EXTRACTION_FAILED, dumpResult.GetErrorData()));
				else
					Debug.Log(Resources.UI_UNITYPLUGIN_T4_EXTRACTION_COMPLETE + "\r\n" + dumpResult.GetOutputData());
			}
		}
		public static IEnumerable TouchGameDataFile(string path)
		{
			if (path == null) throw new ArgumentNullException("path");

			var gameDataFile = default(FileStream);
			foreach (var attempt in Enumerable.Range(1, 5))
			{
				try
				{
					gameDataFile = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
					break;
				}
				catch (IOException e)
				{
					if (Settings.Current.Verbose)
						Debug.LogWarning("Attempt #" + attempt + " to touch " + path + " file has failed with IO error: " + Environment.NewLine + e);
					if (gameDataFile != null)
						gameDataFile.Dispose();
				}
				yield return Promise.Delayed(TimeSpan.FromSeconds(1));
			}
			yield return gameDataFile;
		}
		public static IEnumerable CheckForUpdatesAsync(Action<string, float> progressCallback = null)
		{
			foreach (var step in CheckForCharonUpdatesAsync(progressCallback))
				yield return step;
			foreach (var step in CheckForUnityAssetUpdatesAsync(progressCallback))
				yield return step;
		}
		public static IEnumerable CheckForUnityAssetUpdatesAsync(Action<string, float> progressCallback = null)
		{
			var assetPath = typeof(Settings).Assembly.Location;
			var currentVersion = Settings.GetCurrentAssetVersion();
			if (currentVersion == null || assetPath == null)
			{
				Debug.Log("This asset is build from sources and can't be updated.");
				yield break;
			}

			var toolName = Path.GetFileNameWithoutExtension(Path.GetFileName(assetPath));

			if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_GETTING_AVAILABLE_BUILDS, 0.10f);

			var updateServerAddress = Settings.Current.GetServerAddress();
			var getBuildsHeaders = new NameValueCollection { { "Accept", "application/json" } };
			var getBuildsUrl = new Uri(updateServerAddress, "Build?product=Charon_Unity");
			var getBuildsRequest = HttpUtils.GetJson<JsonValue>(getBuildsUrl, getBuildsHeaders);
			yield return getBuildsRequest;

			var response = getBuildsRequest.GetResult();
			if (response["error"] != null) throw new InvalidOperationException(string.Format("Request to '{0}' has failed with message from server: {1}.", getBuildsUrl, response["error"].Stringify()));
			var builds = (JsonArray)response["result"];
			var lastBuild =
			(
				from build in builds
				let buildObj = (JsonObject)build
				let version = new Version(buildObj["Version"].As<string>())
				orderby version descending
				select build
			).FirstOrDefault();
			var availableVersion = lastBuild != null ? new Version(lastBuild["Version"].As<string>()) : null;

			if (availableVersion == null || currentVersion >= availableVersion)
			{
				if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1.0f);

				Debug.Log(string.Format("{0} version '{1}' is up to date.", Path.GetFileName(assetPath), currentVersion));
				yield break;
			}

			var availableBuildSize = lastBuild["Size"].As<long>();
			if (!AskForUpdate(currentVersion, toolName, availableVersion, availableBuildSize))
			{
				yield break;
			}

			if (progressCallback != null) progressCallback(string.Format(Resources.UI_UNITYPLUGIN_PROGRESS_DOWNLOADINGS, 0, 0), 0.10f);

			var downloadHeaders = new NameValueCollection { { "Accept", "application/octet-stream" } };
			var downloadUrl = new Uri(updateServerAddress, "Build?product=Charon_Unity&id=" + Uri.EscapeDataString(availableVersion.ToString()));
			var downloadPath = Path.GetTempFileName();
			yield return HttpUtils.DownloadToFile(downloadUrl, downloadPath, downloadHeaders, (read, total) =>
			{
				if (progressCallback == null || total == 0)
					return;
				progressCallback(string.Format(Resources.UI_UNITYPLUGIN_PROGRESS_DOWNLOADINGS, (float)read / 1024 / 1024, total / 1024 / 1024), 0.10f + (0.80f * Math.Min(1.0f, (float)read / total)));
			});

			var windows = UnityEngine.Resources.FindObjectsOfTypeAll<GameDataEditorWindow>();
			foreach (var window in windows) window.Close();
			GameDataEditorProcess.EndGracefully();

			try
			{
				File.Delete(assetPath);
				File.Move(downloadPath, assetPath);
			}
			catch (Exception moveError)
			{
				Debug.LogWarning(string.Format("Failed to move downloaded file from '{0}' to {1}. {2}.", downloadPath, assetPath, moveError.Message));
				Debug.LogError(moveError);
			}
			finally
			{
				// ReSharper disable once EmptyGeneralCatchClause
				try { if (File.Exists(downloadPath)) File.Delete(downloadPath); }
				catch { }
			}

			Debug.Log(string.Format("{1} version is '{0}'. Update is complete.", availableVersion, Path.GetFileName(assetPath)));

			if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1.0f);

			AssetDatabase.ImportAsset(PathUtils.MakeProjectRelative(assetPath), ImportAssetOptions.ForceSynchronousImport);
		}
		public static IEnumerable CheckForCharonUpdatesAsync(Action<string, float> progressCallback = null)
		{
			if (CharonCli.CheckRequirements() == RequirementsCheckResult.MissingRuntime)
			{
				Debug.LogWarning("Missing required runtime.");
				yield break;
			}

			var currentVersion = default(Version);
			var charonPath = Path.GetFullPath(Settings.CharonPath);
			var charonConfigPath = charonPath + ".config";
			var toolName = Path.GetFileNameWithoutExtension(Path.GetFileName(charonPath));

			if (File.Exists(charonPath))
			{
				if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_CHECKING_TOOLS_VERSION, 0.05f);

				var checkToolsVersion = CharonCli.GetVersionAsync();
				yield return checkToolsVersion.IgnoreFault();

				currentVersion = checkToolsVersion.HasErrors ? default(Version) : checkToolsVersion.GetResult();
			}

			if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_GETTING_AVAILABLE_BUILDS, 0.10f);

			var updateServerAddress = Settings.Current.GetServerAddress();
			var getBuildsHeaders = new NameValueCollection { { "Accept", "application/json" } };
			var getBuildsUrl = new Uri(updateServerAddress, "Build?product=Charon");
			var getBuildsRequest = HttpUtils.GetJson<JsonValue>(getBuildsUrl, getBuildsHeaders);
			yield return getBuildsRequest;
			var response = getBuildsRequest.GetResult();
			if (response["error"] != null) throw new InvalidOperationException(string.Format("Request to '{0}' has failed with message from server: {1}.", getBuildsUrl, response["error"].Stringify()));
			var builds = (JsonArray)response["result"];
			var buildsByVersion = builds.ToDictionary(b => new Version(b["Version"].As<string>()));
			var lastBuild =
			(
				from buildKv in buildsByVersion
				orderby buildKv.Key descending
				select buildKv.Value
			).FirstOrDefault();
			var availableVersion = lastBuild != null ? new Version(lastBuild["Version"].As<string>()) : null;

			if (availableVersion == null || (currentVersion != null && currentVersion >= availableVersion))
			{
				if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1.0f);

				Debug.Log(string.Format("{0} version '{1}' is up to date.", Path.GetFileName(charonPath), currentVersion));
				yield break;
			}

			var availableBuildSize = lastBuild["Size"].As<long>();
			var desiredVersion = string.IsNullOrEmpty(Settings.Current.EditorVersion) ? null : new Version(Settings.Current.EditorVersion);
			if (currentVersion == null && desiredVersion != null && buildsByVersion.ContainsKey(desiredVersion))
			{
				availableVersion = desiredVersion;
			}
			if (currentVersion != null && !AskForUpdate(currentVersion, toolName, availableVersion, availableBuildSize))
			{
				yield break;
			}

			if (progressCallback != null) progressCallback(string.Format(Resources.UI_UNITYPLUGIN_PROGRESS_DOWNLOADINGS, 0, 0), 0.10f);

			var downloadHeaders = new NameValueCollection { { "Accept", "application/octet-stream" } };
			var downloadUrl = new Uri(updateServerAddress, "Build?product=Charon&id=" + Uri.EscapeDataString(availableVersion.ToString()));
			var downloadPath = Path.GetTempFileName();
			yield return HttpUtils.DownloadToFile(downloadUrl, downloadPath, downloadHeaders, (read, total) =>
			{
				if (progressCallback == null || total == 0)
					return;
				progressCallback(string.Format(Resources.UI_UNITYPLUGIN_PROGRESS_DOWNLOADINGS, (float)read / 1024 / 1024, total / 1024 / 1024), 0.10f + (0.80f * Math.Min(1.0f, (float)read / total)));
			});

			var windows = UnityEngine.Resources.FindObjectsOfTypeAll<GameDataEditorWindow>();
			foreach (var window in windows) window.Close();
			GameDataEditorProcess.EndGracefully();

			try
			{
				if (File.Exists(charonPath))
					File.Delete(charonPath);
				if (Directory.Exists(charonPath))
					Directory.Delete(charonPath);

				var toolsDirectory = Path.GetDirectoryName(charonPath);
				System.Diagnostics.Debug.Assert(toolsDirectory != null, "toolsDirectory != null");
				if (Directory.Exists(toolsDirectory) == false)
					Directory.CreateDirectory(toolsDirectory);

				File.Move(downloadPath, charonPath);

				// ensure config file
				if (File.Exists(charonConfigPath) == false)
				{
					var embeddedConfigStream = typeof(Menu).Assembly.GetManifestResourceStream("GameDevWare.Charon.Charon.exe.config");
					if (embeddedConfigStream != null)
					{
						using (embeddedConfigStream)
						using (var configFileStream = File.Create(charonConfigPath, 8 * 1024, FileOptions.SequentialScan))
						{
							var buffer = new byte[8 * 1024];
							var read = 0;
							while ((read = embeddedConfigStream.Read(buffer, 0, buffer.Length)) > 0)
								configFileStream.Write(buffer, 0, read);
						}
					}
				}
			}
			catch (Exception moveError)
			{
				Debug.LogWarning(string.Format("Failed to move downloaded file from '{0}' to {1}. {2}.", downloadPath, charonPath, moveError.Message));
				Debug.LogError(moveError);
			}
			finally
			{
				// ReSharper disable once EmptyGeneralCatchClause
				try { if (File.Exists(downloadPath)) File.Delete(downloadPath); }
				catch { }
			}

			if (File.Exists(charonPath))
			{
				if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_CHECKING_TOOLS_VERSION, 0.95f);

				var checkToolsVersion = CharonCli.GetVersionAsync();
				yield return checkToolsVersion.IgnoreFault();
				currentVersion = checkToolsVersion.HasErrors ? default(Version) : checkToolsVersion.GetResult();
			}

			Settings.Current.EditorVersion = currentVersion != null ? currentVersion.ToString() : null;
			Settings.Current.Save();

			Debug.Log(string.Format("{1} version is '{0}'. Update is complete.", currentVersion, Path.GetFileName(charonPath)));

			if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1.0f);
		}

		private static bool AskForUpdate(Version currentVersion, string toolName, Version availableVersion, long availableBuildSize)
		{
			if (WillingToUpdate)
				return true;

			return WillingToUpdate = EditorUtility.DisplayDialog(
								Resources.UI_UNITYPLUGIN_UPDATE_AVAILABLE_TITLE,
								string.Format(Resources.UI_UNITYPLUGIN_UPDATE_AVAILABLE_MESSAGE, currentVersion, availableVersion, toolName),
								string.Format(Resources.UI_UNITYPLUGIN_DOWNLOAD_BUTTON, availableBuildSize / 1024.0f / 1024.0f));
		}
	}
}

