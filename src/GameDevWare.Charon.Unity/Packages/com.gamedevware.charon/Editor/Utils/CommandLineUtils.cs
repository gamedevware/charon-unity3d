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
using System.Threading.Tasks;
using UnityEngine;

namespace GameDevWare.Charon.Editor.Utils
{
	internal static class CommandLineUtils
	{
		public static async Task<ToolRunResult> RunAsync(ToolRunOptions options)
		{
			if (options == null) throw new ArgumentNullException(nameof(options));

			if (options.CaptureStandardError)
				options.StartInfo.RedirectStandardError = true;
			if (options.CaptureStandardOutput)
				options.StartInfo.RedirectStandardOutput = true;

			if (options.CaptureStandardError || options.CaptureStandardOutput)
			{
#if UNITY_EDITOR_WIN
				options.StartInfo.UseShellExecute = false;
				options.StartInfo.Arguments = $"/c \"\"{options.StartInfo.FileName}\" {options.StartInfo.Arguments}\"";
				options.StartInfo.FileName = "cmd.exe";
#else
				options.StartInfo.UseShellExecute = false;
				options.StartInfo.Arguments = $" -- \"{options.StartInfo.FileName}\" {options.StartInfo.Arguments}";
				options.StartInfo.FileName = "/usr/bin/env";
#endif
			}


			var logger = CharonEditorModule.Instance.Logger;
			logger.Log(LogType.Assert, $"Starting process '{options.StartInfo.FileName}' at '{options.StartInfo.WorkingDirectory}' with arguments '{options.StartInfo.Arguments}'.");

			var processStarted = DateTime.UtcNow;
			var timeout = options.ExecutionTimeout;
			if (timeout <= TimeSpan.Zero)
				timeout = TimeSpan.MaxValue;

			var process = Process.Start(options.StartInfo);
			if (process == null)
				throw new InvalidOperationException("Unknown process start error.");

			var result = new ToolRunResult(options, process);
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
