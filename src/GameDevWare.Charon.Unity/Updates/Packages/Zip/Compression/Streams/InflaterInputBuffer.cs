using System;
using System.IO;
using System.Security.Cryptography;

namespace GameDevWare.Charon.Unity.Updates.Packages.Zip.Compression.Streams
{
	/// <summary>
	/// An input buffer customised for use by <see cref="InflaterInputStream"/>
	/// </summary>
	/// <remarks>
	/// The buffer supports decryption of incoming data.
	/// </remarks>
	internal class InflaterInputBuffer
	{
		#region Constructors

		/// <summary>
		/// Initialise a new instance of <see cref="InflaterInputBuffer"/> with a default buffer size
		/// </summary>
		/// <param name="stream">The stream to buffer.</param>
		public InflaterInputBuffer(Stream stream) : this(stream, 4096)
		{
		}

		/// <summary>
		/// Initialise a new instance of <see cref="InflaterInputBuffer"/>
		/// </summary>
		/// <param name="stream">The stream to buffer.</param>
		/// <param name="bufferSize">The size to use for the buffer</param>
		/// <remarks>A minimum buffer size of 1KB is permitted.  Lower sizes are treated as 1KB.</remarks>
		public InflaterInputBuffer(Stream stream, int bufferSize)
		{
			this.inputStream = stream;
			if (bufferSize < 1024)
			{
				bufferSize = 1024;
			}
			this.rawData = new byte[bufferSize];
			this.clearText = this.rawData;
		}

		#endregion Constructors

		/// <summary>
		/// Get the length of bytes bytes in the <see cref="RawData"/>
		/// </summary>
		public int RawLength
		{
			get
			{
				return this.rawLength;
			}
		}

		/// <summary>
		/// Get the contents of the raw data buffer.
		/// </summary>
		/// <remarks>This may contain encrypted data.</remarks>
		public byte[] RawData
		{
			get
			{
				return this.rawData;
			}
		}

		/// <summary>
		/// Get the number of useable bytes in <see cref="ClearText"/>
		/// </summary>
		public int ClearTextLength
		{
			get
			{
				return this.clearTextLength;
			}
		}

		/// <summary>
		/// Get the contents of the clear text buffer.
		/// </summary>
		public byte[] ClearText
		{
			get
			{
				return this.clearText;
			}
		}

		/// <summary>
		/// Get/set the number of bytes available
		/// </summary>
		public int Available
		{
			get { return this.available; }
			set { this.available = value; }
		}

		/// <summary>
		/// Call <see cref="Inflater.SetInput(byte[], int, int)"/> passing the current clear text buffer contents.
		/// </summary>
		/// <param name="inflater">The inflater to set input for.</param>
		public void SetInflaterInput(Inflater inflater)
		{
			if (this.available > 0)
			{
				inflater.SetInput(this.clearText, this.clearTextLength - this.available, this.available);
				this.available = 0;
			}
		}

		/// <summary>
		/// Fill the buffer from the underlying input stream.
		/// </summary>
		public void Fill()
		{
			this.rawLength = 0;
			int toRead = this.rawData.Length;

			while (toRead > 0)
			{
				int count = this.inputStream.Read(this.rawData, this.rawLength, toRead);
				if (count <= 0)
				{
					break;
				}
				this.rawLength += count;
				toRead -= count;
			}

			if (this.cryptoTransform != null)
			{
				this.clearTextLength = this.cryptoTransform.TransformBlock(this.rawData, 0, this.rawLength, this.clearText, 0);
			}
			else
			{
				this.clearTextLength = this.rawLength;
			}

			this.available = this.clearTextLength;
		}

		/// <summary>
		/// Read a buffer directly from the input stream
		/// </summary>
		/// <param name="buffer">The buffer to fill</param>
		/// <returns>Returns the number of bytes read.</returns>
		public int ReadRawBuffer(byte[] buffer)
		{
			return this.ReadRawBuffer(buffer, 0, buffer.Length);
		}

