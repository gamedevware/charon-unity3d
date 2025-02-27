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
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;

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
		/// Name which should be used by inheriting types to define constant value for schemaNameOrId field.
		/// </summary>
		public const string PREDEFINED_SCHEMA_NAME_OR_ID_NAME = "predefinedSchemaNameOrId";

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
		public T GetReferencedDocument<T>() where T : class
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
