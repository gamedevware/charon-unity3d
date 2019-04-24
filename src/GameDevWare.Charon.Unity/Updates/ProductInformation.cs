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
		}

		public static ProductInformation[] GetKnownProducts()
		{
			return new[] {
				new ProductInformation(PRODUCT_CHARON, Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_CHARON_NAME, disabled: false) {
					CurrentVersion = CharonCli.GetVersionAsync().IgnoreFault(),
					Location = Path.GetFullPath(Settings.CharonExecutablePath),
					ExpectedVersion = String.IsNullOrEmpty(Settings.Current.EditorVersion) ? default(SemanticVersion) : new SemanticVersion(Settings.Current.EditorVersion)
				},
				new ProductInformation(PRODUCT_CHARON_UNITY, Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_CHARON_UNITY_PLUGIN_NAME, disabled: !IsAssemblyLoaded(PRODUCT_CHARON_UNITY_ASSEMBLY)) {
					CurrentVersion = Promise.FromResult(GetAssemblyVersion(PRODUCT_CHARON_UNITY_ASSEMBLY)),
					Location = GetAssemblyLocation(PRODUCT_CHARON_UNITY_ASSEMBLY)
				},
				new ProductInformation(PRODUCT_EXPRESSIONS, Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_EXPRESSIONS_PLUGIN_NAME, disabled: !IsAssemblyLoaded(PackageManager.PRODUCT_EXPRESSIONS_ASSEMBLY)) {
					CurrentVersion = Promise.FromResult(GetAssemblyVersion(PackageManager.PRODUCT_EXPRESSIONS_ASSEMBLY)),
					Location = GetAssemblyLocation(PackageManager.PRODUCT_EXPRESSIONS_ASSEMBLY)
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
		public const string PRODUCT_CHARON_UNITY = "GameDevWare.Charon.Unity";
		public const string PRODUCT_CHARON_UNITY_ASSEMBLY = "GameDevWare.Charon.Unity";
		public const string PRODUCT_EXPRESSIONS = "GameDevWare.Dynamic.Expressions";
		public const string PRODUCT_TEXT_TEMPLATES = "GameDevWare.TextTransform";
		public const string PRODUCT_TEXT_TEMPLATES_ASSEMBLY = "GameDevWare.TextTransform";
	}
}
