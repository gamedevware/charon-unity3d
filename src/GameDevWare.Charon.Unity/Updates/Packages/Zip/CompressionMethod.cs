namespace GameDevWare.Charon.Unity.Updates.Packages.Zip
{
	/// <summary>
	/// The kind of compression used for an entry in an archive
	/// </summary>
	internal enum CompressionMethod
	{
		/// <summary>
		/// A direct copy of the file contents is held in the archive
		/// </summary>
		Stored = 0,

		/// <summary>
		/// Common Zip compression method using a sliding dictionary
		/// of up to 32KB and secondary compression from Huffman/Shannon-Fano trees
		/// </summary>
		Deflated = 8,

		/// <summary>
		/// An extension to deflate with a 64KB window. Not supported by #Zip currently
		/// </summary>
		Deflate64 = 9,

		/// <summary>
		/// BZip2 compression. Not supported by #Zip.
		/// </summary>
		BZip2 = 12,

		/// <summary>
		/// WinZip special for AES encryption, Now supported by #Zip.
		/// </summary>
		WinZipAES = 99,
	}
}