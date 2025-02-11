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
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;

namespace GameDevWare.Charon.Editor.Utils
{
	internal static class CommandLineUtils
	{
		public static async Task<RunResult> RunAsync(RunOptions options)
		{
			if (options == null) throw new ArgumentNullException(nameof(options));

			if (options.CaptureStandardError)
				options.StartInfo.RedirectStandardError = true;
			if (options.CaptureStandardOutput)
				options.StartInfo.RedirectStandardOutput = true;

			var logger = CharonEditorModule.Instance.Logger;
			logger.Log(LogType.Assert, $"Starting process '{options.StartInfo.FileName}' at '{options.StartInfo.WorkingDirectory}' with arguments '{options.StartInfo.Arguments}'.");

			var processStarted = DateTime.UtcNow;
			var timeout = options.ExecutionTimeout;
			if (timeout <= TimeSpan.Zero)
				timeout = TimeSpan.MaxValue;

			var process = Process.Start(options.StartInfo);
			if (process == null)
				throw new InvalidOperationException("Unknown process start error.");

			var result = new RunResult(options, process);
			if (options.WaitForExit == false)
			{
				//if (Settings.Current.Verbose)
				//	logger.Log(LogType.Assert, string.Format("Yielding started process '{0}' at '{1}' with arguments '{2}'.", options.StartInfo.FileName, options.StartInfo.WorkingDirectory, options.StartInfo.Arguments));
				return result;
			}

			var hasExited = false;
			while (hasExited == false)
			{
				if (DateTime.UtcNow - processStarted > timeout)
				{
					throw new TimeoutException();
				}

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
				await Task.Delay(TimeSpan.FromMilliseconds(50));
			}

			processStarted = DateTime.UtcNow;
			timeout = options.TerminationTimeout;
			if (timeout <= TimeSpan.Zero)
				timeout = TimeSpan.MaxValue;

			while (result.HasPendingData)
			{
				if (DateTime.UtcNow - processStarted > timeout)
				{
					throw new TimeoutException();
				}

				await Task.Delay(TimeSpan.FromMilliseconds(50));
			}

			result.ExitCode = process.ExitCode;

			logger.Log(LogType.Assert, $"Process #{result.ProcessId} '{options.StartInfo.FileName}' has exited with code {result.ExitCode}.");

			return result;
		}
	}
}
