using GameDevWare.Charon.Unity.Json;

namespace GameDevWare.Charon.Unity.ServerApi
{
	public class ApiKeyAuthenticateRequest
	{
		[JsonMember("apiKey")]
		public string ApiKey;
	}
}