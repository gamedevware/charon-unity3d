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
	/// Status returned returned by <see cref="ZipTestResultHandler"/> during testing.
	/// </summary>
	/// <seealso cref="ZipFile.TestArchive(bool)">TestArchive</seealso>
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal class TestStatus
	{
		#region Constructors

		/// <summary>
		/// Initialise a new instance of <see cref="TestStatus"/>
		/// </summary>
		/// <param name="file">The <see cref="ZipFile"/> this status applies to.</param>
		public TestStatus(ZipFile file)
		{
			this.file_ = file;
		}

		#endregion Constructors

		#region Properties

		/// <summary>
		/// Get the current <see cref="TestOperation"/> in progress.
		/// </summary>
		public TestOperation Operation
		{
			get { return this.operation_; }
		}

		/// <summary>
		/// Get the <see cref="ZipFile"/> this status is applicable to.
		/// </summary>
		public ZipFile File
		{
			get { return this.file_; }
		}

		/// <summary>
		/// Get the current/last entry tested.
		/// </summary>
		public ZipEntry Entry
		{
			get { return this.entry_; }
		}

		/// <summary>
		/// Get the number of errors detected so far.
		/// </summary>
		public int ErrorCount
		{
			get { return this.errorCount_; }
		}

		/// <summary>
		/// Get the number of bytes tested so far for the current entry.
		/// </summary>
		public long BytesTested
		{
			get { return this.bytesTested_; }
		}

		/// <summary>
		/// Get a value indicating wether the last entry test was valid.
		/// </summary>
		public bool EntryValid
		{
			get { return this.entryValid_; }
		}

		#endregion Properties

		#region Internal API

		internal void AddError()
		{
			this.errorCount_++;
			this.entryValid_ = false;
		}

		internal void SetOperation(TestOperation operation)
		{
			this.operation_ = operation;
		}

		internal void SetEntry(ZipEntry entry)
		{
			this.entry_ = entry;
			this.entryValid_ = true;
			this.bytesTested_ = 0;
		}

		internal void SetBytesTested(long value)
		{
			this.bytesTested_ = value;
		}

		#endregion Internal API

		#region Instance Fields

		private readonly ZipFile file_;
		private ZipEntry entry_;
		private bool entryValid_;
		private int errorCount_;
		private long bytesTested_;
		private TestOperation operation_;

		#endregion Instance Fields
	}
}