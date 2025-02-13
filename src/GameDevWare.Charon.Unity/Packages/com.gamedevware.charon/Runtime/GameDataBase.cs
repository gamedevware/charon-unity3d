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
using UnityEngine;

namespace GameDevWare.Charon
{
	/// <summary>
	/// Base class for all assets that store game data.
	/// </summary>
	[PublicAPI, Serializable]
	public class GameDataBase : ScriptableObject
	{
		/// <summary>
		/// Contains all settings related to game data generation and import.
		/// </summary>
		[HideInInspector, SerializeField]
		public GameDataSettings settings;

		/// <summary>
		/// Gets the revision hash of the game data. Default implementation returns a string of 32 zeros.
		/// </summary>
		public virtual string RevisionHash => new string('0', 32);

		/// <summary>
		/// Gets the version of the game data. Default implementation returns an empty string.
		/// </summary>
		public virtual string GameDataVersion => string.Empty;

		/// <summary>
		/// Saves game data from the specified stream into this asset using the specified format.
		/// This method is primarily used for importing data in the editor.
		/// </summary>
		/// <param name="gameDataStream">The stream containing the game data.</param>
		/// <param name="format">The format of the game data stream.</param>
		public virtual void Save(Stream gameDataStream, GameDataFormat format)
		{
		}

		/// <summary>
		/// Attempts to load game data from the specified stream using the specified format.
		/// This method is primarily used for importing data in the editor. Derived classes may implement patch-enabled loading.
		/// </summary>
		/// <param name="gameDataStream">The stream containing the game data.</param>
		/// <param name="format">The format of the game data stream.</param>
		/// <returns>True if the data was successfully loaded; otherwise, false.</returns>
		public virtual bool TryLoad(Stream gameDataStream, GameDataFormat format)
		{
			return false;
		}

		/// <summary>
		/// Finds a document of the specified schema by its ID.
		/// </summary>
		/// <param name="schemaNameOrId">The name or ID of the schema.</param>
		/// <param name="id">The ID of the document.</param>
		/// <returns>The document if found; otherwise, null. Used by <see cref="GameDataDocumentReference"/>.</returns>
		public virtual object FindGameDataDocumentById(string schemaNameOrId, string id)
		{
			return null;
		}

		/// <summary>
		/// Retrieves all document IDs for the specified schema. Used by <see cref="GameDataDocumentReferenceDrawer"/>.
		/// </summary>
		/// <param name="schemaNameOrId">The name or ID of the schema.</param>
		/// <returns>A list of all document IDs for the specified schema.</returns>
		public virtual IEnumerable<string> GetDocumentIds(string schemaNameOrId)
		{
			return Array.Empty<string>();
		}

		/// <summary>
		/// Retrieves all schema names defined in the derived game data class. Used by <see cref="GameDataDocumentReferenceDrawer"/>.
		/// </summary>
		/// <returns>A list of all schema names.</returns>
		public virtual IEnumerable<string> GetDocumentSchemaNames()
		{
			return Array.Empty<string>();
		}
	}
}
