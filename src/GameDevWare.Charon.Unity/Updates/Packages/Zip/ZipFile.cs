using System;
using System.Collections;
using System.IO;
using GameDevWare.Charon.Unity.Updates.Packages.Zip.Checksum;
using GameDevWare.Charon.Unity.Updates.Packages.Zip.Compression;
using GameDevWare.Charon.Unity.Updates.Packages.Zip.Compression.Streams;

namespace GameDevWare.Charon.Unity.Updates.Packages.Zip
{
	/// <summary>
	/// This class represents a Zip archive.  You can ask for the contained
	/// entries, or get an input stream for a file entry.  The entry is
	/// automatically decompressed.
	///
	/// You can also update the archive adding or deleting entries.
	///
	/// This class is thread safe for input:  You can open input streams for arbitrary
	/// entries in different threads.
	/// <br/>
	/// <br/>Author of the original java version : Jochen Hoenicke
	/// </summary>
	/// <example>
	/// <code>
	/// using System;
	/// using System.Text;
	/// using System.Collections;
	/// using System.IO;
	///
	/// using ICSharpCode.SharpZipLib.Zip;
	///
	/// class MainClass
	/// {
	/// 	static public void Main(string[] args)
	/// 	{
	/// 		using (ZipFile zFile = new ZipFile(args[0])) {
	/// 			Console.WriteLine("Listing of : " + zFile.Name);
	/// 			Console.WriteLine("");
	/// 			Console.WriteLine("Raw Size    Size      Date     Time     Name");
	/// 			Console.WriteLine("--------  --------  --------  ------  ---------");
	/// 			foreach (ZipEntry e in zFile) {
	/// 				if ( e.IsFile ) {
	/// 					DateTime d = e.DateTime;
	/// 					Console.WriteLine("{0, -10}{1, -10}{2}  {3}   {4}", e.Size, e.CompressedSize,
	/// 						d.ToString("dd-MM-yy"), d.ToString("HH:mm"),
	/// 						e.Name);
	/// 				}
	/// 			}
	/// 		}
	/// 	}
	/// }
	/// </code>
	/// </example>
	internal class ZipFile : IEnumerable, IDisposable
	{
		#region Constructors

		/// <summary>
		/// Opens a Zip file with the given name for reading.
		/// </summary>
		/// <param name="name">The name of the file to open.</param>
		/// <exception cref="ArgumentNullException">The argument supplied is null.</exception>
		/// <exception cref="IOException">
		/// An i/o error occurs
		/// </exception>
		/// <exception cref="ZipException">
		/// The file doesn't contain a valid zip archive.
		/// </exception>
		public ZipFile(string name)
		{
			if (name == null) throw new ArgumentNullException("name");

			this.name_ = name;

			this.baseStream_ = File.Open(name, FileMode.Open, FileAccess.Read, FileShare.Read);
			this.isStreamOwner = true;

			try
			{
				this.ReadEntries();
			}
			catch
			{
				this.DisposeInternal(true);
				throw;
			}
		}

		/// <summary>
		/// Opens a Zip file reading the given <see cref="FileStream"/>.
		/// </summary>
		/// <param name="file">The <see cref="FileStream"/> to read archive data from.</param>
		/// <exception cref="ArgumentNullException">The supplied argument is null.</exception>
		/// <exception cref="IOException">
		/// An i/o error occurs.
		/// </exception>
		/// <exception cref="ZipException">
		/// The file doesn't contain a valid zip archive.
		/// </exception>
		public ZipFile(FileStream file) :
			this(file, false)
		{

		}

		/// <summary>
		/// Opens a Zip file reading the given <see cref="FileStream"/>.
		/// </summary>
		/// <param name="file">The <see cref="FileStream"/> to read archive data from.</param>
		/// <param name="leaveOpen">true to leave the <see cref="FileStream">file</see> open when the ZipFile is disposed, false to dispose of it</param>
		/// <exception cref="ArgumentNullException">The supplied argument is null.</exception>
		/// <exception cref="IOException">
		/// An i/o error occurs.
		/// </exception>
		/// <exception cref="ZipException">
		/// The file doesn't contain a valid zip archive.
		/// </exception>
		public ZipFile(FileStream file, bool leaveOpen)
		{
			if (file == null)
			{
				throw new ArgumentNullException("file");
			}

			if (!file.CanSeek)
			{
				throw new ArgumentException("Stream is not seekable", "file");
			}

			this.baseStream_ = file;
			this.name_ = file.Name;
			this.isStreamOwner = !leaveOpen;

			try
			{
				this.ReadEntries();
			}
			catch
			{
				this.DisposeInternal(true);
				throw;
			}
		}

		/// <summary>
		/// Opens a Zip file reading the given <see cref="Stream"/>.
		/// </summary>
		/// <param name="stream">The <see cref="Stream"/> to read archive data from.</param>
		/// <exception cref="IOException">
		/// An i/o error occurs
		/// </exception>
		/// <exception cref="ZipException">
		/// The stream doesn't contain a valid zip archive.<br/>
		/// </exception>
		/// <exception cref="ArgumentException">
		/// The <see cref="Stream">stream</see> doesnt support seeking.
		/// </exception>
		/// <exception cref="ArgumentNullException">
		/// The <see cref="Stream">stream</see> argument is null.
		/// </exception>
		public ZipFile(Stream stream) :
			this(stream, false)
		{

		}

		/// <summary>
		/// Opens a Zip file reading the given <see cref="Stream"/>.
		/// </summary>
		/// <param name="stream">The <see cref="Stream"/> to read archive data from.</param>
		/// <param name="leaveOpen">true to leave the <see cref="Stream">stream</see> open when the ZipFile is disposed, false to dispose of it</param>
		/// <exception cref="IOException">
		/// An i/o error occurs
		/// </exception>
		/// <exception cref="ZipException">
		/// The stream doesn't contain a valid zip archive.<br/>
		/// </exception>
		/// <exception cref="ArgumentException">
		/// The <see cref="Stream">stream</see> doesnt support seeking.
		/// </exception>
		/// <exception cref="ArgumentNullException">
		/// The <see cref="Stream">stream</see> argument is null.
		/// </exception>
		public ZipFile(Stream stream, bool leaveOpen)
		{
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}

			if (!stream.CanSeek)
			{
				throw new ArgumentException("Stream is not seekable", "stream");
			}

			this.baseStream_ = stream;
			this.isStreamOwner = !leaveOpen;

			if (this.baseStream_.Length > 0)
			{
				try
				{
					this.ReadEntries();
				}
				catch
				{
					this.DisposeInternal(true);
					throw;
				}
			}
			else
			{
				this.entries_ = new ZipEntry[0];
				this.isNewArchive_ = true;
			}
		}

		/// <summary>
		/// Initialises a default <see cref="ZipFile"/> instance with no entries and no file storage.
		/// </summary>
		internal ZipFile()
		{
			this.entries_ = new ZipEntry[0];
			this.isNewArchive_ = true;
		}

		#endregion Constructors

		#region Destructors and Closing

		/// <summary>
		/// Finalize this instance.
		/// </summary>
		~ZipFile()
		{
			this.Dispose(false);
		}

