/*
	Copyright (c) 2025 Denis Zykov

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

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
				gameDataClassName = Path.GetFileNameWithoutExtension(Path.GetFileName(gameDataPath)).Trim('.', '_'),
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
