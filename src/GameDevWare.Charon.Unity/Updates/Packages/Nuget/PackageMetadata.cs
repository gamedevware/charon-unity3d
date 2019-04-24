using System.Collections.Generic;
using GameDevWare.Charon.Unity.Json;

namespace GameDevWare.Charon.Unity.Updates.Packages.Nuget
{
	internal sealed class PackageMetadata
	{
		[JsonMember("items")]
		public List<PackageCatalogPage> Pages;
	}
}
