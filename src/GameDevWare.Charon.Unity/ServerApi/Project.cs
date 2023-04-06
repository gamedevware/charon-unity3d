using System;
using GameDevWare.Charon.Unity.Json;

namespace GameDevWare.Charon.Unity.ServerApi
{
	[Serializable]
	internal class Project
	{
		[JsonMember("id")]
		public string Id;
		[JsonMember("name")]
		public string Name;
		[JsonMember("pictureUrl")]
		public string PictureUrl;
		[JsonMember("branches")]
		public Branch[] Branches;
	}
}
