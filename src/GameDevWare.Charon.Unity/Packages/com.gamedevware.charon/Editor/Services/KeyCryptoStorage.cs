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
		private byte[] masterKey;
		private readonly byte[] initializationVector;

		public KeyCryptoStorage(ILogger logger)
		{
			this.logger = logger;
			this.baseDirectory = Path.Combine(CharonFileUtils.LibraryCharonPath, "Keys");

			this.masterKey = new byte[32];
			this.initializationVector = Convert.FromBase64String("Much//LiKEs=");
		}

		public void Initialize()
		{
			this.masterKey = this.InitializeMasterKey();
		}

		public string GetKey(Uri host)
		{
			if (host == null) throw new ArgumentNullException(nameof(host));

			try
			{
				var keyFileName = Path.Combine(this.baseDirectory, this.GetFileNameFor(host));
				if (!File.Exists(keyFileName))
				{
					return null;
				}

				using (var fileStream = File.OpenRead(keyFileName))
				using (var cryptoStream = this.CreateDecryptStream(fileStream))
				using (var textStream = new StreamReader(cryptoStream, Encoding.UTF8))
				{
					return textStream.ReadToEnd().Trim('\0');
				}
			}
			catch (Exception error)
			{

				this.logger.Log(LogType.Warning, $"Failed to decrypt API Key for '{host}' host.");
				this.logger.Log(LogType.Warning, error);

				return null;
			}
		}
		public void StoreKey(Uri host, string key)
		{
			if (host == null) throw new ArgumentNullException(nameof(host));
			if (key == null) throw new ArgumentNullException(nameof(key));

			try
			{
				var keyFileName = Path.Combine(this.baseDirectory, this.GetFileNameFor(host));

				if (!Directory.Exists(this.baseDirectory))
				{
					Directory.CreateDirectory(this.baseDirectory);
				}

				using (var fileStream = File.Create(keyFileName))
				using (var cryptoStream = this.CreateEncryptStream(fileStream))
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
				var keyFileName = Path.Combine(this.baseDirectory, this.GetFileNameFor(host));
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
			tripleDes.Key = this.masterKey;
			tripleDes.IV = this.initializationVector;
			tripleDes.Mode = CipherMode.ECB;
			tripleDes.Padding = PaddingMode.PKCS7;
			var encryptStream = new CryptoStream(fileStream, tripleDes.CreateEncryptor(), CryptoStreamMode.Write);

			return encryptStream;
		}
		private CryptoStream CreateDecryptStream(Stream fileStream)
		{
			var tripleDes = TripleDES.Create();
			tripleDes.Key = this.masterKey;
			tripleDes.IV = this.initializationVector;
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

				EditorApplication.update += this.SaveMasterKey;

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
			EditorApplication.update -= this.SaveMasterKey;

			try
			{
				var base64NewKey = Convert.ToBase64String(this.masterKey);
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
