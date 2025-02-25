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
using System.IO;
using System.Text;
using GameDevWare.Charon.Editor.Utils;
using UnityEngine;

namespace GameDevWare.Charon.Editor.Cli
{
	public class CharonServerProcess : IDisposable
	{
		private readonly ToolRunResult toolRunResult;
		private readonly ILogger logger;

		public readonly Process Process;
		public readonly string GameDataFilePath;
		public readonly Uri ListenAddress;
		public readonly string LockFilePath;

		public CharonServerProcess(ToolRunResult toolRunResult, string gameDataFilePath, Uri listenAddress, string lockFilePath)
		{
			if (toolRunResult == null) throw new ArgumentNullException(nameof(toolRunResult));
			if (listenAddress == null) throw new ArgumentNullException(nameof(listenAddress));
			if (lockFilePath == null) throw new ArgumentNullException(nameof(lockFilePath));

			this.logger = CharonEditorModule.Instance.Logger;
			this.Process = toolRunResult.Process;
			this.toolRunResult = toolRunResult;
			this.ListenAddress = listenAddress;
			this.LockFilePath = lockFilePath;
			this.GameDataFilePath = gameDataFilePath;
		}

		public void EndGracefully()
		{
			var stopError = default(Exception);
			try
			{
				this.logger.Log(LogType.Assert, $"Trying to end process with id {this.Process.Id}.");

				this.Process.EndGracefully();

				this.logger.Log(LogType.Assert, $"Successfully ended process with id {this.Process.Id}.");
			}
			catch (Exception endError)
			{
				stopError = endError;

				this.logger.Log(LogType.Assert, $"Failed to get process by id {this.Process.Id}.\r\n{endError}");
			}

			try
			{
				if (File.Exists(this.LockFilePath))
					File.Delete(this.LockFilePath);
			}
			catch (Exception lockDeleteError)
			{
				stopError = stopError ?? lockDeleteError;
				this.logger.Log(LogType.Assert, $"Failed to stop running process with id {this.Process.Id}.\r\n{stopError}");
				throw stopError;
			}
		}

		public static void FindAndEndGracefully(string lockFilePath)
		{
			if (File.Exists(lockFilePath) == false)
				return;

			var logger = CharonEditorModule.Instance.Logger;
			var pidStr = default(string);
			using (var lockFileStream = new FileStream(lockFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 1024,
						FileOptions.WriteThrough))
				pidStr = new StreamReader(lockFileStream, detectEncodingFromByteOrderMarks: true).ReadLine();

			if (string.IsNullOrEmpty(pidStr) || int.TryParse(pidStr, out var pid) == false)
				return;

			var stopError = default(Exception);
			try
			{
				logger.Log(LogType.Assert, $"Trying to end process with id {pidStr}.");

				using (var process = Process.GetProcessById(pid))
				{
					process.EndGracefully();
				}

				logger.Log(LogType.Assert, $"Successfully ended process with id {pidStr}.");
			}
			catch (Exception endError)
			{
				stopError = endError;
				logger.Log(LogType.Assert, $"Failed to get process by id {pidStr}.\r\n{endError}");
			}

			try
			{
				if (File.Exists(lockFilePath))
					File.Delete(lockFilePath);
			}
			catch (Exception lockDeleteError)
			{
				stopError = stopError ?? lockDeleteError;
				logger.Log(LogType.Assert, $"Failed to stop running process with id {pidStr}.\r\n{stopError}");
				throw stopError;
			}
		}

		public static string GetLockFileNameFor(string fileName)
		{
			if (fileName == null) throw new ArgumentNullException(nameof(fileName));

			var bytes = Encoding.UTF8.GetBytes(fileName.ToLowerInvariant());
			using var hash = System.Security.Cryptography.MD5.Create();
			return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant() + ".lock";
		}

		/// <inheritdoc />
		public void Dispose()
		{
			this.EndGracefully();
			((IDisposable)this.toolRunResult)?.Dispose();
		}
	}
}
