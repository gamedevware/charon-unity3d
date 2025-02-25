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
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GameDevWare.Charon.Editor.Utils;
using UnityEngine;

namespace GameDevWare.Charon.Editor.Services
{
	public class LogArchiver: IDisposable
	{
		private readonly CancellationTokenSource stopArchiving;
		private Task archivingTask;
		private readonly ILogger logger;

		public LogArchiver(ILogger logger)
		{
			this.logger = logger;
			this.stopArchiving = new CancellationTokenSource();
		}

		public void Initialize()
		{
			this.archivingTask = this.ArchiveLogsAsync(this.stopArchiving.Token);
			this.archivingTask.LogFaultAsError();
		}

		private async Task ArchiveLogsAsync(CancellationToken cancellationToken)
		{
			await Task.Yield();

			while (!cancellationToken.IsCancellationRequested)
			{
				var logsDirectory = CharonFileUtils.LibraryCharonLogsPath;
				if (!Directory.Exists(logsDirectory))
				{
					this.logger.Log(LogType.Assert, $"Charon log directory '{logsDirectory}' does not exists.");
					break;
				}

				var allLogFiles = Directory.GetFiles(logsDirectory);

				// clean temp file
				allLogFiles.Where(TempFile)
					.ToList()
					.ForEach(CharonFileUtils.SafeFileDelete);

				// clean old logs
				allLogFiles.Where(FromMonthAgo)
					.ToList()
					.ForEach(CharonFileUtils.SafeFileDelete);

				// archive logs into one file
				var lastWeekFiles = allLogFiles.Where(NotZipFile).Where(FromLastWeek).ToList();
				await this.ArchiveOldLogsAsync(logsDirectory, lastWeekFiles, cancellationToken);

				await Task.Delay(TimeSpan.FromDays(1), cancellationToken).ConfigureAwait(false);
			}

			static bool TempFile(string logPath) => Path.GetExtension(logPath) == "tmp";
			static bool NotZipFile(string logPath) => Path.GetExtension(logPath) != "zip";
			static bool FromLastWeek(string logPath) => File.GetCreationTimeUtc(logPath) < GetWeekStartTime();
			static bool FromMonthAgo(string logPath) => File.GetCreationTimeUtc(logPath) < DateTime.UtcNow.Date - TimeSpan.FromDays(30);
			static DateTime GetWeekStartTime() => DateTime.UtcNow.Date - TimeSpan.FromDays((int)DateTime.UtcNow.Date.DayOfWeek);
		}

		private async Task ArchiveOldLogsAsync(string logsDirectory, List<string> lastWeekFiles, CancellationToken cancellationToken)
		{
			if (logsDirectory == null) throw new ArgumentNullException(nameof(logsDirectory));
			if (lastWeekFiles == null) throw new ArgumentNullException(nameof(lastWeekFiles));

			if (lastWeekFiles.Count <= 0)
			{
				return;
			}

			var tmpZipArchiveFilePath = Path.Combine(logsDirectory, $"{Guid.NewGuid():N}.tmp");
			var resultZipArchiveFilePath = Path.Combine(logsDirectory, $"{GetWeekStartTime():yyyy_M_dd}.charon.unity.zip");
			var hasSucceed = false;
			try
			{
				using var tempArchiveFile = File.OpenWrite(tmpZipArchiveFilePath);
				using var newArchive = new ZipArchive(tempArchiveFile, ZipArchiveMode.Create);
				foreach (var lastWeekFile in lastWeekFiles)
				{
					cancellationToken.ThrowIfCancellationRequested();

					var zipEntry = newArchive.CreateEntry(Path.GetFileName(lastWeekFile));
					using var logFile = File.OpenRead(lastWeekFile);
					using var zipEntryStream = zipEntry.Open();
					await logFile.CopyToAsync(zipEntryStream, cancellationToken).ConfigureAwait(false);
				}

				lastWeekFiles.ForEach(CharonFileUtils.SafeFileDelete);
				hasSucceed = true;
			}
			catch (Exception archiveError)
			{
				cancellationToken.ThrowIfCancellationRequested();

				this.logger.Log(LogType.Assert, "Failed to archive logs from last week due to an error.");
				this.logger.Log(LogType.Assert, archiveError);
			}
			finally
			{
				if (!hasSucceed)
				{
					CharonFileUtils.SafeFileDelete(tmpZipArchiveFilePath);
				}
			}

			if (hasSucceed)
			{
				File.Move(tmpZipArchiveFilePath, resultZipArchiveFilePath);
			}

			static DateTime GetWeekStartTime() => DateTime.UtcNow.Date - TimeSpan.FromDays((int)DateTime.UtcNow.Date.DayOfWeek);
		}

		/// <inheritdoc />
		public void Dispose()
		{
			this.stopArchiving.Cancel();
			this.stopArchiving?.Dispose();
		}
	}
}
