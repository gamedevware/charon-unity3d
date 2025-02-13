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
using System.Linq;
using JetBrains.Annotations;

// ReSharper disable once SuspiciousTypeConversion.Global

namespace GameDevWare.Charon
{
	/// <summary>
	/// Represents a general reference to a document within game data.
	/// Use this type as a field in Unity ScriptableObjects or Components and configure it in the inspector.
	/// The referenced document can be retrieved using the <see cref="GameDataDocumentReference.GetReferencedDocument{T}"/> method.
	/// </summary>
	[PublicAPI, Serializable]
	public class GameDataDocumentReference
	{
		/// <summary>
		/// The identifier of the referenced document.
		/// </summary>
		[CanBeNull]
		public string id;

		/// <summary>
		/// The schema or type of the referenced document.
		/// </summary>
		[CanBeNull]
		public string schemaNameOrId;

		/// <summary>
		/// The game data asset containing the referenced document.
		/// </summary>
		[CanBeNull]
		public GameDataBase gameData;

		/// <summary>
		/// Indicates whether the reference is unpopulated.
		/// Returns true if the reference lacks an ID, schema name, or game data asset.
		/// </summary>
		public bool IsEmpty => string.IsNullOrEmpty(this.id) || string.IsNullOrEmpty(this.schemaNameOrId) || this.gameData == null;

		/// <summary>
		/// Indicates whether the reference points to an existing document.
		/// Returns true if the reference is populated and the document exists in the game data asset.
		/// </summary>
		public bool IsValid => !this.IsEmpty &&
			this.gameData!.GetDocumentSchemaNames().Contains(this.schemaNameOrId, StringComparer.Ordinal) &&
			this.gameData.GetDocumentIds(this.schemaNameOrId).Contains(this.id, StringComparer.Ordinal);

		/// <summary>
		/// Retrieves the document of type <typeparamref name="T"/> referenced by this instance.
		/// </summary>
		/// <typeparam name="T">The type of the document.</typeparam>
		/// <returns>The referenced document if valid; otherwise, null.</returns>
		[CanBeNull]
		public object GetReferencedDocument<T>() where T : class
		{
			if (this.IsEmpty)
			{
				return null;
			}

			return (T)this.gameData?.FindGameDataDocumentById(this.schemaNameOrId, this.id);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.IsEmpty ? "<empty>" : $"{this.schemaNameOrId}, Id: {this.id}";
		}
	}
}
