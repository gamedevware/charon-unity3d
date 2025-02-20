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
