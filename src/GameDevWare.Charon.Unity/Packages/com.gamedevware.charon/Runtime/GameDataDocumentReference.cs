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
using JetBrains.Annotations;
using UnityEngine;

// ReSharper disable once SuspiciousTypeConversion.Global

namespace GameDevWare.Charon
{
	[PublicAPI, Serializable]
	public class GameDataDocumentReference
	{
		[CanBeNull]
		public string id;
		[CanBeNull]
		public string schemaNameOrId;
		[CanBeNull]
		public GameDataBase gameData;

		public bool IsValid => !string.IsNullOrEmpty(this.id) && !string.IsNullOrEmpty(this.schemaNameOrId) && this.gameData != null;

		[CanBeNull]
		public object GetReferencedDocument<T>() where T : class
		{
			if (!this.IsValid)
			{
				return null;
			}

			return (T)this.gameData?.FindGameDataDocumentById(this.schemaNameOrId, this.id);
		}
	}
}
