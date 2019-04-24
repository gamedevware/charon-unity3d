namespace GameDevWare.Charon.Unity.Updates.Packages.Zip
{
	internal class EntryPatchData
	{
		public long SizePatchOffset
		{
			get { return this.sizePatchOffset_; }
			set { this.sizePatchOffset_ = value; }
		}

		public long CrcPatchOffset
		{
			get { return this.crcPatchOffset_; }
			set { this.crcPatchOffset_ = value; }
		}

		#region Instance Fields

		private long sizePatchOffset_;
		private long crcPatchOffset_;

		#endregion Instance Fields
	}
}