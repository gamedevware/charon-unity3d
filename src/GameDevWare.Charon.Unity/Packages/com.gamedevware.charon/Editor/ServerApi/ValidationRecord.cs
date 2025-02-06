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
using GameDevWare.Charon.Unity.Json;
using JetBrains.Annotations;

namespace GameDevWare.Charon.Unity.ServerApi
{
	[Serializable, UsedImplicitly(ImplicitUseTargetFlags.WithMembers), PublicAPI]
	public class ValidationRecord
	{
		[JsonMember("id")]
		public object Id;
		[JsonMember("entityName"), Obsolete]
		public string EntityName;
		[JsonMember("entityId"), Obsolete]
		public string EntityId;
		[JsonMember("schemaName")]
		public string SchemaName;
		[JsonMember("schemaId")]
		public string SchemaId;
		[JsonMember("errors")]
		public ValidationError[] Errors;

		public bool HasErrors
		{
			get { return this.Errors == null || this.Errors.Length == 0; }
		}
	}
}