namespace GameDevWare.Charon.Unity.Updates.Packages.Zip
{
	/// <summary>
	/// Holds data pertinent to a data descriptor.
	/// </summary>
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