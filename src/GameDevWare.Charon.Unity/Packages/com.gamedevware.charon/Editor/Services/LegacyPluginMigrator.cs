using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GameDevWare.Charon.Editor.Json;
using GameDevWare.Charon.Editor.Routines;
using GameDevWare.Charon.Editor.Utils;
using GameDevWare.Charon.Editor.Windows;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace GameDevWare.Charon.Editor.Services
{
	public class LegacyPluginMigrator
	{
		private static readonly string LegacyPluginPath = Path.Combine("Assets", "Editor", "GameDevWare.Charon");
		private static readonly string[] LegacyPluginFileNames = new string[] {
			"GameDevWare.Charon.Unity.dll",
			"GameDevWare.Charon.Unity.Settings.json",
			"GameDevWare.Charon.Unity.dll.sha1",
			"GameDevWare.Charon.Unity.xml",
			"GameDevWare.Charon.xml",
			"Documentation.odt",
			"Documentation.pdf",
		};

		private readonly ILogger logger;

		public LegacyPluginMigrator(ILogger logger)
		{
			this.logger = logger;
		}

		public void Initialize()
		{
			if (!this.IsLegacyPluginExists())
			{
				return;
			}

			this.logger.Log(LogType.Assert, "Legacy plugin installation found. Asking user about migration.");

			MigrationPromptWindow.ShowAsync();
		}

		public async Task MigrateAsync(Action<string, float> progressCallback)
		{
			EditorApplication.LockReloadAssemblies();
			try
			{
				progressCallback.Invoke(Resources.UI_UNITYPLUGIN_PROGRESS_DELETING_FILES, 0.0f);
				this.DeleteLegacyPluginFiles();
				progressCallback?.Invoke(Resources.UI_UNITYPLUGIN_PROGRESS_MIGRATING, 0.05f);
				var convertedGameDataAssets = this.ConvertExistingGameDataAssets();
				progressCallback?.Invoke(Resources.UI_UNITYPLUGIN_PROGRESS_MIGRATING, 0.20f);
				var reimportTask = ReimportAssetsRoutine.ScheduleAsync(
					convertedGameDataAssets,
					progressCallback.Sub(0.20f, 1.00f)
				);
				reimportTask.LogFaultAsError();
				reimportTask.ContinueWithHideProgressBar();
				await reimportTask.ConfigureAwait(true);
				progressCallback?.Invoke(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1.00f);
			}
			finally
			{
				EditorApplication.UnlockReloadAssemblies();
				ProgressUtils.HideProgressBar();
			}
		}

		public bool IsLegacyPluginExists()
		{
			return File.Exists(Path.GetFullPath(Path.Combine(LegacyPluginPath, LegacyPluginFileNames[0])));
		}
		private void DeleteLegacyPluginFiles()
		{
			var legacyPluginPath = Path.GetFullPath(LegacyPluginPath);
			var legacyPluginPathMeta = Path.GetFullPath(LegacyPluginPath).TrimEnd('\\', '/') + ".meta";
			if (!Directory.Exists(legacyPluginPath))
			{
				return;
			}

			foreach (var fileNameToDelete in LegacyPluginFileNames)
			{
				var filePath = Path.Combine(legacyPluginPath, fileNameToDelete);
				var filePathMeta = Path.Combine(legacyPluginPath, fileNameToDelete + ".meta");

				CharonFileUtils.SafeFileDelete(filePath);
				CharonFileUtils.SafeFileDelete(filePathMeta);
			}

			// delete empty GameDevWare.Charon folder
			if (Directory.GetFiles(legacyPluginPath).Length == 0 &&
				Directory.GetDirectories(legacyPluginPath).Length == 0)
			{
				Directory.Delete(legacyPluginPath, recursive: true);
				CharonFileUtils.SafeFileDelete(legacyPluginPathMeta);
			}
		}
		private string[] ConvertExistingGameDataAssets()
		{
			var gameDataPaths = from id in AssetDatabase.FindAssets("t:DefaultAsset").Concat(AssetDatabase.FindAssets("t:TextAsset"))
								let path = CharonFileUtils.GetProjectRelativePath(AssetDatabase.GUIDToAssetPath(id))
								where path != null && IsGameDataFile(path)
								select path;

			var convertedGameDataAssetPaths = new List<string>();
			foreach (var gameDataPath in gameDataPaths)
			{
				var gameDataSettings = new GameDataSettings();
				if(!this.TryCopyGameDataAssetSettings(gameDataPath, gameDataSettings, out var gameDataAssetPath) ||
					!this.TryCreateGameDataAsset(gameDataAssetPath, gameDataPath, gameDataSettings, out var gameDataAsset))
				{
					this.logger.Log(LogType.Warning, $"Failed to migrate game data at '{gameDataPath}' due to an error while converting its settings or creating asset file. " +
						"Please manually delete the old .asset file (if it exists) along with any related generated source code files, then re-import the game data file from Inspector window.");
					continue;
				}
				var importer = AssetImporter.GetAtPath(gameDataPath);
				if (importer != null && !string.IsNullOrEmpty(importer.userData))
				{
					importer.userData = null;
					EditorUtility.SetDirty(AssetDatabase.LoadAssetAtPath<UnityObject>(gameDataPath));
					importer.SaveAndReimport();
				}

				convertedGameDataAssetPaths.Add(AssetDatabase.GetAssetPath(gameDataAsset));
			}
			return convertedGameDataAssetPaths.ToArray();

			static bool IsGameDataFile(string path)
			{
				// ReSharper disable StringLiteralTypo
				return path.EndsWith(".gdjs", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".gdmp", StringComparison.OrdinalIgnoreCase);
				// ReSharper enable StringLiteralTypo
			}
		}

		private bool TryCreateGameDataAsset(string gameDataAssetPath, string gameDataPath, GameDataSettings gameDataSettings, out GameDataBase gameDataAsset)
		{
			try
			{
				var isStreamingAssetsDirectory = gameDataPath.StartsWith(Path.Combine("Assets", "StreamingAssets"), StringComparison.OrdinalIgnoreCase);
				if (string.IsNullOrEmpty(gameDataAssetPath))
				{
					if (isStreamingAssetsDirectory)
					{
						gameDataAssetPath = Path.Combine("Assets", Path.GetFileNameWithoutExtension(gameDataPath) + ".asset");
					}
					else
					{
						gameDataAssetPath = Path.Combine(Path.GetDirectoryName(gameDataPath) ?? "Assets", Path.GetFileNameWithoutExtension(gameDataPath) + ".asset");
					}
				}

				var gameDataFileGuid = AssetDatabase.AssetPathToGUID(gameDataPath);
				gameDataAsset = ScriptableObject.CreateInstance<GameDataBase>();
				gameDataAsset.settings = gameDataSettings;
				gameDataAsset.settings.gameDataFileGuid = gameDataFileGuid;

				var oldGameDataAsset = AssetDatabase.LoadAssetAtPath<UnityObject>(gameDataAssetPath);
				if (oldGameDataAsset != null)
				{
					UnityObject.DestroyImmediate(oldGameDataAsset, allowDestroyingAssets: true);
				}

				CharonFileUtils.SafeFileDelete(gameDataAssetPath);

				AssetDatabase.CreateAsset(gameDataAsset, gameDataAssetPath);
				return true;
			}
			catch (Exception createError)
			{
				this.logger.Log(LogType.Warning, $"Failed to create game data asset at '{gameDataAssetPath}' due to an error.");
				this.logger.LogException(createError);

				gameDataAsset = null;
				return false;
			}
		}
		private bool TryCopyGameDataAssetSettings(string gameDataPath, GameDataSettings gameDataSettings, out string gameDataAssetPath)
		{
			const string ASSET_GENERATION_PATH = "AssetGenerationPath";
			const string CODE_GENERATION_PATH = "CodeGenerationPath";
			const string GAME_DATA_CLASS_NAME = "GameDataClassName";
			const string NAMESPACE = "Namespace";
			const string DOCUMENT_CLASS_NAME = "DocumentClassName";
			const string OPTIMIZATIONS = "Optimizations";
			const string LINE_ENDING = "LineEnding";
			const string INDENTATION = "Indentation";
			const string SPLIT_SOURCE_CODE_FILES = "SplitSourceCodeFiles";
			const string SERVER_ADDRESS = "ServerAddress";
			const string PROJECT_ID = "ProjectId";
			const string PROJECT_NAME = "ProjectName";
			const string BRANCH_NAME = "BranchName";
			const string BRANCH_ID = "BranchId";

			gameDataAssetPath = null;
			var legacySettingsJson = AssetImporter.GetAtPath(gameDataPath)?.userData;
			if (string.IsNullOrEmpty(legacySettingsJson))
			{
				return false;
			}

			try
			{
				var legacySettings = (JsonObject)JsonValue.Parse(legacySettingsJson);
				var assetGenerationPath = GetStringValueOrNull(legacySettings[ASSET_GENERATION_PATH]);
				var codeGenerationPath = GetStringValueOrNull(legacySettings[CODE_GENERATION_PATH]);
				var gameDataClassName = GetStringValueOrNull(legacySettings[GAME_DATA_CLASS_NAME]);
				var gameDataNamespace = GetStringValueOrNull(legacySettings[NAMESPACE]);
				var documentClassName = GetStringValueOrNull(legacySettings[DOCUMENT_CLASS_NAME]);
				var optimizations = GetIntValueOrNull(legacySettings[OPTIMIZATIONS]);
				var lineEnding = GetIntValueOrNull(legacySettings[LINE_ENDING]);
				var indentation = GetIntValueOrNull(legacySettings[INDENTATION]);
				var splitSourceCodeFiles = GetBoolValueOrNull(legacySettings[SPLIT_SOURCE_CODE_FILES]);

				var serverAddress = GetStringValueOrNull(legacySettings[SERVER_ADDRESS]);
				var projectId = GetStringValueOrNull(legacySettings[PROJECT_ID]);
				var projectName = GetStringValueOrNull(legacySettings[PROJECT_NAME]);
				var branchId = GetStringValueOrNull(legacySettings[BRANCH_ID]);
				var branchName = GetStringValueOrNull(legacySettings[BRANCH_NAME]);

				if (!string.IsNullOrEmpty(assetGenerationPath))
				{
					gameDataAssetPath = assetGenerationPath;
				}
				if (!string.IsNullOrEmpty(codeGenerationPath))
				{
					gameDataSettings.codeGenerationPath = codeGenerationPath;
				}
				if (!string.IsNullOrEmpty(gameDataClassName))
				{
					gameDataSettings.gameDataClassName = gameDataClassName;
				}
				if (!string.IsNullOrEmpty(gameDataNamespace))
				{
					gameDataSettings.gameDataNamespace = gameDataNamespace;
				}
				if (!string.IsNullOrEmpty(documentClassName))
				{
					gameDataSettings.gameDataDocumentClassName = documentClassName;
				}
				if (optimizations != null)
				{
					gameDataSettings.optimizations = optimizations.Value;
				}
				if (lineEnding != null)
				{
					gameDataSettings.lineEnding = lineEnding.Value;
				}
				if (indentation != null)
				{
					gameDataSettings.indentation = indentation.Value;
				}
				if (splitSourceCodeFiles != null)
				{
					gameDataSettings.splitSourceCodeFiles = splitSourceCodeFiles.Value;
				}
				if (!string.IsNullOrEmpty(serverAddress) &&
					!string.IsNullOrEmpty(projectId) && !string.IsNullOrEmpty(projectName) &&
					!string.IsNullOrEmpty(branchId) && !string.IsNullOrEmpty(branchName))
				{
					gameDataSettings.serverAddress = serverAddress;
					gameDataSettings.projectId = projectId;
					gameDataSettings.projectName = projectName;
					gameDataSettings.branchName = branchName;
					gameDataSettings.branchId = branchId;
				}
			}
			catch (Exception jsonParseError)
			{
				this.logger.Log(LogType.Warning, "Failed to read game data settings from importer's userData field due to a JSON parsing error.");
				this.logger.LogException(jsonParseError);
				return false;
			}

			return true;

			static string GetStringValueOrNull(JsonValue value)
			{
				return value.JsonType switch {
					JsonType.String => Convert.ToString(value.ToObject(), CultureInfo.InvariantCulture),
					JsonType.Number => Convert.ToString(value.ToObject(), CultureInfo.InvariantCulture),
					JsonType.Object => null,
					JsonType.Array => null,
					JsonType.Boolean => Convert.ToString(value.ToObject(), CultureInfo.InvariantCulture),
					_ => throw new ArgumentOutOfRangeException()
				};
			}
			static int? GetIntValueOrNull(JsonValue value)
			{
				return value.JsonType switch {
					JsonType.String => int.TryParse((string)value.ToObject(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue) ? intValue : null,
					JsonType.Number => Convert.ToInt32(value.ToObject(), CultureInfo.InvariantCulture),
					JsonType.Object => null,
					JsonType.Array => null,
					JsonType.Boolean => null,
					_ => throw new ArgumentOutOfRangeException()
				};
			}
			static bool? GetBoolValueOrNull(JsonValue value)
			{
				return value.JsonType switch {
					JsonType.String => bool.TryParse((string)value.ToObject(), out var boolValue) ? boolValue : null,
					JsonType.Number => Convert.ToInt32(value.ToObject(), CultureInfo.InvariantCulture) > 0,
					JsonType.Object => null,
					JsonType.Array => null,
					JsonType.Boolean => (bool)value.ToObject(),
					_ => throw new ArgumentOutOfRangeException()
				};
			}
		}
	}
}
