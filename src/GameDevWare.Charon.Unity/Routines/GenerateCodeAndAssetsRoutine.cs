﻿/*
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

using GameDevWare.Charon.Unity.Async;
using GameDevWare.Charon.Unity.Utils;
using GameDevWare.Charon.Unity.Windows;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace GameDevWare.Charon.Unity.Routines
{
	[PublicAPI, UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public static class GenerateCodeAndAssetsRoutine
	{
		public static Promise Run(string path = null, Action<string, float> progressCallback = null)
		{
			return new Async.Coroutine(GenerateCodeAndAssets(path, progressCallback));
		}
		public static Promise Schedule(string path = null, Action<string, float> progressCallback = null, string coroutineId = null)
		{
			return CoroutineScheduler.Schedule(GenerateCodeAndAssets(path, progressCallback), coroutineId);
		}

		private static IEnumerable GenerateCodeAndAssets(string path, Action<string, float> progressCallback)
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
				var codeGenerationPath = FileHelper.MakeProjectRelative(gameDataSettings.CodeGenerationPath);
				if (gameDataSettings.Generator == (int)GameDataSettings.CodeGenerator.None)
				{

					continue;
				}

				var optimizations = gameDataSettings.Optimizations;

				// trying to touch gamedata file
				var readGameDataTask = FileHelper.ReadFileAsync(gameDataPath, 5);
				yield return readGameDataTask;

				if (readGameDataTask.GetResult().Length == 0)
				{
					if (Settings.Current.Verbose)
						Debug.LogWarning(string.Format("Code generation was skipped for an empty file '{0}'.", gameDataPath));
					continue;
				}
				readGameDataTask.GetResult().Dispose(); // release touched file

				var generator = (GameDataSettings.CodeGenerator)gameDataSettings.Generator;
				switch (generator)
				{
					case GameDataSettings.CodeGenerator.CSharpCodeAndAsset:
						if (!string.IsNullOrEmpty(gameDataSettings.AssetGenerationPath))
						{
							AssetGenerator.AddPath(gameDataPath);
							optimizations &= ~(int)(SourceCodeGenerationOptimizations.DisableJsonSerialization |
								SourceCodeGenerationOptimizations.DisableMessagePackSerialization
							);

							var assetCodeGenerationPath = Path.Combine(gameDataSettings.CodeGenerationPath,
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
						var startTime = Stopwatch.StartNew();
						if (Settings.Current.Verbose)
							Debug.Log(string.Format("Staring C# code generation for game data '{0}'.", gameDataPath));

						if (progressCallback != null)
							progressCallback(string.Format(Resources.UI_UNITYPLUGIN_GENERATE_CODE_FOR, gameDataPath), (float)i / total);

						var generateProcess = CharonCli.GenerateCSharpCodeAsync
						(
							gameDataPath,
							Path.GetFullPath(codeGenerationPath),
							(SourceCodeGenerationOptimizations)optimizations,
							gameDataSettings.DocumentClassName,
							gameDataSettings.GameDataClassName,
							gameDataSettings.Namespace,
							(SourceCodeIndentation)gameDataSettings.Indentation,
							(SourceCodeLineEndings)gameDataSettings.LineEnding,
							gameDataSettings.SplitSourceCodeFiles
						);
						yield return generateProcess;

						if (Settings.Current.Verbose)
						{
							Debug.Log(string.Format("C# code generation is finished, exit code: '{0}'", generateProcess.GetResult().ExitCode));
						}

						using (var generateResult = generateProcess.GetResult())
						{
							if (generateResult.ExitCode != 0)
							{
								Debug.LogWarning(string.Format(Resources.UI_UNITYPLUGIN_GENERATE_FAILED_DUE_ERRORS, gameDataPath, generateResult.GetErrorData()));
							}
							else
							{
								forceReImportList.AddRange(Directory.GetFiles(codeGenerationPath, "*.cs").Select(FileHelper.MakeProjectRelative));

								if (Settings.Current.Verbose)
									Debug.Log(string.Format("C# code generation for game data '{0}' is finished successfully in '{1}'.", gameDataPath, startTime.Elapsed));
							}
						}
						break;
					case GameDataSettings.CodeGenerator.None:
					default:
						Debug.LogError("Unknown code/asset generator type " + (GameDataSettings.CodeGenerator)gameDataSettings.Generator + ".");
						break;
				}
			}
			if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_GENERATE_REFRESHING_ASSETS, 0.99f);
			foreach (var forceReImportPath in forceReImportList)
			{
				AssetDatabase.ImportAsset(forceReImportPath, ImportAssetOptions.ForceUpdate);
			}
			if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1);
		}

	}
}
