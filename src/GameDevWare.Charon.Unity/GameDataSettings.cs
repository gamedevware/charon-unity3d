/*
	Copyright (c) 2017 Denis Zykov

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
using GameDevWare.Charon.Unity.Json;
using GameDevWare.Charon.Unity.Utils;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.Charon.Unity
{
	internal sealed class GameDataSettings
	{
		public enum CodeGenerator
		{
			None,
			CSharp,
			CSharpCodeAndAsset
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
		public string DocumentClassName;
		public string[] ScriptingAssemblies;
		public int Options;
		public int LineEnding;
		public int Indentation;

		public static GameDataSettings CreateDefault(string gameDataPath)
		{
			if (gameDataPath == null) throw new ArgumentNullException("gameDataPath");

			var settings = new GameDataSettings();
			settings.Generator = (int)CodeGenerator.CSharp;
			settings.AutoGeneration = true;
			settings.LineEnding = (int)LineEndings.Windows;
			settings.Indentation = (int)Indentations.Tab;
			settings.AssetGenerationPath = Path.ChangeExtension(gameDataPath, ".asset");
			settings.CodeGenerationPath = Path.ChangeExtension(gameDataPath, ".cs");
			settings.GameDataClassName = Path.GetFileNameWithoutExtension(gameDataPath);
			settings.Namespace = (Path.GetDirectoryName(gameDataPath) ?? "").Replace("\\", ".").Replace("/", ".");
			settings.DocumentClassName = "Document";
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
					gameDataSettings = JsonValue.Parse(gameDataSettingsJson).As<GameDataSettings>();

				if (gameDataSettings != null)
				{
					gameDataSettings.CodeGenerationPath = FileAndPathUtils.MakeProjectRelative(gameDataSettings.CodeGenerationPath);
					gameDataSettings.AssetGenerationPath = FileAndPathUtils.MakeProjectRelative(gameDataSettings.AssetGenerationPath);
					if (string.IsNullOrEmpty(gameDataSettings.CodeGenerationPath))
						gameDataSettings.CodeGenerationPath = Path.ChangeExtension(gameDataPath, "cs");
					gameDataSettings.Validate();
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
				this.Validate();

				var gameDataObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(gameDataPath);
				var importer = AssetImporter.GetAtPath(gameDataPath);
				importer.userData = JsonObject.From(this).Stringify();
				EditorUtility.SetDirty(gameDataObj);
				importer.SaveAndReimport();
			}
			catch (Exception e) { Debug.LogError("Failed to save gamedata's settings: " + e); }
		}

		public void Validate()
		{
			if (string.IsNullOrEmpty(this.Namespace))
				this.Namespace = (Path.GetDirectoryName(this.CodeGenerationPath) ?? "").Replace("\\", ".").Replace("/", ".");
			if (string.IsNullOrEmpty(this.Namespace))
				this.Namespace = "Assets";
			if (string.IsNullOrEmpty(this.GameDataClassName))
				this.GameDataClassName = "GameData";
			if (string.IsNullOrEmpty(this.DocumentClassName))
				this.DocumentClassName = "Document";
			if (Enum.IsDefined(typeof(Indentations), (Indentations)this.Indentation) == false)
				this.Indentation = (int)Indentations.Tab;
			if (Enum.IsDefined(typeof(CodeGenerator), (CodeGenerator)this.Generator) == false)
				this.Generator = (int)CodeGenerator.None;
			if (Enum.IsDefined(typeof(LineEndings), (LineEndings)this.LineEnding) == false)
				this.LineEnding = (int)LineEndings.Windows;
		}
	}
}
