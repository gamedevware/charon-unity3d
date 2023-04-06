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
