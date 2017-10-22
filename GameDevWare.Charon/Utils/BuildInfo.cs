using System;

namespace GameDevWare.Charon.Utils
{
	public class BuildInfo
	{
		public string FileId { get; set; }
		public Version Version { get; set; }
		public long Size { get; set; }
		public string FileName { get; set; }
		public string ReleaseNotes { get; set; }
		public string Hash { get; set; }
		public string HashAlgorithm { get; set; }
	}
}