		/// <summary>
		/// Closes the ZipFile.  If the stream is <see cref="IsStreamOwner">owned</see> then this also closes the underlying input stream.
		/// Once closed, no further instance methods should be called.
		/// </summary>
		/// <exception cref="System.IO.IOException">
		/// An i/o error occurs.
		/// </exception>
		public void Close()
		{
			this.DisposeInternal(true);
			GC.SuppressFinalize(this);
		}

		#endregion Destructors and Closing

		#region Creators

		/// <summary>
		/// Create a new <see cref="ZipFile"/> whose data will be stored in a file.
		/// </summary>
		/// <param name="fileName">The name of the archive to create.</param>
		/// <returns>Returns the newly created <see cref="ZipFile"/></returns>
		/// <exception cref="ArgumentNullException"><paramref name="fileName"></paramref> is null</exception>
		public static ZipFile Create(string fileName)
		{
			if (fileName == null)
			{
				throw new ArgumentNullException("fileName");
			}

			FileStream fs = File.Create(fileName);

			return new ZipFile
			{
				name_ = fileName,
				baseStream_ = fs,
				isStreamOwner = true
			};
		}

		/// <summary>
		/// Create a new <see cref="ZipFile"/> whose data will be stored on a stream.
		/// </summary>
		/// <param name="outStream">The stream providing data storage.</param>
		/// <returns>Returns the newly created <see cref="ZipFile"/></returns>
		/// <exception cref="ArgumentNullException"><paramref name="outStream"> is null</paramref></exception>
		/// <exception cref="ArgumentException"><paramref name="outStream"> doesnt support writing.</paramref></exception>
		public static ZipFile Create(Stream outStream)
		{
			if (outStream == null)
			{
				throw new ArgumentNullException("outStream");
			}

			if (!outStream.CanWrite)
			{
				throw new ArgumentException("Stream is not writeable", "outStream");
			}

			if (!outStream.CanSeek)
			{
				throw new ArgumentException("Stream is not seekable", "outStream");
			}

			var result = new ZipFile
			{
				baseStream_ = outStream
			};
			return result;
		}

		#endregion Creators

		#region Properties

		/// <summary>
		/// Get/set a flag indicating if the underlying stream is owned by the ZipFile instance.
		/// If the flag is true then the stream will be closed when <see cref="Close">Close</see> is called.
		/// </summary>
		/// <remarks>
		/// The default value is true in all cases.
		/// </remarks>
		public bool IsStreamOwner
		{
			get { return this.isStreamOwner; }
			set { this.isStreamOwner = value; }
		}

		/// <summary>
		/// Get a value indicating wether
		/// this archive is embedded in another file or not.
		/// </summary>
		public bool IsEmbeddedArchive
		{
			// Not strictly correct in all circumstances currently
			get { return this.offsetOfFirstEntry > 0; }
		}

		/// <summary>
		/// Get a value indicating that this archive is a new one.
		/// </summary>
		public bool IsNewArchive
		{
			get { return this.isNewArchive_; }
		}

		/// <summary>
		/// Gets the comment for the zip file.
		/// </summary>
		public string ZipFileComment
		{
			get { return this.comment_; }
		}

		/// <summary>
		/// Gets the name of this zip file.
		/// </summary>
		public string Name
		{
			get { return this.name_; }
		}

		/// <summary>
		/// Gets the number of entries in this zip file.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// The Zip file has been closed.
		/// </exception>
		[Obsolete("Use the Count property instead")]
		public int Size
		{
			get
			{
				return this.entries_.Length;
			}
		}

		/// <summary>
		/// Get the number of entries contained in this <see cref="ZipFile"/>.
		/// </summary>
		public long Count
		{
			get
			{
				return this.entries_.Length;
			}
		}

		/// <summary>
		/// Indexer property for ZipEntries
		/// </summary>
		[System.Runtime.CompilerServices.IndexerNameAttribute("EntryByIndex")]
		public ZipEntry this[int index]
		{
			get
			{
				return (ZipEntry)this.entries_[index].Clone();
			}
		}

		#endregion Properties

		#region Input Handling

		/// <summary>
		/// Gets an enumerator for the Zip entries in this Zip file.
		/// </summary>
		/// <returns>Returns an <see cref="IEnumerator"/> for this archive.</returns>
		/// <exception cref="ObjectDisposedException">
		/// The Zip file has been closed.
		/// </exception>
		public IEnumerator GetEnumerator()
		{
			if (this.isDisposed_)
			{
				throw new ObjectDisposedException("ZipFile");
			}

			return new ZipEntryEnumerator(this.entries_);
		}

		/// <summary>
		/// Return the index of the entry with a matching name
		/// </summary>
		/// <param name="name">Entry name to find</param>
		/// <param name="ignoreCase">If true the comparison is case insensitive</param>
		/// <returns>The index position of the matching entry or -1 if not found</returns>
		/// <exception cref="ObjectDisposedException">
		/// The Zip file has been closed.
		/// </exception>
		public int FindEntry(string name, bool ignoreCase)
		{
			if (this.isDisposed_)
			{
				throw new ObjectDisposedException("ZipFile");
			}

			// TODO: This will be slow as the next ice age for huge archives!
			for (int i = 0; i < this.entries_.Length; i++)
			{
				if (string.Compare(name, this.entries_[i].Name, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) == 0)
				{
					return i;
				}
			}
			return -1;
		}

		/// <summary>
		/// Searches for a zip entry in this archive with the given name.
		/// String comparisons are case insensitive
		/// </summary>
		/// <param name="name">
		/// The name to find. May contain directory components separated by slashes ('/').
		/// </param>
		/// <returns>
		/// A clone of the zip entry, or null if no entry with that name exists.
		/// </returns>
		/// <exception cref="ObjectDisposedException">
		/// The Zip file has been closed.
		/// </exception>
		public ZipEntry GetEntry(string name)
		{
			if (this.isDisposed_)
			{
				throw new ObjectDisposedException("ZipFile");
			}

			int index = this.FindEntry(name, true);
			return (index >= 0) ? (ZipEntry)this.entries_[index].Clone() : null;
		}

		/// <summary>
		/// Gets an input stream for reading the given zip entry data in an uncompressed form.
		/// Normally the <see cref="ZipEntry"/> should be an entry returned by GetEntry().
		/// </summary>
		/// <param name="entry">The <see cref="ZipEntry"/> to obtain a data <see cref="Stream"/> for</param>
		/// <returns>An input <see cref="Stream"/> containing data for this <see cref="ZipEntry"/></returns>
		/// <exception cref="ObjectDisposedException">
		/// The ZipFile has already been closed
		/// </exception>
		/// <exception cref="ZipException">
		/// The compression method for the entry is unknown
		/// </exception>
		/// <exception cref="IndexOutOfRangeException">
		/// The entry is not found in the ZipFile
		/// </exception>
		public Stream GetInputStream(ZipEntry entry)
		{
			if (entry == null)
			{
				throw new ArgumentNullException("entry");
			}

			if (this.isDisposed_)
			{
				throw new ObjectDisposedException("ZipFile");
			}

			long index = entry.ZipFileIndex;
			if ((index < 0) || (index >= this.entries_.Length) || (this.entries_[index].Name != entry.Name))
			{
				index = this.FindEntry(entry.Name, true);
				if (index < 0)
				{
					throw new ZipException("Entry cannot be found");
				}
			}
			return this.GetInputStream(index);
		}

