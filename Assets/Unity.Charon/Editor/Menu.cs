/*
	Copyright (c) 2015 Denis Zykov

	This is part of Charon Game Data Editor Unity Plugin.

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
using Assets.Unity.Charon.Editor.Json;
using Assets.Unity.Charon.Editor.Tasks;
using Assets.Unity.Charon.Editor.Utils;
using Assets.Unity.Charon.Editor.Windows;
using UnityEditor;
using UnityEngine;

// ReSharper disable UnusedMember.Local
namespace Assets.Unity.Charon.Editor
{
	static class Menu
	{
		[MenuItem("Tools/Charon/Scan for new Assets")]
		private static void ScanForGameData()
		{
			if (!ScanForGameDataCheck()) return;

			var scanCoroutine = CoroutineScheduler.Schedule
			(
				ScanForGameDataAsync(
					progressCallback: ProgressUtils.ShowProgressBar("Scanning Assets..."))
			);
			scanCoroutine.ContinueWith(ProgressUtils.HideProgressBar, null);
			FocusConsoleWindow();
		}
		[MenuItem("Tools/Charon/Scan for new Assets", true)]
		private static bool ScanForGameDataCheck()
		{
			// why not here :D
			UnityEditor.Menu.SetChecked("Tools/Charon/Verbose Logs", Settings.Current.Verbose);
			UnityEditor.Menu.SetChecked("Tools/Charon/Recovery Scripts", !Settings.Current.SuppressRecoveryScripts);

			return !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
		}

		[MenuItem("Tools/Charon/Generate Code and Assets")]
		private static void GenerateCodeAndAssets()
		{
			if (!GenerateCodeAndAssetsCheck()) return;

			var generateCoroutine = CoroutineScheduler.Schedule
			(
				GenerateCodeAndAssetsAsync(
					progressCallback: ProgressUtils.ShowProgressBar("Generating Code and Assets..."))
			);
			generateCoroutine.ContinueWith(ProgressUtils.HideProgressBar, null);
			FocusConsoleWindow();
		}
		[MenuItem("Tools/Charon/Generate Code and Assets", true)]
		private static bool GenerateCodeAndAssetsCheck()
		{
			return !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
		}

		[MenuItem("Tools/Charon/Validate Assets")]
		private static void ValidateAll()
		{
			if (!ValidateAllCheck()) return;

			var validateCoroutine = CoroutineScheduler.Schedule<Dictionary<string, object>>(
				ValidateAsync(
					progressCallback: ProgressUtils.ShowProgressBar("Validating Assets...")
				)
			);
			validateCoroutine.ContinueWith(ProgressUtils.HideProgressBar, null);
			FocusConsoleWindow();
		}
		[MenuItem("Tools/Charon/Validate Assets", true)]
		private static bool ValidateAllCheck()
		{
			return !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
		}

		[MenuItem("Tools/Charon/Migrate Assets")]
		private static void MigrateAll()
		{
			if (!MigrateAllCheck()) return;

			var migrateCoroutine = CoroutineScheduler.Schedule
			(
				MigrateAsync(
					  progressCallback: ProgressUtils.ShowProgressBar("Migrating Assets...")
				)
			);
			migrateCoroutine.ContinueWith(ProgressUtils.HideProgressBar, null);
			FocusConsoleWindow();
		}
		[MenuItem("Tools/Charon/Migrate Assets", true)]
		private static bool MigrateAllCheck()
		{
			return !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
		}

		[MenuItem("Tools/Charon/Extract T4 Templates")]
		private static void ExtractT4Templates()
		{
			if (!ExtractT4TemplatesCheck())
				return;

			var extractionPath = EditorUtility.OpenFolderPanel("Specify extraction location...", "", "");
			if (string.IsNullOrEmpty(extractionPath))
				return;

			CoroutineScheduler.Schedule
			(
				ExtractT4Templates(extractionPath)
			);
			FocusConsoleWindow();
		}
		[MenuItem("Tools/Charon/Extract T4 Templates", true)]
		private static bool ExtractT4TemplatesCheck()
		{
			return !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
		}

		[MenuItem("Tools/Charon/Verbose Logs")]
		private static void VerboseLogs()
		{
			Settings.Current.Verbose = !Settings.Current.Verbose;
			UnityEditor.Menu.SetChecked("Tools/Charon/Verbose Logs", Settings.Current.Verbose);
			Settings.Current.Save();
		}

		[MenuItem("Tools/Charon/Recovery Scripts")]
		private static void RecoveryScripts()
		{
			Settings.Current.SuppressRecoveryScripts = !Settings.Current.SuppressRecoveryScripts;
			UnityEditor.Menu.SetChecked("Tools/Charon/Recovery Scripts", !Settings.Current.SuppressRecoveryScripts);
			Settings.Current.Save();
		}

		[MenuItem("Tools/Charon/About...")]
		private static void About()
		{
			EditorWindow.GetWindow<AboutCharonWindow>(utility: true);
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
			Settings.Current.GameDataPaths.Add(gameDataPath);
			Settings.Current.Version++;

			CoroutineScheduler.Schedule(MigrateAsync(gameDataPath));
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

			var consoleWindow = EditorWindow.GetWindow(consoleWindowType);
			consoleWindow.Focus();
		}

		public static IEnumerable ScanForGameDataAsync(Action<string, float> progressCallback = null)
		{
			switch (ToolsUtils.CheckTools())
			{
				case ToolsCheckResult.MissingMono: yield return UpdateRuntimeWindow.ShowAsync(); break;
				case ToolsCheckResult.MissingTools: yield return UpdateToolsWindow.ShowAsync(); break;
				case ToolsCheckResult.Ok: break;
				default: throw new InvalidOperationException("Unknown Tools check result.");
			}

			if (progressCallback != null) progressCallback("Scanning assets", 0);
			var gameDataFiles = (from id in AssetDatabase.FindAssets("t:TextAsset")
								 let path = AssetDatabase.GUIDToAssetPath(id)
								 where path != null && path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
								 select path);

			var found = 0;
			foreach (var gameDataPath in gameDataFiles)
			{
				if (Settings.Current.GameDataPaths.Contains(gameDataPath))
					continue;
				Settings.Current.GameDataPaths.Add(gameDataPath);
			}

			var paths = Settings.Current.GameDataPaths.ToArray();
			var total = paths.Length;
			for (var i = 0; i < paths.Length; i++)
			{
				var gameDataPath = paths[i];
				var fullGameDataPath = Path.GetFullPath(gameDataPath);
				if (progressCallback != null) progressCallback("Current target is " + gameDataPath, (float)i / total);
				if (!File.Exists(fullGameDataPath))
				{
					Debug.LogWarning(string.Format("TextAsset at '{0}' is not found.", gameDataPath));
					Settings.Current.GameDataPaths.Remove(gameDataPath);
					continue;
				}

				if (Settings.Current.Verbose) Debug.Log(string.Format("Checking '{0}'...", gameDataPath));
				if (progressCallback != null) progressCallback("Running validation tool for " + gameDataPath, (float)i / total);
				var errorText = new StringBuilder();
				var checkProcess = new ExecuteCommandTask
				(
					Settings.Current.ToolsPath,
					null,
					(s, ea) => { if (string.IsNullOrEmpty(ea.Data) == false) errorText.Append(ea.Data); },
					"VALIDATE", fullGameDataPath,
					Settings.Current.Verbose ? "--verbose" : ""
				);
				checkProcess.RequireDotNetRuntime();
				checkProcess.Start();

				yield return checkProcess;
				if (Settings.Current.Verbose) Debug.Log(string.Format("Check complete exit code: '{0}'", checkProcess.ExitCode));
				if (checkProcess.ExitCode != 0)
				{
					Debug.LogWarning(string.Format("TextAsset '{0}' is skipped because of validation error.", gameDataPath));
					if (Settings.Current.Verbose)
						Debug.LogWarning("Validation error: " + Environment.NewLine + errorText);
					Settings.Current.GameDataPaths.Remove(gameDataPath);
				}
				else
				{
					if (Settings.Current.Verbose) Debug.Log(string.Format("Adding '{0}' to tracked GameData files.", gameDataPath));
					found++;
				}
			}
			if (progressCallback != null) progressCallback("Done", 1);

			Debug.Log(string.Format("Scan complete: {0} files are located. Total {1} files are tracked.", found, Settings.Current.GameDataPaths.Count));
			Settings.Current.Version++;
			Settings.Current.Save();
		}
		public static IEnumerable GenerateCodeAndAssetsAsync(string path = null, Action<string, float> progressCallback = null)
		{
			switch (ToolsUtils.CheckTools())
			{
				case ToolsCheckResult.MissingMono: yield return UpdateRuntimeWindow.ShowAsync(); break;
				case ToolsCheckResult.MissingTools: yield return UpdateToolsWindow.ShowAsync(); break;
				case ToolsCheckResult.Ok: break;
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
				if (progressCallback != null) progressCallback("Current target is " + gameDataPath, (float)i / total);


				var gameDataObj = AssetDatabase.LoadAssetAtPath(gameDataPath, typeof(TextAsset));
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
					var errorText = new StringBuilder();
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
							if (progressCallback != null) progressCallback("Running generation tools for " + gameDataPath, (float)i / total);
							var generateProcess = new ExecuteCommandTask
							(
								Settings.Current.ToolsPath,
								null,
								(sender, args) => { if (!string.IsNullOrEmpty(args.Data)) errorText.Append(args.Data); },
								generator == GameDataSettings.CodeGenerator.CSharp ? "GENERATECSHARPCODE" : "GENERATEUNITYCSHARPCODE",
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
							generateProcess.RequireDotNetRuntime();
							generateProcess.Start();
							yield return generateProcess;

							if (Settings.Current.Verbose) Debug.Log(string.Format("Generation complete, exit code: '{0}'", generateProcess.ExitCode));
							if (generateProcess.ExitCode != 0)
							{
								Debug.LogWarning(string.Format("Code generation for '{0}' failed due errors: {1}.", gameDataPath, errorText));
							}
							else
							{
								if (Settings.Current.Verbose) Debug.Log(string.Format("Code generation for '{0}' is complete.", gameDataPath));

								forceReImportList.Add(codeGenerationPath);

								if (gameDataSettings.LineEnding != 0 ||
									gameDataSettings.Identation != 0)
								{
									if (progressCallback != null) progressCallback("Re-formatting generated code for " + gameDataPath, (float)i / total);

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
									switch ((GameDataSettings.Identations)gameDataSettings.Identation)
									{
										case GameDataSettings.Identations.Tab:
											// already tabs
											break;
										case GameDataSettings.Identations.FourSpaces:
											code.Replace("\t", "    ");
											break;
										case GameDataSettings.Identations.TwoSpaces:
											code.Replace("\t", "  ");
											break;
										default:
											throw new InvalidOperationException(string.Format("Unknown indentation value '{0}' is set for {1}", gameDataSettings.Identation, gameDataPath));
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
			if (progressCallback != null) progressCallback("Refreshing assets", 0.99f);
			foreach (var forceReImportPath in forceReImportList)
				AssetDatabase.ImportAsset(forceReImportPath, ImportAssetOptions.ForceUpdate);
			if (progressCallback != null) progressCallback("Done", 1.0f);
		}
		public static IEnumerable GenerateAssetsAsync(string[] paths, Action<string, float> progressCallback = null)
		{
			var total = paths.Length;
			for (var i = 0; i < paths.Length; i++)
			{
				var gameDataPath = paths[i];
				if (File.Exists(gameDataPath) == false)
					continue;
				if (progressCallback != null) progressCallback("Current target is " + gameDataPath, (float)i / total);


				var gameDataObj = AssetDatabase.LoadAssetAtPath(gameDataPath, typeof(TextAsset));
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
						Debug.LogError("Asset generation failed: unable to find GameData's type in CSharp assemblies. Make sure there is no compilation errors.");
						continue;
					}

					var gameDataJson = gameDataTextReader.ReadToEnd();
					var gameDataAsset = (ScriptableObject)ScriptableObject.CreateInstance(gameDataType);
					gameDataAsset.SetField("jsonText", gameDataJson);
					AssetDatabase.CreateAsset(gameDataAsset, assetGenerationPath);
					AssetDatabase.SaveAssets();
				}
			}
			if (progressCallback != null) progressCallback("Refreshing assets", 0.99f);
			AssetDatabase.Refresh(ImportAssetOptions.Default);
			if (progressCallback != null) progressCallback("Done", 1.0f);
		}
		public static IEnumerable MigrateAsync(string path = null, Action<string, float> progressCallback = null)
		{
			switch (ToolsUtils.CheckTools())
			{
				case ToolsCheckResult.MissingMono: yield return UpdateRuntimeWindow.ShowAsync(); break;
				case ToolsCheckResult.MissingTools: yield return UpdateToolsWindow.ShowAsync(); break;
				case ToolsCheckResult.Ok: break;
				default: throw new InvalidOperationException("Unknown Tools check result.");
			}

			var paths = !string.IsNullOrEmpty(path) ? new string[] { path } : Settings.Current.GameDataPaths.ToArray();
			var total = paths.Length;
			for (var i = 0; i < paths.Length; i++)
			{
				var gameDataPath = paths[i];
				if (File.Exists(gameDataPath) == false)
					continue;
				if (progressCallback != null) progressCallback("Current target is " + gameDataPath, (float)i / total);

				var errorText = new StringBuilder();
				if (Settings.Current.Verbose) Debug.Log(string.Format("Migrating GameData at '{0}'...", gameDataPath));
				if (progressCallback != null) progressCallback("Running migration tools for " + gameDataPath, (float)i / total);
				var migrateProcess = new ExecuteCommandTask
				(
					Settings.Current.ToolsPath,
					null,
					(sender, args) => { if (!string.IsNullOrEmpty(args.Data)) errorText.Append(args.Data); },
					"MIGRATE",
					Path.GetFullPath(gameDataPath),
					Settings.Current.Verbose ? "--verbose" : ""
				);
				migrateProcess.RequireDotNetRuntime();
				migrateProcess.Start();
				yield return migrateProcess;

				if (Settings.Current.Verbose) Debug.Log(string.Format("Migration complete, exit code: '{0}'", migrateProcess.ExitCode));
				if (migrateProcess.ExitCode != 0)
					Debug.LogWarning(string.Format("Migration of '{0}' has failed due errors: {1}.", gameDataPath, errorText));
				else if (Settings.Current.Verbose)
					Debug.Log(string.Format("Migration of '{0}' is complete.", gameDataPath));
			}
			if (progressCallback != null) progressCallback("Done", 1.0f);
		}
		public static IEnumerable ValidateAsync(string path = null, Action<string, float> progressCallback = null)
		{
			switch (ToolsUtils.CheckTools())
			{
				case ToolsCheckResult.MissingMono: yield return UpdateRuntimeWindow.ShowAsync(); break;
				case ToolsCheckResult.MissingTools: yield return UpdateToolsWindow.ShowAsync(); break;
				case ToolsCheckResult.Ok: break;
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
				if (progressCallback != null) progressCallback("Current target is " + gameDataPath, (float)i / total);

				var errorText = new StringBuilder();
				var outputText = new StringBuilder();
				if (Settings.Current.Verbose) Debug.Log(string.Format("Validating GameData at '{0}'...", gameDataPath));
				if (progressCallback != null) progressCallback("Running validation tool for " + gameDataPath, (float)i / total);
				var validateProcess = new ExecuteCommandTask
				(
					Settings.Current.ToolsPath,
					(sender, args) => { if (!string.IsNullOrEmpty(args.Data)) outputText.Append(args.Data); },
					(sender, args) => { if (!string.IsNullOrEmpty(args.Data)) errorText.Append(args.Data); },
					"VALIDATE", Path.GetFullPath(gameDataPath),
					"--output", "out",
					"--outputFormat", "json",
					Settings.Current.Verbose ? "--verbose" : ""
				);
				validateProcess.RequireDotNetRuntime();
				validateProcess.Start();
				yield return validateProcess;

				if (Settings.Current.Verbose) Debug.Log(string.Format("Validation complete, exit code: '{0}'", validateProcess.ExitCode));
				if (validateProcess.ExitCode != 0)
				{
					reports.Add(gameDataPath, errorText.ToString());
					Debug.LogWarning(string.Format("Validation of '{0}' has failed due errors: {1}.", gameDataPath, errorText));
				}
				else
				{
					try
					{
						var report = default(JsonObject);
						reports.Add(gameDataPath, report = (JsonObject)JsonValue.Parse(outputText.ToString()));
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

						Debug.Log(string.Format("Validation of '{0}' is complete. Result: {1}, Errors: {2}.", gameDataPath, success ? "success" : "failure", totalErrors));

					}
					catch (Exception e)
					{
						Debug.LogWarning(string.Format("Validation of '{0}' has failed due errors: {1}.", gameDataPath, e));
						reports[gameDataPath] = e.Unwrap().ToString();
					}
				}
			}
			if (progressCallback != null) progressCallback("Done", 1.0f);
			yield return reports;
		}
		public static IEnumerable ExtractT4Templates(string extractionPath)
		{
			switch (ToolsUtils.CheckTools())
			{
				case ToolsCheckResult.MissingMono: yield return UpdateRuntimeWindow.ShowAsync(); break;
				case ToolsCheckResult.MissingTools: yield return UpdateToolsWindow.ShowAsync(); break;
				case ToolsCheckResult.Ok: break;
				default: throw new InvalidOperationException("Unknown Tools check result.");
			}

			var outputText = new StringBuilder();
			var errorText = new StringBuilder();
			if (Settings.Current.Verbose) Debug.Log(string.Format("Extracting T4 Templates to '{0}'...", extractionPath));
			var generateProcess = new ExecuteCommandTask
			(
				Settings.Current.ToolsPath,
				(sender, args) => { if (!string.IsNullOrEmpty(args.Data)) outputText.Append(args.Data); },
				(sender, args) => { if (!string.IsNullOrEmpty(args.Data)) errorText.Append(args.Data); },
				"DUMPCODEGENERATOR", Path.GetFullPath(extractionPath),
				Settings.Current.Verbose ? "--verbose" : ""
			);
			generateProcess.RequireDotNetRuntime();
			generateProcess.Start();
			yield return generateProcess;

			if (errorText.Length > 0)
				Debug.LogWarning("T4 Template extraction failed: " + errorText);
			else
				Debug.Log("T4 Template extracted successfully. " + outputText);
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
	}
}

