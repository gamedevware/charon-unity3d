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
using Assets.Editor.GameDevWare.Charon.Json;
using Assets.Editor.GameDevWare.Charon.Tasks;
using Assets.Editor.GameDevWare.Charon.Utils;
using Assets.Editor.GameDevWare.Charon.Windows;
using UnityEditor;
using UnityEngine;

// ReSharper disable UnusedMember.Local
namespace Assets.Editor.GameDevWare.Charon
{
	internal static class Menu
	{
		public const string ToolsPrefix = "Tools/Charon/";
		public const string TroubleshootingPrefix = ToolsPrefix + Resources.UI_UNITYPLUGIN_MENUTROUBLESHOOTING + "/";

		[MenuItem(ToolsPrefix + Resources.UI_UNITYPLUGIN_MENUSCANFORNEWASSETS, false, 1)]
		private static void ScanForGameData()
		{
			if (!ScanForGameDataCheck()) return;

			var scanCoroutine = CoroutineScheduler.Schedule
			(
				ScanForGameDataAsync(
					progressCallback: ProgressUtils.ShowProgressBar(Resources.UI_UNITYPLUGIN_MENUSCANNINGASSETS))
			);
			scanCoroutine.ContinueWith(ProgressUtils.HideProgressBar);
			FocusConsoleWindow();
		}
		[MenuItem(ToolsPrefix + Resources.UI_UNITYPLUGIN_MENUSCANFORNEWASSETS, true, 1)]
		private static bool ScanForGameDataCheck()
		{
			// why not here :D
			UnityEditor.Menu.SetChecked(TroubleshootingPrefix + Resources.UI_UNITYPLUGIN_MENUVERBOSELOGS, Settings.Current.Verbose);
			UnityEditor.Menu.SetChecked(TroubleshootingPrefix + Resources.UI_UNITYPLUGIN_MENURECOVERYSCRIPTS, !Settings.Current.SuppressRecoveryScripts);

			return !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
		}

		[MenuItem(ToolsPrefix + Resources.UI_UNITYPLUGIN_MENUGENERATECODEANDASSETS, false, 2)]
		private static void GenerateCodeAndAssets()
		{
			if (!GenerateCodeAndAssetsCheck()) return;

			var generateCoroutine = CoroutineScheduler.Schedule
			(
				GenerateCodeAndAssetsAsync(
					progressCallback: ProgressUtils.ShowProgressBar(Resources.UI_UNITYPLUGIN_GENERATINGCODEANDASSETS))
			);
			generateCoroutine.ContinueWith(ProgressUtils.HideProgressBar);
			FocusConsoleWindow();
		}
		[MenuItem(ToolsPrefix + Resources.UI_UNITYPLUGIN_MENUGENERATECODEANDASSETS, true, 2)]
		private static bool GenerateCodeAndAssetsCheck()
		{
			return !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
		}

		[MenuItem(ToolsPrefix + Resources.UI_UNITYPLUGIN_MENUVALIDATEASSETS, false, 3)]
		private static void ValidateAll()
		{
			if (!ValidateAllCheck()) return;

			var validateCoroutine = CoroutineScheduler.Schedule<Dictionary<string, object>>(
				ValidateAsync(
					progressCallback: ProgressUtils.ShowProgressBar(Resources.UI_UNITYPLUGIN_VALIDATINGASSETS)
				)
			);
			validateCoroutine.ContinueWith(ProgressUtils.HideProgressBar);
			FocusConsoleWindow();
		}
		[MenuItem(ToolsPrefix + Resources.UI_UNITYPLUGIN_MENUVALIDATEASSETS, true, 3)]
		private static bool ValidateAllCheck()
		{
			return !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
		}

		[MenuItem(ToolsPrefix + Resources.UI_UNITYPLUGIN_MENUEXTRACTT4TEMPLATES, false, 5)]
		private static void ExtractT4Templates()
		{
			if (!ExtractT4TemplatesCheck())
				return;

			var extractionPath = EditorUtility.OpenFolderPanel(Resources.UI_UNITYPLUGIN_SPECIFYEXTRACTIONLOCTITLE, "", "");
			if (string.IsNullOrEmpty(extractionPath))
				return;

			CoroutineScheduler.Schedule
			(
				ExtractT4Templates(extractionPath)
			);
			FocusConsoleWindow();
		}
		[MenuItem(ToolsPrefix + Resources.UI_UNITYPLUGIN_MENUEXTRACTT4TEMPLATES, true, 5)]
		private static bool ExtractT4TemplatesCheck()
		{
			return !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
		}

