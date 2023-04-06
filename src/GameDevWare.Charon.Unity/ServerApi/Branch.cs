using GameDevWare.Charon.Unity.Json;
using System;

namespace GameDevWare.Charon.Unity.ServerApi
{
	[Serializable]
	internal class Branch
	{
		[JsonMember("id")]
		public string Id;
		[JsonMember("name")]
		public string Name;
		[JsonMember("isPrimary")]
		public bool IsPrimary;
	}
}