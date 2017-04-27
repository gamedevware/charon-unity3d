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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using GameDevWare.Charon.Async;

using Debug = UnityEngine.Debug;

namespace GameDevWare.Charon.Utils
{
	public static class CharonCli
	{
		public const string DATA_DIRECTORY = "./Library/Charon/";
		public const string TEMP_DIRECTORY = DATA_DIRECTORY + "Temp/";

		public static CharonCheckResult CheckCharon()
		{
			if (String.IsNullOrEmpty(Settings.Current.ToolsPath) || !File.Exists(Settings.Current.ToolsPath))
				return CharonCheckResult.MissingExecutable;

			if (RuntimeInformation.IsWindows)
			{
				if (DotNetRuntimeInformation.GetVersion() == null && (String.IsNullOrEmpty(MonoRuntimeInformation.MonoPath) || File.Exists(MonoRuntimeInformation.MonoPath) == false))
					return CharonCheckResult.MissingRuntime;
			}
			else
			{
				if (String.IsNullOrEmpty(MonoRuntimeInformation.MonoPath) || File.Exists(MonoRuntimeInformation.MonoPath) == false)
					return CharonCheckResult.MissingRuntime;
			}
			return CharonCheckResult.Ok;
		}

		internal static Promise<Process> Listen(string gameDataPath, int port, bool shadowCopy = true)
		{
			if (string.IsNullOrEmpty(gameDataPath)) throw new ArgumentException("Value cannot be null or empty.", "gameDataPath");
			if (port <= 0 || port > ushort.MaxValue) throw new ArgumentOutOfRangeException("port");

			var charonPath = Path.GetFullPath(Settings.Current.ToolsPath);

			if (File.Exists(gameDataPath) == false) throw new IOException(string.Format("File '{0}' doesn't exists.", gameDataPath));
			if (File.Exists(charonPath) == false) throw new IOException(string.Format("File '{0}' doesn't exists.", charonPath));

			if (shadowCopy)
			{
				var charonMd5 = PathUtils.ComputeMd5Hash(charonPath);
				var shadowDirectory = Path.GetFullPath(Path.Combine(TEMP_DIRECTORY, charonMd5));
				if (Directory.Exists(shadowDirectory) == false)
				{
					if (Settings.Current.Verbose)
						Debug.Log("Making shadow copy of '" + Path.GetFileName(charonPath) + "' to '" + shadowDirectory + "'.");

					var shadowCharonPath = Path.Combine(shadowDirectory, Path.GetFileName(charonPath));
					File.Copy(charonPath, shadowCharonPath, overwrite: true);

					var configPath = charonPath + ".config";
					var configShadowPath = shadowCharonPath + ".config";
					if (File.Exists(configPath))
					{
						if (Settings.Current.Verbose)
							Debug.Log("Making shadow copy of '" + Path.GetFileName(configPath) + "' to '" + shadowDirectory + "'.");

						File.Copy(configPath, configShadowPath);
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

			var unityPid = Process.GetCurrentProcess().Id;
			var scriptingAssemblies = FindAndLoadScriptingAssemblies(gameDataPath);
			var runTask = CommandLine.Run(
				new RunOptions
				(
					charonPath,

					RunOptions.FlattenArguments(
						"LISTEN", Path.GetFullPath(gameDataPath),
						"--port", port.ToString(),
						"--parentPid", unityPid.ToString(),
						"--environment", "Unity",
						"--extensions", Settings.SupportedExtensions,
						"--scriptAssemblies", scriptingAssemblies,
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
							{ "CHARON_APP_DATA", Settings.GetAppDataPath() },
							{ "CHARON_LICENSE_SERVER", Settings.Current.LicenseServerAddress }
						}
					}
				}
			);

			return runTask.ContinueWith(t => t.GetResult().Process);
		}

		public static Promise UpdateCharonExecutableAsync(Action<string, float> progressCallback = null)
		{
			return new Coroutine(Menu.CheckForUpdatesAsync(progressCallback));
		}

		public static Promise<RunResult> CreateDocumentAsync(string gameDataPath, string entity, CommandInput input, CommandOutput output)
		{
			if (gameDataPath == null) throw new ArgumentNullException("gameDataPath");
			if (entity == null) throw new ArgumentNullException("entity");
			if (input == null) throw new ArgumentNullException("input");
			if (output == null) throw new ArgumentNullException("output");

			if (File.Exists(gameDataPath) == false) throw new IOException(string.Format("GameData file '{0}' doesn't exists.", gameDataPath));

			var runTask = RunInternal
			(
				RunOptions.FlattenArguments(
					"DATA", "CREATE", gameDataPath,
					"--entity", entity,
					"--input", input.Type,
					"--inputFormat", input.Format,
					"--inputFormattingOptions", input.FormattingOptions,
					"--output", output.Type,
					"--outputFormat", output.Format,
					"--outputFormattingOptions", output.FormattingOptions
				)
			);
			return output.Capture(runTask);
		}

		public static Promise<RunResult> UpdateDocumentAsync(string gameDataPath, string entity, CommandInput input, CommandOutput output)
		{
			return UpdateDocumentAsync(gameDataPath, entity, string.Empty, input, output);
		}
		public static Promise<RunResult> UpdateDocumentAsync(string gameDataPath, string entity, string id, CommandInput input, CommandOutput output)
		{
			if (gameDataPath == null) throw new ArgumentNullException("gameDataPath");
			if (entity == null) throw new ArgumentNullException("entity");
			if (id == null) throw new ArgumentNullException("id");
			if (input == null) throw new ArgumentNullException("input");
			if (output == null) throw new ArgumentNullException("output");

			if (File.Exists(gameDataPath) == false) throw new IOException(string.Format("GameData file '{0}' doesn't exists.", gameDataPath));

			var runTask = RunInternal
			(
				RunOptions.FlattenArguments(
					"DATA", "UPDATE", gameDataPath,
					"--entity", entity,
					"--id", id,
					"--input", input.Type,
					"--inputFormat", input.Format,
					"--inputFormattingOptions", input.FormattingOptions,
					"--output", output.Type,
					"--outputFormat", output.Format,
					"--outputFormattingOptions", output.FormattingOptions
				)
			);
			return output.Capture(runTask);
		}

		public static Promise<RunResult> DeleteDocumentAsync(string gameDataPath, string entity, CommandInput input, CommandOutput output)
		{
			return DeleteDocumentAsync(gameDataPath, entity, string.Empty, input, output);
		}
		public static Promise<RunResult> DeleteDocumentAsync(string gameDataPath, string entity, string id, CommandInput input, CommandOutput output)
		{
			if (gameDataPath == null) throw new ArgumentNullException("gameDataPath");
			if (entity == null) throw new ArgumentNullException("entity");
			if (id == null) throw new ArgumentNullException("id");
			if (input == null) throw new ArgumentNullException("input");
			if (output == null) throw new ArgumentNullException("output");

			if (File.Exists(gameDataPath) == false) throw new IOException(string.Format("GameData file '{0}' doesn't exists.", gameDataPath));

			var runTask = RunInternal
			(
				RunOptions.FlattenArguments(
					"DATA", "DELETE", gameDataPath,
					"--entity", entity,
					"--id", id,
					"--input", input.Type,
					"--inputFormat", input.Format,
					"--inputFormattingOptions", input.FormattingOptions,
					"--output", output.Type,
					"--outputFormat", output.Format,
					"--outputFormattingOptions", output.FormattingOptions
				)
			);
			return output.Capture(runTask);
		}

		public static Promise<RunResult> GetDocumentAsync(string gameDataPath, string entity, string id, CommandOutput output)
		{
			if (gameDataPath == null) throw new ArgumentNullException("gameDataPath");
			if (entity == null) throw new ArgumentNullException("entity");
			if (id == null) throw new ArgumentNullException("id");
			if (output == null) throw new ArgumentNullException("output");

			if (File.Exists(gameDataPath) == false) throw new IOException(string.Format("GameData file '{0}' doesn't exists.", gameDataPath));

			var runTask = RunInternal
			(
				RunOptions.FlattenArguments(
					"DATA", "FIND", gameDataPath,
					"--entity", entity,
					"--id", id,
					"--output", output.Type,
					"--outputFormat", output.Format,
					"--outputFormattingOptions", output.FormattingOptions
				)
			);
			return output.Capture(runTask);
		}

		public static Promise<RunResult> ImportAsync(string gameDataPath, CommandInput input, ImportMode mode)
		{
			return ImportAsync(gameDataPath, new string[0], input, mode);
		}
		public static Promise<RunResult> ImportAsync(string gameDataPath, string[] entities, CommandInput input, ImportMode mode)
		{
			if (gameDataPath == null) throw new ArgumentNullException("gameDataPath");
			if (entities == null) throw new ArgumentNullException("entities");
			if (input == null) throw new ArgumentNullException("input");

			if (Enum.IsDefined(typeof(ImportMode), mode) == false) throw new ArgumentException("Unknown import mode.", "mode");
			if (File.Exists(gameDataPath) == false) throw new IOException(string.Format("GameData file '{0}' doesn't exists.", gameDataPath));

			var runTask = RunInternal
			(
				RunOptions.FlattenArguments(
					"DATA", "IMPORT", gameDataPath,
					"--entities", entities,
					"--mode", mode,
					"--input", input.Type,
					"--inputFormat", input.Format,
					"--inputFormattingOptions", input.FormattingOptions
				)
			);
			return runTask;
		}

		public static Promise<RunResult> ExportAsync(string gameDataPath, CommandOutput output, ExportMode mode)
		{
			return ExportAsync(gameDataPath, new string[0], output, mode);
		}
		public static Promise<RunResult> ExportAsync(string gameDataPath, string[] entities, CommandOutput output, ExportMode mode)
		{
			return ExportAsync(gameDataPath, entities, new string[0], output, mode);
		}
		public static Promise<RunResult> ExportAsync(string gameDataPath, string[] entities, string[] attributes, CommandOutput output, ExportMode mode)
		{
			return ExportAsync(gameDataPath, entities, attributes, new CultureInfo[0], output, mode);
		}
		public static Promise<RunResult> ExportAsync(string gameDataPath, string[] entities, string[] attributes, CultureInfo[] languages, CommandOutput output, ExportMode mode)
		{
			if (gameDataPath == null) throw new ArgumentNullException("gameDataPath");
			if (entities == null) throw new ArgumentNullException("entities");
			if (attributes == null) throw new ArgumentNullException("attributes");
			if (languages == null) throw new ArgumentNullException("languages");
			if (output == null) throw new ArgumentNullException("output");

			if (Enum.IsDefined(typeof(ExportMode), mode) == false) throw new ArgumentException("Unknown export mode.", "mode");
			if (File.Exists(gameDataPath) == false) throw new IOException(string.Format("GameData file '{0}' doesn't exists.", gameDataPath));

			var runTask = RunInternal
			(
				RunOptions.FlattenArguments(
					"DATA", "EXPORT", gameDataPath,
					"--entities", entities,
					"--attributes", attributes,
					"--languages", Array.ConvertAll(languages, l => l.Name),
					"--mode", mode,
					"--output", output.Type,
					"--outputFormat", output.Format,
					"--outputFormattingOptions", output.FormattingOptions
				)
			);
			return runTask;
		}

		public static Promise<RunResult> BackupAsync(string gameDataPath, CommandOutput output)
		{
			if (gameDataPath == null) throw new ArgumentNullException("gameDataPath");
			if (output == null) throw new ArgumentNullException("output");

			if (File.Exists(gameDataPath) == false) throw new IOException(string.Format("GameData file '{0}' doesn't exists.", gameDataPath));

			var runTask = RunInternal
			(
				RunOptions.FlattenArguments(
					"DATA", "Backup", gameDataPath,
					"--output", output.Type,
					"--outputFormat", output.Format,
					"--outputFormattingOptions", output.FormattingOptions
				)
			);
			return output.Capture(runTask);
		}

		public static Promise<RunResult> RestoreAsync(string gameDataPath, CommandInput input)
		{
			if (gameDataPath == null) throw new ArgumentNullException("gameDataPath");
			if (input == null) throw new ArgumentNullException("input");

			var runTask = RunInternal
			(
				RunOptions.FlattenArguments(
					"DATA", "RESTORE", gameDataPath,
					"--input", input.Type,
					"--inputFormat", input.Format,
					"--inputFormattingOptions", input.FormattingOptions
				)
			);
			return runTask;
		}

		public static Promise<RunResult> ValidateAsync(string gameDataPath, ValidationOptions validationOptions, CommandOutput output)
		{
			if (gameDataPath == null) throw new ArgumentNullException("gameDataPath");
			if (output == null) throw new ArgumentNullException("output");

			if (File.Exists(gameDataPath) == false) throw new IOException(string.Format("GameData file '{0}' doesn't exists.", gameDataPath));

			var runTask = RunInternal
			(
				RunOptions.FlattenArguments(
					"DATA", "VALIDATE", gameDataPath,
					"--validationOptions", ((int)validationOptions).ToString(),
					"--output", output.Type,
					"--outputFormat", output.Format,
					"--outputFormattingOptions", output.FormattingOptions
				)
			);
			return output.Capture(runTask);
		}

		public static Promise<RunResult> GenerateCSharpCodeAsync(string gameDataPath, string outputFilePath,
			CodeGenerationOptions options = CodeGenerationOptions.HideReferences | CodeGenerationOptions.HideLocalizedStrings,
			string documentClassName = "Document", string apiClassName = "GameData",
			string @namespace = "GameParameters",
			string outputEncoding = "utf-8")
		{
			if (gameDataPath == null) throw new ArgumentNullException("gameDataPath");
			if (outputFilePath == null) throw new ArgumentNullException("outputFilePath");
			if (documentClassName == null) throw new ArgumentNullException("documentClassName");
			if (apiClassName == null) throw new ArgumentNullException("apiClassName");
			if (@namespace == null) throw new ArgumentNullException("namespace");
			if (outputEncoding == null) throw new ArgumentNullException("outputEncoding");

			if (File.Exists(gameDataPath) == false) throw new IOException(string.Format("GameData file '{0}' doesn't exists.", gameDataPath));

			var runTask = RunInternal
			(
				RunOptions.FlattenArguments
				(
					"GENERATE", "CSHARPCODE", gameDataPath,
					"--documentClassName", documentClassName,
					"--apiClassName", apiClassName,
					"--namespace", @namespace,
					"--options", ((int)options).ToString(),
					"--output", outputFilePath,
					"--outputEncoding", outputEncoding
				)
			);
			return runTask;
		}

		public static Promise<RunResult> GenerateUnityCSharpCodeAsync(string gameDataPath, string outputFilePath,
			CodeGenerationOptions options = CodeGenerationOptions.HideReferences | CodeGenerationOptions.HideLocalizedStrings,
			string documentClassName = "Document", string apiClassName = "GameData",
			string @namespace = "GameParameters",
			string outputEncoding = "utf-8")
		{
			if (gameDataPath == null) throw new ArgumentNullException("gameDataPath");
			if (outputFilePath == null) throw new ArgumentNullException("outputFilePath");
			if (documentClassName == null) throw new ArgumentNullException("documentClassName");
			if (apiClassName == null) throw new ArgumentNullException("apiClassName");
			if (@namespace == null) throw new ArgumentNullException("namespace");
			if (outputEncoding == null) throw new ArgumentNullException("outputEncoding");

			if (File.Exists(gameDataPath) == false) throw new IOException(string.Format("GameData file '{0}' doesn't exists.", gameDataPath));

			var runTask = RunInternal
			(
				RunOptions.FlattenArguments
				(
					"GENERATE", "UNITYCSHARPCODE", gameDataPath,
					"--documentClassName", documentClassName,
					"--apiClassName", apiClassName,
					"--namespace", @namespace,
					"--options", ((int)options).ToString(),
					"--output", outputFilePath,
					"--outputEncoding", outputEncoding
				)
			);
			return runTask;
		}

		public static Promise<RunResult> DumpTemplatesAsync(string outputDirectory)
		{
			if (string.IsNullOrEmpty(outputDirectory)) throw new ArgumentException("Value cannot be null or empty.", "outputDirectory");

			if (Directory.Exists(outputDirectory) == false) throw new IOException(string.Format("Directory '{0}' doesn't exists.", outputDirectory));

			var runTask = RunInternal
			(
				"GENERATE", "TEMPLATES",
				"--outputDirectory", outputDirectory
			);
			return runTask;

		}

		public static Promise<RunResult> ReportIssueAsync(string reporter, IssueType type, string description)
		{
			return ReportIssueAsync(reporter, type, description, new string[0]);
		}
		public static Promise<RunResult> ReportIssueAsync(string reporter, IssueType type, string description, string[] attachments)
		{
			if (string.IsNullOrEmpty(reporter)) throw new ArgumentException("Value cannot be null or empty.", "reporter");
			if (description == null) throw new ArgumentNullException("description");
			if (attachments == null) throw new ArgumentNullException("attachments");

			var runTask = RunInternal
			(
				RunOptions.FlattenArguments
				(
					"SERVER", "REPORTISSUE",
					"--reporter", reporter,
					"--type", type,
					"--description", description,
					"--attachments", attachments
				)
			);
			return runTask;
		}

		public static Promise<Version> GetGameDataVersionAsync(string gameDataPath)
		{
			if (string.IsNullOrEmpty(gameDataPath)) throw new ArgumentException("Value cannot be null or empty.", "gameDataPath");

			if (File.Exists(gameDataPath) == false) throw new IOException(string.Format("GameData file '{0}' doesn't exists.", gameDataPath));

			var runTask = RunInternal("DATA", "VERSION", gameDataPath);
			return runTask.ContinueWith(r =>
			{
				using (var result = r.GetResult())
					return new Version(result.GetOutputData());
			});
		}

		public static Promise<Version> GetVersionAsync()
		{
			var runTask = RunInternal("VERSION");
			return runTask.ContinueWith(r =>
			{
				using (var result = r.GetResult())
					return new Version(result.GetOutputData());
			});
		}

		private static Promise<RunResult> RunInternal(params string[] arguments)
		{
			var charonPath = Settings.Current.ToolsPath;

			if (File.Exists(charonPath) == false) throw new IOException(string.Format("File '{0}' doesn't exists.", charonPath));

			arguments = arguments.Concat(new [] { Settings.Current.Verbose ? "--verbose" : "" }).ToArray();

			return CommandLine.Run(new RunOptions(charonPath, arguments)
			{
				CaptureStandardOutput = true,
				CaptureStandardError = true,
				ExecutionTimeout = TimeSpan.FromSeconds(30),
				RequireDotNetRuntime = true,
				WaitForExit = true,
				StartInfo =
				{
					EnvironmentVariables =
					{
						{ "CHARON_APP_DATA", Settings.GetAppDataPath() },
						{ "CHARON_LICENSE_SERVER", Settings.Current.LicenseServerAddress },
					}
				}
			});
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

	}
}