		[MenuItem(TroubleshootingPrefix + Resources.UI_UNITYPLUGIN_MENUVERBOSELOGS, false, 9)]
		private static void VerboseLogs()
		{
			Settings.Current.Verbose = !Settings.Current.Verbose;
			UnityEditor.Menu.SetChecked(TroubleshootingPrefix + Resources.UI_UNITYPLUGIN_MENUVERBOSELOGS, Settings.Current.Verbose);
			Settings.Current.Save();
		}

		[MenuItem(TroubleshootingPrefix + Resources.UI_UNITYPLUGIN_MENURECOVERYSCRIPTS, false, 8)]
		private static void RecoveryScripts()
		{
			Settings.Current.SuppressRecoveryScripts = !Settings.Current.SuppressRecoveryScripts;
			UnityEditor.Menu.SetChecked(TroubleshootingPrefix + Resources.UI_UNITYPLUGIN_MENURECOVERYSCRIPTS, !Settings.Current.SuppressRecoveryScripts);
			Settings.Current.Save();
		}

		[MenuItem(TroubleshootingPrefix + Resources.UI_UNITYPLUGIN_MENUREPORTISSUE, false, 7)]
		private static void ReportIssue()
		{
			UnityEditor.EditorWindow.GetWindow<ReportIssueWindow>(utility: true);
		}

		[MenuItem(TroubleshootingPrefix + Resources.UI_UNITYPLUGIN_MENUCHECKRUNTIME, false, 6)]
		private static void CheckRuntime()
		{
			UpdateRuntimeWindow.ShowAsync(autoClose: false);
		}

		[MenuItem(ToolsPrefix + Resources.UI_UNITYPLUGIN_MENUDOCUMENTATION, false, 10)]
		private static void ShowDocumentation()
		{
			Application.OpenURL("https://github.com/deniszykov/charon-unity3d/blob/master/README.md");
		}

		[MenuItem(ToolsPrefix + Resources.UI_UNITYPLUGIN_MENUCHECKUPDATES, false, 11)]
		private static void CheckUpdates()
		{
			if (!CheckUpdatesCheck()) return;

			var checkUpdatesCheck = CoroutineScheduler.Schedule(
				CheckForUpdatesAsync(
					progressCallback: ProgressUtils.ShowProgressBar(Resources.UI_UNITYPLUGIN_MENUCHECKUPDATES)
				)
			);
			checkUpdatesCheck.ContinueWith(ProgressUtils.HideProgressBar);
			FocusConsoleWindow();
		}

		[MenuItem(ToolsPrefix + Resources.UI_UNITYPLUGIN_MENUCHECKUPDATES, true, 11)]
		private static bool CheckUpdatesCheck()
		{
			return !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
		}

		[MenuItem(ToolsPrefix + Resources.UI_UNITYPLUGIN_MENUABOUT, false, 12)]
		private static void About()
		{
			UnityEditor.EditorWindow.GetWindow<AboutWindow>(utility: true);
		}

		[MenuItem("Assets/Create/GameData")]
		private static void CreateGameDataAsset()
		{
			if (!CreateGameDataAssetCheck()) return;

			var location = Path.GetFullPath(AssetDatabase.GetAssetPath(Selection.activeObject));
			if (File.Exists(location))
				location = Path.GetDirectoryName(location);

			var i = 1;
			var gameDataPath = Path.Combine(location, "GameData.json");

			while (File.Exists(gameDataPath))
				gameDataPath = Path.Combine(location, "GameData" + (i++) + ".json");


			gameDataPath = FileUtils.MakeProjectRelative(gameDataPath);

			File.WriteAllText(gameDataPath, "{ \"ToolsVersion\": \"0.0.0.0\" }");
			AssetDatabase.Refresh();
			Settings.Current.AddGameDataPath(gameDataPath);
			EditorUtility.SetDirty(Settings.Current);
		}
		[MenuItem("Assets/Create/GameData", true)]
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

			var consoleWindow = UnityEditor.EditorWindow.GetWindow(consoleWindowType);
			consoleWindow.Focus();
		}

