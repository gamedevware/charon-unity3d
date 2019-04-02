/*
	Copyright (c) 2017 Denis Zykov

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
using System.Threading;

namespace GameDevWare.Charon.Unity.Utils
{
	internal static class ProcessExtensions
	{
		public static void EndGracefully(int processId)
		{
			try
			{
				using (var process = Process.GetProcessById(processId))
					EndGracefully(process);
			}
			catch (ArgumentException)
			{
				// ignore
			}
			catch (InvalidOperationException)
			{
				// ignore
			}
		}
		public static void EndGracefully(this Process process)
		{
			if (process == null) throw new ArgumentNullException("process");

			try
			{
				if (process.HasExited)
					return;

				//Debug.Log(string.Format("Trying to kill process with id '{0}'.", process.Id));

				process.CloseMainWindow();
				if (WaitForExit(process, TimeSpan.FromSeconds(0.5)))
					return;

				process.Kill();
				if (WaitForExit(process, TimeSpan.FromSeconds(0.5)))
					return;

				throw new InvalidOperationException("Process doesn't respond to Kill signal.");
			}
			catch (Exception error)
			{
				UnityEngine.Debug.LogWarning(string.Format("Failed to kill process with id {0} because of error: {1}{2}", process.Id, Environment.NewLine, error.Message));
			}
		}

		private static bool WaitForExit(Process process, TimeSpan timeout)
		{
			var hasExited = false;
			var dt = DateTime.UtcNow;
			while (DateTime.UtcNow - dt < timeout)
			{
				try
				{
					process.Refresh();
					hasExited = process.HasExited;
				}
				catch (InvalidOperationException)
				{
					// ignored
				}
				catch (System.ComponentModel.Win32Exception)
				{
					// ignored
				}
				Thread.Sleep(0);
			}
			return hasExited;
		}
	}
}
