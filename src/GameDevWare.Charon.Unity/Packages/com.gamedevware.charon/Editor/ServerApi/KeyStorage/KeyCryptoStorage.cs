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
using System.Security.Cryptography;
using System.Text;
using UnityEditor;

namespace GameDevWare.Charon.Unity.ServerApi.KeyStorage
{
	public static class KeyCryptoStorage
	{
		private const string MASTER_KEY_PROPERTY_NAME = "APIKeyStorageIV";

		private static readonly string BaseDirectory;
		private static byte[] MasterKey;
		private static byte[] InitializationVector;

		static KeyCryptoStorage()
		{
			BaseDirectory = Path.Combine(Settings.LibraryCharonPath, "Keys");

			MasterKey = InitializeMasterKey();
			InitializationVector = Convert.FromBase64String("Much//LiKEs=");
		}

		public static string GetKey(Uri host)
		{
			if (host == null) throw new ArgumentNullException("host");

			try
			{
				var keyFileName = Path.Combine(BaseDirectory, GetFileNameFor(host));
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
				if (Settings.Current.Verbose)
				{
					UnityEngine.Debug.LogWarning(string.Format("Failed to decrypt API Key for '{0}' host.", host));
					UnityEngine.Debug.LogWarning(error);
				}

				return null;
			}
		}
		public static void StoreKey(Uri host, string key)
		{
			if (host == null) throw new ArgumentNullException("host");
			if (key == null) throw new ArgumentNullException("key");

			try
			{
				var keyFileName = Path.Combine(BaseDirectory, GetFileNameFor(host));

				if (!Directory.Exists(BaseDirectory))
				{
					Directory.CreateDirectory(BaseDirectory);
				}

				using (var fileStream = File.Create(keyFileName))
				using (var cryptoStream = CreateEncryptStream(fileStream))
				using (var textStream = new StreamWriter(cryptoStream, Encoding.UTF8))
				{
					textStream.Write(key);
					textStream.Flush();
				}

				if (Settings.Current.Verbose)
				{
					UnityEngine.Debug.Log(string.Format("Successfully stored API Key for '{0}' host into file.", host));
				}
			}
			catch (Exception error)
			{
				if (Settings.Current.Verbose)
				{
					UnityEngine.Debug.LogWarning(string.Format("Failed to encrypt API Key for '{0}' host and save into file.", host));
					UnityEngine.Debug.LogWarning(error);
				}
			}
		}
		public static void DeleteKey(Uri host)
		{
			if (host == null) throw new ArgumentNullException("host");

			try
			{
				var keyFileName = Path.Combine(BaseDirectory, GetFileNameFor(host));
				if (File.Exists(keyFileName))
				{
					File.Delete(keyFileName);
				}
			}
			catch (Exception error)
			{
				if (Settings.Current.Verbose)
				{
					UnityEngine.Debug.LogWarning(string.Format("Failed to delete API Key for '{0}' host.", host));
					UnityEngine.Debug.LogWarning(error);
				}
			}
		}

		private static CryptoStream CreateEncryptStream(Stream fileStream)
		{
			var tripleDes = TripleDES.Create();
			tripleDes.Key = MasterKey;
			tripleDes.IV = InitializationVector;
			tripleDes.Mode = CipherMode.ECB;
			tripleDes.Padding = PaddingMode.PKCS7;
			var encryptStream = new CryptoStream(fileStream, tripleDes.CreateEncryptor(), CryptoStreamMode.Write);

			return encryptStream;
		}
		private static CryptoStream CreateDecryptStream(Stream fileStream)
		{
			var tripleDes = TripleDES.Create();
			tripleDes.Key = MasterKey;
			tripleDes.IV = InitializationVector;
			tripleDes.Mode = CipherMode.ECB;
			tripleDes.Padding = PaddingMode.PKCS7;
			var decryptStream = new CryptoStream(fileStream, tripleDes.CreateDecryptor(), CryptoStreamMode.Read);
			
			return decryptStream;
		}

		private static string GetFileNameFor(Uri host)
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
		private static byte[] InitializeMasterKey()
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

				if (Settings.Current.Verbose)
				{
					UnityEngine.Debug.Log("A new Master key for API keys is created.");
				}

				var random = RandomNumberGenerator.Create();
				var newMasterKey = new byte[24];
				random.GetNonZeroBytes(newMasterKey);

				EditorApplication.update += SaveMasterKey;

				return newMasterKey;
			}
			catch (Exception error)
			{
				UnityEngine.Debug.LogError("Master key from EditorPrefs initialization failed. Probably all stored API Keys will be lost.");
				UnityEngine.Debug.LogError(error);

				return Guid.NewGuid().ToByteArray(); // random key
			}
		}
		private static void SaveMasterKey()
		{
			EditorApplication.update -= SaveMasterKey;

			try
			{
				var base64NewKey = Convert.ToBase64String(MasterKey);
				EditorPrefs.SetString(MASTER_KEY_PROPERTY_NAME, base64NewKey);

				if (Settings.Current.Verbose)
				{
					UnityEngine.Debug.Log("Master key for API keys saved to EditorPrefs.");
				}
			}
			catch (Exception error)
			{
				UnityEngine.Debug.LogError("Master key save to EditorPrefs failed. Probably all stored API Keys will be lost.");
				UnityEngine.Debug.LogError(error);
			}
		}
	}
}