		public static IEnumerable ScanForGameDataAsync(Action<string, float> progressCallback = null)
		{
			switch (Utils.ToolsRunner.CheckCharon())
			{
				case CharonCheckResult.MissingRuntime: yield return UpdateRuntimeWindow.ShowAsync(); break;
				case CharonCheckResult.MissingExecutable: yield return Utils.ToolsRunner.UpdateCharonExecutable(progressCallback); break;
				case CharonCheckResult.Ok: break;
				default: throw new InvalidOperationException("Unknown Tools check result.");
			}

			if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_MENUSCANNINGASSETS, 0);
			var gameDataFiles = (from id in AssetDatabase.FindAssets("t:TextAsset").Concat(AssetDatabase.FindAssets("t:DefaultAsset"))
								 let path = AssetDatabase.GUIDToAssetPath(id)
								 where path != null && path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
								 select path);

			var found = 0;
			foreach (var gameDataPath in gameDataFiles)
			{
				if (Settings.Current.GameDataPaths.Contains(gameDataPath))
					continue;
				Settings.Current.AddGameDataPath(gameDataPath);
			}

			var paths = Settings.Current.GameDataPaths.ToArray();
			var total = paths.Length;
			for (var i = 0; i < paths.Length; i++)
			{
				var gameDataPath = paths[i];
				var fullGameDataPath = Path.GetFullPath(gameDataPath);
				if (progressCallback != null) progressCallback(string.Format(Resources.UI_UNITYPLUGIN_PROGRESSCURRENTTARGETIS, gameDataPath), (float)i / total);
				if (!File.Exists(fullGameDataPath))
				{
					Debug.LogWarning(string.Format("Asset at '{0}' is not found.", gameDataPath));
					Settings.Current.RemoveGameDataPath(gameDataPath);
					continue;
				}

				if (Settings.Current.Verbose) Debug.Log(string.Format("Checking '{0}'...", gameDataPath));
				if (progressCallback != null) progressCallback(string.Format(Resources.UI_UNITYPLUGIN_SCANRUNVALIDATIONFOR, gameDataPath), (float)i / total);

				var checkProcess = ToolsRunner.RunCharonAsTool
				(
					"DATA", "VALIDATE", fullGameDataPath,
					Settings.Current.Verbose ? "--verbose" : ""
				);
				yield return checkProcess;
				var checkResult = checkProcess.GetResult();
				if (Settings.Current.Verbose) Debug.Log(string.Format("Check complete exit code: '{0}'", checkResult.ExitCode));
				if (checkResult.ExitCode != 0)
				{
					Debug.LogWarning(string.Format(Resources.UI_UNITYPLUGIN_SCANASSETSKIPPED, gameDataPath));
					if (Settings.Current.Verbose) Debug.LogWarning("Validation errors: " + Environment.NewLine + checkResult.GetOutputData() + checkResult.GetErrorData());
					Settings.Current.RemoveGameDataPath(gameDataPath);
				}
				else
				{
					if (Settings.Current.Verbose) Debug.Log(string.Format("Adding '{0}' to tracked GameData files.", gameDataPath));
					found++;
				}
			}
			if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESSDONE, 1);

			Debug.Log(string.Format(Resources.UI_UNITYPLUGIN_SCANCOMPLETE, found, Settings.Current.GameDataPaths.Length));
			Settings.Current.Save();
		}
		public static IEnumerable GenerateCodeAndAssetsAsync(string path = null, Action<string, float> progressCallback = null)
		{
			switch (Utils.ToolsRunner.CheckCharon())
			{
				case CharonCheckResult.MissingRuntime: yield return UpdateRuntimeWindow.ShowAsync(); break;
				case CharonCheckResult.MissingExecutable: yield return Utils.ToolsRunner.UpdateCharonExecutable(progressCallback); break;
				case CharonCheckResult.Ok: break;
				default: throw new InvalidOperationException("Unknown Tools check result.");
			}

			var paths = !string.IsNullOrEmpty(path) ? new string[] { path } : Settings.Current.GameDataPaths.ToArray();
			var total = paths.Length;
			var forceReImportList = new List<string>();
			for (var i = 0; i < paths.Length; i++)
			{
				var gameDataPath = paths[i];
				if (File.Exists(gameDataPath) == false)
					continue;
				if (progressCallback != null) progressCallback(string.Format(Resources.UI_UNITYPLUGIN_PROGRESSCURRENTTARGETIS, gameDataPath), (float)i / total);


				var gameDataObj = AssetDatabase.LoadAssetAtPath(gameDataPath, typeof(UnityEngine.Object));
				var assetImport = AssetImporter.GetAtPath(gameDataPath);
				if (assetImport == null)
					continue;

				var gameDataSettings = GameDataSettings.Load(gameDataObj);
				var codeGenerationPath = FileUtils.MakeProjectRelative(gameDataSettings.CodeGenerationPath);
				if (gameDataSettings.Generator == (int)GameDataSettings.CodeGenerator.None)
					continue;

				var generationOptions = gameDataSettings.Options;
				// trying to touch gamedata file
				var touchGamedata = new Coroutine<FileStream>(TouchGameDataFile(gameDataPath));
				yield return touchGamedata;

				using (touchGamedata.GetResult())
				{
					var generator = (GameDataSettings.CodeGenerator)gameDataSettings.Generator;
					switch (generator)
					{
						case GameDataSettings.CodeGenerator.CSharpCodeAndAsset:
							if (!string.IsNullOrEmpty(gameDataSettings.AssetGenerationPath))
							{
								AssetGenerator.Instance.AddPath(gameDataPath);
								generationOptions &= ~(int)GameDataSettings.CodeGenerationOptions.SuppressJsonSerialization;
							}
							goto generateCSharpCode;
						case GameDataSettings.CodeGenerator.CSharp:
							generateCSharpCode:
							if (Settings.Current.Verbose) Debug.Log(string.Format("Generating C# code for '{0}'...", gameDataPath));
							if (progressCallback != null) progressCallback(string.Format(Resources.UI_UNITYPLUGIN_GENERATECODEFOR, gameDataPath), (float)i / total);
							var generateProcess = ToolsRunner.RunCharonAsTool
							(
								"DATA", generator == GameDataSettings.CodeGenerator.CSharp ? "GENERATECSHARPCODE" : "GENERATEUNITYCSHARPCODE",
								Path.GetFullPath(gameDataPath),
								"--namespace",
								gameDataSettings.Namespace,
								"--gameDataClassName",
								gameDataSettings.GameDataClassName,
								"--entryClassName",
								gameDataSettings.EntryClassName,
								"--options",
								generationOptions.ToString(),
								"--output",
								Path.GetFullPath(codeGenerationPath),
								Settings.Current.Verbose ? "--verbose" : ""
							);
							yield return generateProcess;

							if (Settings.Current.Verbose) Debug.Log(string.Format("Generation complete, exit code: '{0}'", generateProcess.GetResult().ExitCode));
							if (generateProcess.GetResult().ExitCode != 0)
							{
								Debug.LogWarning(string.Format(Resources.UI_UNITYPLUGIN_GENERATEFAILEDDUEERRORS, gameDataPath, generateProcess.GetResult().GetErrorData()));
							}
							else
							{
								if (Settings.Current.Verbose) Debug.Log(string.Format("Code generation for '{0}' is complete.", gameDataPath));

								forceReImportList.Add(codeGenerationPath);

								if (gameDataSettings.LineEnding != 0 ||
									gameDataSettings.Indentation != 0)
								{
									if (progressCallback != null) progressCallback(string.Format(Resources.UI_UNITYPLUGIN_GENERATEREFORMATCODE, gameDataPath), (float)i / total);

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
							break;
						default:
							Debug.LogError("Unknown code/asset generator type " + (GameDataSettings.CodeGenerator)gameDataSettings.Generator + ".");
							break;
					}
				}
			}
			if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_GENERATEREFRESHINGASSETS, 0.99f);
			foreach (var forceReImportPath in forceReImportList)
				AssetDatabase.ImportAsset(forceReImportPath, ImportAssetOptions.ForceUpdate);
			if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESSDONE, 1);
		}
		public static IEnumerable GenerateAssetsAsync(string[] paths, Action<string, float> progressCallback = null)
		{
			var total = paths.Length;
			for (var i = 0; i < paths.Length; i++)
			{
				var gameDataPath = paths[i];
				if (File.Exists(gameDataPath) == false)
					continue;
				if (progressCallback != null) progressCallback(string.Format(Resources.UI_UNITYPLUGIN_PROGRESSCURRENTTARGETIS, gameDataPath), (float)i / total);


				var gameDataObj = AssetDatabase.LoadAssetAtPath(gameDataPath, typeof(UnityEngine.Object));
				var assetImport = AssetImporter.GetAtPath(gameDataPath);
				if (assetImport == null)
					continue;

				var gameDataSettings = GameDataSettings.Load(gameDataObj);
				var assetGenerationPath = FileUtils.MakeProjectRelative(gameDataSettings.AssetGenerationPath);
				if (string.IsNullOrEmpty(assetGenerationPath))
					continue;

				// trying to touch gamedata file
				var touchGamedata = new Coroutine<FileStream>(TouchGameDataFile(gameDataPath));
				yield return touchGamedata;

				using (touchGamedata.GetResult())
				using (var gameDataTextReader = new StreamReader(gameDataPath, Encoding.UTF8))
				{
					var gameDataType = Type.GetType(gameDataSettings.Namespace + "." + gameDataSettings.GameDataClassName + ", Assembly-CSharp", throwOnError: false) ??
									   Type.GetType(gameDataSettings.Namespace + "." + gameDataSettings.GameDataClassName + ", Assembly-CSharp-firstpass", throwOnError: false) ??
									   Type.GetType(gameDataSettings.Namespace + "." + gameDataSettings.GameDataClassName + ", Assembly-CSharp-Editor", throwOnError: false);
					if (gameDataType == null)
					{
						Debug.LogError(Resources.UI_UNITYPLUGIN_GENERATEASSETCANTFINDGAMEDATACLASS);
						continue;
					}

					var gameDataJson = gameDataTextReader.ReadToEnd();
					var gameDataAsset = (ScriptableObject)ScriptableObject.CreateInstance(gameDataType);
					gameDataAsset.SetFieldValue("jsonText", gameDataJson);
					AssetDatabase.CreateAsset(gameDataAsset, assetGenerationPath);
					AssetDatabase.SaveAssets();
				}
			}
			if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_GENERATEREFRESHINGASSETS, 0.99f);
			AssetDatabase.Refresh(ImportAssetOptions.Default);
			if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESSDONE, 1);
		}
		public static IEnumerable ValidateAsync(string path = null, Action<string, float> progressCallback = null)
		{
			switch (Utils.ToolsRunner.CheckCharon())
			{
				case CharonCheckResult.MissingRuntime: yield return UpdateRuntimeWindow.ShowAsync(); break;
				case CharonCheckResult.MissingExecutable: yield return Utils.ToolsRunner.UpdateCharonExecutable(progressCallback); break;
				case CharonCheckResult.Ok: break;
				default: throw new InvalidOperationException("Unknown Tools check result.");
			}

			var reports = new Dictionary<string, object>();
			var paths = !string.IsNullOrEmpty(path) ? new string[] { path } : Settings.Current.GameDataPaths.ToArray();
			var total = paths.Length;
			for (var i = 0; i < paths.Length; i++)
			{
				var gameDataPath = paths[i];
				if (File.Exists(gameDataPath) == false)
					continue;
				if (progressCallback != null) progressCallback(string.Format(Resources.UI_UNITYPLUGIN_PROGRESSCURRENTTARGETIS, gameDataPath), (float)i / total);

				if (Settings.Current.Verbose) Debug.Log(string.Format("Validating GameData at '{0}'...", gameDataPath));
				if (progressCallback != null) progressCallback(string.Format(Resources.UI_UNITYPLUGIN_VALIDATERUNFOR, gameDataPath), (float)i / total);
				var validateProcess = ToolsRunner.RunCharonAsTool
				(
					"DATA", "VALIDATE", Path.GetFullPath(gameDataPath),
					"--output", "out",
					"--outputFormat", "json",
					Settings.Current.Verbose ? "--verbose" : ""
				);
				yield return validateProcess;

				if (Settings.Current.Verbose) Debug.Log(string.Format("Validation complete, exit code: '{0}'", validateProcess.GetResult().ExitCode));
				if (validateProcess.GetResult().ExitCode != 0)
				{
					reports.Add(gameDataPath, validateProcess.GetResult().GetErrorData());
					Debug.LogWarning(string.Format(Resources.UI_UNITYPLUGIN_VALIDATEFAILEDDUEERRORS, gameDataPath, validateProcess.GetResult().GetErrorData()));
				}
				else
				{
					try
					{
						var report = default(JsonObject);
						reports.Add(gameDataPath, report = (JsonObject)JsonValue.Parse(validateProcess.GetResult().GetOutputData()));
						var success = (bool)report["success"];
						var totalErrors = 0;
						if (!success)
						{
							var items = (JsonArray)report["items"];
							System.Diagnostics.Debug.Assert(items != null, "items != null");
							foreach (JsonObject record in items)
							{
								var errors = record.ContainsKey("errors") ? ((JsonArray)record["errors"]) : null;
								if (errors != null)
								{
									foreach (JsonObject error in errors)
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

						Debug.Log(string.Format(Resources.UI_UNITYPLUGIN_VALIDATECOMPLETE, gameDataPath, success ? "success" : "failure", totalErrors));

					}
					catch (Exception e)
					{
						Debug.LogWarning(string.Format(Resources.UI_UNITYPLUGIN_VALIDATEFAILEDDUEERRORS, gameDataPath, e));
						reports[gameDataPath] = e.Unwrap().ToString();
					}
				}
			}
			if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESSDONE, 1);
			yield return reports;
		}
		public static IEnumerable ExtractT4Templates(string extractionPath)
		{
			switch (Utils.ToolsRunner.CheckCharon())
			{
				case CharonCheckResult.MissingRuntime: yield return UpdateRuntimeWindow.ShowAsync(); break;
				case CharonCheckResult.MissingExecutable: yield return Utils.ToolsRunner.UpdateCharonExecutable(ProgressUtils.ReportToLog(Resources.UI_UNITYPLUGIN_MENUCHECKUPDATES)); break;
				case CharonCheckResult.Ok: break;
				default: throw new InvalidOperationException("Unknown Tools check result.");
			}

			if (Settings.Current.Verbose) Debug.Log(string.Format("Extracting T4 Templates to '{0}'...", extractionPath));
			var dumpProcess = ToolsRunner.RunCharonAsTool
			(
				"DUMPCODEGENERATOR", Path.GetFullPath(extractionPath),
				Settings.Current.Verbose ? "--verbose" : ""
			);
			yield return dumpProcess;

			if (string.IsNullOrEmpty(dumpProcess.GetResult().GetErrorData()) == false)
				Debug.LogWarning(string.Format(Resources.UI_UNITYPLUGIN_T4EXTRACTIONFAILED, dumpProcess.GetResult().GetErrorData()));
			else
				Debug.Log(Resources.UI_UNITYPLUGIN_T4EXTRACTIONCOMPLETE + "\r\n" + dumpProcess.GetResult().GetOutputData());
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
						Debug.LogWarning("Attempt #" + attempt + " to touch " + path + " file has failed with IO error: " + e);
				}
				yield return Promise.Delayed(TimeSpan.FromSeconds(1));
			}
			yield return gameDataFile;
		}
		public static IEnumerable CheckForUpdatesAsync(Action<string, float> progressCallback = null)
		{
			var toolsVersion = default(Version);
			var toolsPath = Path.GetFullPath(Settings.Current.ToolsPath);
			if (File.Exists(toolsPath))
			{
				if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESSCHECKINGTOOLSVERSION, 0.05f);

				var checkToolsVersion = ToolsRunner.RunCharonAsTool("VERSION");
				yield return checkToolsVersion.IgnoreFault();

				var outputData = checkToolsVersion.HasErrors == false ? checkToolsVersion.GetResult().GetOutputData() : null;
				if (string.IsNullOrEmpty(outputData) == false)
					toolsVersion = new Version(outputData);
			}

			if (Settings.Current.Verbose)
				Debug.Log(string.Format("Current tools path: {0}.", toolsPath));

			if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESSGETTINGAVAILABLEBUILDS, 0.10f);

			var licenseServerAddress = Settings.Current.GetLicenseServerAddress();
			var getBuildsHeaders = new NameValueCollection { { "Accept", "application/json" } };
			var getBuildsUrl = new Uri(licenseServerAddress, "Build?product=Charon");
			var getBuildsRequest = new GetRequest<JsonValue>(getBuildsUrl, getBuildsHeaders);
			yield return getBuildsRequest;

			var response = getBuildsRequest.GetResult();
			if (response.ContainsKey("Message")) throw new InvalidOperationException(string.Format("Request to '{0}' has failed with message from server: {1}.", getBuildsUrl, response["Message"]));
			var builds = (JsonArray)response["Result"];
			var lastVersion =
			(
				from build in builds
				let buildObj = (JsonObject)build
				let version = new Version(buildObj["Version"].As<string>())
				orderby version descending
				select version
			).FirstOrDefault();

			if (lastVersion == null)
			{
				if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESSDONE, 1.0f);

				Debug.Log(string.Format("{0} version '{1}' is up to date.", Path.GetFileName(toolsPath), toolsVersion));
				yield break;
			}
			else if (toolsVersion != null && toolsVersion >= lastVersion)
			{
				if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESSDONE, 1.0f);

				Debug.Log(string.Format("{0} version '{1}' is up to date.", Path.GetFileName(toolsPath), toolsVersion));
				yield break;
			}

			if (toolsVersion != null && !EditorUtility.DisplayDialog(
					Resources.UI_UNITYPLUGIN_UPDATEAVAILABLETITLE,
					string.Format(Resources.UI_UNITYPLUGIN_UPDATEAVAILABLEMESSAGE, toolsVersion, lastVersion),
					Resources.UI_UNITYPLUGIN_DOWNLOADBUTTON)
			)
			{
				yield break;
			}

			if (progressCallback != null) progressCallback(string.Format(Resources.UI_UNITYPLUGIN_PROGRESSDOWNLOADINGS, 0, 0), 0.10f);

			var downloadHeaders = new NameValueCollection { { "Accept", "application/octet-stream" } };
			var downloadUrl = new Uri(licenseServerAddress, "Build?product=Charon&id=" + Uri.EscapeDataString(lastVersion.ToString()));
			var downloadPath = Path.GetTempFileName();
			yield return new FileDownloadRequest(downloadUrl, downloadPath, downloadHeaders, (readed, total) =>
			{
				if (progressCallback == null || total == 0)
					return;

				progressCallback(string.Format(Resources.UI_UNITYPLUGIN_PROGRESSDOWNLOADINGS, (float)readed / 1024 / 1024, total / 1024 / 1024), 0.10f + (0.80f * Math.Min(1.0f, (float)readed / total)));
			});

			try
			{
				if (File.Exists(toolsPath))
					File.Delete(toolsPath);
				if (Directory.Exists(toolsPath))
					Directory.Delete(toolsPath);

				if (Directory.Exists(Path.GetDirectoryName(toolsPath)) == false)
					Directory.CreateDirectory(Path.GetDirectoryName(toolsPath));

				File.Move(downloadPath, toolsPath);
			}
			catch (Exception moveError)
			{
				Debug.LogWarning(string.Format("Failed to move downloaded file from '{0}' to {1}. {2}.", downloadPath, toolsPath, moveError.Message));
				Debug.LogError(moveError);
			}
			finally
			{
				// ReSharper disable once EmptyGeneralCatchClause
				try { if (File.Exists(downloadPath)) File.Delete(downloadPath); }
				catch { }
			}

			// ReSharper disable once EmptyGeneralCatchClause
			try { if (Directory.Exists(ToolsRunner.ToolShadowCopyPath)) Directory.Delete(ToolsRunner.ToolShadowCopyPath, true); }
			catch { Debug.LogWarning(string.Format("Failed to delete directory with old copy of tools '{0}'. Please restart unity or delete it manually.", Utils.ToolsRunner.ToolShadowCopyPath)); }

			ToolsRunner.ToolShadowCopyPath = FileUtil.GetUniqueTempPathInProject();

			if (File.Exists(toolsPath))
			{
				if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESSCHECKINGTOOLSVERSION, 0.95f);

				var checkToolsVersion = ToolsRunner.RunCharonAsTool("VERSION");
				yield return checkToolsVersion.IgnoreFault();

				var outputData = checkToolsVersion.HasErrors == false ? checkToolsVersion.GetResult().GetOutputData() : null;
				if (string.IsNullOrEmpty(outputData) == false)
					toolsVersion = new Version(outputData);
			}

			Debug.Log(string.Format("{1} version is '{0}'. Update is complete.", toolsVersion, Path.GetFileName(toolsPath)));

			if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESSDONE, 1.0f);
		}
	}
}

