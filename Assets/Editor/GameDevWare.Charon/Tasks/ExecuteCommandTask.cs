/*
	Copyright (c) 2016 Denis Zykov

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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Assets.Editor.GameDevWare.Charon.Utils;

namespace Assets.Editor.GameDevWare.Charon.Tasks
{
	public sealed class ExecuteCommandTask : Task
	{
		private readonly ProcessStartInfo startInfo;
		private readonly DataReceivedEventHandler outputDataReceived;
		private readonly DataReceivedEventHandler errorDataReceived;
		private readonly object syncLock;
		private Process process;
		private bool aborted;
		private bool errorDataClosed;
		private bool outputDataClosed;

		public ProcessStartInfo StartInfo { get { return this.startInfo; } }
		public bool IsRunning { get { return this.process != null && this.process.HasExited == false; } }
		public string Verb { get { return this.startInfo.Verb; } set { this.startInfo.Verb = value; } }
		public int ExitCode { get; private set; }
		public int ProcessId { get; private set; }

		public ExecuteCommandTask(string processPath, params string[] arguments)
		{
			if (processPath == null) throw new ArgumentNullException("processPath");
			if (arguments == null) throw new ArgumentNullException("arguments");

			this.startInfo = new ProcessStartInfo(processPath)
			{
				Arguments = ConcatArguments(arguments),
				UseShellExecute = false,
				WorkingDirectory = Path.GetFullPath("./"),
				EnvironmentVariables =
				{
					{ "UNITY_PROJECT_PATH", Path.GetFullPath("./").Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) }
				},
				CreateNoWindow = true
			};
		}

		public ExecuteCommandTask(string processPath, DataReceivedEventHandler outputDataReceived, DataReceivedEventHandler errorDataReceived, params string[] arguments)
			: this(processPath, arguments)
		{
			this.syncLock = new object();
			this.errorDataClosed = errorDataReceived == null;
			this.outputDataClosed = outputDataReceived == null;
			this.outputDataReceived = outputDataReceived;
			this.errorDataReceived = errorDataReceived;
			this.startInfo.RedirectStandardError = errorDataReceived != null;
			this.startInfo.RedirectStandardOutput = outputDataReceived != null;
		}
		~ExecuteCommandTask()
		{
			this.Kill();
		}

		protected override IEnumerable InitAsync()
		{
			yield return this.StartedEvent;

			if (Settings.Current.Verbose)
				UnityEngine.Debug.Log(string.Format("Starting process '{0}' at '{1}' with arguments '{2}'.", this.startInfo.FileName, this.startInfo.WorkingDirectory, this.startInfo.Arguments));

			if (this.aborted)
				yield break;

			var process = default(Process);
			var error = default(Exception);
			try { process = Process.Start(this.startInfo); }
			catch (Exception e) { error = e; }

			if (error != null || process == null)
			{
				yield return int.MinValue;
				yield break;
			}

			if (this.aborted)
				yield break;

			this.process = process;
			this.ProcessId = process.Id;

			if (this.outputDataReceived != null)
			{
				process.OutputDataReceived += (sender, args) =>
				{
					lock (this.syncLock)
					{
						if (args.Data == null)
							this.outputDataClosed = true;

						this.outputDataReceived(sender, args);
					}
				};
				process.BeginOutputReadLine();
			}
			if (this.errorDataReceived != null)
			{
				process.ErrorDataReceived += (sender, args) =>
				{
					lock (this.syncLock)
					{
						if (args.Data == null)
							this.errorDataClosed = true;

						this.errorDataReceived(sender, args);
					}
				};
				process.BeginErrorReadLine();
			}

			var hasExited = false;
			var exitCode = int.MinValue;
			while (hasExited == false)
			{
				if (this.aborted)
					yield break;

				try
				{
					process.Refresh();
					hasExited = process.HasExited;
					if (hasExited)
						exitCode = process.ExitCode;
				}
				catch (InvalidOperationException)
				{
					break;
				}
				catch (Win32Exception)
				{
					// ignored
				}
				yield return null;
			}

			this.ExitCode = exitCode;
			if (Settings.Current.Verbose)
				UnityEngine.Debug.Log(string.Format("Process '{0}' has exited with code {1}.", this.startInfo.FileName, this.ExitCode));

			if (this.errorDataClosed == false || this.outputDataClosed == false)
			{
				if (Settings.Current.Verbose)
					UnityEngine.Debug.Log(string.Format("Waiting till process '{0}' output/error data received.", this.startInfo.FileName));

				while (this.errorDataClosed == false || this.outputDataClosed == false)
					yield return null;
			}

			this.process = null;
			yield return null;
			process.Dispose();
			yield return null;
			if (Settings.Current.Verbose)
				UnityEngine.Debug.Log(string.Format("Process '{0}' has beed disposed.", this.startInfo.FileName));
		}
		private IEnumerable KillAsync(Process currentProcess)
		{
			yield return Promise.Delayed(TimeSpan.FromSeconds(1));

			var attempt = 1;
			while (!currentProcess.HasExited)
			{
				try
				{

					currentProcess.Kill();
					Promise.Delayed(TimeSpan.FromSeconds(0.5));
					currentProcess.Refresh();
					attempt++;
				}
				catch (Exception e)
				{
					UnityEngine.Debug.LogWarning("Attempt #" + attempt + " to kill process " + Path.GetFileName(startInfo.FileName) + " has failed: " + e.Message);
					if (attempt > 10)
						throw;
				}
			}
		}

		public bool Kill()
		{
			this.aborted = true;
			var currentProcess = this.process;
			if (currentProcess == null)
				return false;

			if (Settings.Current.Verbose)
				UnityEngine.Debug.Log(string.Format("Trying to kill process '{0}'.", this.startInfo.FileName));

			var attempt = 1;
			while (!currentProcess.HasExited)
			{
				try
				{

					currentProcess.Kill();
					currentProcess.Refresh();
				}
				catch (Exception)
				{
					if (attempt > 10)
						return false;
				}
			}
			return true;
		}
		public Promise Close()
		{
			this.aborted = true;
			var currentProcess = this.process;
			if (currentProcess == null || currentProcess.HasExited)
				return Promise.Fulfilled;

			if (Settings.Current.Verbose)
				UnityEngine.Debug.Log(string.Format("Trying to close process '{0}'.", this.startInfo.FileName));

			try
			{
				currentProcess.CloseMainWindow();
				System.Threading.Thread.Sleep(100);
				currentProcess.Refresh();
			}
			catch (Exception e)
			{
				UnityEngine.Debug.LogWarning("Failed to shutdown process " + Path.GetFileName(startInfo.FileName) + ": " + e.Message);
			}

			if (!currentProcess.HasExited)
				return new Coroutine(KillAsync(currentProcess));

			return Promise.Fulfilled;
		}
		public void RequireDotNetRuntime()
		{
#if UNITY_EDITOR_WIN
			if (ToolsUtils.Get45or451FromRegistry() != null)
				return;
#endif
			if (string.IsNullOrEmpty(ToolsUtils.MonoPath))
				return;

			this.StartInfo.Arguments = ConcatArguments(this.startInfo.FileName) + " " + this.StartInfo.Arguments;
			this.StartInfo.FileName = ToolsUtils.MonoPath;
		}
		protected override void OnStop()
		{
			this.Kill();
		}

		private string ConcatArguments(params string[] arguments)
		{
			for (int i = 0; i < arguments.Length; i++)
			{
				var arg = arguments[i];
				if (string.IsNullOrEmpty(arg))
					continue;

				if (arg.IndexOfAny(new char[] { '"', ' ' }) != -1)
				{
					arguments[i] =
					"\"" + arg
						.Replace(@"\", @"\\")
						.Replace("\"", "\\\"") +
					"\"";
				}
			}
			return string.Join(" ", arguments);
		}
	}
}
