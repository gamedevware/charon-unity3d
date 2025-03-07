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
using GameDevWare.Charon.Editor.Cli;

namespace GameDevWare.Charon.Editor.Utils
{
	internal static class FormatsExtensions
	{
		public static string GetFormatName(this ExportFormat exportFormat)
		{
			return exportFormat switch {
				ExportFormat.Json => CharonCli.FORMAT_JSON,
				ExportFormat.MessagePack => CharonCli.FORMAT_MESSAGE_PACK,
				ExportFormat.Bson => CharonCli.FORMAT_BSON,
				ExportFormat.Xslx => CharonCli.FORMAT_XLSX,
				ExportFormat.Xliff1 => CharonCli.FORMAT_XLIFF1,
				ExportFormat.Xliff2 => CharonCli.FORMAT_XLIFF2,
				_ => throw new ArgumentOutOfRangeException(nameof(exportFormat), exportFormat, null)
			};
		}
		public static string GetFormatName(this BackupFormat backupFormat)
		{
			return backupFormat switch {
				BackupFormat.Json => CharonCli.FORMAT_JSON,
				BackupFormat.MessagePack => CharonCli.FORMAT_MESSAGE_PACK,
				_ => throw new ArgumentOutOfRangeException(nameof(backupFormat), backupFormat, null)
			};
		}

		public static string GetExtensionFromGameDataFormat(this GameDataFormat storeFormat)
		{
			return storeFormat switch {
				GameDataFormat.Json => ".gdjs",
				GameDataFormat.MessagePack => ".gdmp",
				_ => throw new ArgumentOutOfRangeException()
			};
		}
		public static GameDataFormat? GetGameDataFormatForExtension(string fileName)
		{
			if (fileName == null) throw new ArgumentNullException(nameof(fileName));

			return Path.GetExtension(fileName) switch {
				".json" or ".gdjs" => GameDataFormat.Json,
				".msgpkg" or ".msgpack" or ".gdmp" => GameDataFormat.MessagePack,
				_ => null
			};
		}
		public static GameDataFormat? GetGameDataFormatForContentType(string contentType)
		{
			return contentType.ToLowerInvariant() switch {
				"application/json" => GameDataFormat.Json,
				"application/x-msgpack" => GameDataFormat.MessagePack,
				_ => null
			};
		}
	}
}
