using System.Collections.Generic;
using GameDevWare.Charon.Unity.Json;

namespace GameDevWare.Charon.Unity.Updates.Packages.Nuget
{
	internal class ServiceIndex
	{
		[JsonMember("version")]
		public string Version;
		[JsonMember("resources")]
		public List<ServiceDefinition> Services;
	}
}
