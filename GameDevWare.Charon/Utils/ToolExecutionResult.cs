﻿using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

#pragma warning disable 420

namespace GameDevWare.Charon.Utils
{
	public sealed class ToolExecutionResult : IDisposable
	{
		private const int StandartOutputOpened = 0x1 << 1;
		private const int StandartErrorOpened = 0x1 << 2;

		private readonly StringBuilder standartOutputBuffer;
		private readonly StringBuilder standartErrorBuffer;
		private volatile int standartStreamOpenStatus = 0;

		public bool HasPendingData { get { return this.standartStreamOpenStatus != 0; } }

		public Process Process { get; private set; }
		public int ProcessId { get; private set; }
		public int ExitCode { get; set; }

		public ToolExecutionResult(ToolExecutionOptions options, Process process)
		{
			this.Process = process;
			this.ProcessId = process.Id;
			this.ExitCode = int.MinValue;

			if (options.StartInfo.RedirectStandardOutput)
			{
				this.standartStreamOpenStatus |= StandartOutputOpened;
				this.standartOutputBuffer = new StringBuilder();
				process.OutputDataReceived += this.OnOutputDataReceived;
				process.BeginOutputReadLine();
			}
			if (options.StartInfo.RedirectStandardError)
			{
				this.standartErrorBuffer = new StringBuilder();
				this.standartStreamOpenStatus |= StandartErrorOpened;
				process.ErrorDataReceived += this.OnErrorDataReceived;
				process.BeginErrorReadLine();
			}
		}
		~ToolExecutionResult()
		{
			this.Dispose(false);
		}

		public string GetOutputData()
		{
			return this.standartOutputBuffer == null ? null : this.standartOutputBuffer.ToString();
		}
		public string GetErrorData()
		{
			return this.standartErrorBuffer == null ? null : this.standartErrorBuffer.ToString();
		}

		private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data == null)
			{
				int status, removedStatus;
				do
				{
					status = this.standartStreamOpenStatus;
					removedStatus = status & ~StandartErrorOpened;
				} while (Interlocked.CompareExchange(ref this.standartStreamOpenStatus, removedStatus, status) != status);
				this.Process.CancelErrorRead();
			}
			else
			{
				this.standartErrorBuffer.Append(e.Data);
			}
		}
		private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data == null)
			{
				int status, removedStatus;
				do
				{
					status = this.standartStreamOpenStatus;
					removedStatus = status & ~StandartOutputOpened;
				} while (Interlocked.CompareExchange(ref this.standartStreamOpenStatus, removedStatus, status) != status);
				this.Process.CancelOutputRead();
			}
			else
			{
				this.standartOutputBuffer.Append(e.Data);
			}
		}

		private void Dispose(bool disposed)
		{
			if (disposed)
				GC.SuppressFinalize(this);

			if (this.standartOutputBuffer != null)
				this.Process.CancelOutputRead();
			if (this.standartErrorBuffer != null)
				this.Process.CancelErrorRead();
			this.Process.Dispose();
		}

		public void Dispose()
		{

		}
	}
}
