using System;
using System.IO;

namespace GameDevWare.Charon.Unity.Updates.Packages.Zip
{
	// TODO: Sort out wether tagged data is useful and what a good implementation might look like.
	// Its just a sketch of an idea at the moment.

	/// <summary>
	/// ExtraData tagged value interface.
	/// </summary>
	internal interface ITaggedData
	{
		/// <summary>
		/// Get the ID for this tagged data value.
		/// </summary>
		short TagID { get; }

		/// <summary>
		/// Set the contents of this instance from the data passed.
		/// </summary>
		/// <param name="data">The data to extract contents from.</param>
		/// <param name="offset">The offset to begin extracting data from.</param>
		/// <param name="count">The number of bytes to extract.</param>
		void SetData(byte[] data, int offset, int count);

		/// <summary>
		/// Get the data representing this instance.
		/// </summary>
		/// <returns>Returns the data for this instance.</returns>
		byte[] GetData();
	}

	/// <summary>
	/// A raw binary tagged value
	/// </summary>
	internal class RawTaggedData : ITaggedData
	{
		/// <summary>
		/// Initialise a new instance.
		/// </summary>
		/// <param name="tag">The tag ID.</param>
		public RawTaggedData(short tag)
		{
			this._tag = tag;
		}

		#region ITaggedData Members

		/// <summary>
		/// Get the ID for this tagged data value.
		/// </summary>
		public short TagID
		{
			get { return this._tag; }
			set { this._tag = value; }
		}

		/// <summary>
		/// Set the data from the raw values provided.
		/// </summary>
		/// <param name="data">The raw data to extract values from.</param>
		/// <param name="offset">The index to start extracting values from.</param>
		/// <param name="count">The number of bytes available.</param>
		public void SetData(byte[] data, int offset, int count)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}

