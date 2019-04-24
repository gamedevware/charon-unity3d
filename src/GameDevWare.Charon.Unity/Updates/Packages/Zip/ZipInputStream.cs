using System;
using System.IO;
using GameDevWare.Charon.Unity.Updates.Packages.Zip.Checksum;
using GameDevWare.Charon.Unity.Updates.Packages.Zip.Compression;
using GameDevWare.Charon.Unity.Updates.Packages.Zip.Compression.Streams;

namespace GameDevWare.Charon.Unity.Updates.Packages.Zip
{
	/// <summary>
	/// This is an InflaterInputStream that reads the files baseInputStream an zip archive
	/// one after another.  It has a special method to get the zip entry of
	/// the next file.  The zip entry contains information about the file name
	/// size, compressed size, Crc, etc.
	/// It includes support for Stored and Deflated entries.
	/// <br/>
	/// <br/>Author of the original java version : Jochen Hoenicke
	/// </summary>
	///
	/// <example> This sample shows how to read a zip file
	/// <code lang="C#">
	/// using System;
	/// using System.Text;
	/// using System.IO;
	///
	/// using ICSharpCode.SharpZipLib.Zip;
	///
	/// class MainClass
	/// {
	/// 	public static void Main(string[] args)
	/// 	{
	/// 		using ( ZipInputStream s = new ZipInputStream(File.OpenRead(args[0]))) {
	///
	/// 			ZipEntry theEntry;
	/// 			const int size = 2048;
	/// 			byte[] data = new byte[2048];
	///
	/// 			while ((theEntry = s.GetNextEntry()) != null) {
	///                 if ( entry.IsFile ) {
	/// 				    Console.Write("Show contents (y/n) ?");
	/// 				    if (Console.ReadLine() == "y") {
	/// 				    	while (true) {
	/// 				    		size = s.Read(data, 0, data.Length);
	/// 				    		if (size > 0) {
	/// 				    			Console.Write(new ASCIIEncoding().GetString(data, 0, size));
	/// 				    		} else {
	/// 				    			break;
	/// 				    		}
	/// 				    	}
	/// 				    }
	/// 				}
	/// 			}
	/// 		}
	/// 	}
	/// }
	/// </code>
	/// </example>
	internal class ZipInputStream : InflaterInputStream
	{
		#region Instance Fields

		/// <summary>
		/// Delegate for reading bytes from a stream.
		/// </summary>
		private delegate int ReadDataHandler(byte[] b, int offset, int length);

		/// <summary>
		/// The current reader this instance.
		/// </summary>
		private ReadDataHandler internalReader;

		private Crc32 crc = new Crc32();
		private ZipEntry entry;

		private long size;
		private int method;
		private int flags;
		private string password;

		#endregion Instance Fields

		#region Constructors

		/// <summary>
		/// Creates a new Zip input stream, for reading a zip archive.
		/// </summary>
		/// <param name="baseInputStream">The underlying <see cref="Stream"/> providing data.</param>
		public ZipInputStream(Stream baseInputStream)
			: base(baseInputStream, new Inflater(true))
		{
			this.internalReader = new ReadDataHandler(this.ReadingNotAvailable);
		}

		/// <summary>
		/// Creates a new Zip input stream, for reading a zip archive.
		/// </summary>
		/// <param name="baseInputStream">The underlying <see cref="Stream"/> providing data.</param>
		/// <param name="bufferSize">Size of the buffer.</param>
		public ZipInputStream(Stream baseInputStream, int bufferSize)
			: base(baseInputStream, new Inflater(true), bufferSize)
		{
			this.internalReader = new ReadDataHandler(this.ReadingNotAvailable);
		}

		#endregion Constructors

		/// <summary>
		/// Optional password used for encryption when non-null
		/// </summary>
		/// <value>A password for all encrypted <see cref="ZipEntry">entries </see> in this <see cref="ZipInputStream"/></value>
		public string Password
		{
			get
			{
				return this.password;
			}
			set
			{
				this.password = value;
			}
		}

