using System.Collections.Generic;
using GameDevWare.Charon.Unity.Json;

namespace GameDevWare.Charon.Unity.Updates.Packages.Nuget
{
	internal class PackageCatalogPage
	{
		[JsonMember("items")]
		public List<PackageVersion> Versions;
	}
}