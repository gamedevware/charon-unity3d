/*
	Copyright � 2000-2018 SharpZipLib Contributors

	Permission is hereby granted, free of charge, to any person obtaining a copy of this
	software and associated documentation files (the "Software"), to deal in the Software
	without restriction, including without limitation the rights to use, copy, modify, merge,
	publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
	to whom the Software is furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in all copies or
	substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
	INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
	PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
	FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
	OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
	DEALINGS IN THE SOFTWARE.
 */

using System;
using JetBrains.Annotations;

namespace GameDevWare.Charon.Unity.Updates.Packages.Zip
{
	/// <summary>
	/// Defines the contents of the general bit flags field for an archive entry.
	/// </summary>
	[Flags]
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal enum GeneralBitFlags
	{
		/// <summary>
		/// Bit 0 if set indicates that the file is encrypted
		/// </summary>
		Encrypted = 0x0001,

		/// <summary>
		/// Bits 1 and 2 - Two bits defining the compression method (only for Method 6 Imploding and 8,9 Deflating)
		/// </summary>
		Method = 0x0006,

		/// <summary>
		/// Bit 3 if set indicates a trailing data desciptor is appended to the entry data
		/// </summary>
		Descriptor = 0x0008,

		/// <summary>
		/// Bit 4 is reserved for use with method 8 for enhanced deflation
		/// </summary>
		ReservedPKware4 = 0x0010,

		/// <summary>
		/// Bit 5 if set indicates the file contains Pkzip compressed patched data.
		/// Requires version 2.7 or greater.
		/// </summary>
		Patched = 0x0020,

		/// <summary>
		/// Bit 6 if set indicates strong encryption has been used for this entry.
		/// </summary>
		StrongEncryption = 0x0040,

		/// <summary>
		/// Bit 7 is currently unused
		/// </summary>
		Unused7 = 0x0080,

		/// <summary>
		/// Bit 8 is currently unused
		/// </summary>
		Unused8 = 0x0100,

		/// <summary>
		/// Bit 9 is currently unused
		/// </summary>
		Unused9 = 0x0200,

		/// <summary>
		/// Bit 10 is currently unused
		/// </summary>
		Unused10 = 0x0400,

		/// <summary>
		/// Bit 11 if set indicates the filename and
		/// comment fields for this file must be encoded using UTF-8.
		/// </summary>
		UnicodeText = 0x0800,

		/// <summary>
		/// Bit 12 is documented as being reserved by PKware for enhanced compression.
		/// </summary>
		EnhancedCompress = 0x1000,

		/// <summary>
		/// Bit 13 if set indicates that values in the local header are masked to hide
		/// their actual values, and the central directory is encrypted.
		/// </summary>
		/// <remarks>
		/// Used when encrypting the central directory contents.
		/// </remarks>
		HeaderMasked = 0x2000,

		/// <summary>
		/// Bit 14 is documented as being reserved for use by PKware
		/// </summary>
		ReservedPkware14 = 0x4000,

		/// <summary>
		/// Bit 15 is documented as being reserved for use by PKware
		/// </summary>
		ReservedPkware15 = 0x8000
	}
}