		/// <summary>
		/// Gets a value indicating if there is a current entry and it can be decompressed
		/// </summary>
		/// <remarks>
		/// The entry can only be decompressed if the library supports the zip features required to extract it.
		/// See the <see cref="ZipEntry.Version">ZipEntry Version</see> property for more details.
		/// </remarks>
		public bool CanDecompressEntry
		{
			get
			{
				return (this.entry != null) && this.entry.CanDecompress;
			}
		}

		/// <summary>
		/// Advances to the next entry in the archive
		/// </summary>
		/// <returns>
		/// The next <see cref="ZipEntry">entry</see> in the archive or null if there are no more entries.
		/// </returns>
		/// <remarks>
		/// If the previous entry is still open <see cref="CloseEntry">CloseEntry</see> is called.
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		/// Input stream is closed
		/// </exception>
		/// <exception cref="ZipException">
		/// Password is not set, password is invalid, compression method is invalid,
		/// version required to extract is not supported
		/// </exception>
		public ZipEntry GetNextEntry()
		{
			if (this.crc == null)
			{
				throw new InvalidOperationException("Closed.");
			}

			if (this.entry != null)
			{
				this.CloseEntry();
			}

			int header = this.inputBuffer.ReadLeInt();

			if (header == ZipConstants.CentralHeaderSignature ||
				header == ZipConstants.EndOfCentralDirectorySignature ||
				header == ZipConstants.CentralHeaderDigitalSignature ||
				header == ZipConstants.ArchiveExtraDataSignature ||
				header == ZipConstants.Zip64CentralFileHeaderSignature)
			{
				// No more individual entries exist
				this.Dispose();
				return null;
			}

			// -jr- 07-Dec-2003 Ignore spanning temporary signatures if found
			// Spanning signature is same as descriptor signature and is untested as yet.
			if ((header == ZipConstants.SpanningTempSignature) || (header == ZipConstants.SpanningSignature))
			{
				header = this.inputBuffer.ReadLeInt();
			}

			if (header != ZipConstants.LocalHeaderSignature)
			{
				throw new ZipException("Wrong Local header signature: 0x" + string.Format("{0:X}", header));
			}

			var versionRequiredToExtract = (short)this.inputBuffer.ReadLeShort();

			this.flags = this.inputBuffer.ReadLeShort();
			this.method = this.inputBuffer.ReadLeShort();
			var dostime = (uint)this.inputBuffer.ReadLeInt();
			int crc2 = this.inputBuffer.ReadLeInt();
			this.csize = this.inputBuffer.ReadLeInt();
			this.size = this.inputBuffer.ReadLeInt();
			int nameLen = this.inputBuffer.ReadLeShort();
			int extraLen = this.inputBuffer.ReadLeShort();

			bool isCrypted = (this.flags & 1) == 1;

			byte[] buffer = new byte[nameLen];
			this.inputBuffer.ReadRawBuffer(buffer);

			string name = ZipStrings.ConvertToStringExt(this.flags, buffer);

			this.entry = new ZipEntry(name, versionRequiredToExtract)
			{
				Flags = this.flags,
				CompressionMethod = (CompressionMethod)this.method
			};

			if ((this.flags & 8) == 0)
			{
				this.entry.Crc = crc2 & 0xFFFFFFFFL;
				this.entry.Size = this.size & 0xFFFFFFFFL;
				this.entry.CompressedSize = this.csize & 0xFFFFFFFFL;

				this.entry.CryptoCheckValue = (byte)((crc2 >> 24) & 0xff);
			}
			else
			{
				// This allows for GNU, WinZip and possibly other archives, the PKZIP spec
				// says these values are zero under these circumstances.
				if (crc2 != 0)
				{
					this.entry.Crc = crc2 & 0xFFFFFFFFL;
				}

				if (this.size != 0)
				{
					this.entry.Size = this.size & 0xFFFFFFFFL;
				}

				if (this.csize != 0)
				{
					this.entry.CompressedSize = this.csize & 0xFFFFFFFFL;
				}

				this.entry.CryptoCheckValue = (byte)((dostime >> 8) & 0xff);
			}

			this.entry.DosTime = dostime;

			// If local header requires Zip64 is true then the extended header should contain
			// both values.

			// Handle extra data if present.  This can set/alter some fields of the entry.
			if (extraLen > 0)
			{
				byte[] extra = new byte[extraLen];
				this.inputBuffer.ReadRawBuffer(extra);
				this.entry.ExtraData = extra;
			}

			this.entry.ProcessExtraData(true);
			if (this.entry.CompressedSize >= 0)
			{
				this.csize = this.entry.CompressedSize;
			}

			if (this.entry.Size >= 0)
			{
				this.size = this.entry.Size;
			}

			if (this.method == (int)CompressionMethod.Stored && (!isCrypted && this.csize != this.size || (isCrypted && this.csize - ZipConstants.CryptoHeaderSize != this.size)))
			{
				throw new ZipException("Stored, but compressed != uncompressed");
			}

			// Determine how to handle reading of data if this is attempted.
			if (this.entry.IsCompressionMethodSupported())
			{
				this.internalReader = new ReadDataHandler(this.InitialRead);
			}
			else
			{
				this.internalReader = new ReadDataHandler(this.ReadingNotSupported);
			}

			return this.entry;
		}

