using System;
using System.IO;

namespace GameDevWare.Charon.Unity.Updates.Packages.Zip
{
	/// <summary>
	/// This class assists with writing/reading from Zip files.
	/// </summary>
	internal class ZipHelperStream : Stream
	{
		#region Constructors

		/// <summary>
		/// Initialise an instance of this class.
		/// </summary>
		/// <param name="name">The name of the file to open.</param>
		public ZipHelperStream(string name)
		{
			this.stream_ = new FileStream(name, FileMode.Open, FileAccess.ReadWrite);
			this.isOwner_ = true;
		}

		/// <summary>
		/// Initialise a new instance of <see cref="ZipHelperStream"/>.
		/// </summary>
		/// <param name="stream">The stream to use.</param>
		public ZipHelperStream(Stream stream)
		{
			this.stream_ = stream;
		}

		#endregion Constructors

		/// <summary>
		/// Get / set a value indicating wether the the underlying stream is owned or not.
		/// </summary>
		/// <remarks>If the stream is owned it is closed when this instance is closed.</remarks>
		public bool IsStreamOwner
		{
			get { return this.isOwner_; }
			set { this.isOwner_ = value; }
		}

		#region Base Stream Methods

		public override bool CanRead
		{
			get { return this.stream_.CanRead; }
		}

		public override bool CanSeek
		{
			get { return this.stream_.CanSeek; }
		}

		public override bool CanTimeout
		{
			get { return this.stream_.CanTimeout; }
		}

		public override long Length
		{
			get { return this.stream_.Length; }
		}

		public override long Position
		{
			get { return this.stream_.Position; }
			set { this.stream_.Position = value; }
		}

		public override bool CanWrite
		{
			get { return this.stream_.CanWrite; }
		}

		public override void Flush()
		{
			this.stream_.Flush();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return this.stream_.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			this.stream_.SetLength(value);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return this.stream_.Read(buffer, offset, count);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			this.stream_.Write(buffer, offset, count);
		}

		/// <summary>
		/// Close the stream.
		/// </summary>
		/// <remarks>
		/// The underlying stream is closed only if <see cref="IsStreamOwner"/> is true.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			Stream toClose = this.stream_;
			this.stream_ = null;
			if (this.isOwner_ && (toClose != null))
			{
				this.isOwner_ = false;
				toClose.Dispose();
			}
		}

		#endregion Base Stream Methods

		// Write the local file header
		// TODO: ZipHelperStream.WriteLocalHeader is not yet used and needs checking for ZipFile and ZipOuptutStream usage
		private void WriteLocalHeader(ZipEntry entry, EntryPatchData patchData)
		{
			CompressionMethod method = entry.CompressionMethod;
			bool headerInfoAvailable = true; // How to get this?
			bool patchEntryHeader = false;

			this.WriteLEInt(ZipConstants.LocalHeaderSignature);

			this.WriteLEShort(entry.Version);
			this.WriteLEShort(entry.Flags);
			this.WriteLEShort((byte)method);
			this.WriteLEInt((int)entry.DosTime);

			if (headerInfoAvailable == true)
			{
				this.WriteLEInt((int)entry.Crc);
				if (entry.LocalHeaderRequiresZip64)
				{
					this.WriteLEInt(-1);
					this.WriteLEInt(-1);
				}
				else
				{
					this.WriteLEInt(entry.IsCrypted ? (int)entry.CompressedSize + ZipConstants.CryptoHeaderSize : (int)entry.CompressedSize);
					this.WriteLEInt((int)entry.Size);
				}
			}
			else
			{
				if (patchData != null)
				{
					patchData.CrcPatchOffset = this.stream_.Position;
				}
				this.WriteLEInt(0);  // Crc

				if (patchData != null)
				{
					patchData.SizePatchOffset = this.stream_.Position;
				}

				// For local header both sizes appear in Zip64 Extended Information
				if (entry.LocalHeaderRequiresZip64 && patchEntryHeader)
				{
					this.WriteLEInt(-1);
					this.WriteLEInt(-1);
				}
				else
				{
					this.WriteLEInt(0);  // Compressed size
					this.WriteLEInt(0);  // Uncompressed size
				}
			}

			byte[] name = ZipStrings.ConvertToArray(entry.Flags, entry.Name);

			if (name.Length > 0xFFFF)
			{
				throw new ZipException("Entry name too long.");
			}

			var ed = new ZipExtraData(entry.ExtraData);

			if (entry.LocalHeaderRequiresZip64 && (headerInfoAvailable || patchEntryHeader))
			{
				ed.StartNewEntry();
				if (headerInfoAvailable)
				{
					ed.AddLeLong(entry.Size);
					ed.AddLeLong(entry.CompressedSize);
				}
				else
				{
					ed.AddLeLong(-1);
					ed.AddLeLong(-1);
				}
				ed.AddNewEntry(1);

				if (!ed.Find(1))
				{
					throw new ZipException("Internal error cant find extra data");
				}

				if (patchData != null)
				{
					patchData.SizePatchOffset = ed.CurrentReadIndex;
				}
			}
			else
			{
				ed.Delete(1);
			}

			byte[] extra = ed.GetEntryData();

			this.WriteLEShort(name.Length);
			this.WriteLEShort(extra.Length);

			if (name.Length > 0)
			{
				this.stream_.Write(name, 0, name.Length);
			}

			if (entry.LocalHeaderRequiresZip64 && patchEntryHeader)
			{
				patchData.SizePatchOffset += this.stream_.Position;
			}

			if (extra.Length > 0)
			{
				this.stream_.Write(extra, 0, extra.Length);
			}
		}

