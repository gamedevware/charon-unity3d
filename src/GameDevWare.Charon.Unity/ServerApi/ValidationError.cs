using System;
using GameDevWare.Charon.Unity.Json;
using JetBrains.Annotations;

namespace GameDevWare.Charon.Unity.ServerApi
{
	[Serializable, UsedImplicitly(ImplicitUseTargetFlags.WithMembers), PublicAPI]
	public class ValidationError
	{
		[JsonMember("path")]
		public string Path;
		[JsonMember("message")]
		public string Message;
		[JsonMember("code")]
		public string Code;
	}
}