		/// <summary>
		/// Read data descriptor at the end of compressed data.
		/// </summary>
		private void ReadDataDescriptor()
		{
			if (this.inputBuffer.ReadLeInt() != ZipConstants.DataDescriptorSignature)
			{
				throw new ZipException("Data descriptor signature not found");
			}

			this.entry.Crc = this.inputBuffer.ReadLeInt() & 0xFFFFFFFFL;

			if (this.entry.LocalHeaderRequiresZip64)
			{
				this.csize = this.inputBuffer.ReadLeLong();
				this.size = this.inputBuffer.ReadLeLong();
			}
			else
			{
				this.csize = this.inputBuffer.ReadLeInt();
				this.size = this.inputBuffer.ReadLeInt();
			}
			this.entry.CompressedSize = this.csize;
			this.entry.Size = this.size;
		}

		/// <summary>
		/// Complete cleanup as the final part of closing.
		/// </summary>
		/// <param name="testCrc">True if the crc value should be tested</param>
		private void CompleteCloseEntry(bool testCrc)
		{
			this.StopDecrypting();

			if ((this.flags & 8) != 0)
			{
				this.ReadDataDescriptor();
			}

			this.size = 0;

			if (testCrc &&
				((this.crc.Value & 0xFFFFFFFFL) != this.entry.Crc) && (this.entry.Crc != -1))
			{
				throw new ZipException("CRC mismatch");
			}

			this.crc.Reset();

			if (this.method == (int)CompressionMethod.Deflated)
			{
				this.inf.Reset();
			}
			this.entry = null;
		}

		/// <summary>
		/// Closes the current zip entry and moves to the next one.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// The stream is closed
		/// </exception>
		/// <exception cref="ZipException">
		/// The Zip stream ends early
		/// </exception>
		public void CloseEntry()
		{
			if (this.crc == null)
			{
				throw new InvalidOperationException("Closed");
			}

			if (this.entry == null)
			{
				return;
			}

			if (this.method == (int)CompressionMethod.Deflated)
			{
				if ((this.flags & 8) != 0)
				{
					// We don't know how much we must skip, read until end.
					byte[] tmp = new byte[4096];

					// Read will close this entry
					while (this.Read(tmp, 0, tmp.Length) > 0)
					{
					}
					return;
				}

				this.csize -= this.inf.TotalIn;
				this.inputBuffer.Available += this.inf.RemainingInput;
			}

			if ((this.inputBuffer.Available > this.csize) && (this.csize >= 0))
			{
				this.inputBuffer.Available = (int)((long)this.inputBuffer.Available - this.csize);
			}
			else
			{
				this.csize -= this.inputBuffer.Available;
				this.inputBuffer.Available = 0;
				while (this.csize != 0)
				{
					long skipped = this.Skip(this.csize);

					if (skipped <= 0)
					{
						throw new ZipException("Zip archive ends early.");
					}

					this.csize -= skipped;
				}
			}

			this.CompleteCloseEntry(false);
		}

