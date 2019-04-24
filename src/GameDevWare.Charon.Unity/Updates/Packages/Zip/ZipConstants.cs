using System;

namespace GameDevWare.Charon.Unity.Updates.Packages.Zip
{
	#region Enumerations

	#endregion Enumerations

	/// <summary>
	/// This class contains constants used for Zip format files
	/// </summary>
	internal static class ZipConstants
	{
		#region Versions

		/// <summary>
		/// The version made by field for entries in the central header when created by this library
		/// </summary>
		/// <remarks>
		/// This is also the Zip version for the library when comparing against the version required to extract
		/// for an entry.  See <see cref="ZipEntry.CanDecompress"/>.
		/// </remarks>
		public const int VersionMadeBy = 51; // was 45 before AES

		/// <summary>
		/// The version made by field for entries in the central header when created by this library
		/// </summary>
		/// <remarks>
		/// This is also the Zip version for the library when comparing against the version required to extract
		/// for an entry.  See <see cref="ZipInputStream.CanDecompressEntry">ZipInputStream.CanDecompressEntry</see>.
		/// </remarks>
		[Obsolete("Use VersionMadeBy instead")]
		public const int VERSION_MADE_BY = 51;

		/// <summary>
		/// The minimum version required to support strong encryption
		/// </summary>
		public const int VersionStrongEncryption = 50;

		/// <summary>
		/// The minimum version required to support strong encryption
		/// </summary>
		[Obsolete("Use VersionStrongEncryption instead")]
		public const int VERSION_STRONG_ENCRYPTION = 50;

		/// <summary>
		/// Version indicating AES encryption
		/// </summary>
		public const int VERSION_AES = 51;

		/// <summary>
		/// The version required for Zip64 extensions (4.5 or higher)
		/// </summary>
		public const int VersionZip64 = 45;

		#endregion Versions

		#region Header Sizes

		/// <summary>
		/// Size of local entry header (excluding variable length fields at end)
		/// </summary>
		public const int LocalHeaderBaseSize = 30;

		/// <summary>
		/// Size of local entry header (excluding variable length fields at end)
		/// </summary>
		[Obsolete("Use LocalHeaderBaseSize instead")]
		public const int LOCHDR = 30;

		/// <summary>
		/// Size of Zip64 data descriptor
		/// </summary>
		public const int Zip64DataDescriptorSize = 24;

		/// <summary>
		/// Size of data descriptor
		/// </summary>
		public const int DataDescriptorSize = 16;

		/// <summary>
		/// Size of data descriptor
		/// </summary>
		[Obsolete("Use DataDescriptorSize instead")]
		public const int EXTHDR = 16;

		/// <summary>
		/// Size of central header entry (excluding variable fields)
		/// </summary>
		public const int CentralHeaderBaseSize = 46;

		/// <summary>
		/// Size of central header entry
		/// </summary>
		[Obsolete("Use CentralHeaderBaseSize instead")]
		public const int CENHDR = 46;

		/// <summary>
		/// Size of end of central record (excluding variable fields)
		/// </summary>
		public const int EndOfCentralRecordBaseSize = 22;

		/// <summary>
		/// Size of end of central record (excluding variable fields)
		/// </summary>
		[Obsolete("Use EndOfCentralRecordBaseSize instead")]
		public const int ENDHDR = 22;

		/// <summary>
		/// Size of 'classic' cryptographic header stored before any entry data
		/// </summary>
		public const int CryptoHeaderSize = 12;

		/// <summary>
		/// Size of cryptographic header stored before entry data
		/// </summary>
		[Obsolete("Use CryptoHeaderSize instead")]
		public const int CRYPTO_HEADER_SIZE = 12;

		#endregion Header Sizes

		#region Header Signatures

		/// <summary>
		/// Signature for local entry header
		/// </summary>
		public const int LocalHeaderSignature = 'P' | ('K' << 8) | (3 << 16) | (4 << 24);

		/// <summary>
		/// Signature for local entry header
		/// </summary>
		[Obsolete("Use LocalHeaderSignature instead")]
		public const int LOCSIG = 'P' | ('K' << 8) | (3 << 16) | (4 << 24);

		/// <summary>
		/// Signature for spanning entry
		/// </summary>
		public const int SpanningSignature = 'P' | ('K' << 8) | (7 << 16) | (8 << 24);

		/// <summary>
		/// Signature for spanning entry
		/// </summary>
		[Obsolete("Use SpanningSignature instead")]
		public const int SPANNINGSIG = 'P' | ('K' << 8) | (7 << 16) | (8 << 24);

		/// <summary>
		/// Signature for temporary spanning entry
		/// </summary>
		public const int SpanningTempSignature = 'P' | ('K' << 8) | ('0' << 16) | ('0' << 24);

		/// <summary>
		/// Signature for temporary spanning entry
		/// </summary>
		[Obsolete("Use SpanningTempSignature instead")]
		public const int SPANTEMPSIG = 'P' | ('K' << 8) | ('0' << 16) | ('0' << 24);

		/// <summary>
		/// Signature for data descriptor
		/// </summary>
		/// <remarks>
		/// This is only used where the length, Crc, or compressed size isnt known when the
		/// entry is created and the output stream doesnt support seeking.
		/// The local entry cannot be 'patched' with the correct values in this case
		/// so the values are recorded after the data prefixed by this header, as well as in the central directory.
		/// </remarks>
		public const int DataDescriptorSignature = 'P' | ('K' << 8) | (7 << 16) | (8 << 24);

