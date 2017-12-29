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
using System.Linq;
using System.Text;
using GameDevWare.Charon.Json;
using GameDevWare.Charon.Utils;
using UnityEngine;

namespace GameDevWare.Charon
{
	[Serializable]
	public class Settings
	{
		public const string PREF_PREFIX = "Charon_";

		public const string DEFAULT_SERVER_ADDRESS = "http://gamedevware.com/service/api/";

		public static readonly string AppDataPath;
		public static readonly string BasePath;
		public static readonly string SettingsPath;
		public static readonly string CharonPath;

		public const string EXTENSION_FORMULAS = "Formulas";

		public static readonly Encoding DefaultEncoding = Encoding.UTF8;
		public static readonly Settings Current;
		public static readonly string[] SupportedExtensions;

		public int Browser;
		public string BrowserPath;
		public string ServerAddress;
		public string EditorVersion;
		public int EditorPort;
		public bool Verbose;

		static Settings()
		{
			if (typeof(Settings).Assembly.GetName().Name == "GameDevWare.Charon")
				BasePath = Path.GetDirectoryName(typeof(Settings).Assembly.Location);

			if (BasePath == null)
				BasePath = Path.GetFullPath("Assets/Editor/GameDevWare.Charon");

			AppDataPath = Path.GetFullPath("./Library/Charon/");
			SettingsPath = Path.Combine(BasePath, "GameDevWare.Charon.Settings.json");
			CharonPath = Path.Combine(AppDataPath, "Charon.exe");

			Current = Load();

			var expressionsAreLoaded = AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "GameDevWare.Dynamic.Expressions" || a.GetType("GameDevWare.Dynamic.Expressions.AotCompilation", throwOnError: false) != null);
			SupportedExtensions = new[] { expressionsAreLoaded ? EXTENSION_FORMULAS : "None" };
		}

		private static Settings Load()
		{

			var settings = default(Settings);
			try { settings = JsonValue.Parse(File.ReadAllText(SettingsPath, DefaultEncoding)).As<Settings>(); }
			catch (Exception readError) { Debug.LogWarning("Failed to read settings for Charon: " + readError.Message); }

			if (settings == null)
			{
				settings = new Settings
				{
					EditorPort = new System.Random().Next(10000, 50000),
					ServerAddress = null,
					Verbose = false
				};

				try { File.WriteAllText(SettingsPath, JsonObject.From(settings).Stringify(), DefaultEncoding); }
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
				var currentContent = File.Exists(SettingsPath) ? File.ReadAllText(SettingsPath, DefaultEncoding) : null;
				if (string.Equals(content, currentContent, StringComparison.OrdinalIgnoreCase))
					return; // no changes

				File.WriteAllText(SettingsPath, content, DefaultEncoding);
			}
			catch (Exception e)
			{
				Debug.LogError(string.Format("Failed to save settings for Charon in file '{0}'.", SettingsPath));
				Debug.LogError(e);
			}
		}

		private void Validate()
		{
			if (this.EditorPort < 5000)
				this.EditorPort = 5000;
			if (this.EditorPort > 65535)
				this.EditorPort = 65535;
		}

		internal Uri GetServerAddress()
		{
			if (string.IsNullOrEmpty(this.ServerAddress) || this.ServerAddress.All(char.IsWhiteSpace))
				return new Uri(DEFAULT_SERVER_ADDRESS);
			return new Uri(this.ServerAddress);
		}
		internal static string GetLocalUserDataPath()
		{
			return Path.Combine(Path.Combine(AppDataPath, "Users"), FileAndPathUtils.SanitizeFileName(Environment.UserName ?? "Default"));
		}
		internal static Version GetCurrentAssetVersion()
		{
			if (typeof(Settings).Assembly.GetName().Name == "GameDevWare.Charon")
				return typeof(Settings).Assembly.GetName().Version;
			else
				return null;
		}

		public override string ToString()
		{
			return string.Format("Browser: {0}, Browser path: {1}, Server Address: {2}, Editor Port: {3}, Editor Version: {4}, Verbose: {5}", (BrowserType)this.Browser, this.BrowserPath, this.ServerAddress, this.EditorPort, this.EditorVersion, this.Verbose);
		}
	}
}

