/*
	Copyright © 2000-2018 SharpZipLib Contributors

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

using JetBrains.Annotations;

namespace GameDevWare.Charon.Unity.Updates.Packages.Zip
{
	/// <summary>
	/// The kind of compression used for an entry in an archive
	/// </summary>
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
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