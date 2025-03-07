/*
	Copyright (c) 2025 GameDevWare, Denis Zykov

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
using System.Threading;
using System.Threading.Tasks;

namespace GameDevWare.Charon.Editor.Services
{
	internal class RoutineQueue: IDisposable
	{
		private readonly SemaphoreSlim runLock = new SemaphoreSlim(1, 1);
		private volatile int queueSize = 0;

		public bool IsRunning => this.runLock.CurrentCount == 0;

		public async Task Schedule(Func<Task> runFunc, CancellationToken cancellationToken)
		{
			Interlocked.Increment(ref this.queueSize);
			await this.runLock.WaitAsync(cancellationToken).ConfigureAwait(true);
			Interlocked.Decrement(ref this.queueSize);
			try
			{
				await runFunc().ConfigureAwait(true);
			}
			finally
			{
				this.runLock.Release();
			}
		}
		public async Task<ResultT> Schedule<ResultT>(Func<Task<ResultT>> runFunc, CancellationToken cancellationToken)
		{
			Interlocked.Increment(ref this.queueSize);
			await this.runLock.WaitAsync(cancellationToken).ConfigureAwait(true);
			Interlocked.Decrement(ref this.queueSize);
			try
			{
				return await runFunc().ConfigureAwait(true);
			}
			finally
			{
				this.runLock.Release();
			}
		}

		/// <inheritdoc />
		public void Dispose()
		{
			this.runLock?.Dispose();
		}

		/// <inheritdoc />
		public override string ToString() => $"Queue Size: {this.queueSize}, Is Running: {this.IsRunning}";
	}
}
