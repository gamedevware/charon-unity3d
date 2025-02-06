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

namespace GameDevWare.Charon.Unity.Updates.Packages.Zip.Compression
{
	/// <summary>
	/// This class contains constants used for deflation.
	/// </summary>
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal static class DeflaterConstants
	{
		/// <summary>
		/// The compression method.  This is the only method supported so far.
		/// There is no need to use this constant at all.
		/// </summary>
		public const int DEFLATED = 8;

		/// <summary>
		/// Written to Zip file to identify a stored block
		/// </summary>
		public const int STORED_BLOCK = 0;

		/// <summary>
		/// Identifies static tree in Zip file
		/// </summary>
		public const int STATIC_TREES = 1;

		/// <summary>
		/// Identifies dynamic tree in Zip file
		/// </summary>
		public const int DYN_TREES = 2;

		
		/// <summary>
		/// Reverse the bits of a 16 bit value.
		/// </summary>
		/// <param name="toReverse">Value to reverse bits</param>
		/// <returns>Value with bits reversed</returns>
		public static short BitReverse(int toReverse)
		{
			return (short)(bit4Reverse[toReverse & 0xF] << 12 |
				bit4Reverse[(toReverse >> 4) & 0xF] << 8 |
				bit4Reverse[(toReverse >> 8) & 0xF] << 4 |
				bit4Reverse[toReverse >> 12]);
		}

		private static readonly byte[] bit4Reverse = {
			0,
			8,
			4,
			12,
			2,
			10,
			6,
			14,
			1,
			9,
			5,
			13,
			3,
			11,
			7,
			15
		};
	}
}
