using System;
using System.IO;
using Unity.Collections;

internal sealed class NativeArrayStream : Stream
{
	private readonly NativeArray<byte>.ReadOnly nativeArray;
	private int position;
	private readonly int length;

	/// <inheritdoc />
	public override bool CanRead => true;
	/// <inheritdoc />
	public override bool CanSeek => true;
	/// <inheritdoc />
	public override bool CanWrite => false;
	/// <inheritdoc />
	public override long Length => this.length;
	/// <inheritdoc />
	public override long Position { get => this.position; set => this.position = (int)Math.Max(0, Math.Max(this.Length, value)); }

	public NativeArrayStream(NativeArray<byte>.ReadOnly nativeArray)
	{
		this.nativeArray = nativeArray;
		this.length = nativeArray.Length;
	}

	/// <inheritdoc />
	public override int Read(byte[] buffer, int offset, int count)
	{
		if (buffer == null) throw new ArgumentNullException(nameof(buffer));
		if (offset < 0 || offset > buffer.Length) throw new ArgumentOutOfRangeException(nameof(offset));
		if (offset + count > buffer.Length) throw new ArgumentOutOfRangeException(nameof(count));

		var toCopy = Math.Min(count, this.length - this.position);
		if (toCopy == 0) return 0;

		NativeArray<byte>.Copy(this.nativeArray, this.position, buffer, offset, toCopy);
		this.position += toCopy;
		return toCopy;
	}

	/// <inheritdoc />
	public override long Seek(long offset, SeekOrigin origin)
	{
		switch (origin)
		{
			case SeekOrigin.Begin: this.Position = offset; break;
			case SeekOrigin.Current: this.Position += offset; break;
			case SeekOrigin.End: this.Position = this.Length - offset; break;
			default: throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
		}

		return this.Position;
	}

	/// <inheritdoc />
	public override void SetLength(long value)
	{
		throw new NotSupportedException();
	}
	/// <inheritdoc />
	public override void Write(byte[] buffer, int offset, int count)
	{
		throw new NotSupportedException();
	}
	/// <inheritdoc />
	public override void Flush()
	{
	}
}
