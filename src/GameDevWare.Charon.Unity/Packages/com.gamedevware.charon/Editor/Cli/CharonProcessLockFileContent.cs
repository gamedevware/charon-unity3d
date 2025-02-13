using System;
using System.IO;
using System.Text;
using GameDevWare.Charon.Editor.Utils;

namespace GameDevWare.Charon.Editor.Cli
{
	public class CharonProcessLockFileContent
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