		/// <summary>
		/// Locates a block with the desired <paramref name="signature"/>.
		/// </summary>
		/// <param name="signature">The signature to find.</param>
		/// <param name="endLocation">Location, marking the end of block.</param>
		/// <param name="minimumBlockSize">Minimum size of the block.</param>
		/// <param name="maximumVariableData">The maximum variable data.</param>
		/// <returns>Eeturns the offset of the first byte after the signature; -1 if not found</returns>
		public long LocateBlockWithSignature(int signature, long endLocation, int minimumBlockSize, int maximumVariableData)
		{
			long pos = endLocation - minimumBlockSize;
			if (pos < 0)
			{
				return -1;
			}

			long giveUpMarker = Math.Max(pos - maximumVariableData, 0);

			// TODO: This loop could be optimised for speed.
			do
			{
				if (pos < giveUpMarker)
				{
					return -1;
				}
				this.Seek(pos--, SeekOrigin.Begin);
			} while (this.ReadLEInt() != signature);

			return this.Position;
		}

		/// <summary>
		/// Write Zip64 end of central directory records (File header and locator).
		/// </summary>
		/// <param name="noOfEntries">The number of entries in the central directory.</param>
		/// <param name="sizeEntries">The size of entries in the central directory.</param>
		/// <param name="centralDirOffset">The offset of the dentral directory.</param>
		public void WriteZip64EndOfCentralDirectory(long noOfEntries, long sizeEntries, long centralDirOffset)
		{
			long centralSignatureOffset = centralDirOffset + sizeEntries;
			this.WriteLEInt(ZipConstants.Zip64CentralFileHeaderSignature);
			this.WriteLELong(44);    // Size of this record (total size of remaining fields in header or full size - 12)
			this.WriteLEShort(ZipConstants.VersionMadeBy);   // Version made by
			this.WriteLEShort(ZipConstants.VersionZip64);   // Version to extract
			this.WriteLEInt(0);      // Number of this disk
			this.WriteLEInt(0);      // number of the disk with the start of the central directory
			this.WriteLELong(noOfEntries);       // No of entries on this disk
			this.WriteLELong(noOfEntries);       // Total No of entries in central directory
			this.WriteLELong(sizeEntries);       // Size of the central directory
			this.WriteLELong(centralDirOffset);  // offset of start of central directory
											// zip64 extensible data sector not catered for here (variable size)

			// Write the Zip64 end of central directory locator
			this.WriteLEInt(ZipConstants.Zip64CentralDirLocatorSignature);

			// no of the disk with the start of the zip64 end of central directory
			this.WriteLEInt(0);

			// relative offset of the zip64 end of central directory record
			this.WriteLELong(centralSignatureOffset);

			// total number of disks
			this.WriteLEInt(1);
		}

