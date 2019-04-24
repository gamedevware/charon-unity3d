namespace GameDevWare.Charon.Unity.Updates.Packages.Zip
{
	/// <summary>
	/// Determines how entries are tested to see if they should use Zip64 extensions or not.
	/// </summary>
	internal enum UseZip64
	{
		/// <summary>
		/// Zip64 will not be forced on entries during processing.
		/// </summary>
		/// <remarks>An entry can have this overridden if required <see cref="ZipEntry.ForceZip64"></see></remarks>
		Off,

		/// <summary>
		/// Zip64 should always be used.
		/// </summary>
		On,

		/// <summary>
		/// #ZipLib will determine use based on entry values when added to archive.
		/// </summary>
		Dynamic,
	}
}