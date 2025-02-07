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
using System.Collections.Concurrent;
using System.Threading;
using GameDevWare.Charon.Editor.Cli;

namespace GameDevWare.Charon.Editor.Services
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
