using System.Xml.Serialization;

namespace GameDevWare.Charon.Unity.Updates.Packages.Nuget
{
	[XmlRoot("metadata", IsNullable = false)]
	public sealed class PackageSpecification
	{
		[XmlElement("id")]
		public string Id;

		[XmlElement("version")]
		public string Version;

		[XmlElement("authors")]
		public string Authors;

		[XmlElement("owners")]
		public string Owners;

		[XmlElement("description")]
		public string Description;

		[XmlElement("copyright")]
		public string Copyright;

		[XmlElement("releaseNotes")]
		public string ReleaseNotes;
	}
}
