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
using System.IO;
using System.Text;
using Assets.Editor.GameDevWare.Charon.Utils;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Assets.Editor.GameDevWare.Charon
{
	class RecoveryScripts
	{
		private const string RECOVERYSCRIPTS_PATH = "./Library/Charon/RecoveryScripts";
#if UNITY_EDITOR_WIN
		private const string RECOVERYSCRIPTS_EXTENTIONS = ".bat";
#else
		private const string RECOVERYSCRIPTS_EXTENTIONS = ".sh";
#endif

		public static void Clear()
		{
			if (File.Exists(RECOVERYSCRIPTS_PATH))
				File.Delete(RECOVERYSCRIPTS_PATH);
		}
		public static void Generate()
		{
			try
			{
				var paths = Settings.Current.GameDataPaths.ToArray();
				for (var i = 0; i < paths.Length; i++)
				{
					var gameDataPath = paths[i];
					if (File.Exists(gameDataPath) == false)
						continue;

					var assetImport = AssetImporter.GetAtPath(gameDataPath);
					if (assetImport == null)
						continue;

					GenerateCodeGeneratorScripts(gameDataPath);
					GenerateEditScripts(gameDataPath);
				}
			}
			catch (Exception e)
			{
				if (!Settings.Current.Verbose)
					return;

				Debug.LogError("Failed to create recovery scripts: ");
				Debug.LogError(e);
			}
		}

		private static void GenerateEditScripts(string gameDataPath)
		{
			var editScriptOutput = "Edit_" + FileUtils.SanitizeFileName(gameDataPath) + RECOVERYSCRIPTS_EXTENTIONS;
			var output = new StringBuilder();
			output.AppendLine("cd ../../..");

			var appDataPath = Path.GetFullPath(Settings.GetAppDataPath()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			var unityProjectPath = Path.GetFullPath("./").TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			var licenseServer = Settings.Current.LicenseServerAddress;
			var selectedLicense = Settings.Current.SelectedLicense;

#if UNITY_EDITOR_WIN
			output.Append("SET UNITY_PROJECT_PATH=").Append(unityProjectPath).AppendLine();
			output.Append("SET CHARON_APP_DATA=").Append(appDataPath).AppendLine();
			output.Append("SET CHARON_LICENSE_SERVER=").Append(licenseServer).AppendLine();
			output.Append("SET CHARON_SELECTED_LICENSE=").Append(selectedLicense).AppendLine();
#else
			output.Append("UNITY_PROJECT_PATH=").Append(unityProjectPath).AppendLine();
			output.Append("CHARON_APP_DATA=").Append(appDataPath).AppendLine();
			output.Append("CHARON_LICENSE_SERVER=").Append(licenseServer).AppendLine();
			output.Append("CHARON_SELECTED_LICENSE=").Append(selectedLicense).AppendLine();
#endif

			if (string.IsNullOrEmpty(ToolsUtils.MonoPath) == false)
				output.Append("\"").Append(ToolsUtils.MonoPath).Append("\" ");

			output
				.Append("\"").Append(Settings.Current.ToolsPath).Append("\"")
				.Append(" LISTEN ")
				.Append("\"").Append(gameDataPath).Append("\"")
				.Append(" ")
				.Append("--launchDefaultBrowser").Append(" ").Append(true)
				.Append(" ")
				.Append("--port").Append(" ").Append(Settings.Current.ToolsPort)
				.Append(" ")
				.Append("--verbose")
				.AppendLine();

			if (Directory.Exists(RECOVERYSCRIPTS_PATH) == false)
				Directory.CreateDirectory(RECOVERYSCRIPTS_PATH);

			File.WriteAllText(Path.Combine(RECOVERYSCRIPTS_PATH, editScriptOutput), output.ToString());
		}
		private static void GenerateCodeGeneratorScripts(string gameDataPath)
		{
			var gameDataSettings = GameDataSettings.Load(gameDataPath);
			var codeGenerationPath = FileUtils.MakeProjectRelative(gameDataSettings.CodeGenerationPath);
			if (gameDataSettings.Generator == (int)GameDataSettings.CodeGenerator.None)
				return;

			var generateCodeOutput = "GenerateCodeFor_" + FileUtils.SanitizeFileName(gameDataPath) + RECOVERYSCRIPTS_EXTENTIONS;
			var output = new StringBuilder();
			output.AppendLine("cd ../../..");

			var appDataPath = Path.GetFullPath(Settings.GetAppDataPath()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			var unityProjectPath = Path.GetFullPath("./").TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
#if UNITY_EDITOR_WIN
			output.Append("SET UNITY_PROJECT_PATH=").Append(unityProjectPath).AppendLine();
			output.Append("SET CHARON_APP_DATA=").Append(appDataPath).AppendLine();
#else
			output.Append("UNITY_PROJECT_PATH=").Append(unityProjectPath).AppendLine();
			output.Append("CHARON_APP_DATA=").Append(appDataPath).AppendLine();
#endif
			var generationOptions = gameDataSettings.Options;
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
					if (string.IsNullOrEmpty(ToolsUtils.MonoPath) == false)
						output.Append("\"").Append(ToolsUtils.MonoPath).Append("\" ");

					output
						.Append("\"").Append(Settings.Current.ToolsPath).Append("\"")
						.Append(" ")
						.Append("DATA ")
						.Append(generator == GameDataSettings.CodeGenerator.CSharp ? "GENERATECSHARPCODE" : "GENERATEUNITYCSHARPCODE")
						.Append(" ")
						.Append("\"").Append(gameDataPath).Append("\"")
						.Append(" ")
						.Append("--namespace").Append(" ").Append(gameDataSettings.Namespace)
						.Append(" ")
						.Append("--gameDataClassName").Append(" ").Append(gameDataSettings.GameDataClassName)
						.Append(" ")
						.Append("--entryClassName").Append(" ").Append(gameDataSettings.EntryClassName)
						.Append(" ")
						.Append("--options").Append(" ").Append("\"").Append(generationOptions.ToString()).Append("\"")
						.Append(" ")
						.Append("--output").Append(" ").Append("\"").Append(codeGenerationPath).Append("\"")
						.Append(" ")
						.Append("--verbose")
						.AppendLine();
					break;
				default:
					Debug.LogError("Unknown code/asset generator type " + (GameDataSettings.CodeGenerator)gameDataSettings.Generator + ".");
					break;
			}

			if (Directory.Exists(RECOVERYSCRIPTS_PATH) == false)
				Directory.CreateDirectory(RECOVERYSCRIPTS_PATH);

			File.WriteAllText(Path.Combine(RECOVERYSCRIPTS_PATH, generateCodeOutput), output.ToString());
		}
	}
}
