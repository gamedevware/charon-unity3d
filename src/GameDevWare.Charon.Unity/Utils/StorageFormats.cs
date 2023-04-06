/*
	Copyright (c) 2023 Denis Zykov

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

namespace GameDevWare.Charon.Unity.Utils
{
	internal static class StorageFormats
	{
		public static string GetStoreFormatExtension(GameDataStoreFormat storeFormat)
		{
			switch (storeFormat)
			{
				case GameDataStoreFormat.Json:
					return ".gdjs";
				case GameDataStoreFormat.MessagePack:
					return ".gdmp";
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
		public static GameDataStoreFormat? GetStoreFormat(string fileName)
		{
			if (fileName == null) throw new ArgumentNullException("fileName");

			switch (Path.GetExtension(fileName))
			{
				case ".gdjs": return GameDataStoreFormat.Json;
				case ".gdmp": return GameDataStoreFormat.MessagePack;
			}

			return null;
		}
	}
}
