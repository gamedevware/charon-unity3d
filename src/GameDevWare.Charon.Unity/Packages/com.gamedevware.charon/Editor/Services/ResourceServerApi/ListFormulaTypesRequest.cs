using System.Runtime.Serialization;

namespace Editor.Services.ResourceServerApi
{
	[DataContract]
	internal class ListFormulaTypesRequest
	{
		[DataMember(Name = "unityAssetId")]
		public string GameDataAssetId;
		[DataMember(Name = "skip")]
		public int Skip;
		[DataMember(Name = "take")]
		public int Take;
		[DataMember(Name = "query")]
		public string Query;
	}
}
