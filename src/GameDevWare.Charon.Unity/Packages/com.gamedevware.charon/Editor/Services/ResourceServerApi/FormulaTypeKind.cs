using System.Runtime.Serialization;

namespace Editor.Services.ResourceServerApi
{
	public enum FormulaTypeKind
	{
		[DataMember(Name = "class")]
		Class = 0,
		[DataMember(Name = "enum")]
		Enum = 1,
		[DataMember(Name = "interface")]
		Interface = 2,
		[DataMember(Name = "structure")]
		Structure = 3,
		[DataMember(Name = "delegate")]
		Delegate = 4,
	}
}
