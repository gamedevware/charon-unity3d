/*
	Copyright (c) 2025 GameDevWare, Denis Zykov

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

		public static void SafeDirectoryDelete(DirectoryInfo directory)
		{
			if (directory == null) throw new ArgumentNullException(nameof(directory));

			SafeDirectoryDelete(directory.FullName);
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
		public static void SafeFileDelete(FileInfo file)
		{
			if (file == null) throw new ArgumentNullException(nameof(file));

			SafeFileDelete(file.FullName);
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
		public static bool HasSameContent(string sourceFilePath, string targetFilePath)
		{
			if (sourceFilePath == null) throw new ArgumentNullException(nameof(sourceFilePath));
			if (targetFilePath == null) throw new ArgumentNullException(nameof(targetFilePath));

			if (!File.Exists(sourceFilePath) || !File.Exists(targetFilePath))
			{
				return false;
			}

			var sourceBytes = default(byte[]);
			var targetBytes = default(byte[]);

			try { sourceBytes = File.ReadAllBytes(sourceFilePath); }
			catch { /* ignore read errors */ }
			try { targetBytes = File.ReadAllBytes(targetFilePath); }
			catch { /* ignore read errors */ }

			return sourceBytes != null && targetBytes != null && sourceBytes.SequenceEqual(targetBytes);
		}
	}
}
