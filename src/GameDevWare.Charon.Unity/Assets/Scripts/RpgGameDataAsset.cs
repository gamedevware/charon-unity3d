//------------------------------------------------------------------------------
// <auto-generated>
//	 This code was generated by a tool.
//	 Changes to this file may cause incorrect behavior and will be lost if
//	 the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

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

// ReSharper disable All

namespace Assets.Scripts
{
	using System;
	using System.IO;
	using System.Collections.Generic;
	using System.Text;
	using UnityEngine;
	using Unity.Collections;
	using GameDevWare.Charon;

	[Serializable, PreferBinarySerialization]
    [global::System.CodeDom.Compiler.GeneratedCode("gamedevware-charon-editor", "2025.1.1.0")]
	public partial class RpgGameDataAsset : GameDataBase, ISerializationCallbackReceiver
	{
		[SerializeField, HideInInspector]
		private byte[] gameDataBytes;
		[SerializeField, HideInInspector]
		private GameDataFormat format;
		[NonSerialized, HideInInspector]
		public Assets.Scripts.RpgGameData GameData;

		/// <inheritdoc />
		public override string RevisionHash => this.GameData?.RevisionHash;
		/// <inheritdoc />
		public override string GameDataVersion => this.GameData?.GameDataVersion;

		private RpgGameDataAsset()
		{

		}

		/// <inheritdoc />
		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
			this.OnBeforeSerialize();
		}
		/// <inheritdoc />
		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			this.OnAfterDeserialize();

			if (this.gameDataBytes == null || this.gameDataBytes.Length == 0)
			{
				return;
			}

			using var gameDataStream = new MemoryStream(this.gameDataBytes, 0, this.gameDataBytes.Length, writable: false);
			if (!this.TryLoad(gameDataStream, this.format))
			{
				throw new InvalidOperationException(string.Format("Unknown file format '{0}'.", this.format));
			}
		}

		/// <inheritdoc />
		public override void Save(Stream gameDataStream, GameDataFormat format)
		{
			if (gameDataStream == null) throw new ArgumentNullException(nameof(gameDataStream));

			this.OnBeforeSave(ref gameDataStream, ref format);

			this.format = format;
			this.gameDataBytes = new byte[(int)gameDataStream.Length];
			gameDataStream.Position = 0;

			var offset = 0;
			var read = 0;
			while ((read = gameDataStream.Read(this.gameDataBytes, offset, this.gameDataBytes.Length - offset)) > 0 &&
				offset < this.gameDataBytes.Length)
			{
				offset += read;
			}

			if (offset != this.gameDataBytes.Length) throw new InvalidOperationException("Failed to read whole stream into byte array.");

			this.OnAfterSave();

			using var gameDataMemoryStream = new MemoryStream(this.gameDataBytes, 0, this.gameDataBytes.Length, writable: false);
			if (!this.TryLoad(gameDataMemoryStream, this.format))
			{
				throw new InvalidOperationException(string.Format("Unknown file format '{0}'.", this.format));
			}
		}

		/// <inheritdoc />
		public override bool TryLoad(Stream gameDataStream, GameDataFormat format)
		{
			if (gameDataStream == null) throw new ArgumentNullException(nameof(gameDataStream));

			this.OnBeforeSave(ref gameDataStream, ref format);

			var success = false;
			switch (format)
			{
				case GameDataFormat.Json:
					this.GameData = new Assets.Scripts.RpgGameData(gameDataStream, new Formatters.GameDataLoadOptions { Format = Formatters.GameDataFormat.Json });
					success = true;
					break;
				case GameDataFormat.MessagePack:
					this.GameData = new Assets.Scripts.RpgGameData(gameDataStream, new Formatters.GameDataLoadOptions { Format = Formatters.GameDataFormat.MessagePack });
					success = true;
					break;
				default:
					success = false;
					break;
			}

			this.OnAfterLoad(ref success);
			return success;
		}

		/// <inheritdoc />
		public override object FindGameDataDocumentById(string schemaNameOrId, string id)
		{
			if (schemaNameOrId == null) throw new ArgumentNullException(nameof(schemaNameOrId));

			return this.GameData.FindDocument(schemaNameOrId, id);
		}
		/// <inheritdoc />
		public override IEnumerable<string> GetDocumentIds(string schemaNameOrId)
		{
			if (schemaNameOrId == null) throw new ArgumentNullException(nameof(schemaNameOrId));

			return this.GameData.GetDocumentIds(schemaNameOrId);
		}
		/// <inheritdoc />
		public override IEnumerable<string> GetDocumentSchemaNames()
		{
			return this.GameData.GetDocumentSchemaNames();
		}

		/// <summary>
		/// Extension method invoked at the start of the <see cref="Save"/> method.
		/// </summary>
		/// <param name="gameDataStream">The data stream used to save the asset. Must be a seekable stream with a known length.</param>
		/// <param name="format">The format of the game data stream.</param>
		partial void OnBeforeSave(ref Stream gameDataStream, ref GameDataFormat format);

		/// <summary>
		/// Extension method invoked at the end of the <see cref="Save"/> method.
		/// </summary>
		partial void OnAfterSave();

		/// <summary>
		/// Extension method invoked at the start of the <see cref="TryLoad"/> method.
		/// </summary>
		/// <param name="gameDataStream">The data stream used to load the asset. Must be a seekable stream with a known length.</param>
		/// <param name="format">The format of the game data stream.</param>
		partial void OnBeforeLoad(ref Stream gameDataStream, ref GameDataFormat format);

		/// <summary>
		/// Extension method invoked at the end of the <see cref="TryLoad"/> method.
		/// </summary>
		/// <param name="success">A flag indicating whether the load operation was successful.</param>
		partial void OnAfterLoad(ref bool success);

		/// <summary>
		/// Extension method invoked at the end of the <see cref="ISerializationCallbackReceiver.OnAfterDeserialize"/> method.
		/// </summary>
		partial void OnAfterDeserialize();

		/// <summary>
		/// Extension method invoked at the end of the <see cref="ISerializationCallbackReceiver.OnBeforeSerialize"/> method.
		/// </summary>
		partial void OnBeforeSerialize();
	}
}

