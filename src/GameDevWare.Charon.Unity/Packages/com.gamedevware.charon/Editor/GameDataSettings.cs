/*
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

		// ReSharper disable once EmptyConstructor
		public GameDataSettings()
		{
		}

		public int Generator;
		public int LanguageVersion;
		public bool AutoGeneration;
		public string AssetGenerationPath;
		public string CodeGenerationPath;
		public string GameDataClassName;
		public string Namespace;
		public string DocumentClassName;
		public string[] ScriptingAssemblies;
		public int Optimizations;
		public int LineEnding;
		public int Indentation;
		public bool SplitSourceCodeFiles;
		public string ServerAddress;
		public string ProjectId;
		public string ProjectName;
		public string BranchName;
		public string BranchId;
		public bool AutoSynchronization;

		public bool IsConnected
		{
			get
			{
				return string.IsNullOrEmpty(this.ServerAddress) == false &&
					string.IsNullOrEmpty(this.ProjectId) == false &&
					string.IsNullOrEmpty(this.BranchId) == false;
			}
		}

		public static GameDataSettings CreateDefault(string gameDataPath)
		{
			if (gameDataPath == null) throw new ArgumentNullException("gameDataPath");

			var settings = new GameDataSettings();
			settings.Generator = (int)CodeGenerator.CSharp;
			settings.LanguageVersion = (int)CSharpLanguageVersion.CSharp73;
			settings.AutoGeneration = true;
			settings.LineEnding = (int)SourceCodeLineEndings.Windows;
			settings.Indentation = (int)Unity.SourceCodeIndentation.Tabs;
			settings.AssetGenerationPath = Path.ChangeExtension(gameDataPath, "asset");
			settings.CodeGenerationPath = Path.GetDirectoryName(gameDataPath) ?? ".";
			settings.GameDataClassName = Path.GetFileNameWithoutExtension(gameDataPath).Trim('.', '_');
			settings.Namespace = (Path.GetDirectoryName(gameDataPath) ?? "").Replace("\\", ".").Replace("/", ".");
			settings.DocumentClassName = "Document";
			settings.Optimizations = 0;
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
					gameDataSettings.CodeGenerationPath = FileHelper.MakeProjectRelative(gameDataSettings.CodeGenerationPath);
					gameDataSettings.AssetGenerationPath = FileHelper.MakeProjectRelative(gameDataSettings.AssetGenerationPath);
					if (string.IsNullOrEmpty(gameDataSettings.CodeGenerationPath) ||
						gameDataSettings.CodeGenerationPath.EndsWith(".cs"))
					{
						gameDataSettings.CodeGenerationPath = Path.GetDirectoryName(gameDataSettings.CodeGenerationPath ?? gameDataPath) ?? ".";
					}

					gameDataSettings.Validate();
				}

			}
			catch (Exception loadError)
			{
				Debug.LogError(string.Format("Failed to read game's data settings from '{0}': {1}", gameDataPath, loadError.Unwrap().Message));
				Debug.LogError(loadError.Unwrap());
			}

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
				importer.userData = JsonObject.From(this).Stringify(pretty: false);
				EditorUtility.SetDirty(gameDataObj);
				importer.SaveAndReimport();
			}
			catch (Exception saveError)
			{
				Debug.LogError(string.Format("Failed to write game's data settings to '{0}': {1}", gameDataPath, saveError.Unwrap().Message));
				Debug.LogError(saveError.Unwrap());
			}
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
			if (Enum.IsDefined(typeof(SourceCodeIndentation), (SourceCodeIndentation)this.Indentation) == false)
				this.Indentation = (int)Unity.SourceCodeIndentation.Tabs;
			if (Enum.IsDefined(typeof(CodeGenerator), (CodeGenerator)this.Generator) == false)
				this.Generator = (int)CodeGenerator.None;
			if (Enum.IsDefined(typeof(SourceCodeLineEndings), (SourceCodeLineEndings)this.LineEnding) == false)
				this.LineEnding = (int)SourceCodeLineEndings.Windows;
		}

		public Uri MakeDataSourceUrl()
		{
			if (!this.IsConnected)
			{
				throw new InvalidOperationException("Data source URL could be created only for connected game data.");
			}

			return new Uri(new Uri(this.ServerAddress), string.Format("view/data/{0}/{1}/", this.ProjectId, this.BranchId));
		}
	}
}
