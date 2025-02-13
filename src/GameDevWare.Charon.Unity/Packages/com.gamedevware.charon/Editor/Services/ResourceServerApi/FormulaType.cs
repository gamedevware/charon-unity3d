using System.Runtime.Serialization;

namespace Editor.Services.ResourceServerApi
{
	public sealed class FormulaType
	{
		[DataMember(Name = "sourceCodeLanguage")]
		public string SourceCodeLanguage;

		[DataMember(Name = "kind")]
		public FormulaTypeKind Kind;

		[DataMember(Name = "name")]
		public string Name;

		[DataMember(Name = "packageOrNamespaceName")]
		public string PackageOrNamespaceName;

		[DataMember(Name = "fullName")]
		public string FullName;

		[DataMember(Name = "moduleName")]
		public string ModuleName;
	};
}
