/*
	Copyright (c) 2025 Denis Zykov

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

#pragma warning disable 420

namespace GameDevWare.Charon.Editor.Utils
{
	public sealed class ToolRunResult : IDisposable
	{
		private const int STANDARD_OUTPUT_OPENED = 0x1 << 1;
		private const int STANDARD_ERROR_OPENED = 0x1 << 2;

		private readonly StringBuilder standardOutputBuffer;
		private readonly StringBuilder standardErrorBuffer;
		private volatile int standardStreamOpenStatus = 0;

		public bool HasPendingData => this.standardStreamOpenStatus != 0;

		public Process Process { get; private set; }
		public int ProcessId { get; private set; }
		public int ExitCode { get; set; }

		public ToolRunResult(ToolRunOptions options, Process process)
		{
			this.Process = process;
			this.ProcessId = process.Id;
			this.ExitCode = int.MinValue;

			if (options.StartInfo.RedirectStandardOutput)
			{
				this.standardStreamOpenStatus |= STANDARD_OUTPUT_OPENED;
				this.standardOutputBuffer = new StringBuilder();
				process.OutputDataReceived += this.OnOutputDataReceived;
				process.BeginOutputReadLine();
			}
			if (options.StartInfo.RedirectStandardError)
			{
				this.standardErrorBuffer = new StringBuilder();
				this.standardStreamOpenStatus |= STANDARD_ERROR_OPENED;
				process.ErrorDataReceived += this.OnErrorDataReceived;
				process.BeginErrorReadLine();
			}
		}
		~ToolRunResult()
		{
			this.Dispose(false);
		}

		public string GetOutputData()
		{
			return this.standardOutputBuffer == null ? null : this.standardOutputBuffer.ToString();
		}
		public string GetErrorData()
		{
			return this.standardErrorBuffer == null ? null : this.standardErrorBuffer.ToString();
		}

		private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data == null)
			{
				int status, removedStatus;
				do
				{
					status = this.standardStreamOpenStatus;
					removedStatus = status & ~STANDARD_ERROR_OPENED;
				} while (Interlocked.CompareExchange(ref this.standardStreamOpenStatus, removedStatus, status) != status);
				this.Process.CancelErrorRead();
			}
			else
			{
				this.standardErrorBuffer.Append(e.Data);
			}
		}
		private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data == null)
			{
				int status, removedStatus;
				do
				{
					status = this.standardStreamOpenStatus;
					removedStatus = status & ~STANDARD_OUTPUT_OPENED;
				} while (Interlocked.CompareExchange(ref this.standardStreamOpenStatus, removedStatus, status) != status);
				this.Process.CancelOutputRead();
			}
			else
			{
				this.standardOutputBuffer.Append(e.Data);
			}
		}

		private void Dispose(bool disposed)
		{
			if (disposed)
				GC.SuppressFinalize(this);

			try
			{
				if (this.standardOutputBuffer != null)
					this.Process.CancelOutputRead();
				if (this.standardErrorBuffer != null)
					this.Process.CancelErrorRead();

				this.Process.Dispose();
			}
			catch
			{
				/* ignore dispose errors */
			}
		}

		void IDisposable.Dispose()
		{
			// disposing Process instance cause strange behavior in Unity's version of mono
			// this.Dispose(disposed: true);
		}
	}
}