		/// <summary>
		/// Returns 1 if there is an entry available
		/// Otherwise returns 0.
		/// </summary>
		public override int Available
		{
			get
			{
				return this.entry != null ? 1 : 0;
			}
		}

		/// <summary>
		/// Returns the current size that can be read from the current entry if available
		/// </summary>
		/// <exception cref="ZipException">Thrown if the entry size is not known.</exception>
		/// <exception cref="InvalidOperationException">Thrown if no entry is currently available.</exception>
		public override long Length
		{
			get
			{
				if (this.entry != null)
				{
					if (this.entry.Size >= 0)
					{
						return this.entry.Size;
					}
					else
					{
						throw new ZipException("Length not available for the current entry");
					}
				}
				else
				{
					throw new InvalidOperationException("No current entry");
				}
			}
		}

		/// <summary>
		/// Reads a byte from the current zip entry.
		/// </summary>
		/// <returns>
		/// The byte or -1 if end of stream is reached.
		/// </returns>
		public override int ReadByte()
		{
			byte[] b = new byte[1];
			if (this.Read(b, 0, 1) <= 0)
			{
				return -1;
			}
			return b[0] & 0xff;
		}

		/// <summary>
		/// Handle attempts to read by throwing an <see cref="InvalidOperationException"/>.
		/// </summary>
		/// <param name="destination">The destination array to store data in.</param>
		/// <param name="offset">The offset at which data read should be stored.</param>
		/// <param name="count">The maximum number of bytes to read.</param>
		/// <returns>Returns the number of bytes actually read.</returns>
		private int ReadingNotAvailable(byte[] destination, int offset, int count)
		{
			throw new InvalidOperationException("Unable to read from this stream");
		}

		/// <summary>
		/// Handle attempts to read from this entry by throwing an exception
		/// </summary>
		private int ReadingNotSupported(byte[] destination, int offset, int count)
		{
			throw new ZipException("The compression method for this entry is not supported");
		}

		/// <summary>
		/// Perform the initial read on an entry which may include
		/// reading encryption headers and setting up inflation.
		/// </summary>
		/// <param name="destination">The destination to fill with data read.</param>
		/// <param name="offset">The offset to start reading at.</param>
		/// <param name="count">The maximum number of bytes to read.</param>
		/// <returns>The actual number of bytes read.</returns>
		private int InitialRead(byte[] destination, int offset, int count)
		{
			if (!this.CanDecompressEntry)
			{
				throw new ZipException("Library cannot extract this entry. Version required is (" + this.entry.Version + ")");
			}

			// Handle encryption if required.
			if (this.entry.IsCrypted)
			{
				if (this.password == null)
				{
					throw new ZipException("No password set.");
				}

				// Generate and set crypto transform...
				
				byte[] cryptbuffer = new byte[ZipConstants.CryptoHeaderSize];
				this.inputBuffer.ReadClearTextBuffer(cryptbuffer, 0, ZipConstants.CryptoHeaderSize);

				if (cryptbuffer[ZipConstants.CryptoHeaderSize - 1] != this.entry.CryptoCheckValue)
				{
					throw new ZipException("Invalid password");
				}

				if (this.csize >= ZipConstants.CryptoHeaderSize)
				{
					this.csize -= ZipConstants.CryptoHeaderSize;
				}
				else if ((this.entry.Flags & (int)GeneralBitFlags.Descriptor) == 0)
				{
					throw new ZipException(string.Format("Entry compressed size {0} too small for encryption", this.csize));
				}
			}
			else
			{
				this.inputBuffer.CryptoTransform = null;
			}

			if ((this.csize > 0) || ((this.flags & (int)GeneralBitFlags.Descriptor) != 0))
			{
				if ((this.method == (int)CompressionMethod.Deflated) && (this.inputBuffer.Available > 0))
				{
					this.inputBuffer.SetInflaterInput(this.inf);
				}

				this.internalReader = new ReadDataHandler(this.BodyRead);
				return this.BodyRead(destination, offset, count);
			}
			else
			{
				this.internalReader = new ReadDataHandler(this.ReadingNotAvailable);
				return 0;
			}
		}