		/// <summary>
		/// Write the required records to end the central directory.
		/// </summary>
		/// <param name="noOfEntries">The number of entries in the directory.</param>
		/// <param name="sizeEntries">The size of the entries in the directory.</param>
		/// <param name="startOfCentralDirectory">The start of the central directory.</param>
		/// <param name="comment">The archive comment.  (This can be null).</param>
		public void WriteEndOfCentralDirectory(long noOfEntries, long sizeEntries,
			long startOfCentralDirectory, byte[] comment)
		{
			if ((noOfEntries >= 0xffff) ||
				(startOfCentralDirectory >= 0xffffffff) ||
				(sizeEntries >= 0xffffffff))
			{
				this.WriteZip64EndOfCentralDirectory(noOfEntries, sizeEntries, startOfCentralDirectory);
			}

			this.WriteLEInt(ZipConstants.EndOfCentralDirectorySignature);

			// TODO: ZipFile Multi disk handling not done
			this.WriteLEShort(0);                    // number of this disk
			this.WriteLEShort(0);                    // no of disk with start of central dir

			// Number of entries
			if (noOfEntries >= 0xffff)
			{
				this.WriteLEUshort(0xffff);  // Zip64 marker
				this.WriteLEUshort(0xffff);
			}
			else
			{
				this.WriteLEShort((short)noOfEntries);          // entries in central dir for this disk
				this.WriteLEShort((short)noOfEntries);          // total entries in central directory
			}

			// Size of the central directory
			if (sizeEntries >= 0xffffffff)
			{
				this.WriteLEUint(0xffffffff);    // Zip64 marker
			}
			else
			{
				this.WriteLEInt((int)sizeEntries);
			}

			// offset of start of central directory
			if (startOfCentralDirectory >= 0xffffffff)
			{
				this.WriteLEUint(0xffffffff);    // Zip64 marker
			}
			else
			{
				this.WriteLEInt((int)startOfCentralDirectory);
			}

			int commentLength = (comment != null) ? comment.Length : 0;

			if (commentLength > 0xffff)
			{
				throw new ZipException(string.Format("Comment length({0}) is too long can only be 64K", commentLength));
			}

			this.WriteLEShort(commentLength);

			if (commentLength > 0)
			{
				this.Write(comment, 0, comment.Length);
			}
		}

		#region LE value reading/writing

		/// <summary>
		/// Read an unsigned short in little endian byte order.
		/// </summary>
		/// <returns>Returns the value read.</returns>
		/// <exception cref="IOException">
		/// An i/o error occurs.
		/// </exception>
		/// <exception cref="EndOfStreamException">
		/// The file ends prematurely
		/// </exception>
		public int ReadLEShort()
		{
			int byteValue1 = this.stream_.ReadByte();

			if (byteValue1 < 0)
			{
				throw new EndOfStreamException();
			}

			int byteValue2 = this.stream_.ReadByte();
			if (byteValue2 < 0)
			{
				throw new EndOfStreamException();
			}

			return byteValue1 | (byteValue2 << 8);
		}

		/// <summary>
		/// Read an int in little endian byte order.
		/// </summary>
		/// <returns>Returns the value read.</returns>
		/// <exception cref="IOException">
		/// An i/o error occurs.
		/// </exception>
		/// <exception cref="System.IO.EndOfStreamException">
		/// The file ends prematurely
		/// </exception>
		public int ReadLEInt()
		{
			return this.ReadLEShort() | (this.ReadLEShort() << 16);
		}

		/// <summary>
		/// Read a long in little endian byte order.
		/// </summary>
		/// <returns>The value read.</returns>
		public long ReadLELong()
		{
			return (uint)this.ReadLEInt() | ((long)this.ReadLEInt() << 32);
		}

