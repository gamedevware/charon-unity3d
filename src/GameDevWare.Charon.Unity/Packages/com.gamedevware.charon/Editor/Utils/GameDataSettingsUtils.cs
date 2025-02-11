using System;
using System.IO;

namespace GameDevWare.Charon.Editor.Utils
{
	internal static class GameDataSettingsUtils
	{
		public static GameDataSettings CreateDefault(string gameDataPath, string gameDataFileGuid)
		{
			if (gameDataFileGuid == null) throw new ArgumentNullException(nameof(gameDataFileGuid));
			if (gameDataPath == null) throw new ArgumentNullException(nameof(gameDataPath));

			var settings = new GameDataSettings {
				clearOutputDirectory = false,
				splitSourceCodeFiles =  false,
				lineEnding = (int)SourceCodeLineEndings.Windows,
				indentation = (int)SourceCodeIndentation.Tabs,
				gameDataFileGuid = gameDataFileGuid,
				codeGenerationPath = "",
				gameDataClassName = Path.GetFileNameWithoutExtension(gameDataPath).Trim('.', '_'),
				gameDataNamespace = (Path.GetDirectoryName(gameDataPath) ?? "").Replace("\\", ".").Replace("/", "."),
				gameDataDocumentClassName = "Document",
				optimizations = 0 // none,
			};
			return settings;
		}

		public static void Validate(this GameDataSettings gameDataSettings)
		{
			if (string.IsNullOrEmpty(gameDataSettings.gameDataNamespace))
				gameDataSettings.gameDataNamespace = (Path.GetDirectoryName(gameDataSettings.codeGenerationPath) ?? "").Replace("\\", ".").Replace("/", ".");
			if (string.IsNullOrEmpty(gameDataSettings.gameDataNamespace))
				gameDataSettings.gameDataNamespace = "Assets";
			if (string.IsNullOrEmpty(gameDataSettings.gameDataClassName))
				gameDataSettings.gameDataClassName = "GameData";
			if (string.IsNullOrEmpty(gameDataSettings.gameDataDocumentClassName))
				gameDataSettings.gameDataDocumentClassName = "Document";
			if (Enum.IsDefined(typeof(SourceCodeIndentation), (SourceCodeIndentation)gameDataSettings.indentation) == false)
				gameDataSettings.indentation = (int)SourceCodeIndentation.Tabs;
			if (Enum.IsDefined(typeof(SourceCodeLineEndings), (SourceCodeLineEndings)gameDataSettings.lineEnding) == false)
				gameDataSettings.lineEnding = (int)SourceCodeLineEndings.Windows;
		}
	}
}
