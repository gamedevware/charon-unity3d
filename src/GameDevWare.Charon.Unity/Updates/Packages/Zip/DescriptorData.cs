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
	/// Holds data pertinent to a data descriptor.
	/// </summary>
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal class DescriptorData
	{
		/// <summary>
		/// Get /set the compressed size of data.
		/// </summary>
		public long CompressedSize
		{
			get { return this.compressedSize; }
			set { this.compressedSize = value; }
		}

		/// <summary>
		/// Get / set the uncompressed size of data
		/// </summary>
		public long Size
		{
			get { return this.size; }
			set { this.size = value; }
		}

		/// <summary>
		/// Get /set the crc value.
		/// </summary>
		public long Crc
		{
			get { return this.crc; }
			set { this.crc = (value & 0xffffffff); }
		}

		#region Instance Fields

		private long size;
		private long compressedSize;
		private long crc;

		#endregion Instance Fields
	}
}