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
using System.IO;
using System.Text;
using GameDevWare.Charon.Editor.Utils;
using UnityEngine;

namespace GameDevWare.Charon.Editor.Cli
{
	public class CharonServerProcess : IDisposable
	{
		private readonly RunResult runResult;
		private readonly ILogger logger;

		public readonly Process Process;
		public readonly string GameDataFilePath;
		public readonly Uri ListenAddress;
		public readonly string LockFilePath;

		public CharonServerProcess(RunResult runResult, string gameDataFilePath, Uri listenAddress, string lockFilePath)
		{
			if (runResult == null) throw new ArgumentNullException(nameof(runResult));
			if (listenAddress == null) throw new ArgumentNullException(nameof(listenAddress));
			if (lockFilePath == null) throw new ArgumentNullException(nameof(lockFilePath));

			this.logger = CharonEditorModule.Instance.Logger;
			this.Process = runResult.Process;
			this.runResult = runResult;
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
			((IDisposable)this.runResult)?.Dispose();
		}
	}
}
