using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GameDevWare.Charon.Editor.Utils;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameDevWare.Charon.Editor.Routines
{
	[PublicAPI]
	public static class ReimportAssetsRoutine
	{
		public static Task ScheduleAsync(string[] paths = null, Action<string, float> progressCallback = null, CancellationToken cancellationToken = default)
		{
			return CharonEditorModule.Instance.Routines.Schedule(() => RunAsync(paths, progressCallback, cancellationToken), cancellationToken);
		}
		public static async Task RunAsync(string[] paths, Action<string, float> progressCallback, CancellationToken cancellationToken = default)
		{
			if (paths == null) throw new ArgumentNullException(nameof(paths));

			cancellationToken.ThrowIfCancellationRequested();

			var total = paths.Length;
			var assetCreationProgress = progressCallback?.Sub(0.00f, 0.30f);
			progressCallback?.Invoke(Resources.UI_UNITYPLUGIN_PROGRESS_IMPORTING, 0.00f);

			for (var i = 0; i < paths.Length; i++)
			{
				var gameDataFilePath = paths[i];
				var pathSpecificProgress = assetCreationProgress?.Sub((float)i / total, i + 1.0f / total);

				pathSpecificProgress?.Invoke(Resources.UI_UNITYPLUGIN_PROGRESS_IMPORTING, 0.05f);
				var gameDataFile = AssetDatabase.LoadAssetAtPath<Object>(gameDataFilePath);
				if (gameDataFile == null)
				{
					continue;
				}

				var gameDataAsset = GameDataAssetUtils.GetAssociatedGameDataAsset(gameDataFile);
				var gameDataPath = AssetDatabase.GetAssetPath(gameDataFile);
				var gameDataFileGuid = AssetDatabase.AssetPathToGUID(gameDataPath);

				var gameDataAssetPath = string.Empty;
				if (gameDataAsset == null) // crate new asset and code
				{
					pathSpecificProgress?.Invoke(Resources.UI_UNITYPLUGIN_CREATING_GAMEDATA_ASSET, 0.05f);
					gameDataAssetPath = CreateNewGameDataAsset(gameDataFile, gameDataPath, gameDataFileGuid);
				}
				else
				{
					gameDataAssetPath = AssetDatabase.GetAssetPath(gameDataAsset);
				}

				CharonEditorModule.Instance.AssetImporter.ImportOnStart(gameDataAssetPath);
			}

			progressCallback?.Invoke(Resources.UI_UNITYPLUGIN_GENERATING_SOURCE_CODE, 0.30f);

			await GenerateSourceCodeRoutine.RunAsync(
				paths: paths,
				progressCallback: progressCallback?.Sub(0.30f, 1.00f),
				cancellationToken: CancellationToken.None
			).ConfigureAwait(true);
		}

		public static string CreateNewGameDataAsset(Object creationFolder, string gameDataPath, string gameDataFileGuid)
		{
			if (!ValidateCreationOptions(creationFolder, Path.GetFileNameWithoutExtension(gameDataPath), out var errorMessage))
			{
				throw new InvalidOperationException(errorMessage);
			}

			var newGameDataAssetPath = CharonFileUtils.GetProjectRelativePath(Path.Combine(Path.GetDirectoryName(gameDataPath) ?? "",
				Path.GetFileNameWithoutExtension(gameDataPath) + ".asset"));
			var gameDataAsset = ScriptableObject.CreateInstance<GameDataBase>();
			gameDataAsset.settings = GameDataSettingsUtils.CreateDefault(gameDataPath, gameDataFileGuid);
			AssetDatabase.CreateAsset(gameDataAsset, newGameDataAssetPath);
			return newGameDataAssetPath;
		}
		public static bool ValidateCreationOptions(Object folder, string gameDataName, out string errorMessage)
		{
			var gameDataDirectory = GetBaseDirectory(folder);
			var isStreamingAssetsDirectory =
				gameDataDirectory.StartsWith(Path.GetFullPath(Path.Combine("Assets", "StreamingAssets")), StringComparison.OrdinalIgnoreCase);
			if (isStreamingAssetsDirectory)
			{
				errorMessage = Resources.UI_UNITYPLUGIN_CREATING_GAME_DATA_NO_STREAMING_ASSETS;
				return false;
			}

			if (!GameDataAssetUtils.IsValidName(gameDataName))
			{
				errorMessage = Resources.UI_UNITYPLUGIN_CREATING_GAME_DATA_INVALID_NAME;
				return false;
			}

			var collidedAssetPath = GameDataAssetUtils.FindNameCollision(gameDataName);
			if (collidedAssetPath != null)
			{
				errorMessage = string.Format(Resources.UI_UNITYPLUGIN_CREATING_GAME_DATA_IS_USED, collidedAssetPath);
			}

			errorMessage = null;
			return true;
		}
		public static string GetBaseDirectory(Object folder)
		{
			var gameDataDirectory = "Assets";
			if (folder != null)
			{
				gameDataDirectory = AssetDatabase.GetAssetPath(folder);
				if (File.Exists(gameDataDirectory))
				{
					gameDataDirectory = Path.GetDirectoryName(gameDataDirectory) ?? gameDataDirectory;
				}
			}

			return gameDataDirectory;
		}
	}
}