		/// <summary>
		/// Creates an input stream reading a zip entry
		/// </summary>
		/// <param name="entryIndex">The index of the entry to obtain an input stream for.</param>
		/// <returns>
		/// An input <see cref="Stream"/> containing data for this <paramref name="entryIndex"/>
		/// </returns>
		/// <exception cref="ObjectDisposedException">
		/// The ZipFile has already been closed
		/// </exception>
		/// <exception cref="ZipException">
		/// The compression method for the entry is unknown
		/// </exception>
		/// <exception cref="IndexOutOfRangeException">
		/// The entry is not found in the ZipFile
		/// </exception>
		public Stream GetInputStream(long entryIndex)
		{
			if (this.isDisposed_)
			{
				throw new ObjectDisposedException("ZipFile");
			}

			long start = this.LocateEntry(this.entries_[entryIndex]);
			CompressionMethod method = this.entries_[entryIndex].CompressionMethod;
			Stream result = new PartialInputStream(this, start, this.entries_[entryIndex].CompressedSize);

			if (this.entries_[entryIndex].IsCrypted == true)
			{
				result = this.CreateAndInitDecryptionStream(result, this.entries_[entryIndex]);
				if (result == null)
				{
					throw new ZipException("Unable to decrypt this entry");
				}
			}

			switch (method)
			{
				case CompressionMethod.Stored:
					// read as is.
					break;

				case CompressionMethod.Deflated:
					// No need to worry about ownership and closing as underlying stream close does nothing.
					result = new InflaterInputStream(result, new Inflater(true));
					break;

				default:
					throw new ZipException("Unsupported compression method " + method);
			}

			return result;
		}

		#endregion Input Handling

		#region Archive Testing

		/// <summary>
		/// Test an archive for integrity/validity
		/// </summary>
		/// <param name="testData">Perform low level data Crc check</param>
		/// <returns>true if all tests pass, false otherwise</returns>
		/// <remarks>Testing will terminate on the first error found.</remarks>
		public bool TestArchive(bool testData)
		{
			return this.TestArchive(testData, TestStrategy.FindFirstError, null);
		}

		/// <summary>
		/// Test an archive for integrity/validity
		/// </summary>
		/// <param name="testData">Perform low level data Crc check</param>
		/// <param name="strategy">The <see cref="TestStrategy"></see> to apply.</param>
		/// <param name="resultHandler">The <see cref="ZipTestResultHandler"></see> handler to call during testing.</param>
		/// <returns>true if all tests pass, false otherwise</returns>
		/// <exception cref="ObjectDisposedException">The object has already been closed.</exception>
		public bool TestArchive(bool testData, TestStrategy strategy, ZipTestResultHandler resultHandler)
		{
			if (this.isDisposed_)
			{
				throw new ObjectDisposedException("ZipFile");
			}

			var status = new TestStatus(this);

			if (resultHandler != null)
				resultHandler.Invoke(status, null);

			HeaderTest test = testData ? (HeaderTest.Header | HeaderTest.Extract) : HeaderTest.Header;

			bool testing = true;

			try
			{
				int entryIndex = 0;

				while (testing && (entryIndex < this.Count))
				{
					if (resultHandler != null)
					{
						status.SetEntry(this[entryIndex]);
						status.SetOperation(TestOperation.EntryHeader);
						resultHandler(status, null);
					}

					try
					{
						this.TestLocalHeader(this[entryIndex], test);
					}
					catch (ZipException ex)
					{
						status.AddError();
						if (resultHandler != null)
							resultHandler.Invoke(status, string.Format("Exception during test - '{0}'", ex.Message));

						testing &= strategy != TestStrategy.FindFirstError;
					}

					if (testing && testData && this[entryIndex].IsFile)
					{
						if (resultHandler != null)
						{
							status.SetOperation(TestOperation.EntryData);
							resultHandler(status, null);
						}

						var crc = new Crc32();

						using (Stream entryStream = this.GetInputStream(this[entryIndex]))
						{
							byte[] buffer = new byte[4096];
							long totalBytes = 0;
							int bytesRead;
							while ((bytesRead = entryStream.Read(buffer, 0, buffer.Length)) > 0)
							{
								crc.Update(new ArraySegment<byte>(buffer, 0, bytesRead));

								if (resultHandler != null)
								{
									totalBytes += bytesRead;
									status.SetBytesTested(totalBytes);
									resultHandler(status, null);
								}
							}
						}

						if (this[entryIndex].Crc != crc.Value)
						{
							status.AddError();

							if (resultHandler != null)
								resultHandler.Invoke(status, "CRC mismatch");

							testing &= strategy != TestStrategy.FindFirstError;
						}

						if ((this[entryIndex].Flags & (int)GeneralBitFlags.Descriptor) != 0)
						{
							var helper = new ZipHelperStream(this.baseStream_);
							var data = new DescriptorData();
							helper.ReadDataDescriptor(this[entryIndex].LocalHeaderRequiresZip64, data);
							if (this[entryIndex].Crc != data.Crc)
							{
								status.AddError();
							}

							if (this[entryIndex].CompressedSize != data.CompressedSize)
							{
								status.AddError();
							}

							if (this[entryIndex].Size != data.Size)
							{
								status.AddError();
							}
						}
					}

					if (resultHandler != null)
					{
						status.SetOperation(TestOperation.EntryComplete);
						resultHandler(status, null);
					}

					entryIndex += 1;
				}

				if (resultHandler != null)
				{
					status.SetOperation(TestOperation.MiscellaneousTests);
					resultHandler(status, null);
				}

				// TODO: the 'Corrina Johns' test where local headers are missing from
				// the central directory.  They are therefore invisible to many archivers.
			}
			catch (Exception ex)
			{
				status.AddError();

				if (resultHandler != null)
					resultHandler.Invoke(status, string.Format("Exception during test - '{0}'", ex.Message));
			}

			if (resultHandler != null)
			{
				status.SetOperation(TestOperation.Complete);
				status.SetEntry(null);
				resultHandler(status, null);
			}

			return (status.ErrorCount == 0);
		}

		[Flags]
		private enum HeaderTest
		{
			Extract = 0x01,     // Check that this header represents an entry whose data can be extracted
			Header = 0x02,     // Check that this header contents are valid
		}

