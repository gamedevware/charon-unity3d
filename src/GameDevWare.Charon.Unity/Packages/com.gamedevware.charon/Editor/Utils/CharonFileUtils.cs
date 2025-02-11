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
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GameDevWare.Charon.Editor.Utils
{
	internal static class CharonFileUtils
	{
		private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

		public static readonly string TempPath;
		public static readonly string LibraryCharonPath;
		public static readonly string LibraryCharonLogsPath;
		public static readonly string CharonAppContentPath;
		public static readonly string PluginBasePath;

		static CharonFileUtils()
		{
			PluginBasePath = Path.GetFullPath("./Packages/com.gamedevware.charon");

			LibraryCharonPath = Path.GetFullPath("./Library/Charon/");
			TempPath = Path.GetFullPath("./Temp/");
			CharonAppContentPath = Path.Combine(Path.Combine(LibraryCharonPath, "preferences"), SanitizeFileName(Environment.UserName ?? "Default"));
			LibraryCharonLogsPath = Path.Combine(LibraryCharonPath, "logs");

			if (!Directory.Exists(LibraryCharonPath))
			{
				Directory.CreateDirectory(LibraryCharonPath);
			}
			if (!Directory.Exists(TempPath))
			{
				Directory.CreateDirectory(TempPath);
			}
			if (!Directory.Exists(CharonAppContentPath))
			{
				Directory.CreateDirectory(CharonAppContentPath);
			}
			if (!Directory.Exists(LibraryCharonLogsPath))
			{
				Directory.CreateDirectory(LibraryCharonLogsPath);
			}
		}

		public static string GetProjectRelativePath(string path)
		{
			if (string.IsNullOrEmpty(path)) return null;

			var fullPath = Path.GetFullPath(Environment.CurrentDirectory).Replace("\\", "/");
			path = Path.GetFullPath(path).Replace("\\", "/");

			if (path[path.Length - 1] == Path.DirectorySeparatorChar || path[path.Length - 1] == Path.DirectorySeparatorChar)
				path = path.Substring(0, path.Length - 1);
			if (fullPath[fullPath.Length - 1] == Path.DirectorySeparatorChar || fullPath[fullPath.Length - 1] == Path.DirectorySeparatorChar)
				fullPath = fullPath.Substring(0, fullPath.Length - 1);

			if (path == fullPath)
				path = ".";
			else if (path.StartsWith(fullPath, StringComparison.Ordinal))
				path = path.Substring(fullPath.Length + 1);
			else
				path = null;

			return path;
		}

		public static string ComputeNameHash(string value, string hashAlgorithmName = "MD5")
		{
			using var hashAlgorithm = HashAlgorithm.Create(hashAlgorithmName) ?? new MD5CryptoServiceProvider();
			var valueBytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
			var valueHash = hashAlgorithm.ComputeHash(valueBytes);
			return BitConverter.ToString(valueHash).Replace("-", "").ToLower();
		}
		public static string ComputeHash(string path, string hashAlgorithmName = "MD5", int tries = 5)
		{
			if (path == null) throw new ArgumentNullException(nameof(path));
			if (tries <= 0) throw new ArgumentOutOfRangeException(nameof(tries));

			foreach (var attempt in Enumerable.Range(1, tries))
			{
				try
				{
					using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
					using var hashAlgorithm = HashAlgorithm.Create(hashAlgorithmName) ?? new MD5CryptoServiceProvider();
					var hashBytes = hashAlgorithm.ComputeHash(fs);
					return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
				}
				catch (IOException exception)
				{
					CharonEditorModule.Instance.Logger.Log(LogType.Warning, "Attempt #" + attempt + " to compute hash of " + path + " has failed with IO error: " + exception);

					if (attempt == tries)
						throw;
				}
				Thread.Sleep(100);
			}

			return new string('0', 32); // never happens
		}

		public static string GetRandomTempDirectory(bool createIfNotExists = false)
		{
			var directoryPath = string.Empty;
			do
			{
				directoryPath = Path.Combine(TempPath, BitConverter.ToString(Guid.NewGuid().ToByteArray()).Replace("-", "").ToLower());
			} while (Directory.Exists(directoryPath));

			if (createIfNotExists)
			{
				Directory.CreateDirectory(directoryPath);
			}
			return directoryPath;
		}

		public static async Task<FileStream> ReadFileAsync(string path, int maxAttempts = 5)
		{
			if (path == null) throw new ArgumentNullException(nameof(path));
			if (maxAttempts < 1) throw new ArgumentOutOfRangeException(nameof(maxAttempts));

			var fileStream = default(FileStream);
			foreach (var attempt in Enumerable.Range(1, maxAttempts))
			{
				try
				{
					fileStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
					break;
				}
				catch (IOException openError)
				{
					CharonEditorModule.Instance.Logger.Log(LogType.Warning, "Attempt #" + attempt + " to read " + path + " file has failed with IO error: " + Environment.NewLine + openError);
				}

				fileStream?.Dispose();

				await Task.Delay(TimeSpan.FromSeconds(1));
			}
			return fileStream;
		}

		public static string SanitizeFileName(string path)
		{
			var fileName = new StringBuilder(path);
			for (var c = 0; c < fileName.Length; c++)
			{
				if (Array.IndexOf(InvalidFileNameChars, fileName[c]) != -1)
					fileName[c] = '_';
			}
			return fileName.ToString();
		}

		public static void SafeDirectoryDelete(string directoryPath)
		{
			if (directoryPath == null) throw new ArgumentNullException(nameof(directoryPath));

			try
			{
				Directory.Delete(directoryPath, recursive: true);
			}
			catch (Exception error)
			{
				CharonEditorModule.Instance.Logger.Log(LogType.Warning, $"Failed to delete directory '{directoryPath}' due error.");
				CharonEditorModule.Instance.Logger.Log(LogType.Warning, error);
			}
		}
		public static void SafeFileDelete(string filePath)
		{
			if (filePath == null) throw new ArgumentNullException(nameof(filePath));

			try
			{
				File.Delete(filePath);
			}
			catch (Exception error)
			{
				CharonEditorModule.Instance.Logger.Log(LogType.Warning,$"Failed to delete file '{filePath}' due error.");
				CharonEditorModule.Instance.Logger.Log(LogType.Warning,error);
			}
		}
	}
}
