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
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using Unity.Collections;
using UnityEngine;

namespace GameDevWare.Charon
{
	[PublicAPI, Serializable]
	public class GameDataBase: ScriptableObject
	{
		protected sealed class NativeArrayStream : Stream
		{
			private readonly NativeArray<byte> nativeArray;
			private int position;
			private readonly int length;

			/// <inheritdoc />
			public override bool CanRead => true;
			/// <inheritdoc />
			public override bool CanSeek => true;
			/// <inheritdoc />
			public override bool CanWrite => false;
			/// <inheritdoc />
			public override long Length => this.length;
			/// <inheritdoc />
			public override long Position { get => this.position; set => this.position = (int)Math.Max(0, Math.Max(this.Length, value)); }

			public NativeArrayStream(NativeArray<byte> nativeArray)
			{
				this.nativeArray = nativeArray;
				this.length = nativeArray.Length;
			}

			/// <inheritdoc />
			public override int Read(byte[] buffer, int offset, int count)
			{
				if (buffer == null) throw new ArgumentNullException(nameof(buffer));
				if (offset < 0 || offset > buffer.Length) throw new ArgumentOutOfRangeException(nameof(offset));
				if (offset + count > buffer.Length) throw new ArgumentOutOfRangeException(nameof(count));

				var toCopy = Math.Min(count, this.length - this.position);
				NativeArray<byte>.Copy(this.nativeArray, this.position, buffer, offset, toCopy);
				this.position += toCopy;
				return toCopy;
			}

			/// <inheritdoc />
			public override long Seek(long offset, SeekOrigin origin)
			{
				switch (origin)
				{
					case SeekOrigin.Begin: this.Position = offset; break;
					case SeekOrigin.Current: this.Position += offset; break;
					case SeekOrigin.End: this.Position = this.Length - offset; break;
					default: throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
				}

				return this.Position;
			}

			/// <inheritdoc />
			public override void SetLength(long value)
			{
				throw new NotSupportedException();
			}
			/// <inheritdoc />
			public override void Write(byte[] buffer, int offset, int count)
			{
				throw new NotSupportedException();
			}
			/// <inheritdoc />
			public override void Flush()
			{
			}
		}

		/// <summary>
		/// All game data generation/import related settings
		/// </summary>
		[HideInInspector, SerializeField] public GameDataSettings settings;

		public virtual string RevisionHash => new string('0', 32);
		public virtual string GameDataVersion => string.Empty;

		/// <summary>
		/// Save game data from specified game data bytes using specified file format into this asset.
		/// Used for import from editor.
		/// </summary>
		/// <param name="gameDataBytes">Game data bytes.</param>
		/// <param name="format">Format of game data bytes.</param>
		public void Save(NativeArray<byte> gameDataBytes, GameDataFormat format)
		{
			this.Save(new NativeArrayStream(gameDataBytes), format);
		}

		/// <summary>
		/// Save game data from specified game data file/stream using specified file format into this asset.
		/// Used for import from editor.
		/// </summary>
		/// <param name="gameDataStream">Game data stream.</param>
		/// <param name="format">Format of game data stream.</param>
		public virtual void Save(Stream gameDataStream, GameDataFormat format)
		{
		}

		/// <summary>
		/// Try to load game data from specified game data bytes using specified file format.
		/// Used for import from editor. Patch-enabled load method is defined on derived class.
		/// </summary>
		/// <param name="gameDataBytes">Game data bytes.</param>
		/// <param name="format">Format of game data bytes.</param>
		/// <returns>True if successfully loaded. False if not.</returns>
		public bool TryLoad(NativeArray<byte> gameDataBytes, GameDataFormat format)
		{
			return TryLoad(new NativeArrayStream(gameDataBytes), format);
		}

		/// <summary>
		/// Try to load game data from specified game data file/stream using specified file format.
		/// Used for import from editor. Patch-enabled load method is defined on derived class.
		/// </summary>
		/// <param name="gameDataStream">Game data stream.</param>
		/// <param name="format">Format of game data stream.</param>
		/// <returns>True if successfully loaded. False if not.</returns>
		public virtual bool TryLoad(Stream gameDataStream, GameDataFormat format)
		{
			return false;
		}

		/// <summary>
		///  Find document of specified <paramref name="schemaNameOrId"/> by <paramref name="id"/>.
		/// </summary>
		/// <param name="schemaNameOrId">Name or id of the schema.</param>
		/// <param name="id">Id of document.</param>
		/// <returns>Returns nullptr if document or schema is not found. Used by GameDataDocumentReference.</returns>
		public virtual object FindGameDataDocumentById(string schemaNameOrId, string id)
		{
			return null;
		}

		/// <summary>
		/// Get all document ids by specified <paramref name="schemaNameOrId"/>. Used by GameDataDocumentReferenceDrawer.
		/// </summary>
		/// <param name="schemaNameOrId"></param>
		/// <returns>Returns list of all IDs for specified schema.</returns>
		public virtual IEnumerable<string> GetDocumentIds(string schemaNameOrId)
		{
			return Array.Empty<string>();
		}

		/// <summary>
		/// Get all schema names defined in derived game data class. Used by GameDataDocumentReferenceDrawer.
		/// </summary>
		/// <returns>Returns list of all schema names.</returns>
		public virtual IEnumerable<string> GetDocumentSchemaNames()
		{
			return Array.Empty<string>();
		}
	}
}
