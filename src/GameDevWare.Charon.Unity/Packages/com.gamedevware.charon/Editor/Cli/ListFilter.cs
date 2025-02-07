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

namespace GameDevWare.Charon.Editor.Cli
{
	public class ListFilter
	{
		public string PropertyName;
		public ListFilterOperation Operation;
		public string Value;

		public ListFilter(string propertyName, ListFilterOperation operation, string value)
		{
			if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));
			if (value == null) throw new ArgumentNullException(nameof(value));

			this.PropertyName = propertyName;
			this.Operation = operation;
			this.Value = value;
		}

		public string GetOperationName()
		{
			switch (this.Operation)
			{
				case ListFilterOperation.GreaterThan: return "GreaterThan";
				case ListFilterOperation.GreaterThanOrEqual: return "GreaterThanOrEqual";
				case ListFilterOperation.LessThan: return "LessThan";
				case ListFilterOperation.LessThanOrEqual: return "LessThanOrEqual";
				case ListFilterOperation.Equal: return "Equal";
				case ListFilterOperation.NotEqual: return "NotEqual";
				case ListFilterOperation.Like: return "Like";
				case ListFilterOperation.In: return "In";
				default: return "INVALID_OPERATION";
			}
		}

		public string GetValueQuoted()
		{
			bool isQuoted = this.Value.StartsWith("\"") && this.Value.EndsWith("\"");
			bool bHasInvalidChars = !isQuoted && (this.Value.IndexOf(" ", StringComparison.Ordinal) >= 0 || this.Value.IndexOf("\"", StringComparison.Ordinal) > 0);

			if (string.IsNullOrEmpty(this.Value))
			{
				return "\"\"";
			}
			else if (bHasInvalidChars)
			{
				return string.Concat("\"", this.Value.Replace("\"", "\"\""), "\"");
			}
			else
			{
				return this.Value;
			}
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.PropertyName + " " + this.GetOperationName() + " " + this.GetValueQuoted();
		}
	}
}