		/// <summary>
		/// Test a local header against that provided from the central directory
		/// </summary>
		/// <param name="entry">
		/// The entry to test against
		/// </param>
		/// <param name="tests">The type of <see cref="HeaderTest">tests</see> to carry out.</param>
		/// <returns>The offset of the entries data in the file</returns>
		private long TestLocalHeader(ZipEntry entry, HeaderTest tests)
		{
			lock (this.baseStream_)
			{
				bool testHeader = (tests & HeaderTest.Header) != 0;
				bool testData = (tests & HeaderTest.Extract) != 0;

				this.baseStream_.Seek(this.offsetOfFirstEntry + entry.Offset, SeekOrigin.Begin);
				if ((int)this.ReadLEUint() != ZipConstants.LocalHeaderSignature)
				{
					throw new ZipException(string.Format("Wrong local header signature @{0:X}", this.offsetOfFirstEntry + entry.Offset));
				}

				var extractVersion = (short)(this.ReadLEUshort() & 0x00ff);
				var localFlags = (short)this.ReadLEUshort();
				var compressionMethod = (short)this.ReadLEUshort();
				var fileTime = (short)this.ReadLEUshort();
				var fileDate = (short)this.ReadLEUshort();
				uint crcValue = this.ReadLEUint();
				long compressedSize = this.ReadLEUint();
				long size = this.ReadLEUint();
				int storedNameLength = this.ReadLEUshort();
				int extraDataLength = this.ReadLEUshort();

				byte[] nameData = new byte[storedNameLength];
				StreamUtils.ReadFully(this.baseStream_, nameData);

				byte[] extraData = new byte[extraDataLength];
				StreamUtils.ReadFully(this.baseStream_, extraData);

				var localExtraData = new ZipExtraData(extraData);

				// Extra data / zip64 checks
				if (localExtraData.Find(1))
				{
					// 2010-03-04 Forum 10512: removed checks for version >= ZipConstants.VersionZip64
					// and size or compressedSize = MaxValue, due to rogue creators.

					size = localExtraData.ReadLong();
					compressedSize = localExtraData.ReadLong();

					if ((localFlags & (int)GeneralBitFlags.Descriptor) != 0)
					{
						// These may be valid if patched later
						if ((size != -1) && (size != entry.Size))
						{
							throw new ZipException("Size invalid for descriptor");
						}

						if ((compressedSize != -1) && (compressedSize != entry.CompressedSize))
						{
							throw new ZipException("Compressed size invalid for descriptor");
						}
					}
				}
				else
				{
					// No zip64 extra data but entry requires it.
					if ((extractVersion >= ZipConstants.VersionZip64) &&
						(((uint)size == uint.MaxValue) || ((uint)compressedSize == uint.MaxValue)))
					{
						throw new ZipException("Required Zip64 extended information missing");
					}
				}

				if (testData)
				{
					if (entry.IsFile)
					{
						if (!entry.IsCompressionMethodSupported())
						{
							throw new ZipException("Compression method not supported");
						}

						if ((extractVersion > ZipConstants.VersionMadeBy)
							|| ((extractVersion > 20) && (extractVersion < ZipConstants.VersionZip64)))
						{
							throw new ZipException(string.Format("Version required to extract this entry not supported ({0})", extractVersion));
						}

						if ((localFlags & (int)(GeneralBitFlags.Patched | GeneralBitFlags.StrongEncryption | GeneralBitFlags.EnhancedCompress | GeneralBitFlags.HeaderMasked)) != 0)
						{
							throw new ZipException("The library does not support the zip version required to extract this entry");
						}
					}
				}

				if (testHeader)
				{
					if ((extractVersion <= 63) &&   // Ignore later versions as we dont know about them..
						(extractVersion != 10) &&
						(extractVersion != 11) &&
						(extractVersion != 20) &&
						(extractVersion != 21) &&
						(extractVersion != 25) &&
						(extractVersion != 27) &&
						(extractVersion != 45) &&
						(extractVersion != 46) &&
						(extractVersion != 50) &&
						(extractVersion != 51) &&
						(extractVersion != 52) &&
						(extractVersion != 61) &&
						(extractVersion != 62) &&
						(extractVersion != 63)
						)
					{
						throw new ZipException(string.Format("Version required to extract this entry is invalid ({0})", extractVersion));
					}

					// Local entry flags dont have reserved bit set on.
					if ((localFlags & (int)(GeneralBitFlags.ReservedPKware4 | GeneralBitFlags.ReservedPkware14 | GeneralBitFlags.ReservedPkware15)) != 0)
					{
						throw new ZipException("Reserved bit flags cannot be set.");
					}

					// Encryption requires extract version >= 20
					if (((localFlags & (int)GeneralBitFlags.Encrypted) != 0) && (extractVersion < 20))
					{
						throw new ZipException(string.Format("Version required to extract this entry is too low for encryption ({0})", extractVersion));
					}

					// Strong encryption requires encryption flag to be set and extract version >= 50.
					if ((localFlags & (int)GeneralBitFlags.StrongEncryption) != 0)
					{
						if ((localFlags & (int)GeneralBitFlags.Encrypted) == 0)
						{
							throw new ZipException("Strong encryption flag set but encryption flag is not set");
						}

						if (extractVersion < 50)
						{
							throw new ZipException(string.Format("Version required to extract this entry is too low for encryption ({0})", extractVersion));
						}
					}

					// Patched entries require extract version >= 27
					if (((localFlags & (int)GeneralBitFlags.Patched) != 0) && (extractVersion < 27))
					{
						throw new ZipException(string.Format("Patched data requires higher version than ({0})", extractVersion));
					}

					// Central header flags match local entry flags.
					if (localFlags != entry.Flags)
					{
						throw new ZipException("Central header/local header flags mismatch");
					}

					// Central header compression method matches local entry
					if (entry.CompressionMethod != (CompressionMethod)compressionMethod)
					{
						throw new ZipException("Central header/local header compression method mismatch");
					}

					if (entry.Version != extractVersion)
					{
						throw new ZipException("Extract version mismatch");
					}

					// Strong encryption and extract version match
					if ((localFlags & (int)GeneralBitFlags.StrongEncryption) != 0)
					{
						if (extractVersion < 62)
						{
							throw new ZipException("Strong encryption flag set but version not high enough");
						}
					}

					if ((localFlags & (int)GeneralBitFlags.HeaderMasked) != 0)
					{
						if ((fileTime != 0) || (fileDate != 0))
						{
							throw new ZipException("Header masked set but date/time values non-zero");
						}
					}

					if ((localFlags & (int)GeneralBitFlags.Descriptor) == 0)
					{
						if (crcValue != (uint)entry.Crc)
						{
							throw new ZipException("Central header/local header crc mismatch");
						}
					}

					// Crc valid for empty entry.
					// This will also apply to streamed entries where size isnt known and the header cant be patched
					if ((size == 0) && (compressedSize == 0))
					{
						if (crcValue != 0)
						{
							throw new ZipException("Invalid CRC for empty entry");
						}
					}

					// TODO: make test more correct...  can't compare lengths as was done originally as this can fail for MBCS strings
					// Assuming a code page at this point is not valid?  Best is to store the name length in the ZipEntry probably
					if (entry.Name.Length > storedNameLength)
					{
						throw new ZipException("File name length mismatch");
					}

					// Name data has already been read convert it and compare.
					string localName = ZipStrings.ConvertToStringExt(localFlags, nameData);

					// Central directory and local entry name match
					if (localName != entry.Name)
					{
						throw new ZipException("Central header and local header file name mismatch");
					}

					// Directories have zero actual size but can have compressed size
					if (entry.IsDirectory)
					{
						if (size > 0)
						{
							throw new ZipException("Directory cannot have size");
						}

						// There may be other cases where the compressed size can be greater than this?
						// If so until details are known we will be strict.
						if (entry.IsCrypted)
						{
							if (compressedSize > ZipConstants.CryptoHeaderSize + 2)
							{
								throw new ZipException("Directory compressed size invalid");
							}
						}
						else if (compressedSize > 2)
						{
							// When not compressed the directory size can validly be 2 bytes
							// if the true size wasnt known when data was originally being written.
							// NOTE: Versions of the library 0.85.4 and earlier always added 2 bytes
							throw new ZipException("Directory compressed size invalid");
						}
					}
				}

				// Tests that apply to both data and header.

				// Size can be verified only if it is known in the local header.
				// it will always be known in the central header.
				if (((localFlags & (int)GeneralBitFlags.Descriptor) == 0) ||
					((size > 0 || compressedSize > 0) && entry.Size > 0))
				{
					if ((size != 0)
						&& (size != entry.Size))
					{
						throw new ZipException(
							string.Format("Size mismatch between central header({0}) and local header({1})",
								entry.Size, size));
					}

					if ((compressedSize != 0)
						&& (compressedSize != entry.CompressedSize && compressedSize != 0xFFFFFFFF && compressedSize != -1))
					{
						throw new ZipException(
							string.Format("Compressed size mismatch between central header({0}) and local header({1})",
							entry.CompressedSize, compressedSize));
					}
				}

				int extraLength = storedNameLength + extraDataLength;
				return this.offsetOfFirstEntry + entry.Offset + ZipConstants.LocalHeaderBaseSize + extraLength;
			}
		}

