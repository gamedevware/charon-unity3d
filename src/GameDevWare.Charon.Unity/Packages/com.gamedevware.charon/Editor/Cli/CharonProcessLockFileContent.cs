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
using System.IO;
using System.Text;
using GameDevWare.Charon.Editor.Utils;

namespace GameDevWare.Charon.Editor.Cli
{
	internal class CharonProcessLockFileContent
	{
		public readonly int ProcessId;
		public readonly Uri ListenAddress;

		private CharonProcessLockFileContent(int processId, Uri listenAddress)
		{
			this.ProcessId = processId;
			this.ListenAddress = listenAddress;
		}

		public static bool TryReadLockFile(string gameDataPath, out CharonProcessLockFileContent lockFileContentContent)
		{
			lockFileContentContent = null;
			var lockFilePath = Path.Combine(CharonFileUtils.LibraryCharonPath, CharonServerProcess.GetLockFileNameFor(gameDataPath));
			if (!File.Exists(lockFilePath))
			{
				return false;
			}

			// try delete orphained lock
			try { File.Delete(lockFilePath); }
			catch { /* ignore delete attempt errors and continue */ }

			try
			{
				using var lockFileStream = new FileStream(lockFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				using var lockFileReader = new StreamReader(lockFileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, 1024, leaveOpen: true);

				var pidStr = lockFileReader.ReadLine();
				var listenAddressString = lockFileReader.ReadLine();

				if (!int.TryParse(pidStr, out var pid) ||
					!Uri.TryCreate(listenAddressString, UriKind.Absolute, out var listenAddress))
				{
					return false;
				}

				lockFileContentContent = new CharonProcessLockFileContent(pid, listenAddress);
				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}
