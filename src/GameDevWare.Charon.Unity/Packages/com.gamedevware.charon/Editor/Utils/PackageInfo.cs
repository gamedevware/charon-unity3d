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

namespace GameDevWare.Charon.Unity.Utils
{
	internal class PackageInfo : IComparable, IComparable<PackageInfo>
	{
		public string Id { get; set; }
		public SemanticVersion Version { get; set; }

		/// <inheritdoc />
		public int CompareTo(object obj)
		{
			var other = (obj as PackageInfo);
			return this.CompareTo(other);
		}
		/// <inheritdoc />
		public int CompareTo(PackageInfo other)
		{
			if (other == null)
			{
				return 1;
			}
			return this.Version.CompareTo(other.Version);
		}
		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			var other = (obj as PackageInfo);
			if (other == null)
				return false;

			return this.Version == other.Version;
		}
		/// <inheritdoc />
		public override int GetHashCode()
		{
			return this.Version.GetHashCode();
		}
	}
}
