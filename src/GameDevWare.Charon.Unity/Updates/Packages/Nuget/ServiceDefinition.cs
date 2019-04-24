using GameDevWare.Charon.Unity.Json;

namespace GameDevWare.Charon.Unity.Updates.Packages.Nuget
{
	internal sealed class ServiceDefinition
	{
		[JsonMember("@id")]
		public string Id;
		[JsonMember("@type")]
		public string Type;
	}
}