		#endregion Archive Testing

		private const int DefaultBufferSize = 4096;

		/// <summary>
		/// The kind of update to apply.
		/// </summary>
		private enum UpdateCommand
		{
			Copy,       // Copy original file contents.
			Modify,     // Change encryption, compression, attributes, name, time etc, of an existing file.
			Add,        // Add a new file to the archive.
		}

		#region Properties

		/// <summary>
		/// Get / set a value indicating how Zip64 Extension usage is determined when adding entries.
		/// </summary>
		public UseZip64 UseZip64
		{
			get { return this.useZip64_; }
			set { this.useZip64_ = value; }
		}

		#endregion Properties


		#region Disposing

		#region IDisposable Members

		void IDisposable.Dispose()
		{
			this.Close();
		}

		#endregion IDisposable Members

		private void DisposeInternal(bool disposing)
		{
			if (!this.isDisposed_)
			{
				this.isDisposed_ = true;
				this.entries_ = new ZipEntry[0];

				if (this.IsStreamOwner && (this.baseStream_ != null))
				{
					lock (this.baseStream_)
					{
						this.baseStream_.Dispose();
					}
				}
			}
		}

		/// <summary>
		/// Releases the unmanaged resources used by the this instance and optionally releases the managed resources.
		/// </summary>
		/// <param name="disposing">true to release both managed and unmanaged resources;
		/// false to release only unmanaged resources.</param>
		protected virtual void Dispose(bool disposing)
		{
			this.DisposeInternal(disposing);
		}

		#endregion Disposing

		#region Internal routines

		#region Reading

		/// <summary>
		/// Read an unsigned short in little endian byte order.
		/// </summary>
		/// <returns>Returns the value read.</returns>
		/// <exception cref="EndOfStreamException">
		/// The stream ends prematurely
		/// </exception>
		private ushort ReadLEUshort()
		{
			int data1 = this.baseStream_.ReadByte();

			if (data1 < 0)
			{
				throw new EndOfStreamException("End of stream");
			}

			int data2 = this.baseStream_.ReadByte();

			if (data2 < 0)
			{
				throw new EndOfStreamException("End of stream");
			}

			return unchecked((ushort)((ushort)data1 | (ushort)(data2 << 8)));
		}

		/// <summary>
		/// Read a uint in little endian byte order.
		/// </summary>
		/// <returns>Returns the value read.</returns>
		/// <exception cref="IOException">
		/// An i/o error occurs.
		/// </exception>
		/// <exception cref="System.IO.EndOfStreamException">
		/// The file ends prematurely
		/// </exception>
		private uint ReadLEUint()
		{
			return (uint)(this.ReadLEUshort() | (this.ReadLEUshort() << 16));
		}

		private ulong ReadLEUlong()
		{
			return this.ReadLEUint() | ((ulong)this.ReadLEUint() << 32);
		}

		#endregion Reading

		// NOTE this returns the offset of the first byte after the signature.
		private long LocateBlockWithSignature(int signature, long endLocation, int minimumBlockSize, int maximumVariableData)
		{
			using (ZipHelperStream les = new ZipHelperStream(this.baseStream_))
			{
				return les.LocateBlockWithSignature(signature, endLocation, minimumBlockSize, maximumVariableData);
			}
		}