			this._data = new byte[count];
			Array.Copy(data, offset, this._data, 0, count);
		}

		/// <summary>
		/// Get the binary data representing this instance.
		/// </summary>
		/// <returns>The raw binary data representing this instance.</returns>
		public byte[] GetData()
		{
			return this._data;
		}

		#endregion ITaggedData Members

		/// <summary>
		/// Get /set the binary data representing this instance.
		/// </summary>
		/// <returns>The raw binary data representing this instance.</returns>
		public byte[] Data
		{
			get { return this._data; }
			set { this._data = value; }
		}

		#region Instance Fields

		/// <summary>
		/// The tag ID for this instance.
		/// </summary>
		private short _tag;

		private byte[] _data;

		#endregion Instance Fields
	}

	/// <summary>
	/// Class representing extended unix date time values.
	/// </summary>
	internal class ExtendedUnixData : ITaggedData
	{
		/// <summary>
		/// Flags indicate which values are included in this instance.
		/// </summary>
		[Flags]
		public enum Flags : byte
		{
			/// <summary>
			/// The modification time is included
			/// </summary>
			ModificationTime = 0x01,

			/// <summary>
			/// The access time is included
			/// </summary>
			AccessTime = 0x02,

			/// <summary>
			/// The create time is included.
			/// </summary>
			CreateTime = 0x04,
		}

		#region ITaggedData Members

		/// <summary>
		/// Get the ID
		/// </summary>
		public short TagID
		{
			get { return 0x5455; }
		}

		/// <summary>
		/// Set the data from the raw values provided.
		/// </summary>
		/// <param name="data">The raw data to extract values from.</param>
		/// <param name="index">The index to start extracting values from.</param>
		/// <param name="count">The number of bytes available.</param>
		public void SetData(byte[] data, int index, int count)
		{
			using (MemoryStream ms = new MemoryStream(data, index, count, false))
			using (ZipHelperStream helperStream = new ZipHelperStream(ms))
			{
				// bit 0           if set, modification time is present
				// bit 1           if set, access time is present
				// bit 2           if set, creation time is present

				this._flags = (Flags)helperStream.ReadByte();
				if (((this._flags & Flags.ModificationTime) != 0))
				{
					int iTime = helperStream.ReadLEInt();

					this._modificationTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) +
						new TimeSpan(0, 0, 0, iTime, 0);

					// Central-header version is truncated after modification time
					if (count <= 5) return;
				}

				if ((this._flags & Flags.AccessTime) != 0)
				{
					int iTime = helperStream.ReadLEInt();

					this._lastAccessTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) +
						new TimeSpan(0, 0, 0, iTime, 0);
				}

				if ((this._flags & Flags.CreateTime) != 0)
				{
					int iTime = helperStream.ReadLEInt();

					this._createTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) +
						new TimeSpan(0, 0, 0, iTime, 0);
				}
			}
		}

		/// <summary>
		/// Get the binary data representing this instance.
		/// </summary>
		/// <returns>The raw binary data representing this instance.</returns>
		public byte[] GetData()
		{
			using (MemoryStream ms = new MemoryStream())
			using (ZipHelperStream helperStream = new ZipHelperStream(ms))
			{
				helperStream.IsStreamOwner = false;
				helperStream.WriteByte((byte)this._flags);     // Flags
				if ((this._flags & Flags.ModificationTime) != 0)
				{
					TimeSpan span = this._modificationTime - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
					var seconds = (int)span.TotalSeconds;
					helperStream.WriteLEInt(seconds);
				}
				if ((this._flags & Flags.AccessTime) != 0)
				{
					TimeSpan span = this._lastAccessTime - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
					var seconds = (int)span.TotalSeconds;
					helperStream.WriteLEInt(seconds);
				}
				if ((this._flags & Flags.CreateTime) != 0)
				{
					TimeSpan span = this._createTime - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
					var seconds = (int)span.TotalSeconds;
					helperStream.WriteLEInt(seconds);
				}
				return ms.ToArray();
			}
		}

		#endregion ITaggedData Members

		/// <summary>
		/// Test a <see cref="DateTime"> value to see if is valid and can be represented here.</see>
		/// </summary>
		/// <param name="value">The <see cref="DateTime">value</see> to test.</param>
		/// <returns>Returns true if the value is valid and can be represented; false if not.</returns>
		/// <remarks>The standard Unix time is a signed integer data type, directly encoding the Unix time number,
		/// which is the number of seconds since 1970-01-01.
		/// Being 32 bits means the values here cover a range of about 136 years.
		/// The minimum representable time is 1901-12-13 20:45:52,
		/// and the maximum representable time is 2038-01-19 03:14:07.
		/// </remarks>
		public static bool IsValidValue(DateTime value)
		{
			return ((value >= new DateTime(1901, 12, 13, 20, 45, 52)) ||
					(value <= new DateTime(2038, 1, 19, 03, 14, 07)));
		}

		/// <summary>
		/// Get /set the Modification Time
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <seealso cref="IsValidValue"></seealso>
		public DateTime ModificationTime
		{
			get { return this._modificationTime; }
			set
			{
				if (!IsValidValue(value))
				{
					throw new ArgumentOutOfRangeException("value");
				}

				this._flags |= Flags.ModificationTime;
				this._modificationTime = value;
			}
		}

		/// <summary>
		/// Get / set the Access Time
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <seealso cref="IsValidValue"></seealso>
		public DateTime AccessTime
		{
			get { return this._lastAccessTime; }
			set
			{
				if (!IsValidValue(value))
				{
					throw new ArgumentOutOfRangeException("value");
				}

				this._flags |= Flags.AccessTime;
				this._lastAccessTime = value;
			}
		}

		/// <summary>
		/// Get / Set the Create Time
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <seealso cref="IsValidValue"></seealso>
		public DateTime CreateTime
		{
			get { return this._createTime; }
			set
			{
				if (!IsValidValue(value))
				{
					throw new ArgumentOutOfRangeException("value");
				}

				this._flags |= Flags.CreateTime;
				this._createTime = value;
			}
		}

		/// <summary>
		/// Get/set the <see cref="Flags">values</see> to include.
		/// </summary>
		public Flags Include
		{
			get { return this._flags; }
			set { this._flags = value; }
		}

		#region Instance Fields

		private Flags _flags;
		private DateTime _modificationTime = new DateTime(1970, 1, 1);
		private DateTime _lastAccessTime = new DateTime(1970, 1, 1);
		private DateTime _createTime = new DateTime(1970, 1, 1);

		#endregion Instance Fields
	}

	/// <summary>
	/// Class handling NT date time values.
	/// </summary>
	internal class NTTaggedData : ITaggedData
	{
		/// <summary>
		/// Get the ID for this tagged data value.
		/// </summary>
		public short TagID
		{
			get { return 10; }
		}

		/// <summary>
		/// Set the data from the raw values provided.
		/// </summary>
		/// <param name="data">The raw data to extract values from.</param>
		/// <param name="index">The index to start extracting values from.</param>
		/// <param name="count">The number of bytes available.</param>
		public void SetData(byte[] data, int index, int count)
		{
			using (MemoryStream ms = new MemoryStream(data, index, count, false))
			using (ZipHelperStream helperStream = new ZipHelperStream(ms))
			{
				helperStream.ReadLEInt(); // Reserved
				while (helperStream.Position < helperStream.Length)
				{
					int ntfsTag = helperStream.ReadLEShort();
					int ntfsLength = helperStream.ReadLEShort();
					if (ntfsTag == 1)
					{
						if (ntfsLength >= 24)
						{
							long lastModificationTicks = helperStream.ReadLELong();
							this._lastModificationTime = DateTime.FromFileTimeUtc(lastModificationTicks);

							long lastAccessTicks = helperStream.ReadLELong();
							this._lastAccessTime = DateTime.FromFileTimeUtc(lastAccessTicks);

							long createTimeTicks = helperStream.ReadLELong();
							this._createTime = DateTime.FromFileTimeUtc(createTimeTicks);
						}
						break;
					}
					else
					{
						// An unknown NTFS tag so simply skip it.
						helperStream.Seek(ntfsLength, SeekOrigin.Current);
					}
				}
			}
		}

		/// <summary>
		/// Get the binary data representing this instance.
		/// </summary>
		/// <returns>The raw binary data representing this instance.</returns>
		public byte[] GetData()
		{
			using (MemoryStream ms = new MemoryStream())
			using (ZipHelperStream helperStream = new ZipHelperStream(ms))
			{
				helperStream.IsStreamOwner = false;
				helperStream.WriteLEInt(0);       // Reserved
				helperStream.WriteLEShort(1);     // Tag
				helperStream.WriteLEShort(24);    // Length = 3 x 8.
				helperStream.WriteLELong(this._lastModificationTime.ToFileTimeUtc());
				helperStream.WriteLELong(this._lastAccessTime.ToFileTimeUtc());
				helperStream.WriteLELong(this._createTime.ToFileTimeUtc());
				return ms.ToArray();
			}
		}

		/// <summary>
		/// Test a <see cref="DateTime"> valuie to see if is valid and can be represented here.</see>
		/// </summary>
		/// <param name="value">The <see cref="DateTime">value</see> to test.</param>
		/// <returns>Returns true if the value is valid and can be represented; false if not.</returns>
		/// <remarks>
		/// NTFS filetimes are 64-bit unsigned integers, stored in Intel
		/// (least significant byte first) byte order. They determine the
		/// number of 1.0E-07 seconds (1/10th microseconds!) past WinNT "epoch",
		/// which is "01-Jan-1601 00:00:00 UTC". 28 May 60056 is the upper limit
		/// </remarks>
		public static bool IsValidValue(DateTime value)
		{
			bool result = true;
			try
			{
				value.ToFileTimeUtc();
			}
			catch
			{
				result = false;
			}
			return result;
		}

		/// <summary>
		/// Get/set the <see cref="DateTime">last modification time</see>.
		/// </summary>
		public DateTime LastModificationTime
		{
			get { return this._lastModificationTime; }
			set
			{
				if (!IsValidValue(value))
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._lastModificationTime = value;
			}
		}

		/// <summary>
		/// Get /set the <see cref="DateTime">create time</see>
		/// </summary>
		public DateTime CreateTime
		{
			get { return this._createTime; }
			set
			{
				if (!IsValidValue(value))
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._createTime = value;
			}
		}

		/// <summary>
		/// Get /set the <see cref="DateTime">last access time</see>.
		/// </summary>
		public DateTime LastAccessTime
		{
			get { return this._lastAccessTime; }
			set
			{
				if (!IsValidValue(value))
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._lastAccessTime = value;
			}
		}

		#region Instance Fields

		private DateTime _lastAccessTime = DateTime.FromFileTimeUtc(0);
		private DateTime _lastModificationTime = DateTime.FromFileTimeUtc(0);
		private DateTime _createTime = DateTime.FromFileTimeUtc(0);

		#endregion Instance Fields
	}

	/// <summary>
	/// A factory that creates <see cref="ITaggedData">tagged data</see> instances.
	/// </summary>
	internal interface ITaggedDataFactory
	{
		/// <summary>
		/// Get data for a specific tag value.
		/// </summary>
		/// <param name="tag">The tag ID to find.</param>
		/// <param name="data">The data to search.</param>
		/// <param name="offset">The offset to begin extracting data from.</param>
		/// <param name="count">The number of bytes to extract.</param>
		/// <returns>The located <see cref="ITaggedData">value found</see>, or null if not found.</returns>
		ITaggedData Create(short tag, byte[] data, int offset, int count);
	}

	///
	/// <summary>
	/// A class to handle the extra data field for Zip entries
	/// </summary>
	/// <remarks>
	/// Extra data contains 0 or more values each prefixed by a header tag and length.
	/// They contain zero or more bytes of actual data.
	/// The data is held internally using a copy on write strategy.  This is more efficient but
	/// means that for extra data created by passing in data can have the values modified by the caller
	/// in some circumstances.
	/// </remarks>
	sealed internal class ZipExtraData : IDisposable
	{
		#region Constructors

		/// <summary>
		/// Initialise a default instance.
		/// </summary>
		public ZipExtraData()
		{
			this.Clear();
		}

		/// <summary>
		/// Initialise with known extra data.
		/// </summary>
		/// <param name="data">The extra data.</param>
		public ZipExtraData(byte[] data)
		{
			if (data == null)
			{
				this._data = new byte[0];
			}
			else
			{
				this._data = data;
			}
		}

		#endregion Constructors

		/// <summary>
		/// Get the raw extra data value
		/// </summary>
		/// <returns>Returns the raw byte[] extra data this instance represents.</returns>
		public byte[] GetEntryData()
		{
			if (this.Length > ushort.MaxValue)
			{
				throw new ZipException("Data exceeds maximum length");
			}

			return (byte[])this._data.Clone();
		}

		/// <summary>
		/// Clear the stored data.
		/// </summary>
		public void Clear()
		{
			if ((this._data == null) || (this._data.Length != 0))
			{
				this._data = new byte[0];
			}
		}

		/// <summary>
		/// Gets the current extra data length.
		/// </summary>
		public int Length
		{
			get { return this._data.Length; }
		}

		/// <summary>
		/// Get a read-only <see cref="Stream"/> for the associated tag.
		/// </summary>
		/// <param name="tag">The tag to locate data for.</param>
		/// <returns>Returns a <see cref="Stream"/> containing tag data or null if no tag was found.</returns>
		public Stream GetStreamForTag(int tag)
		{
			Stream result = null;
			if (this.Find(tag))
			{
				result = new MemoryStream(this._data, this._index, this._readValueLength, false);
			}
			return result;
		}

		/// <summary>
		/// Get the <see cref="ITaggedData">tagged data</see> for a tag.
		/// </summary>
		/// <typeparam name="T">The tag to search for.</typeparam>
		/// <returns>Returns a <see cref="ITaggedData">tagged value</see> or null if none found.</returns>
		public T GetData<T>()
			where T : class, ITaggedData, new()
		{
			T result = new T();
			if (this.Find(result.TagID))
			{
				result.SetData(this._data, this._readValueStart, this._readValueLength);
				return result;
			}
			else return null;
		}

		/// <summary>
		/// Get the length of the last value found by <see cref="Find"/>
		/// </summary>
		/// <remarks>This is only valid if <see cref="Find"/> has previously returned true.</remarks>
		public int ValueLength
		{
			get { return this._readValueLength; }
		}

		/// <summary>
		/// Get the index for the current read value.
		/// </summary>
		/// <remarks>This is only valid if <see cref="Find"/> has previously returned true.
		/// Initially the result will be the index of the first byte of actual data.  The value is updated after calls to
		/// <see cref="ReadInt"/>, <see cref="ReadShort"/> and <see cref="ReadLong"/>. </remarks>
		public int CurrentReadIndex
		{
			get { return this._index; }
		}

		/// <summary>
		/// Get the number of bytes remaining to be read for the current value;
		/// </summary>
		public int UnreadCount
		{
			get
			{
				if ((this._readValueStart > this._data.Length) ||
					(this._readValueStart < 4))
				{
					throw new ZipException("Find must be called before calling a Read method");
				}

				return this._readValueStart + this._readValueLength - this._index;
			}
		}

		/// <summary>
		/// Find an extra data value
		/// </summary>
		/// <param name="headerID">The identifier for the value to find.</param>
		/// <returns>Returns true if the value was found; false otherwise.</returns>
		public bool Find(int headerID)
		{
			this._readValueStart = this._data.Length;
			this._readValueLength = 0;
			this._index = 0;

			int localLength = this._readValueStart;
			int localTag = headerID - 1;

			// Trailing bytes that cant make up an entry (as there arent enough
			// bytes for a tag and length) are ignored!
			while ((localTag != headerID) && (this._index < this._data.Length - 3))
			{
				localTag = this.ReadShortInternal();
				localLength = this.ReadShortInternal();
				if (localTag != headerID)
				{
					this._index += localLength;
				}
			}

			bool result = (localTag == headerID) && ((this._index + localLength) <= this._data.Length);

			if (result)
			{
				this._readValueStart = this._index;
				this._readValueLength = localLength;
			}

			return result;
		}

		/// <summary>
		/// Add a new entry to extra data.
		/// </summary>
		/// <param name="taggedData">The <see cref="ITaggedData"/> value to add.</param>
		public void AddEntry(ITaggedData taggedData)
		{
			if (taggedData == null)
			{
				throw new ArgumentNullException("taggedData");
			}
			this.AddEntry(taggedData.TagID, taggedData.GetData());
		}

		/// <summary>
		/// Add a new entry to extra data
		/// </summary>
		/// <param name="headerID">The ID for this entry.</param>
		/// <param name="fieldData">The data to add.</param>
		/// <remarks>If the ID already exists its contents are replaced.</remarks>
		public void AddEntry(int headerID, byte[] fieldData)
		{
			if ((headerID > ushort.MaxValue) || (headerID < 0))
			{
				throw new ArgumentOutOfRangeException("headerID");
			}

			int addLength = (fieldData == null) ? 0 : fieldData.Length;

			if (addLength > ushort.MaxValue)
			{
				throw new ArgumentOutOfRangeException("fieldData", "exceeds maximum length");
			}

			// Test for new length before adjusting data.
			int newLength = this._data.Length + addLength + 4;

			if (this.Find(headerID))
			{
				newLength -= (this.ValueLength + 4);
			}

			if (newLength > ushort.MaxValue)
			{
				throw new ZipException("Data exceeds maximum length");
			}

			this.Delete(headerID);

			byte[] newData = new byte[newLength];
			this._data.CopyTo(newData, 0);
			int index = this._data.Length;
			this._data = newData;
			this.SetShort(ref index, headerID);
			this.SetShort(ref index, addLength);
			if (fieldData != null)
			{
				fieldData.CopyTo(newData, index);
			}
		}

		/// <summary>
		/// Start adding a new entry.
		/// </summary>
		/// <remarks>Add data using <see cref="AddData(byte[])"/>, <see cref="AddLeShort"/>, <see cref="AddLeInt"/>, or <see cref="AddLeLong"/>.
		/// The new entry is completed and actually added by calling <see cref="AddNewEntry"/></remarks>
		/// <seealso cref="AddEntry(ITaggedData)"/>
		public void StartNewEntry()
		{
			this._newEntry = new MemoryStream();
		}

		/// <summary>
		/// Add entry data added since <see cref="StartNewEntry"/> using the ID passed.
		/// </summary>
		/// <param name="headerID">The identifier to use for this entry.</param>
		public void AddNewEntry(int headerID)
		{
			byte[] newData = this._newEntry.ToArray();
			this._newEntry = null;
			this.AddEntry(headerID, newData);
		}

		/// <summary>
		/// Add a byte of data to the pending new entry.
		/// </summary>
		/// <param name="data">The byte to add.</param>
		/// <seealso cref="StartNewEntry"/>
		public void AddData(byte data)
		{
			this._newEntry.WriteByte(data);
		}

		/// <summary>
		/// Add data to a pending new entry.
		/// </summary>
		/// <param name="data">The data to add.</param>
		/// <seealso cref="StartNewEntry"/>
		public void AddData(byte[] data)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}

			this._newEntry.Write(data, 0, data.Length);
		}

		/// <summary>
		/// Add a short value in little endian order to the pending new entry.
		/// </summary>
		/// <param name="toAdd">The data to add.</param>
		/// <seealso cref="StartNewEntry"/>
		public void AddLeShort(int toAdd)
		{
			unchecked
			{
				this._newEntry.WriteByte((byte)toAdd);
				this._newEntry.WriteByte((byte)(toAdd >> 8));
			}
		}

		/// <summary>
		/// Add an integer value in little endian order to the pending new entry.
		/// </summary>
		/// <param name="toAdd">The data to add.</param>
		/// <seealso cref="StartNewEntry"/>
		public void AddLeInt(int toAdd)
		{
			unchecked
			{
				this.AddLeShort((short)toAdd);
				this.AddLeShort((short)(toAdd >> 16));
			}
		}

		/// <summary>
		/// Add a long value in little endian order to the pending new entry.
		/// </summary>
		/// <param name="toAdd">The data to add.</param>
		/// <seealso cref="StartNewEntry"/>
		public void AddLeLong(long toAdd)
		{
			unchecked
			{
				this.AddLeInt((int)(toAdd & 0xffffffff));
				this.AddLeInt((int)(toAdd >> 32));
			}
		}

		/// <summary>
		/// Delete an extra data field.
		/// </summary>
		/// <param name="headerID">The identifier of the field to delete.</param>
		/// <returns>Returns true if the field was found and deleted.</returns>
		public bool Delete(int headerID)
		{
			bool result = false;

			if (this.Find(headerID))
			{
				result = true;
				int trueStart = this._readValueStart - 4;

				byte[] newData = new byte[this._data.Length - (this.ValueLength + 4)];
				Array.Copy(this._data, 0, newData, 0, trueStart);

				int trueEnd = trueStart + this.ValueLength + 4;
				Array.Copy(this._data, trueEnd, newData, trueStart, this._data.Length - trueEnd);
				this._data = newData;
			}
			return result;
		}

		#region Reading Support

		/// <summary>
		/// Read a long in little endian form from the last <see cref="Find">found</see> data value
		/// </summary>
		/// <returns>Returns the long value read.</returns>
		public long ReadLong()
		{
			this.ReadCheck(8);
			return (this.ReadInt() & 0xffffffff) | (((long)this.ReadInt()) << 32);
		}

		/// <summary>
		/// Read an integer in little endian form from the last <see cref="Find">found</see> data value.
		/// </summary>
		/// <returns>Returns the integer read.</returns>
		public int ReadInt()
		{
			this.ReadCheck(4);

			int result = this._data[this._index] + (this._data[this._index + 1] << 8) +
				(this._data[this._index + 2] << 16) + (this._data[this._index + 3] << 24);
			this._index += 4;
			return result;
		}

		/// <summary>
		/// Read a short value in little endian form from the last <see cref="Find">found</see> data value.
		/// </summary>
		/// <returns>Returns the short value read.</returns>
		public int ReadShort()
		{
			this.ReadCheck(2);
			int result = this._data[this._index] + (this._data[this._index + 1] << 8);
			this._index += 2;
			return result;
		}

		/// <summary>
		/// Read a byte from an extra data
		/// </summary>
		/// <returns>The byte value read or -1 if the end of data has been reached.</returns>
		public int ReadByte()
		{
			int result = -1;
			if ((this._index < this._data.Length) && (this._readValueStart + this._readValueLength > this._index))
			{
				result = this._data[this._index];
				this._index += 1;
			}
			return result;
		}

		/// <summary>
		/// Skip data during reading.
		/// </summary>
		/// <param name="amount">The number of bytes to skip.</param>
		public void Skip(int amount)
		{
			this.ReadCheck(amount);
			this._index += amount;
		}

		private void ReadCheck(int length)
		{
			if ((this._readValueStart > this._data.Length) ||
				(this._readValueStart < 4))
			{
				throw new ZipException("Find must be called before calling a Read method");
			}

			if (this._index > this._readValueStart + this._readValueLength - length)
			{
				throw new ZipException("End of extra data");
			}

			if (this._index + length < 4)
			{
				throw new ZipException("Cannot read before start of tag");
			}
		}

		/// <summary>
		/// Internal form of <see cref="ReadShort"/> that reads data at any location.
		/// </summary>
		/// <returns>Returns the short value read.</returns>
		private int ReadShortInternal()
		{
			if (this._index > this._data.Length - 2)
			{
				throw new ZipException("End of extra data");
			}

			int result = this._data[this._index] + (this._data[this._index + 1] << 8);
			this._index += 2;
			return result;
		}

		private void SetShort(ref int index, int source)
		{
			this._data[index] = (byte)source;
			this._data[index + 1] = (byte)(source >> 8);
			index += 2;
		}

		#endregion Reading Support

		#region IDisposable Members

		/// <summary>
		/// Dispose of this instance.
		/// </summary>
		public void Dispose()
		{
			if (this._newEntry != null)
			{
				this._newEntry.Dispose();
			}
		}

		#endregion IDisposable Members

		#region Instance Fields

		private int _index;
		private int _readValueStart;
		private int _readValueLength;

		private MemoryStream _newEntry;
		private byte[] _data;

		#endregion Instance Fields
	}
}
