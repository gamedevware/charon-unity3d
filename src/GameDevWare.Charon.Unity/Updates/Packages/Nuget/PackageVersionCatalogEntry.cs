using System;
using GameDevWare.Charon.Unity.Json;
using GameDevWare.Charon.Unity.Utils;

namespace GameDevWare.Charon.Unity.Updates.Packages.Nuget
{
	internal sealed class PackageVersionCatalogEntry
	{
		[JsonMember("title")]
		public string Title;
		[JsonMember("summary")]
		public string Summary;
		[JsonMember("description")]
		public string Description;
		[JsonMember("listed")]
		public bool IsListed = true; // github doesn't have this parameter, so all packages are listed
		[JsonMember("version")]
		public SemanticVersion Version;
		[JsonMember("requireLicenseAcceptance")]
		public bool IsRequireLicenseAcceptance;
		[JsonMember("licenseUrl")]
		public string LicenseUrl;
		[JsonMember("packageContent")]
		public string PackageContent;
		[JsonMember("licenseExpression")]
		public string LicenseExpression;
		[JsonMember("published")]
		public DateTime PublishedAt;
	}
}