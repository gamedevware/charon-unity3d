/*
	Copyright (c) 2023 Denis Zykov

	This is part of "Charon: Game Data Editor" Unity Plugin.

	Charon Game Data Editor Unity Plugin is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see http://www.gnu.org/licenses.
*/

using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

#pragma warning disable 420

namespace GameDevWare.Charon.Unity.Utils
{
	public sealed class RunResult : IDisposable
	{
		private const int STANDARD_OUTPUT_OPENED = 0x1 << 1;
		private const int STANDARD_ERROR_OPENED = 0x1 << 2;

		private readonly StringBuilder standardOutputBuffer;
		private readonly StringBuilder standardErrorBuffer;
		private volatile int standardStreamOpenStatus = 0;

		public bool HasPendingData { get { return this.standardStreamOpenStatus != 0; } }

		public Process Process { get; private set; }
		public int ProcessId { get; private set; }
		public int ExitCode { get; set; }

		public RunResult(RunOptions options, Process process)
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
		~RunResult()
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
