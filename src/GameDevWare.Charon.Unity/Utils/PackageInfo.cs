using System;

namespace GameDevWare.Charon.Unity.Utils
{
	internal class PackageInfo : IComparable, IComparable<PackageInfo>
	{
		public string Id { get; set; }
		public SemanticVersion Version { get; set; }

		/// <inheritdoc />
		public int CompareTo(object obj)
		{
			var other = (obj as PackageInfo);
			return this.CompareTo(other);
		}
		/// <inheritdoc />
		public int CompareTo(PackageInfo other)
		{
			if (other == null)
			{
				return 1;
			}
			return this.Version.CompareTo(other.Version);
		}
		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			var other = (obj as PackageInfo);
			if (other == null)
				return false;

			return this.Version == other.Version;
		}
		/// <inheritdoc />
		public override int GetHashCode()
		{
			return this.Version.GetHashCode();
		}
	}
}
