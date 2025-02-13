using System.Runtime.Serialization;

namespace Editor.Services.ResourceServerApi
{
	[DataContract]
	public class ListFormulaTypesResponse
	{
		[DataMember(Name = "types")]
		public FormulaType[] Types;

		[DataMember(Name = "total")]
		public int Total;
	}
}
