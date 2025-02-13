using System.Runtime.Serialization;

namespace Editor.Services.ResourceServerApi
{
	[DataContract]
	internal class PublishRequest
	{
		[DataMember(Name = "unityAssetId")]
		public string GameDataAssetId;
	}
}
