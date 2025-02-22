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
using System.Collections.Concurrent;
using System.Threading;
using Editor.GameDevWare.Charon.Cli;

namespace Editor.GameDevWare.Charon.Services
{
	internal class CharonProcessList : IDisposable
	{
		private readonly ConcurrentDictionary<CharonServerProcess, CharonServerProcess> processes;
		private readonly Timer updateTimer;

		public CharonProcessList()
		{
			this.processes = new ConcurrentDictionary<CharonServerProcess, CharonServerProcess>();
			this.updateTimer = new Timer(this.OnUpdateProcessStatuses, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
		}
		private void OnUpdateProcessStatuses(object state)
		{
			foreach (var kv in this.processes)
			{
				try
				{
					var process = kv.Key.Process;
					if (!process.HasExited)
					{
						process.Refresh();
					}

					// remove stale entries
					if (DateTime.UtcNow - process.ExitTime > TimeSpan.FromMinutes(1))
					{
						this.processes.TryRemove(kv.Key, out _);
					}
				}
				catch
				{
					/* ignore refresh errors */
				}
			}
		}

		public void Add(CharonServerProcess editorProcess)
		{
			if (editorProcess == null) throw new ArgumentNullException(nameof(editorProcess));

			this.processes.TryAdd(editorProcess, editorProcess);
		}

		public void CloseAll()
		{
			foreach (var kv in this.processes)
			{
				kv.Key.EndGracefully();
			}
		}

		/// <inheritdoc />
		public void Dispose()
		{
			this.updateTimer?.Dispose();
		}
	}
}