		/// <summary>
		/// Search for and read the central directory of a zip file filling the entries array.
		/// </summary>
		/// <exception cref="System.IO.IOException">
		/// An i/o error occurs.
		/// </exception>
		/// <exception cref="ZipException">
		/// The central directory is malformed or cannot be found
		/// </exception>
		private void ReadEntries()
		{
			// Search for the End Of Central Directory.  When a zip comment is
			// present the directory will start earlier
			//
			// The search is limited to 64K which is the maximum size of a trailing comment field to aid speed.
			// This should be compatible with both SFX and ZIP files but has only been tested for Zip files
			// If a SFX file has the Zip data attached as a resource and there are other resources occuring later then
			// this could be invalid.
			// Could also speed this up by reading memory in larger blocks.

			if (this.baseStream_.CanSeek == false)
			{
				throw new ZipException("ZipFile stream must be seekable");
			}

			long locatedEndOfCentralDir = this.LocateBlockWithSignature(ZipConstants.EndOfCentralDirectorySignature,
				this.baseStream_.Length, ZipConstants.EndOfCentralRecordBaseSize, 0xffff);

			if (locatedEndOfCentralDir < 0)
			{
				throw new ZipException("Cannot find central directory");
			}

			// Read end of central directory record
			ushort thisDiskNumber = this.ReadLEUshort();
			ushort startCentralDirDisk = this.ReadLEUshort();
			ulong entriesForThisDisk = this.ReadLEUshort();
			ulong entriesForWholeCentralDir = this.ReadLEUshort();
			ulong centralDirSize = this.ReadLEUint();
			long offsetOfCentralDir = this.ReadLEUint();
			uint commentSize = this.ReadLEUshort();

			if (commentSize > 0)
			{
				byte[] comment = new byte[commentSize];

				StreamUtils.ReadFully(this.baseStream_, comment);
				this.comment_ = ZipStrings.ConvertToString(comment);
			}
			else
			{
				this.comment_ = string.Empty;
			}

			bool isZip64 = false;

			// Check if zip64 header information is required.
			if ((thisDiskNumber == 0xffff) ||
				(startCentralDirDisk == 0xffff) ||
				(entriesForThisDisk == 0xffff) ||
				(entriesForWholeCentralDir == 0xffff) ||
				(centralDirSize == 0xffffffff) ||
				(offsetOfCentralDir == 0xffffffff))
			{
				isZip64 = true;

				long offset = this.LocateBlockWithSignature(ZipConstants.Zip64CentralDirLocatorSignature, locatedEndOfCentralDir, 0, 0x1000);
				if (offset < 0)
				{
					throw new ZipException("Cannot find Zip64 locator");
				}

				// number of the disk with the start of the zip64 end of central directory 4 bytes
				// relative offset of the zip64 end of central directory record 8 bytes
				// total number of disks 4 bytes
				this.ReadLEUint(); // startDisk64 is not currently used
				ulong offset64 = this.ReadLEUlong();
				uint totalDisks = this.ReadLEUint();

				this.baseStream_.Position = (long)offset64;
				long sig64 = this.ReadLEUint();

				if (sig64 != ZipConstants.Zip64CentralFileHeaderSignature)
				{
					throw new ZipException(string.Format("Invalid Zip64 Central directory signature at {0:X}", offset64));
				}

				// NOTE: Record size = SizeOfFixedFields + SizeOfVariableData - 12.
				ulong recordSize = this.ReadLEUlong();
				int versionMadeBy = this.ReadLEUshort();
				int versionToExtract = this.ReadLEUshort();
				uint thisDisk = this.ReadLEUint();
				uint centralDirDisk = this.ReadLEUint();
				entriesForThisDisk = this.ReadLEUlong();
				entriesForWholeCentralDir = this.ReadLEUlong();
				centralDirSize = this.ReadLEUlong();
				offsetOfCentralDir = (long)this.ReadLEUlong();

				// NOTE: zip64 extensible data sector (variable size) is ignored.
			}

			this.entries_ = new ZipEntry[entriesForThisDisk];

			// SFX/embedded support, find the offset of the first entry vis the start of the stream
			// This applies to Zip files that are appended to the end of an SFX stub.
			// Or are appended as a resource to an executable.
			// Zip files created by some archivers have the offsets altered to reflect the true offsets
			// and so dont require any adjustment here...
			// TODO: Difficulty with Zip64 and SFX offset handling needs resolution - maths?
			if (!isZip64 && (offsetOfCentralDir < locatedEndOfCentralDir - (4 + (long)centralDirSize)))
			{
				this.offsetOfFirstEntry = locatedEndOfCentralDir - (4 + (long)centralDirSize + offsetOfCentralDir);
				if (this.offsetOfFirstEntry <= 0)
				{
					throw new ZipException("Invalid embedded zip archive");
				}
			}

			this.baseStream_.Seek(this.offsetOfFirstEntry + offsetOfCentralDir, SeekOrigin.Begin);

			for (ulong i = 0; i < entriesForThisDisk; i++)
			{
				if (this.ReadLEUint() != ZipConstants.CentralHeaderSignature)
				{
					throw new ZipException("Wrong Central Directory signature");
				}

				int versionMadeBy = this.ReadLEUshort();
				int versionToExtract = this.ReadLEUshort();
				int bitFlags = this.ReadLEUshort();
				int method = this.ReadLEUshort();
				uint dostime = this.ReadLEUint();
				uint crc = this.ReadLEUint();
				var csize = (long)this.ReadLEUint();
				var size = (long)this.ReadLEUint();
				int nameLen = this.ReadLEUshort();
				int extraLen = this.ReadLEUshort();
				int commentLen = this.ReadLEUshort();

				int diskStartNo = this.ReadLEUshort();  // Not currently used
				int internalAttributes = this.ReadLEUshort();  // Not currently used

				uint externalAttributes = this.ReadLEUint();
				long offset = this.ReadLEUint();

				byte[] buffer = new byte[Math.Max(nameLen, commentLen)];

				StreamUtils.ReadFully(this.baseStream_, buffer, 0, nameLen);
				string name = ZipStrings.ConvertToStringExt(bitFlags, buffer, nameLen);

				var entry = new ZipEntry(name, versionToExtract, versionMadeBy, (CompressionMethod)method)
				{
					Crc = crc & 0xffffffffL,
					Size = size & 0xffffffffL,
					CompressedSize = csize & 0xffffffffL,
					Flags = bitFlags,
					DosTime = dostime,
					ZipFileIndex = (long)i,
					Offset = offset,
					ExternalFileAttributes = (int)externalAttributes
				};

				if ((bitFlags & 8) == 0)
				{
					entry.CryptoCheckValue = (byte)(crc >> 24);
				}
				else
				{
					entry.CryptoCheckValue = (byte)((dostime >> 8) & 0xff);
				}

				if (extraLen > 0)
				{
					byte[] extra = new byte[extraLen];
					StreamUtils.ReadFully(this.baseStream_, extra);
					entry.ExtraData = extra;
				}

				entry.ProcessExtraData(false);

				if (commentLen > 0)
				{
					StreamUtils.ReadFully(this.baseStream_, buffer, 0, commentLen);
					entry.Comment = ZipStrings.ConvertToStringExt(bitFlags, buffer, commentLen);
				}

				this.entries_[i] = entry;
			}
		}

		/// <summary>
		/// Locate the data for a given entry.
		/// </summary>
		/// <returns>
		/// The start offset of the data.
		/// </returns>
		/// <exception cref="System.IO.EndOfStreamException">
		/// The stream ends prematurely
		/// </exception>
		/// <exception cref="ZipException">
		/// The local header signature is invalid, the entry and central header file name lengths are different
		/// or the local and entry compression methods dont match
		/// </exception>
		private long LocateEntry(ZipEntry entry)
		{
			return this.TestLocalHeader(entry, HeaderTest.Extract);
		}

		private Stream CreateAndInitDecryptionStream(Stream baseStream, ZipEntry entry)
		{
			return baseStream;
		}

		private Stream CreateAndInitEncryptionStream(Stream baseStream, ZipEntry entry)
		{
			return baseStream;
		}

		#endregion Internal routines

		#region Instance Fields

		private bool isDisposed_;
		private string name_;
		private string comment_;
		private string rawPassword_;
		private Stream baseStream_;
		private bool isStreamOwner;
		private long offsetOfFirstEntry;
		private ZipEntry[] entries_;
		private byte[] key;
		private bool isNewArchive_;

		// Default is dynamic which is not backwards compatible and can cause problems
		// with XP's built in compression which cant read Zip64 archives.
		// However it does avoid the situation were a large file is added and cannot be completed correctly.
		// Hint: Set always ZipEntry size before they are added to an archive and this setting isnt needed.
		private UseZip64 useZip64_ = UseZip64.Dynamic;

		#endregion Instance Fields

		#region Support Classes

		/// <summary>
		/// Represents a string from a <see cref="ZipFile"/> which is stored as an array of bytes.
		/// </summary>
		private class ZipString
		{
			#region Constructors

			/// <summary>
			/// Initialise a <see cref="ZipString"/> with a string.
			/// </summary>
			/// <param name="comment">The textual string form.</param>
			public ZipString(string comment)
			{
				this.comment_ = comment;
				this.isSourceString_ = true;
			}

			/// <summary>
			/// Initialise a <see cref="ZipString"/> using a string in its binary 'raw' form.
			/// </summary>
			/// <param name="rawString"></param>
			public ZipString(byte[] rawString)
			{
				this.rawComment_ = rawString;
			}