		/// <summary>
		/// Read a block of bytes from the stream.
		/// </summary>
		/// <param name="buffer">The destination for the bytes.</param>
		/// <param name="offset">The index to start storing data.</param>
		/// <param name="count">The number of bytes to attempt to read.</param>
		/// <returns>Returns the number of bytes read.</returns>
		/// <remarks>Zero bytes read means end of stream.</remarks>
		public override int Read(byte[] buffer, int offset, int count)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer");
			}

			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", "Cannot be negative");
			}

			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", "Cannot be negative");
			}

			if ((buffer.Length - offset) < count)
			{
				throw new ArgumentException("Invalid offset/count combination");
			}

			return this.internalReader(buffer, offset, count);
		}

		/// <summary>
		/// Reads a block of bytes from the current zip entry.
		/// </summary>
		/// <returns>
		/// The number of bytes read (this may be less than the length requested, even before the end of stream), or 0 on end of stream.
		/// </returns>
		/// <exception name="IOException">
		/// An i/o error occured.
		/// </exception>
		/// <exception cref="ZipException">
		/// The deflated stream is corrupted.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// The stream is not open.
		/// </exception>
		private int BodyRead(byte[] buffer, int offset, int count)
		{
			if (this.crc == null)
			{
				throw new InvalidOperationException("Closed");
			}

			if ((this.entry == null) || (count <= 0))
			{
				return 0;
			}

			if (offset + count > buffer.Length)
			{
				throw new ArgumentException("Offset + count exceeds buffer size");
			}

			bool finished = false;

			switch (this.method)
			{
				case (int)CompressionMethod.Deflated:
					count = base.Read(buffer, offset, count);
					if (count <= 0)
					{
						if (!this.inf.IsFinished)
						{
							throw new ZipException("Inflater not finished!");
						}
						this.inputBuffer.Available = this.inf.RemainingInput;

						// A csize of -1 is from an unpatched local header
						if ((this.flags & 8) == 0 &&
							(this.inf.TotalIn != this.csize && this.csize != 0xFFFFFFFF && this.csize != -1 || this.inf.TotalOut != this.size))
						{
							throw new ZipException("Size mismatch: " + this.csize + ";" + this.size + " <-> " + this.inf.TotalIn + ";" + this.inf.TotalOut);
						}
						this.inf.Reset();
						finished = true;
					}
					break;

				case (int)CompressionMethod.Stored:
					if ((count > this.csize) && (this.csize >= 0))
					{
						count = (int)this.csize;
					}

					if (count > 0)
					{
						count = this.inputBuffer.ReadClearTextBuffer(buffer, offset, count);
						if (count > 0)
						{
							this.csize -= count;
							this.size -= count;
						}
					}

					if (this.csize == 0)
					{
						finished = true;
					}
					else
					{
						if (count < 0)
						{
							throw new ZipException("EOF in stored block");
						}
					}
					break;
			}

			if (count > 0)
			{
				this.crc.Update(new ArraySegment<byte>(buffer, offset, count));
			}

			if (finished)
			{
				this.CompleteCloseEntry(true);
			}

			return count;
		}

		/// <summary>
		/// Closes the zip input stream
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			this.internalReader = new ReadDataHandler(this.ReadingNotAvailable);
			this.crc = null;
			this.entry = null;

			base.Dispose(disposing);
		}
	}
}