		/// <summary>
		/// Read a buffer directly from the input stream
		/// </summary>
		/// <param name="outBuffer">The buffer to read into</param>
		/// <param name="offset">The offset to start reading data into.</param>
		/// <param name="length">The number of bytes to read.</param>
		/// <returns>Returns the number of bytes read.</returns>
		public int ReadRawBuffer(byte[] outBuffer, int offset, int length)
		{
			if (length < 0)
			{
				throw new ArgumentOutOfRangeException("length");
			}

			int currentOffset = offset;
			int currentLength = length;

			while (currentLength > 0)
			{
				if (this.available <= 0)
				{
					this.Fill();
					if (this.available <= 0)
					{
						return 0;
					}
				}
				int toCopy = Math.Min(currentLength, this.available);
				System.Array.Copy(this.rawData, this.rawLength - (int)this.available, outBuffer, currentOffset, toCopy);
				currentOffset += toCopy;
				currentLength -= toCopy;
				this.available -= toCopy;
			}
			return length;
		}

		/// <summary>
		/// Read clear text data from the input stream.
		/// </summary>
		/// <param name="outBuffer">The buffer to add data to.</param>
		/// <param name="offset">The offset to start adding data at.</param>
		/// <param name="length">The number of bytes to read.</param>
		/// <returns>Returns the number of bytes actually read.</returns>
		public int ReadClearTextBuffer(byte[] outBuffer, int offset, int length)
		{
			if (length < 0)
			{
				throw new ArgumentOutOfRangeException("length");
			}

			int currentOffset = offset;
			int currentLength = length;

			while (currentLength > 0)
			{
				if (this.available <= 0)
				{
					this.Fill();
					if (this.available <= 0)
					{
						return 0;
					}
				}

				int toCopy = Math.Min(currentLength, this.available);
				Array.Copy(this.clearText, this.clearTextLength - (int)this.available, outBuffer, currentOffset, toCopy);
				currentOffset += toCopy;
				currentLength -= toCopy;
				this.available -= toCopy;
			}
			return length;
		}

		/// <summary>
		/// Read a <see cref="byte"/> from the input stream.
		/// </summary>
		/// <returns>Returns the byte read.</returns>
		public int ReadLeByte()
		{
			if (this.available <= 0)
			{
				this.Fill();
				if (this.available <= 0)
				{
					throw new ZipException("EOF in header");
				}
			}
			byte result = this.rawData[this.rawLength - this.available];
			this.available -= 1;
			return result;
		}

		/// <summary>
		/// Read an <see cref="short"/> in little endian byte order.
		/// </summary>
		/// <returns>The short value read case to an int.</returns>
		public int ReadLeShort()
		{
			return this.ReadLeByte() | (this.ReadLeByte() << 8);
		}

		/// <summary>
		/// Read an <see cref="int"/> in little endian byte order.
		/// </summary>
		/// <returns>The int value read.</returns>
		public int ReadLeInt()
		{
			return this.ReadLeShort() | (this.ReadLeShort() << 16);
		}

		/// <summary>
		/// Read a <see cref="long"/> in little endian byte order.
		/// </summary>
		/// <returns>The long value read.</returns>
		public long ReadLeLong()
		{
			return (uint)this.ReadLeInt() | ((long)this.ReadLeInt() << 32);
		}

		/// <summary>
		/// Get/set the <see cref="ICryptoTransform"/> to apply to any data.
		/// </summary>
		/// <remarks>Set this value to null to have no transform applied.</remarks>
		public ICryptoTransform CryptoTransform
		{
			set
			{
				this.cryptoTransform = value;
				if (this.cryptoTransform != null)
				{
					if (this.rawData == this.clearText)
					{
						if (this.internalClearText == null)
						{
							this.internalClearText = new byte[this.rawData.Length];
						}
						this.clearText = this.internalClearText;
					}
					this.clearTextLength = this.rawLength;
					if (this.available > 0)
					{
						this.cryptoTransform.TransformBlock(this.rawData, this.rawLength - this.available, this.available, this.clearText, this.rawLength - this.available);
					}
				}
				else
				{
					this.clearText = this.rawData;
					this.clearTextLength = this.rawLength;
				}
			}
		}

		#region Instance Fields

		private int rawLength;
		private byte[] rawData;

		private int clearTextLength;
		private byte[] clearText;
		private byte[] internalClearText;

		private int available;

		private ICryptoTransform cryptoTransform;
		private Stream inputStream;

		#endregion Instance Fields
	}
}