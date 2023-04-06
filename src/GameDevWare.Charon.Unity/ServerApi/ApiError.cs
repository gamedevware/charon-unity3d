using GameDevWare.Charon.Unity.Json;
using JetBrains.Annotations;

namespace GameDevWare.Charon.Unity.ServerApi
{
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal class ApiError
	{
		[JsonMember("message")]
		public string Message { get; set; }
		[JsonMember("code")]
		public string Code { get; set; }
	}
}