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
using GameDevWare.Charon.Unity.Async;
using GameDevWare.Charon.Unity.Updates.Packages;
using GameDevWare.Charon.Unity.Utils;

namespace GameDevWare.Charon.Unity.Updates
{
	internal class ProductInformation
	{
		public readonly string Id;
		public readonly string Name;
		public readonly bool Disabled;
		public Promise<SemanticVersion> CurrentVersion;
		public SemanticVersion ExpectedVersion;
		public SemanticVersion MinimalInclusiveVersion;
		public Promise<PackageInfo[]> AllBuilds;
		public string Location;

		public ProductInformation(string id, string name, bool disabled)
		{
			if (id == null) throw new ArgumentNullException("id");
			if (name == null) throw new ArgumentNullException("name");

			this.Id = id;
			this.Name = name;
			this.Disabled = disabled;
			this.CurrentVersion = Promise.FromResult<SemanticVersion>(null);
			this.AllBuilds = disabled ? Promise.FromResult(new PackageInfo[0]) : PackageManager.GetVersions(id);
			this.MinimalInclusiveVersion = new SemanticVersion("0.0.0");
		}

		public static ProductInformation[] GetKnownProducts()
		{
			return new[] {
				new ProductInformation(PRODUCT_CHARON, Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_CHARON_NAME, disabled: false) {
					CurrentVersion = CharonCli.GetVersionAsync().IgnoreFault(),
					Location = Path.GetFullPath(Settings.CharonExePath),
					ExpectedVersion = string.IsNullOrEmpty(Settings.Current.EditorVersion) ? default(SemanticVersion) : new SemanticVersion(Settings.Current.EditorVersion),
					MinimalInclusiveVersion = CharonCli.LegacyToolsVersion
				},
				new ProductInformation(PRODUCT_CHARON_UNITY, Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_CHARON_UNITY_PLUGIN_NAME, disabled: !IsAssemblyLoaded(PRODUCT_CHARON_UNITY_ASSEMBLY)) {
					CurrentVersion = Promise.FromResult(GetAssemblyVersion(PRODUCT_CHARON_UNITY_ASSEMBLY)),
					Location = GetAssemblyLocation(PRODUCT_CHARON_UNITY_ASSEMBLY),
					MinimalInclusiveVersion = CharonCli.LegacyPluginVersion
				},
				new ProductInformation(PRODUCT_EXPRESSIONS, Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_EXPRESSIONS_PLUGIN_NAME, disabled: !IsAssemblyLoaded(PRODUCT_EXPRESSIONS_ASSEMBLY)) {
					CurrentVersion = Promise.FromResult(GetAssemblyVersion(PRODUCT_EXPRESSIONS_ASSEMBLY)),
					Location = GetAssemblyLocation(PRODUCT_EXPRESSIONS_ASSEMBLY)
				},
				new ProductInformation(PRODUCT_TEXT_TEMPLATES, Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_TEXT_TRANSFORM_PLUGIN_NAME, disabled: !IsAssemblyLoaded(PRODUCT_TEXT_TEMPLATES_ASSEMBLY)) {
					CurrentVersion = Promise.FromResult(GetAssemblyVersion(PRODUCT_TEXT_TEMPLATES_ASSEMBLY)),
					Location = GetAssemblyLocation(PRODUCT_TEXT_TEMPLATES_ASSEMBLY)
				}
			};
		}

		private static SemanticVersion GetAssemblyVersion(string assemblyName)
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (assembly.GetName(copiedName: false).Name == assemblyName)
				{
					return new SemanticVersion(assembly.GetInformationalVersion());
				}
			}

			return null;
		}
		private static bool IsAssemblyLoaded(string assemblyName)
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (assembly.GetName(copiedName: false).Name == assemblyName)
					return true;
			}

			return false;
		}
		private static string GetAssemblyLocation(string assemblyName)
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (assembly.GetName(copiedName: false).Name == assemblyName)
					return assembly.Location;
			}

			return null;
		}
		public const string PRODUCT_CHARON = "GameDevWare.Charon";
		public const string PRODUCT_CHARON_PACKAGE = "GameDevWare.Charon";
		public const string PRODUCT_CHARON_UNITY = "GameDevWare.Charon.Unity";
		public const string PRODUCT_CHARON_UNITY_ASSEMBLY = "GameDevWare.Charon.Unity";
		public const string PRODUCT_CHARON_UNITY_PACKAGE = "GameDevWare.Charon.Unity";
		public const string PRODUCT_EXPRESSIONS = "GameDevWare.Dynamic.Expressions";
		public const string PRODUCT_EXPRESSIONS_PACKAGE = "GameDevWare.Dynamic.Expressions";
		public const string PRODUCT_EXPRESSIONS_ASSEMBLY = "GameDevWare.Dynamic.Expressions";
		public const string PRODUCT_TEXT_TEMPLATES = "GameDevWare.TextTransform";
		public const string PRODUCT_TEXT_TEMPLATES_PACKAGE = "GameDevWare.TextTransform";
		public const string PRODUCT_TEXT_TEMPLATES_ASSEMBLY = "GameDevWare.TextTransform";
	}
}
