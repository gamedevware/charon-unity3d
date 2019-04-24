using System;
using GameDevWare.Charon.Unity.Json;
using GameDevWare.Charon.Unity.Utils;

namespace GameDevWare.Charon.Unity.Updates.Packages.Nuget
{
	internal sealed class PackageVersionCatalogEntry
	{
		[JsonMember("title")]
		public string Title;
		[JsonMember("Summary")]
		public string Summary;
		[JsonMember("description")]
		public string Description;
		[JsonMember("listed")]
		public bool IsListed;
		[JsonMember("version")]
		public SemanticVersion Version;
		[JsonMember("requireLicenseAcceptance")]
		public bool IsRequireLicenseAcceptance;
		[JsonMember("licenseUrl")]
		public string LicenseUrl;
		[JsonMember("licenseExpression")]
		public string LicenseExpression;
		[JsonMember("published")]
		public DateTime PublishedAt;
	}
}