using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace GameDevWare.Charon.Editor.Utils
{
	public static class GameDataAssetUtils
	{
		public static GameDataBase GetAssociatedGameDataAsset(UnityObject gameDataFile)
		{
			switch (gameDataFile)
			{
				case GameDataBase gameDataBase:
					return gameDataBase;
				case TextAsset:
				case DefaultAsset:
				{
					var gameDataPath = AssetDatabase.GetAssetPath(gameDataFile);
					var gameDataFileGuid = AssetDatabase.AssetPathToGUID(gameDataPath);
					return AssetDatabase
						.FindAssets("t:" + nameof(GameDataBase))
						.Select(AssetDatabase.GUIDToAssetPath)
						.Select(AssetDatabase.LoadAssetAtPath<GameDataBase>)
						.FirstOrDefault(otherGameDataAsset => otherGameDataAsset.settings.gameDataFileGuid == gameDataFileGuid);
				}
				default:
					return null;
			}
		}
		public static string FindNameCollision(string gameDataName)
		{
			var expectedAssetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
				gameDataName + ".gdjs",
				gameDataName + ".gdmp",
				gameDataName + ".cs",
				gameDataName + "Asset.cs",
				gameDataName + "Asset.asset",
			};
			var textAndScriptingAssets = AssetDatabase.FindAssets("t:" + nameof(TextAsset)).Union(AssetDatabase.FindAssets("t:" + nameof(ScriptableObject)))
				.Select(AssetDatabase.GUIDToAssetPath);
			foreach (var assetFilePath in textAndScriptingAssets)
			{
				var assetFileName = Path.GetFileName(assetFilePath);
				if (expectedAssetNames.Contains(assetFileName))
				{
					return assetFilePath;
				}
			}

			return null;
		}

		public static bool IsValidName(string gameDataName)
		{
			return string.IsNullOrEmpty(gameDataName) == false &&
				gameDataName.All(IsValidNameCharacter) &&
				char.IsDigit(gameDataName[0]) == false &&
				gameDataName[0] != '_';

			static bool IsValidNameCharacter(char value)
			{
				return value >= 'a' && value <= 'z' || value >= 'A' && value <= 'Z' || value == '_' || value >= '0' && value <= '9';
			}
		}
	}
}
