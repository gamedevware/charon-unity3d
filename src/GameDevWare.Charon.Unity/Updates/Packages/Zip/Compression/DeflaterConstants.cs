namespace GameDevWare.Charon.Unity.Updates.Packages.Zip.Compression
{
	/// <summary>
	/// This class contains constants used for deflation.
	/// </summary>
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
