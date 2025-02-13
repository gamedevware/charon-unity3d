using System.Runtime.Serialization;

namespace Editor.Services.ResourceServerApi
{
	[DataContract]
	internal class GenerateSourceCodeRequest
	{
		[DataMember(Name = "unityAssetId")]
		public string GameDataAssetId;
	}
}