			#endregion Constructors

			/// <summary>
			/// Get a value indicating the original source of data for this instance.
			/// True if the source was a string; false if the source was binary data.
			/// </summary>
			public bool IsSourceString
			{
				get { return this.isSourceString_; }
			}

			/// <summary>
			/// Get the length of the comment when represented as raw bytes.
			/// </summary>
			public int RawLength
			{
				get
				{
					this.MakeBytesAvailable();
					return this.rawComment_.Length;
				}
			}

			/// <summary>
			/// Get the comment in its 'raw' form as plain bytes.
			/// </summary>
			public byte[] RawComment
			{
				get
				{
					this.MakeBytesAvailable();
					return (byte[])this.rawComment_.Clone();
				}
			}

			/// <summary>
			/// Reset the comment to its initial state.
			/// </summary>
			public void Reset()
			{
				if (this.isSourceString_)
				{
					this.rawComment_ = null;
				}
				else
				{
					this.comment_ = null;
				}
			}

			private void MakeTextAvailable()
			{
				if (this.comment_ == null)
				{
					this.comment_ = ZipStrings.ConvertToString(this.rawComment_);
				}
			}

			private void MakeBytesAvailable()
			{
				if (this.rawComment_ == null)
				{
					this.rawComment_ = ZipStrings.ConvertToArray(this.comment_);
				}
			}

			/// <summary>
			/// Implicit conversion of comment to a string.
			/// </summary>
			/// <param name="zipString">The <see cref="ZipString"/> to convert to a string.</param>
			/// <returns>The textual equivalent for the input value.</returns>
			static public implicit operator string(ZipString zipString)
			{
				zipString.MakeTextAvailable();
				return zipString.comment_;
			}

			#region Instance Fields

			private string comment_;
			private byte[] rawComment_;
			private readonly bool isSourceString_;

			#endregion Instance Fields
		}

		/// <summary>
		/// An <see cref="IEnumerator">enumerator</see> for <see cref="ZipEntry">Zip entries</see>
		/// </summary>
		private class ZipEntryEnumerator : IEnumerator
		{
			#region Constructors

			public ZipEntryEnumerator(ZipEntry[] entries)
			{
				this.array = entries;
			}

			#endregion Constructors

			#region IEnumerator Members

			public object Current
			{
				get
				{
					return this.array[this.index];
				}
			}

			public void Reset()
			{
				this.index = -1;
			}

			public bool MoveNext()
			{
				return (++this.index < this.array.Length);
			}

			#endregion IEnumerator Members

			#region Instance Fields

			private ZipEntry[] array;
			private int index = -1;

			#endregion Instance Fields
		}

		/// <summary>
		/// An <see cref="UncompressedStream"/> is a stream that you can write uncompressed data
		/// to and flush, but cannot read, seek or do anything else to.
		/// </summary>
		private class UncompressedStream : Stream
		{
			#region Constructors

			public UncompressedStream(Stream baseStream)
			{
				this.baseStream_ = baseStream;
			}

			#endregion Constructors

			/// <summary>
			/// Gets a value indicating whether the current stream supports reading.
			/// </summary>
			public override bool CanRead
			{
				get
				{
					return false;
				}
			}

			/// <summary>
			/// Write any buffered data to underlying storage.
			/// </summary>
			public override void Flush()
			{
				this.baseStream_.Flush();
			}

			/// <summary>
			/// Gets a value indicating whether the current stream supports writing.
			/// </summary>
			public override bool CanWrite
			{
				get
				{
					return this.baseStream_.CanWrite;
				}
			}

			/// <summary>
			/// Gets a value indicating whether the current stream supports seeking.
			/// </summary>
			public override bool CanSeek
			{
				get
				{
					return false;
				}
			}

			/// <summary>
			/// Get the length in bytes of the stream.
			/// </summary>
			public override long Length
			{
				get
				{
					return 0;
				}
			}

			/// <summary>
			/// Gets or sets the position within the current stream.
			/// </summary>
			public override long Position
			{
				get
				{
					return this.baseStream_.Position;
				}
				set
				{
					throw new NotImplementedException();
				}
			}

			/// <summary>
			/// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
			/// </summary>
			/// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
			/// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
			/// <param name="count">The maximum number of bytes to be read from the current stream.</param>
			/// <returns>
			/// The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.
			/// </returns>
			/// <exception cref="T:System.ArgumentException">The sum of offset and count is larger than the buffer length. </exception>
			/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
			/// <exception cref="T:System.NotSupportedException">The stream does not support reading. </exception>
			/// <exception cref="T:System.ArgumentNullException">buffer is null. </exception>
			/// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
			/// <exception cref="T:System.ArgumentOutOfRangeException">offset or count is negative. </exception>
			public override int Read(byte[] buffer, int offset, int count)
			{
				return 0;
			}

			/// <summary>
			/// Sets the position within the current stream.
			/// </summary>
			/// <param name="offset">A byte offset relative to the origin parameter.</param>
			/// <param name="origin">A value of type <see cref="T:System.IO.SeekOrigin"></see> indicating the reference point used to obtain the new position.</param>
			/// <returns>
			/// The new position within the current stream.
			/// </returns>
			/// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
			/// <exception cref="T:System.NotSupportedException">The stream does not support seeking, such as if the stream is constructed from a pipe or console output. </exception>
			/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
			public override long Seek(long offset, SeekOrigin origin)
			{
				return 0;
			}

			/// <summary>
			/// Sets the length of the current stream.
			/// </summary>
			/// <param name="value">The desired length of the current stream in bytes.</param>
			/// <exception cref="T:System.NotSupportedException">The stream does not support both writing and seeking, such as if the stream is constructed from a pipe or console output. </exception>
			/// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
			/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
			public override void SetLength(long value)
			{
			}

			/// <summary>
			/// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
			/// </summary>
			/// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
			/// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
			/// <param name="count">The number of bytes to be written to the current stream.</param>
			/// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
			/// <exception cref="T:System.NotSupportedException">The stream does not support writing. </exception>
			/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
			/// <exception cref="T:System.ArgumentNullException">buffer is null. </exception>
			/// <exception cref="T:System.ArgumentException">The sum of offset and count is greater than the buffer length. </exception>
			/// <exception cref="T:System.ArgumentOutOfRangeException">offset or count is negative. </exception>
			public override void Write(byte[] buffer, int offset, int count)
			{
				this.baseStream_.Write(buffer, offset, count);
			}

			private readonly

			#region Instance Fields

			Stream baseStream_;

			#endregion Instance Fields
		}

		/// <summary>
		/// A <see cref="PartialInputStream"/> is an <see cref="InflaterInputStream"/>
		/// whose data is only a part or subsection of a file.
		/// </summary>
		private class PartialInputStream : Stream
		{
			#region Constructors

