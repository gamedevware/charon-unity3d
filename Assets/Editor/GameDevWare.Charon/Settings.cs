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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Editor.GameDevWare.Charon.Utils;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor.GameDevWare.Charon
{
	[InitializeOnLoad, Serializable]
	class Settings : ScriptableObject
	{
		public const string PREF_PREFIX = "Charon_";
		public const string SETTINGS_PATH = "Assets/Editor/GameDevWare.Charon/Settings.asset";
		public const string DEFAULT_TOOLS_PATH = "Assets/Editor/GameDevWare.Charon/Charon.exe";
		public const string DEFAULT_LICENSE_SERVER_ADDRESS = "http://gamedevware.com/service/api/";
		public static readonly Settings Current;

		public string ToolsPath;
		public string BrowserPath;
		public string LicenseServerAddress;
		public Browser Browser;
		public int ToolsPort;
		public List<string> GameDataPaths;
		public bool Verbose;
		public bool SuppressRecoveryScripts;
		[HideInInspector]
		public string SelectedLicense;
		[HideInInspector]
		public int Version;

		static Settings()
		{
			Current = Load();
		}

		private static Settings Load()
		{
			var settings = AssetDatabase.LoadAssetAtPath<Settings>(SETTINGS_PATH);

			if (settings == null)
			{
				settings = ScriptableObject.CreateInstance<Settings>();
				settings.ToolsPort = 43210;
				settings.ToolsPath = null;
				settings.GameDataPaths = (from id in AssetDatabase.FindAssets("GameData")
										  let path = FileUtils.MakeProjectRelative(AssetDatabase.GUIDToAssetPath(id))
										  where path != null && path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
										  select path).ToList();
				settings.SelectedLicense = null;
				settings.LicenseServerAddress = null;
				settings.Verbose = false;
			}
			settings.Validate();

			return settings;
		}
		public void Save()
		{
			this.Validate();

			try
			{
				if (AssetDatabase.LoadAssetAtPath<Settings>(SETTINGS_PATH) == null)
					AssetDatabase.CreateAsset(this, SETTINGS_PATH);
				EditorUtility.SetDirty(this);
				AssetDatabase.SaveAssets();
			}
			catch (Exception e)
			{
				Debug.LogError(string.Format("Failed to save settings in file '{0}'.", SETTINGS_PATH));
				Debug.LogError(e);
			}

			if (this.SuppressRecoveryScripts)
				RecoveryScripts.Clear();
			else
				RecoveryScripts.Generate();
		}

		public void Validate()
		{
			if (this.GameDataPaths == null) this.GameDataPaths = new List<string>();
			var newPaths = this.GameDataPaths
				.Select<string, string>(FileUtils.MakeProjectRelative)
				.Where(p => !string.IsNullOrEmpty(p) && File.Exists(p))
				.Distinct()
				.ToList();

			if (!newPaths.SequenceEqual(this.GameDataPaths))
			{
				this.GameDataPaths = newPaths;
				this.Version++;
			}

			if (string.IsNullOrEmpty(this.ToolsPath) || File.Exists(this.ToolsPath) == false)
			{
				this.ToolsPath = (from id in AssetDatabase.FindAssets("t:DefaultAsset Charon")
					let path = FileUtils.MakeProjectRelative(AssetDatabase.GUIDToAssetPath(id))
					where path != null && path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
					select path).FirstOrDefault() ?? DEFAULT_TOOLS_PATH;
			}

			this.ToolsPath = FileUtils.MakeProjectRelative(this.ToolsPath) ?? ToolsPath;

			if (this.ToolsPort < 5000)
			{
				this.ToolsPort = 5000;
				this.Version++;
			}
			if (this.ToolsPort > 65535)
			{
				this.ToolsPort = 65535;
				this.Version++;
			}
		}

		public Uri GetLicenseServerAddress()
		{
			if (string.IsNullOrEmpty(this.LicenseServerAddress) || this.LicenseServerAddress.All(char.IsWhiteSpace))
				return new Uri(DEFAULT_LICENSE_SERVER_ADDRESS);
			return new Uri(this.LicenseServerAddress);
		}

		public override string ToString()
		{
			return "Tools Path: " + this.ToolsPath + Environment.NewLine + " " +
				   "Tool Port: " + this.ToolsPort + Environment.NewLine + " " +
				   "Game Data Paths: " + string.Join(", ", this.GameDataPaths.ToArray()) + Environment.NewLine + " ";
		}

		internal static string GetAppDataPath()
		{
			return Path.Combine(Path.GetFullPath("./Library/Charon/Users"), FileUtils.SanitizeFileName(Environment.UserName ?? "Default"));
		}
	}
}