		/// <summary>
		/// Signature for data descriptor
		/// </summary>
		/// <remarks>
		/// This is only used where the length, Crc, or compressed size isnt known when the
		/// entry is created and the output stream doesnt support seeking.
		/// The local entry cannot be 'patched' with the correct values in this case
		/// so the values are recorded after the data prefixed by this header, as well as in the central directory.
		/// </remarks>
		[Obsolete("Use DataDescriptorSignature instead")]
		public const int EXTSIG = 'P' | ('K' << 8) | (7 << 16) | (8 << 24);

		/// <summary>
		/// Signature for central header
		/// </summary>
		[Obsolete("Use CentralHeaderSignature instead")]
		public const int CENSIG = 'P' | ('K' << 8) | (1 << 16) | (2 << 24);

		/// <summary>
		/// Signature for central header
		/// </summary>
		public const int CentralHeaderSignature = 'P' | ('K' << 8) | (1 << 16) | (2 << 24);

		/// <summary>
		/// Signature for Zip64 central file header
		/// </summary>
		public const int Zip64CentralFileHeaderSignature = 'P' | ('K' << 8) | (6 << 16) | (6 << 24);

		/// <summary>
		/// Signature for Zip64 central file header
		/// </summary>
		[Obsolete("Use Zip64CentralFileHeaderSignature instead")]
		public const int CENSIG64 = 'P' | ('K' << 8) | (6 << 16) | (6 << 24);

		/// <summary>
		/// Signature for Zip64 central directory locator
		/// </summary>
		public const int Zip64CentralDirLocatorSignature = 'P' | ('K' << 8) | (6 << 16) | (7 << 24);

		/// <summary>
		/// Signature for archive extra data signature (were headers are encrypted).
		/// </summary>
		public const int ArchiveExtraDataSignature = 'P' | ('K' << 8) | (6 << 16) | (7 << 24);

		/// <summary>
		/// Central header digitial signature
		/// </summary>
		public const int CentralHeaderDigitalSignature = 'P' | ('K' << 8) | (5 << 16) | (5 << 24);

		/// <summary>
		/// Central header digitial signature
		/// </summary>
		[Obsolete("Use CentralHeaderDigitalSignaure instead")]
		public const int CENDIGITALSIG = 'P' | ('K' << 8) | (5 << 16) | (5 << 24);

		/// <summary>
		/// End of central directory record signature
		/// </summary>
		public const int EndOfCentralDirectorySignature = 'P' | ('K' << 8) | (5 << 16) | (6 << 24);

		/// <summary>
		/// End of central directory record signature
		/// </summary>
		[Obsolete("Use EndOfCentralDirectorySignature instead")]
		public const int ENDSIG = 'P' | ('K' << 8) | (5 << 16) | (6 << 24);

		#endregion Header Signatures

		/// <summary>
		/// Default encoding used for string conversion.  0 gives the default system OEM code page.
		/// Using the default code page isnt the full solution neccessarily
		/// there are many variable factors, codepage 850 is often a good choice for
		/// European users, however be careful about compatability.
		/// </summary>
		[Obsolete("Use ZipStrings instead")]
		public static int DefaultCodePage
		{
			get { return ZipStrings.CodePage; }
			set { ZipStrings.CodePage = value; }
		}

		/// <summary> Depracated wrapper for <see cref="ZipStrings.ConvertToString(byte[], int)"/></summary>
		[Obsolete("Use ZipStrings.ConvertToString instead")]
		public static string ConvertToString(byte[] data, int count)
		{
			return ZipStrings.ConvertToString(data, count);
		}

		/// <summary> Depracated wrapper for <see cref="ZipStrings.ConvertToString(byte[])"/></summary>
		[Obsolete("Use ZipStrings.ConvertToString instead")]
		public static string ConvertToString(byte[] data)
		{
			return ZipStrings.ConvertToString(data);
		}

		/// <summary> Depracated wrapper for <see cref="ZipStrings.ConvertToStringExt(int, byte[], int)"/></summary>
		[Obsolete("Use ZipStrings.ConvertToStringExt instead")]
		public static string ConvertToStringExt(int flags, byte[] data, int count)
		{
			return ZipStrings.ConvertToStringExt(flags, data, count);
		}

		/// <summary> Depracated wrapper for <see cref="ZipStrings.ConvertToStringExt(int, byte[])"/></summary>
		[Obsolete("Use ZipStrings.ConvertToStringExt instead")]
		public static string ConvertToStringExt(int flags, byte[] data)
		{
			return ZipStrings.ConvertToStringExt(flags, data);
		}

		/// <summary> Depracated wrapper for <see cref="ZipStrings.ConvertToArray(string)"/></summary>
		[Obsolete("Use ZipStrings.ConvertToArray instead")]
		public static byte[] ConvertToArray(string str)
		{
			return ZipStrings.ConvertToArray(str);
		}

		/// <summary> Depracated wrapper for <see cref="ZipStrings.ConvertToArray(int, string)"/></summary>
		[Obsolete("Use ZipStrings.ConvertToArray instead")]
		public static byte[] ConvertToArray(int flags, string str)
		{
			return ZipStrings.ConvertToArray(flags, str);
		}
	}
}