			/// <summary>
			/// Initialise a new instance of the <see cref="PartialInputStream"/> class.
			/// </summary>
			/// <param name="zipFile">The <see cref="ZipFile"/> containing the underlying stream to use for IO.</param>
			/// <param name="start">The start of the partial data.</param>
			/// <param name="length">The length of the partial data.</param>
			public PartialInputStream(ZipFile zipFile, long start, long length)
			{
				this.start_ = start;
				this.length_ = length;

				// Although this is the only time the zipfile is used
				// keeping a reference here prevents premature closure of
				// this zip file and thus the baseStream_.

				// Code like this will cause apparently random failures depending
				// on the size of the files and when garbage is collected.
				//
				// ZipFile z = new ZipFile (stream);
				// Stream reader = z.GetInputStream(0);
				// uses reader here....
				this.zipFile_ = zipFile;
				this.baseStream_ = this.zipFile_.baseStream_;
				this.readPos_ = start;
				this.end_ = start + length;
			}

			#endregion Constructors

			/// <summary>
			/// Read a byte from this stream.
			/// </summary>
			/// <returns>Returns the byte read or -1 on end of stream.</returns>
			public override int ReadByte()
			{
				if (this.readPos_ >= this.end_)
				{
					// -1 is the correct value at end of stream.
					return -1;
				}

				lock (this.baseStream_)
				{
					this.baseStream_.Seek(this.readPos_++, SeekOrigin.Begin);
					return this.baseStream_.ReadByte();
				}
			}

			/// <summary>
			/// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
			/// </summary>
			/// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
			/// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
			/// <param name="count">The maximum number of bytes to be read from the current stream.</param>
			/// <returns>
			/// The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.
			/// </returns>
			/// <exception cref="T:System.ArgumentException">The sum of offset and count is larger than the buffer length. </exception>
			/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
			/// <exception cref="T:System.NotSupportedException">The stream does not support reading. </exception>
			/// <exception cref="T:System.ArgumentNullException">buffer is null. </exception>
			/// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
			/// <exception cref="T:System.ArgumentOutOfRangeException">offset or count is negative. </exception>
			public override int Read(byte[] buffer, int offset, int count)
			{
				lock (this.baseStream_)
				{
					if (count > this.end_ - this.readPos_)
					{
						count = (int)(this.end_ - this.readPos_);
						if (count == 0)
						{
							return 0;
						}
					}
					// Protect against Stream implementations that throw away their buffer on every Seek
					// (for example, Mono FileStream)
					if (this.baseStream_.Position != this.readPos_)
					{
						this.baseStream_.Seek(this.readPos_, SeekOrigin.Begin);
					}
					int readCount = this.baseStream_.Read(buffer, offset, count);
					if (readCount > 0)
					{
						this.readPos_ += readCount;
					}
					return readCount;
				}
			}

			/// <summary>
			/// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
			/// </summary>
			/// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
			/// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
			/// <param name="count">The number of bytes to be written to the current stream.</param>
			/// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
			/// <exception cref="T:System.NotSupportedException">The stream does not support writing. </exception>
			/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
			/// <exception cref="T:System.ArgumentNullException">buffer is null. </exception>
			/// <exception cref="T:System.ArgumentException">The sum of offset and count is greater than the buffer length. </exception>
			/// <exception cref="T:System.ArgumentOutOfRangeException">offset or count is negative. </exception>
			public override void Write(byte[] buffer, int offset, int count)
			{
				throw new NotSupportedException();
			}

			/// <summary>
			/// When overridden in a derived class, sets the length of the current stream.
			/// </summary>
			/// <param name="value">The desired length of the current stream in bytes.</param>
			/// <exception cref="T:System.NotSupportedException">The stream does not support both writing and seeking, such as if the stream is constructed from a pipe or console output. </exception>
			/// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
			/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
			public override void SetLength(long value)
			{
				throw new NotSupportedException();
			}

			/// <summary>
			/// When overridden in a derived class, sets the position within the current stream.
			/// </summary>
			/// <param name="offset">A byte offset relative to the origin parameter.</param>
			/// <param name="origin">A value of type <see cref="T:System.IO.SeekOrigin"></see> indicating the reference point used to obtain the new position.</param>
			/// <returns>
			/// The new position within the current stream.
			/// </returns>
			/// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
			/// <exception cref="T:System.NotSupportedException">The stream does not support seeking, such as if the stream is constructed from a pipe or console output. </exception>
			/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
			public override long Seek(long offset, SeekOrigin origin)
			{
				long newPos = this.readPos_;

				switch (origin)
				{
					case SeekOrigin.Begin:
						newPos = this.start_ + offset;
						break;

					case SeekOrigin.Current:
						newPos = this.readPos_ + offset;
						break;

					case SeekOrigin.End:
						newPos = this.end_ + offset;
						break;
				}

				if (newPos < this.start_)
				{
					throw new ArgumentException("Negative position is invalid");
				}

				if (newPos >= this.end_)
				{
					throw new IOException("Cannot seek past end");
				}
				this.readPos_ = newPos;
				return this.readPos_;
			}

			/// <summary>
			/// Clears all buffers for this stream and causes any buffered data to be written to the underlying device.
			/// </summary>
			/// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
			public override void Flush()
			{
				// Nothing to do.
			}

			/// <summary>
			/// Gets or sets the position within the current stream.
			/// </summary>
			/// <value></value>
			/// <returns>The current position within the stream.</returns>
			/// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
			/// <exception cref="T:System.NotSupportedException">The stream does not support seeking. </exception>
			/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
			public override long Position
			{
				get { return this.readPos_ - this.start_; }
				set
				{
					long newPos = this.start_ + value;

					if (newPos < this.start_)
					{
						throw new ArgumentException("Negative position is invalid");
					}

					if (newPos >= this.end_)
					{
						throw new InvalidOperationException("Cannot seek past end");
					}
					this.readPos_ = newPos;
				}
			}

			/// <summary>
			/// Gets the length in bytes of the stream.
			/// </summary>
			/// <value></value>
			/// <returns>A long value representing the length of the stream in bytes.</returns>
			/// <exception cref="T:System.NotSupportedException">A class derived from Stream does not support seeking. </exception>
			/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
			public override long Length
			{
				get { return this.length_; }
			}

			/// <summary>
			/// Gets a value indicating whether the current stream supports writing.
			/// </summary>
			/// <value>false</value>
			/// <returns>true if the stream supports writing; otherwise, false.</returns>
			public override bool CanWrite
			{
				get { return false; }
			}

			/// <summary>
			/// Gets a value indicating whether the current stream supports seeking.
			/// </summary>
			/// <value>true</value>
			/// <returns>true if the stream supports seeking; otherwise, false.</returns>
			public override bool CanSeek
			{
				get { return true; }
			}

			/// <summary>
			/// Gets a value indicating whether the current stream supports reading.
			/// </summary>
			/// <value>true.</value>
			/// <returns>true if the stream supports reading; otherwise, false.</returns>
			public override bool CanRead
			{
				get { return true; }
			}

			/// <summary>
			/// Gets a value that determines whether the current stream can time out.
			/// </summary>
			/// <value></value>
			/// <returns>A value that determines whether the current stream can time out.</returns>
			public override bool CanTimeout
			{
				get { return this.baseStream_.CanTimeout; }
			}

			#region Instance Fields

			private ZipFile zipFile_;
			private Stream baseStream_;
			private readonly long start_;
			private readonly long length_;
			private long readPos_;
			private readonly long end_;

			#endregion Instance Fields
		}

		#endregion Support Classes
	}
}
