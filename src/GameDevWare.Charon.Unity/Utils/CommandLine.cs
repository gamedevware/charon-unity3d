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
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text;
using GameDevWare.Charon.Unity.Async;

namespace GameDevWare.Charon.Unity.Utils
{
	internal static class CommandLine
	{
		public static Promise<RunResult> Run(string executablePath, params string[] arguments)
		{
			if (executablePath == null) throw new ArgumentNullException("executablePath");

			return Run(new RunOptions(executablePath, arguments));
		}
		public static Promise<RunResult> Run(RunOptions options)
		{
			if (options == null) throw new ArgumentNullException("options");

			if (options.Schedule)
				return Schedule<RunResult>(RunAsync(options));
			else
				return new Coroutine<RunResult>(RunAsync(options));
		}

		private static Promise<T> Schedule<T>(IEnumerable coroutine)
		{
			return CoroutineScheduler.Schedule<T>(coroutine);
		}

		private static IEnumerable RunAsync(RunOptions options)
		{
			yield return null;

			var isDotNetInstalled = false;

			if (RuntimeInformation.IsWindows)
			{
				isDotNetInstalled = DotNetRuntimeInformation.GetVersion() != null;
			}

			if (options.RequireDotNetRuntime && isDotNetInstalled == false)
			{
				if (string.IsNullOrEmpty(MonoRuntimeInformation.MonoPath))
					throw new InvalidOperationException("No .NET runtime found on machine.");

				options.StartInfo.Arguments = RunOptions.ConcatenateArguments(options.StartInfo.FileName) + " " + options.StartInfo.Arguments;
				options.StartInfo.FileName = MonoRuntimeInformation.MonoPath;
			}

			if (options.CaptureStandardError)
				options.StartInfo.RedirectStandardError = true;
			if (options.CaptureStandardOutput)
				options.StartInfo.RedirectStandardOutput = true;

			if (Settings.Current.Verbose)
				UnityEngine.Debug.Log(string.Format("Starting process '{0}' at '{1}' with arguments '{2}' and environment variables '{3}'.", options.StartInfo.FileName, options.StartInfo.WorkingDirectory, options.StartInfo.Arguments, ConcatenateDictionaryValues(options.StartInfo.EnvironmentVariables)));

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
				//	UnityEngine.Debug.Log(string.Format("Yielding started process '{0}' at '{1}' with arguments '{2}'.", options.StartInfo.FileName, options.StartInfo.WorkingDirectory, options.StartInfo.Arguments));
				yield return result;
				yield break;
			}

			var hasExited = false;
			while (hasExited == false)
			{
				if (DateTime.UtcNow - processStarted > timeout)
					throw new TimeoutException();

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
				yield return Promise.Delayed(TimeSpan.FromMilliseconds(50));
			}

			processStarted = DateTime.UtcNow;
			timeout = options.TerminationTimeout;
			if (timeout <= TimeSpan.Zero)
				timeout = TimeSpan.MaxValue;

			while (result.HasPendingData)
			{
				if (DateTime.UtcNow - processStarted > timeout)
					throw new TimeoutException();

				yield return Promise.Delayed(TimeSpan.FromMilliseconds(50));
			}

			result.ExitCode = process.ExitCode;

			if (Settings.Current.Verbose)
				UnityEngine.Debug.Log(string.Format("Process #{1} '{0}' has exited with code {2}.", options.StartInfo.FileName, result.ProcessId, result.ExitCode));

			yield return result;
		}
		private static string ConcatenateDictionaryValues(StringDictionary dictionary)
		{
			if (dictionary.Count == 0)
				return "";

			var sb = new StringBuilder();
			foreach (string key in dictionary.Keys)
				sb.Append(key).Append("=").Append(dictionary[key]).Append(", ");
			if (sb.Length > 2)
				sb.Length -= 2;
			return sb.ToString();
		}
	}
}
