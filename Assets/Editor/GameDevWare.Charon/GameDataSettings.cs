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
using Assets.Editor.GameDevWare.Charon.Json;
using Assets.Editor.GameDevWare.Charon.Utils;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor.GameDevWare.Charon
{
	sealed class GameDataSettings
	{
		public enum CodeGenerator
		{
			None,
			CSharp,
			CSharpCodeAndAsset
		}

		[Flags]
		public enum CodeGenerationOptions
		{
			LazyReferences = 0x1 << 0,
			HideReferences = 0x1 << 1,
			HideLocalizedStrings = 0x1 << 2,
			SuppressEntryClass = 0x1 << 4,
			SuppressGameDataClass = 0x1 << 5,
			SuppressJsonSerialization = 0x1 << 6,
			SuppressLocalizedStringClass = 0x1 << 7,
			SuppressReferenceClass = 0x1 << 8,
			SuppressDataContractAttributes = 0x1 << 9,
		}

		public enum LineEndings
		{
			Windows = 0,
			Unix
		}

		public enum Indentations
		{
			Tab = 0,
			TwoSpaces,
			FourSpaces
		}

		// ReSharper disable once EmptyConstructor
		public GameDataSettings()
		{
		}

		public int Generator;
		public bool AutoGeneration;
		public string AssetGenerationPath;
		public string CodeGenerationPath;
		public string GameDataClassName;
		public string Namespace;
		public string EntryClassName;
		public int Options;
		public int LineEnding;
		public int Indentation;

		public static GameDataSettings CreateDefault(string gameDataPath)
		{
			if (gameDataPath == null) throw new ArgumentNullException("gameDataPath");

			var settings = new GameDataSettings();
			settings.Generator = (int)CodeGenerator.CSharp;
			settings.AutoGeneration = true;
			settings.LineEnding = (int) LineEndings.Windows;
			settings.Indentation = (int) Indentations.Tab;
			settings.AssetGenerationPath = Path.ChangeExtension(gameDataPath, "asset");
			settings.CodeGenerationPath = Path.ChangeExtension(gameDataPath, "cs");
			settings.GameDataClassName = Path.GetFileNameWithoutExtension(gameDataPath);
			settings.Namespace = Path.GetDirectoryName(gameDataPath).Replace("\\", ".").Replace("/", ".");
			settings.EntryClassName = "Entry";
			settings.Options = (int)(CodeGenerationOptions.HideLocalizedStrings | CodeGenerationOptions.HideReferences | CodeGenerationOptions.SuppressDataContractAttributes);
			return settings;
		}

		public static GameDataSettings Load(UnityEngine.Object gameDataObj)
		{
			if (gameDataObj == null) throw new NullReferenceException("gameDataObj");

			var gameDataPath = AssetDatabase.GetAssetPath(gameDataObj);
			return Load(gameDataPath);
		}
		public static GameDataSettings Load(string gameDataPath)
		{
			if (gameDataPath == null) throw new NullReferenceException("gameDataPath");

			var gameDataSettings = default(GameDataSettings);
			try
			{
				var gameDataSettingsJson = AssetImporter.GetAtPath(gameDataPath).userData;
				if (string.IsNullOrEmpty(gameDataSettingsJson) == false)
					gameDataSettings = JsonObject.Parse(gameDataSettingsJson).As<GameDataSettings>();

				if (gameDataSettings != null)
				{
					gameDataSettings.CodeGenerationPath = FileUtils.MakeProjectRelative(gameDataSettings.CodeGenerationPath);
					gameDataSettings.AssetGenerationPath = FileUtils.MakeProjectRelative(gameDataSettings.AssetGenerationPath);
					if (string.IsNullOrEmpty(gameDataSettings.CodeGenerationPath))
						gameDataSettings.CodeGenerationPath = Path.ChangeExtension(gameDataPath, "cs");
				}
			}
			catch (Exception e) { Debug.LogError("Failed to deserialize gamedata's settings: " + e); }

			if (gameDataSettings == null)
				gameDataSettings = CreateDefault(gameDataPath);

			return gameDataSettings;
		}

		public void Save(string gameDataPath)
		{
			if (gameDataPath == null) throw new ArgumentNullException("gameDataPath");

			try
			{
				var gameDataObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(gameDataPath);
				var importer = AssetImporter.GetAtPath(gameDataPath);
				importer.userData = JsonObject.From(this).Stringify();
				EditorUtility.SetDirty(gameDataObj);
				importer.SaveAndReimport();
			}
			catch (Exception e) { Debug.LogError("Failed to save gamedata's settings: " + e); }
		}
	}
}