		/// <summary>
		/// Write an unsigned short in little endian byte order.
		/// </summary>
		/// <param name="value">The value to write.</param>
		public void WriteLEShort(int value)
		{
			this.stream_.WriteByte((byte)(value & 0xff));
			this.stream_.WriteByte((byte)((value >> 8) & 0xff));
		}

		/// <summary>
		/// Write a ushort in little endian byte order.
		/// </summary>
		/// <param name="value">The value to write.</param>
		public void WriteLEUshort(ushort value)
		{
			this.stream_.WriteByte((byte)(value & 0xff));
			this.stream_.WriteByte((byte)(value >> 8));
		}

		/// <summary>
		/// Write an int in little endian byte order.
		/// </summary>
		/// <param name="value">The value to write.</param>
		public void WriteLEInt(int value)
		{
			this.WriteLEShort(value);
			this.WriteLEShort(value >> 16);
		}

		/// <summary>
		/// Write a uint in little endian byte order.
		/// </summary>
		/// <param name="value">The value to write.</param>
		public void WriteLEUint(uint value)
		{
			this.WriteLEUshort((ushort)(value & 0xffff));
			this.WriteLEUshort((ushort)(value >> 16));
		}

		/// <summary>
		/// Write a long in little endian byte order.
		/// </summary>
		/// <param name="value">The value to write.</param>
		public void WriteLELong(long value)
		{
			this.WriteLEInt((int)value);
			this.WriteLEInt((int)(value >> 32));
		}

		/// <summary>
		/// Write a ulong in little endian byte order.
		/// </summary>
		/// <param name="value">The value to write.</param>
		public void WriteLEUlong(ulong value)
		{
			this.WriteLEUint((uint)(value & 0xffffffff));
			this.WriteLEUint((uint)(value >> 32));
		}

		#endregion LE value reading/writing

		/// <summary>
		/// Write a data descriptor.
		/// </summary>
		/// <param name="entry">The entry to write a descriptor for.</param>
		/// <returns>Returns the number of descriptor bytes written.</returns>
		public int WriteDataDescriptor(ZipEntry entry)
		{
			if (entry == null)
			{
				throw new ArgumentNullException("entry");
			}

			int result = 0;

			// Add data descriptor if flagged as required
			if ((entry.Flags & (int)GeneralBitFlags.Descriptor) != 0)
			{
				// The signature is not PKZIP originally but is now described as optional
				// in the PKZIP Appnote documenting trhe format.
				this.WriteLEInt(ZipConstants.DataDescriptorSignature);
				this.WriteLEInt(unchecked((int)(entry.Crc)));

				result += 8;

				if (entry.LocalHeaderRequiresZip64)
				{
					this.WriteLELong(entry.CompressedSize);
					this.WriteLELong(entry.Size);
					result += 16;
				}
				else
				{
					this.WriteLEInt((int)entry.CompressedSize);
					this.WriteLEInt((int)entry.Size);
					result += 8;
				}
			}

			return result;
		}

		/// <summary>
		/// Read data descriptor at the end of compressed data.
		/// </summary>
		/// <param name="zip64">if set to <c>true</c> [zip64].</param>
		/// <param name="data">The data to fill in.</param>
		/// <returns>Returns the number of bytes read in the descriptor.</returns>
		public void ReadDataDescriptor(bool zip64, DescriptorData data)
		{
			int intValue = this.ReadLEInt();

			// In theory this may not be a descriptor according to PKZIP appnote.
			// In practise its always there.
			if (intValue != ZipConstants.DataDescriptorSignature)
			{
				throw new ZipException("Data descriptor signature not found");
			}

			data.Crc = this.ReadLEInt();

			if (zip64)
			{
				data.CompressedSize = this.ReadLELong();
				data.Size = this.ReadLELong();
			}
			else
			{
				data.CompressedSize = this.ReadLEInt();
				data.Size = this.ReadLEInt();
			}
		}

		#region Instance Fields

		private bool isOwner_;
		private Stream stream_;

		#endregion Instance Fields
	}
}
