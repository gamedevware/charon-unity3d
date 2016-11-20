using System;
using System.Diagnostics;
using System.Threading;

namespace Assets.Editor.GameDevWare.Charon.Utils
{
	public static class ProcessExtentions
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

			if (process.HasExited)
				return;

			try
			{
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
