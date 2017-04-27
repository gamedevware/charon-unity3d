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
using System.Linq;
using System.Text;
using GameDevWare.Charon.Json;
using GameDevWare.Charon.Utils;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.Charon
{
	[Serializable]
	public class Settings
	{
		public const string PREF_PREFIX = "Charon_";
		public const string BASE_PATH = "Assets/Editor/GameDevWare.Charon/";
		public const string SETTINGS_PATH = BASE_PATH + "Settings.json";
		public const string DEFAULT_TOOLS_PATH = BASE_PATH + "Charon.exe";
		public const string DEFAULT_LICENSE_SERVER_ADDRESS = "http://gamedevware.com/service/api/";
		public const string EXTENSION_EXPRESSIONS = "Expressions";

		public static readonly Encoding DefaultEncoding = Encoding.UTF8;
		public static readonly Settings Current;
		public static readonly string[] SupportedExtensions;

		public string ToolsPath;
		public string BrowserPath;
		public string LicenseServerAddress;
		public int Browser;
		public int ToolsPort;
		public bool Verbose;

		static Settings()
		{
			Current = Load();

			var expressionsAreLoaded = AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "GameDevWare.Dynamic.Expressions" || a.GetType("GameDevWare.Dynamic.Expressions.AotCompilation", throwOnError: false) != null);
			SupportedExtensions = new[] { expressionsAreLoaded ? EXTENSION_EXPRESSIONS : "None" };
		}

		private static Settings Load()
		{

			var settings = default(Settings);
			try { settings = JsonValue.Parse(File.ReadAllText(SETTINGS_PATH, DefaultEncoding)).As<Settings>(); }
			catch (Exception readError) { Debug.LogWarning("Failed to read settings for Charon: " + readError.Message); }

			if (settings == null)
			{
				settings = new Settings
				{
					ToolsPort = 43210,
					ToolsPath = null,
					LicenseServerAddress = null,
					Verbose = false
				};

				try { File.WriteAllText(SETTINGS_PATH, JsonObject.From(settings).Stringify(), DefaultEncoding); }
				catch { /* ignore */ }
			}
			settings.Validate();

			return settings;
		}


		internal void Save()
		{
			this.Validate();

			try
			{
				var content = JsonObject.From(this).Stringify();
				var currentContent = File.Exists(SETTINGS_PATH) ? File.ReadAllText(SETTINGS_PATH, DefaultEncoding) : null;
				if (string.Equals(content, currentContent, StringComparison.OrdinalIgnoreCase))
					return; // no changes

				File.WriteAllText(SETTINGS_PATH, content, DefaultEncoding);
			}
			catch (Exception e)
			{
				Debug.LogError(string.Format("Failed to save settings for Charon in file '{0}'.", SETTINGS_PATH));
				Debug.LogError(e);
			}
		}

		private void Validate()
		{
			if (string.IsNullOrEmpty(this.ToolsPath) || File.Exists(this.ToolsPath) == false)
			{
				this.ToolsPath = (from id in AssetDatabase.FindAssets("t:DefaultAsset Charon")
								  let path = PathUtils.MakeProjectRelative(AssetDatabase.GUIDToAssetPath(id))
								  where path != null && path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
								  select path).FirstOrDefault() ?? DEFAULT_TOOLS_PATH;
			}

			this.ToolsPath = PathUtils.MakeProjectRelative(this.ToolsPath) ?? this.ToolsPath;

			if (this.ToolsPort < 5000)
				this.ToolsPort = 5000;
			if (this.ToolsPort > 65535)
				this.ToolsPort = 65535;
		}

		internal Uri GetLicenseServerAddress()
		{
			if (string.IsNullOrEmpty(this.LicenseServerAddress) || this.LicenseServerAddress.All(char.IsWhiteSpace))
				return new Uri(DEFAULT_LICENSE_SERVER_ADDRESS);
			return new Uri(this.LicenseServerAddress);
		}
		internal static string GetAppDataPath()
		{
			return Path.Combine(Path.GetFullPath("./Library/Charon/Users"), PathUtils.SanitizeFileName(Environment.UserName ?? "Default"));
		}

		public override string ToString()
		{
			return "Tools Path: " + this.ToolsPath + Environment.NewLine + " " +
				   "Tool Port: " + this.ToolsPort + Environment.NewLine + " ";
		}
	}
}

