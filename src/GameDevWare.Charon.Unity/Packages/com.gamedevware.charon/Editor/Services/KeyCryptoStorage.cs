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
using System.Security.Cryptography;
using System.Text;
using GameDevWare.Charon.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.Charon.Editor.Services
{
	public class KeyCryptoStorage
	{
		private readonly ILogger logger;
		private const string MASTER_KEY_PROPERTY_NAME = "APIKeyStorageIV";

		private readonly string baseDirectory;
		private readonly byte[] masterKey;
		private readonly byte[] initializationVector;

		public KeyCryptoStorage(ILogger logger)
		{
			this.logger = logger;
			this.baseDirectory = Path.Combine(FileHelper.LibraryCharonPath, "Keys");

			this.masterKey = InitializeMasterKey();
			this.initializationVector = Convert.FromBase64String("Much//LiKEs=");
		}

		public string GetKey(Uri host)
		{
			if (host == null) throw new ArgumentNullException(nameof(host));

			try
			{
				var keyFileName = Path.Combine(baseDirectory, GetFileNameFor(host));
				if (!File.Exists(keyFileName))
				{
					return null;
				}

				using (var fileStream = File.OpenRead(keyFileName))
				using (var cryptoStream = CreateDecryptStream(fileStream))
				using (var textStream = new StreamReader(cryptoStream, Encoding.UTF8))
				{
					return textStream.ReadToEnd().Trim('\0');
				}
			}
			catch (Exception error)
			{

				logger.Log(LogType.Warning, $"Failed to decrypt API Key for '{host}' host.");
				logger.Log(LogType.Warning, error);

				return null;
			}
		}
		public void StoreKey(Uri host, string key)
		{
			if (host == null) throw new ArgumentNullException(nameof(host));
			if (key == null) throw new ArgumentNullException(nameof(key));

			try
			{
				var keyFileName = Path.Combine(baseDirectory, GetFileNameFor(host));

				if (!Directory.Exists(baseDirectory))
				{
					Directory.CreateDirectory(baseDirectory);
				}

				using (var fileStream = File.Create(keyFileName))
				using (var cryptoStream = CreateEncryptStream(fileStream))
				using (var textStream = new StreamWriter(cryptoStream, Encoding.UTF8))
				{
					textStream.Write(key);
					textStream.Flush();
				}

				this.logger.Log(LogType.Assert, $"Successfully stored API Key for '{host}' host into file.");
			}
			catch (Exception error)
			{
				this.logger.Log(LogType.Warning, $"Failed to encrypt API Key for '{host}' host and save into file.");
				this.logger.Log(LogType.Warning, error);
			}
		}
		public void DeleteKey(Uri host)
		{
			if (host == null) throw new ArgumentNullException(nameof(host));

			try
			{
				var keyFileName = Path.Combine(baseDirectory, GetFileNameFor(host));
				if (File.Exists(keyFileName))
				{
					File.Delete(keyFileName);
				}
			}
			catch (Exception error)
			{
				this.logger.Log(LogType.Warning, $"Failed to delete API Key for '{host}' host.");
				this.logger.Log(LogType.Warning, error);
			}
		}

		private CryptoStream CreateEncryptStream(Stream fileStream)
		{
			var tripleDes = TripleDES.Create();
			tripleDes.Key = masterKey;
			tripleDes.IV = initializationVector;
			tripleDes.Mode = CipherMode.ECB;
			tripleDes.Padding = PaddingMode.PKCS7;
			var encryptStream = new CryptoStream(fileStream, tripleDes.CreateEncryptor(), CryptoStreamMode.Write);

			return encryptStream;
		}
		private CryptoStream CreateDecryptStream(Stream fileStream)
		{
			var tripleDes = TripleDES.Create();
			tripleDes.Key = masterKey;
			tripleDes.IV = initializationVector;
			tripleDes.Mode = CipherMode.ECB;
			tripleDes.Padding = PaddingMode.PKCS7;
			var decryptStream = new CryptoStream(fileStream, tripleDes.CreateDecryptor(), CryptoStreamMode.Read);

			return decryptStream;
		}

		private string GetFileNameFor(Uri host)
		{
			var hostAndPort = new StringBuilder(host.GetComponents(UriComponents.HostAndPort | UriComponents.Path, UriFormat.SafeUnescaped));
			for (var i = 0; i < hostAndPort.Length; i++)
			{
				hostAndPort[i] = char.ToLowerInvariant(hostAndPort[i]);
				if (char.IsLetterOrDigit(hostAndPort[i]))
				{
					continue;
				}
				hostAndPort[i] = '_';
			}
			return hostAndPort.ToString();
		}
		private byte[] InitializeMasterKey()
		{
			try
			{
				try
				{
					var base64Key = EditorPrefs.HasKey(MASTER_KEY_PROPERTY_NAME) ? EditorPrefs.GetString(MASTER_KEY_PROPERTY_NAME) : string.Empty;
					if (!string.IsNullOrEmpty(base64Key))
					{
						var key = Convert.FromBase64String(base64Key);
						if (key.Length != 16 && key.Length != 24)
						{
							throw new InvalidOperationException("Invalid tripleDES key. Size should be 16 or 24 bytes.");
						}
						return key;
					}
				}
				catch
				{
					/* ignore errors */
				}

				this.logger.Log(LogType.Assert, "A new Master key for API keys is created.");

				var random = RandomNumberGenerator.Create();
				var newMasterKey = new byte[24];
				random.GetNonZeroBytes(newMasterKey);

				EditorApplication.update += SaveMasterKey;

				return newMasterKey;
			}
			catch (Exception error)
			{
				this.logger.Log(LogType.Warning, "Master key from EditorPrefs initialization failed. Probably all stored API Keys will be lost.");
				this.logger.Log(LogType.Warning, error);

				return Guid.NewGuid().ToByteArray(); // random key
			}
		}
		private void SaveMasterKey()
		{
			EditorApplication.update -= SaveMasterKey;

			try
			{
				var base64NewKey = Convert.ToBase64String(masterKey);
				EditorPrefs.SetString(MASTER_KEY_PROPERTY_NAME, base64NewKey);

				this.logger.Log(LogType.Warning, "Master key for API keys saved to EditorPrefs.");
			}
			catch (Exception error)
			{
				this.logger.Log(LogType.Warning, "Master key save to EditorPrefs failed. Probably all stored API Keys will be lost.");
				this.logger.Log(LogType.Warning, error);
			}
		}
	}
}
