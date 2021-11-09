/*
	Copyright (c) 2017 Denis Zykov

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
using System.IO;
using System.Linq;
using System.Text;
using GameDevWare.Charon.Unity.Async;
using GameDevWare.Charon.Unity.Json;
using GameDevWare.Charon.Unity.Utils;
using GameDevWare.Charon.Unity.Windows;
using UnityEditor;
using UnityEngine;

// ReSharper disable UnusedMember.Local
namespace GameDevWare.Charon.Unity
{
	internal static class Menu
	{
		public const string TOOLS_PREFIX = "Tools/Charon/";
		public const string TROUBLESHOOTING_PREFIX = TOOLS_PREFIX + Resources.UI_UNITYPLUGIN_MENU_TROUBLESHOOTING + "/";
		public const string ADVANCED_PREFIX = TOOLS_PREFIX + Resources.UI_UNITYPLUGIN_MENU_ADVANCED + "/";

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

		[MenuItem(ADVANCED_PREFIX + Resources.UI_UNITYPLUGIN_MENU_EXTRACT_T4_TEMPLATES, false, 8)]
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
		[MenuItem(ADVANCED_PREFIX + Resources.UI_UNITYPLUGIN_MENU_EXTRACT_T4_TEMPLATES, true, 10)]
		private static bool ExtractT4TemplatesCheck()
		{
			return !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
		}

		[MenuItem(TROUBLESHOOTING_PREFIX + Resources.UI_UNITYPLUGIN_MENU_SEND_FEEDBACK, false, 11)]
		private static void SendFeedback()
		{
			EditorWindow.GetWindow<FeedbackWindow>(utility: true);
		}

		[MenuItem(TROUBLESHOOTING_PREFIX + Resources.UI_UNITYPLUGIN_MENU_RESET_PREFERENCES, false, 14)]
		private static void ResetPreferences()
		{
			var userDataDirectory = Settings.GetLocalUserDataPath();
			if (Directory.Exists(userDataDirectory) == false)
				return;

			GameDataEditorWindow.FindAllAndClose();

			Directory.Delete(userDataDirectory, recursive: true);
		}

		[MenuItem(TROUBLESHOOTING_PREFIX + Resources.UI_UNITYPLUGIN_MENU_OPEN_LOGS, false, 17)]
		private static void OpenLogs()
		{
			var logFile = FeedbackWindow.GetCharonLogFilesSortedByCreationTime().FirstOrDefault();
			if (string.IsNullOrEmpty(logFile) == false)
			{
				EditorUtility.OpenWithDefaultApp(logFile);
			}
		}

		[MenuItem(TROUBLESHOOTING_PREFIX + Resources.UI_UNITYPLUGIN_MENU_OPEN_LOGS, true, 17)]
		private static bool OpenLogsCheck()
		{
			return string.IsNullOrEmpty(CharonCli.CharonLogsDirectory) == false && Directory.Exists(CharonCli.CharonLogsDirectory) && Directory.GetFiles(CharonCli.CharonLogsDirectory).Length > 0;
		}

		[MenuItem(TROUBLESHOOTING_PREFIX + Resources.UI_UNITYPLUGIN_MENU_VERBOSE_LOGS, false, 20)]
		private static void VerboseLogs()
		{
			Settings.Current.Verbose = !Settings.Current.Verbose;
			UnityEditor.Menu.SetChecked(TROUBLESHOOTING_PREFIX + Resources.UI_UNITYPLUGIN_MENU_VERBOSE_LOGS, Settings.Current.Verbose);
			Settings.Current.Save();
		}
		[MenuItem(TROUBLESHOOTING_PREFIX + Resources.UI_UNITYPLUGIN_MENU_VERBOSE_LOGS, true, 20)]
		private static bool VerboseLogsCheck()
		{
			UnityEditor.Menu.SetChecked(TROUBLESHOOTING_PREFIX + Resources.UI_UNITYPLUGIN_MENU_VERBOSE_LOGS, Settings.Current.Verbose);
			return true;
		}

		[MenuItem(TROUBLESHOOTING_PREFIX + Resources.UI_UNITYPLUGIN_MENU_CHECK_RUNTIME, false, 22)]
		private static void CheckRuntime()
		{
			UpdateRuntimeWindow.ShowAsync(autoClose: false);
		}

		[MenuItem(TOOLS_PREFIX + Resources.UI_UNITYPLUGIN_MENU_DOCUMENTATION, false, 25)]
		private static void ShowDocumentation()
		{
			Application.OpenURL("https://gamedevware.com/docs/");
		}

		[MenuItem(TOOLS_PREFIX + Resources.UI_UNITYPLUGIN_MENU_CHECK_UPDATES, false, 28)]
		private static void CheckUpdates()
		{
			if (!CheckUpdatesCheck()) return;

			EditorWindow.GetWindow<UpdateWindow>(utility: true);
		}

		[MenuItem(TOOLS_PREFIX + Resources.UI_UNITYPLUGIN_MENU_CHECK_UPDATES, true, 28)]
		private static bool CheckUpdatesCheck()
		{
			return !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
		}

		[MenuItem(TOOLS_PREFIX + Resources.UI_UNITYPLUGIN_MENU_SETTINGS, false, 31)]
		private static void ShowSettings()
		{
			var settingsService = typeof(UnityEditor.EditorApplication).Assembly.GetType("UnityEditor.SettingsService", throwOnError: false, ignoreCase: true);
			var preferencesWindowType = typeof(UnityEditor.EditorApplication).Assembly.GetType("UnityEditor.PreferencesWindow", throwOnError: false, ignoreCase: true);
			var settingsWindowType = typeof(UnityEditor.EditorApplication).Assembly.GetType("UnityEditor.SettingsWindow", throwOnError: false, ignoreCase: true);
			if (settingsService != null)
			{
				settingsService.Invoke("OpenUserPreferences", "Preferences/Charon");
			}
			else if (preferencesWindowType != null)
			{
				var settingsWindow = EditorWindow.GetWindow(preferencesWindowType);
				settingsWindow.Show();
				settingsWindow.Focus();
			}
			else if (settingsWindowType != null)
			{
				var settingsWindow = EditorWindow.GetWindow(settingsWindowType);
				settingsWindow.Show();
				settingsWindow.Focus();
			}
			else
			{
				Debug.LogWarning("Unable to locate preferences window. Please open it manually 'Edit -> Preferences...'.");
			}
		}

		[MenuItem("Assets/Create/" + Resources.UI_UNITYPLUGIN_MENU_CREATE_GAMEDATA + "/" + Resources.UI_UNITYPLUGIN_MENU_CREATE_GAMEDATA_JSON)]
		private static void CreateGameDataJsonAsset()
		{
			if (!CreateGameDataAssetJsonCheck()) return;

			CreateGameData("gdjs");
		}
		[MenuItem("Assets/Create/" + Resources.UI_UNITYPLUGIN_MENU_CREATE_GAMEDATA + "/" + Resources.UI_UNITYPLUGIN_MENU_CREATE_GAMEDATA_JSON, true)]
		private static bool CreateGameDataAssetJsonCheck()
		{
			if (Selection.activeObject == null)
				return false;

			return !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
		}

		[MenuItem("Assets/Create/" + Resources.UI_UNITYPLUGIN_MENU_CREATE_GAMEDATA + "/" + Resources.UI_UNITYPLUGIN_MENU_CREATE_GAMEDATA_BSON)]
		private static void CreateGameDataBsonAsset()
		{
			if (!CreateGameDataAssetBsonCheck()) return;

			CreateGameData("gdbs");
		}
		[MenuItem("Assets/Create/" + Resources.UI_UNITYPLUGIN_MENU_CREATE_GAMEDATA + "/" + Resources.UI_UNITYPLUGIN_MENU_CREATE_GAMEDATA_BSON, true)]
		private static bool CreateGameDataAssetBsonCheck()
		{
			if (Selection.activeObject == null)
				return false;

			return !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
		}

		[MenuItem("Assets/Create/" + Resources.UI_UNITYPLUGIN_MENU_CREATE_GAMEDATA + "/" + Resources.UI_UNITYPLUGIN_MENU_CREATE_GAMEDATA_XML)]
		private static void CreateGameDataXmlAsset()
		{
			if (!CreateGameDataAssetXmlCheck()) return;

			CreateGameData("gdml");
		}
		[MenuItem("Assets/Create/" + Resources.UI_UNITYPLUGIN_MENU_CREATE_GAMEDATA + "/" + Resources.UI_UNITYPLUGIN_MENU_CREATE_GAMEDATA_XML, true)]
		private static bool CreateGameDataAssetXmlCheck()
		{
			if (Selection.activeObject == null)
				return false;

			return !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
		}

		[MenuItem("Assets/Create/" + Resources.UI_UNITYPLUGIN_MENU_CREATE_GAMEDATA + "/" + Resources.UI_UNITYPLUGIN_MENU_CREATE_GAMEDATA_MESSAGEPACK)]
		private static void CreateGameDataMsgPackAsset()
		{
			if (!CreateGameDataAssetMsgPackCheck()) return;

			CreateGameData("gdmp");
		}
		[MenuItem("Assets/Create/" + Resources.UI_UNITYPLUGIN_MENU_CREATE_GAMEDATA + "/" + Resources.UI_UNITYPLUGIN_MENU_CREATE_GAMEDATA_MESSAGEPACK, true)]
		private static bool CreateGameDataAssetMsgPackCheck()
		{
			if (Selection.activeObject == null)
				return false;

			return !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
		}

		private static void CreateGameData(string extension)
		{
			if (extension == null) throw new ArgumentNullException("extension");

			var location = Path.GetFullPath(AssetDatabase.GetAssetPath(Selection.activeObject));
			if (File.Exists(location))
				location = Path.GetDirectoryName(location);

			if (string.IsNullOrEmpty(location) || Directory.Exists(location) == false)
			{
				Debug.LogWarning("Unable to create GameData file because 'Selection.activeObject' is null or wrong asset. Select Folder in Project window and try again.");
				return;
			}

			var i = 1;
			var gameDataPath = Path.Combine(location, "GameData." + extension);

			while (File.Exists(gameDataPath))
				gameDataPath = Path.Combine(location, "GameData" + (i++) + "." + extension);


			gameDataPath = FileAndPathUtils.MakeProjectRelative(gameDataPath);

			File.WriteAllText(gameDataPath, "");
			AssetDatabase.Refresh();
		}

		public static void FocusConsoleWindow()
		{
			var consoleWindowType = typeof(SceneView).Assembly.GetType("UnityEditor.ConsoleWindow", throwOnError: false);
			if (consoleWindowType == null)
				return;

			var consoleWindow = EditorWindow.GetWindow(consoleWindowType);
			consoleWindow.Focus();
		}

		public static IEnumerable GenerateCodeAndAssetsAsync(string path = null, Action<string, float> progressCallback = null)
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

			var paths = !string.IsNullOrEmpty(path) ? new[] { path } : GameDataTracker.All.ToArray();
			var total = paths.Length;
			var forceReImportList = new List<string>();
			for (var i = 0; i < paths.Length; i++)
			{
				var gameDataPath = paths[i];
				if (File.Exists(gameDataPath) == false)
				{
					continue;
				}
				if (progressCallback != null) progressCallback(string.Format(Resources.UI_UNITYPLUGIN_PROGRESS_CURRENT_TARGET_IS, gameDataPath), (float)i / total);

				var gameDataObj = AssetDatabase.LoadAssetAtPath(gameDataPath, typeof(UnityEngine.Object));
				var assetImport = AssetImporter.GetAtPath(gameDataPath);
				if (assetImport == null)
				{
					continue;
				}

				var gameDataSettings = GameDataSettings.Load(gameDataObj);
				var codeGenerationPath = FileAndPathUtils.MakeProjectRelative(gameDataSettings.CodeGenerationPath);
				if (gameDataSettings.Generator == (int)GameDataSettings.CodeGenerator.None)
				{

					continue;
				}

				var generationOptions = gameDataSettings.Options;
				if (Array.IndexOf(Settings.SupportedExtensions, Settings.EXTENSION_FORMULAS) == -1) // no expression library installed
					generationOptions |= (int)CodeGenerationOptions.DisableFormulas;

				// trying to touch gamedata file
				var touchGamedata = new Coroutine<FileStream>(TouchGameDataFile(gameDataPath));
				yield return touchGamedata;

				if (touchGamedata.GetResult().Length == 0)
				{
					if (Settings.Current.Verbose)
						Debug.LogWarning(string.Format("Code generation was skipped for an empty file '{0}'.", gameDataPath));
					continue;
				}
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

							var assetCodeGenerationPath = Path.Combine(Path.GetDirectoryName(gameDataSettings.CodeGenerationPath) ?? "",
								gameDataSettings.GameDataClassName + "Asset.cs");
							var assetCodeGenerator = new AssetLoaderGenerator();
							assetCodeGenerator.AssetClassName = gameDataSettings.GameDataClassName + "Asset";
							assetCodeGenerator.GameDataClassName = gameDataSettings.Namespace + "." + gameDataSettings.GameDataClassName;
							assetCodeGenerator.Namespace = gameDataSettings.Namespace;
							var assetCode = assetCodeGenerator.TransformText();
							File.WriteAllText(assetCodeGenerationPath, assetCode);

							forceReImportList.Add(assetCodeGenerationPath);
						}
						goto generateCSharpCode;
					case GameDataSettings.CodeGenerator.CSharp:
						generateCSharpCode:
						if (Settings.Current.Verbose)
							Debug.Log(string.Format("Generating C# code for '{0}'...", gameDataPath));
						if (progressCallback != null)
							progressCallback(string.Format(Resources.UI_UNITYPLUGIN_GENERATE_CODE_FOR, gameDataPath), (float)i / total);

						var generateProcess = CharonCli.GenerateCSharpCodeAsync
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
				var assetGenerationPath = FileAndPathUtils.MakeProjectRelative(gameDataSettings.AssetGenerationPath);
				if (string.IsNullOrEmpty(assetGenerationPath))
					continue;

				// trying to touch gamedata file
				var touchGamedata = new Coroutine<FileStream>(TouchGameDataFile(gameDataPath));
				yield return touchGamedata;
				if (touchGamedata.GetResult().Length == 0)
					continue;

				using (var file = touchGamedata.GetResult())
				{
					var gameDataBytes = new byte[file.Length];
					int read, offset = 0;
					while ((read = file.Read(gameDataBytes, offset, gameDataBytes.Length - offset)) > 0)
						offset += read;

					var gameDataAssetType = Type.GetType(gameDataSettings.Namespace + "." + gameDataSettings.GameDataClassName + "Asset, Assembly-CSharp", throwOnError: false) ??
									   Type.GetType(gameDataSettings.Namespace + "." + gameDataSettings.GameDataClassName + "Asset, Assembly-CSharp-firstpass", throwOnError: false) ??
									   Type.GetType(gameDataSettings.Namespace + "." + gameDataSettings.GameDataClassName + "Asset, Assembly-CSharp-Editor", throwOnError: false);
					if (gameDataAssetType == null)
					{
						Debug.LogError(Resources.UI_UNITYPLUGIN_GENERATE_ASSET_CANT_FIND_GAMEDATA_CLASS);
						continue;
					}

					var assetDirectory = Path.GetDirectoryName(assetGenerationPath);
					if (assetDirectory != null && !Directory.Exists(assetDirectory))
					{
						Directory.CreateDirectory(assetDirectory);
					}

					var gameDataAsset = ScriptableObject.CreateInstance(gameDataAssetType);
					gameDataAsset.SetFieldValue("dataBytes", gameDataBytes);
					gameDataAsset.SetFieldValue("extension", Path.GetExtension(gameDataPath));
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
			var checkRequirements = CharonCli.CheckRequirementsAsync();
			yield return checkRequirements;

			switch (checkRequirements.GetResult())
			{
				case RequirementsCheckResult.MissingRuntime: yield return UpdateRuntimeWindow.ShowAsync(); break;
				case RequirementsCheckResult.WrongVersion:
				case RequirementsCheckResult.MissingExecutable: yield return CharonCli.DownloadCharon(ProgressUtils.ReportToLog(Resources.UI_UNITYPLUGIN_MENU_CHECK_UPDATES)); break;
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
	}
}

