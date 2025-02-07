/*
	Copyright (c) 2025 Denis Zykov

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
