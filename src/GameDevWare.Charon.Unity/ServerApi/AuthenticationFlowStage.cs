using System;
using GameDevWare.Charon.Unity.Json;

namespace GameDevWare.Charon.Unity.ServerApi
{
	[Serializable]
	public class AuthenticationFlowStage
	{
		[JsonMember("authorizationCode")]
		public string AuthorizationCode;
	}
}