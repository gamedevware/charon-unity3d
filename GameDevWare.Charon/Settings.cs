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
using System.Threading;
using GameDevWare.Charon.Json;
using GameDevWare.Charon.Utils;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.Charon
{
	[Serializable]
	internal class Settings
	{
		public const string PREF_PREFIX = "Charon_";
		public const string SETTINGS_PATH = "Assets/Editor/GameDevWare.Charon/Settings.json";
		public const string DEFAULT_TOOLS_PATH = "Assets/Editor/GameDevWare.Charon/Charon.exe";
		public const string DEFAULT_LICENSE_SERVER_ADDRESS = "http://gamedevware.com/service/api/";

		public static readonly Encoding DefaultEncoding = Encoding.UTF8;
		public static readonly Settings Current;

		public string ToolsPath;
		public string BrowserPath;
		public string LicenseServerAddress;
		public int Browser;
		public int ToolsPort;
		public string[] GameDataPaths;
		public bool Verbose;
		public string SelectedLicense;

		static Settings()
		{
			Current = Load();
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
					GameDataPaths = (from id in AssetDatabase.FindAssets("GameData")
									 let path = FileUtils.MakeProjectRelative(AssetDatabase.GUIDToAssetPath(id))
									 where path != null && path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
									 select path).ToArray(),
					SelectedLicense = null,
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
			if (this.GameDataPaths == null) this.GameDataPaths = new string[0];
			var newPaths = this.GameDataPaths
				.Select<string, string>(FileUtils.MakeProjectRelative)
				.Where(p => !string.IsNullOrEmpty(p) && File.Exists(p))
				.Distinct()
				.ToArray();

			if (!newPaths.SequenceEqual(this.GameDataPaths))
			{
				this.GameDataPaths = newPaths;
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
			return Path.Combine(Path.GetFullPath("./Library/Charon/Users"), FileUtils.SanitizeFileName(Environment.UserName ?? "Default"));
		}

		internal bool RemoveGameDataPath(string pathToRemove)
		{
			if (pathToRemove == null) throw new ArgumentNullException("pathToRemove");

			var oldGameDataPaths = default(string[]);
			var newGameDataPaths = default(string[]);
			do
			{
				oldGameDataPaths = this.GameDataPaths;
				newGameDataPaths = oldGameDataPaths.Where(p => p != pathToRemove).ToArray();
			} while (Interlocked.CompareExchange(ref this.GameDataPaths, newGameDataPaths, oldGameDataPaths) != oldGameDataPaths);

			return oldGameDataPaths.Length != newGameDataPaths.Length;
		}
		internal bool AddGameDataPath(string pathToAdd)
		{
			if (pathToAdd == null) throw new ArgumentNullException("pathToAdd");

			var oldGameDataPaths = default(string[]);
			var newGameDataPaths = default(string[]);
			do
			{
				oldGameDataPaths = this.GameDataPaths;
				newGameDataPaths = oldGameDataPaths.Union(new[] { pathToAdd }).ToArray();
			} while (Interlocked.CompareExchange(ref this.GameDataPaths, newGameDataPaths, oldGameDataPaths) != oldGameDataPaths);

			return oldGameDataPaths.Length != newGameDataPaths.Length;
		}

		public override string ToString()
		{
			return "Tools Path: " + this.ToolsPath + Environment.NewLine + " " +
				   "Tool Port: " + this.ToolsPort + Environment.NewLine + " " +
				   "Game Data Paths: " + string.Join(", ", this.GameDataPaths.ToArray()) + Environment.NewLine + " ";
		}
	}
}

