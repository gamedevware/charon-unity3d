using GameDevWare.Charon.Unity.Json;

namespace GameDevWare.Charon.Unity.Updates.Packages.Nuget
{
	internal class PackageVersion	
	{
		[JsonMember("packageContent")]
		public string PackageContentUrl;
		[JsonMember("catalogEntry")]
		public PackageVersionCatalogEntry CatalogEntry;
	}
}