/*
	Copyright (c) 2017 Denis Zykov

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
	[Flags]
	public enum CodeGenerationOptions
	{
		LazyReferences = 0x1 << 0,

		HideReferences = 0x1 << 1,
		HideLocalizedStrings = 0x1 << 2,

		Unused1 = 0x1 << 3,

		SuppressDocumentClass = 0x1 << 4,
		SuppressApiClass = 0x1 << 5,
		SuppressCollectionClass = 0x1 << 6,
		SuppressLocalizedStringClass = 0x1 << 7,
		SuppressReferenceClass = 0x1 << 8,
		SuppressDataContractAttributes = 0x1 << 9,
		
		DisableFormulas = 0x1 << 10,

		DisableJsonSerialization = 0x1 << 11,
		DisableMessagePackSerialization = 0x1 << 12,
		DisableBsonSerialization = 0x1 << 13,
		DisableXmlSerialization = 0x1 << 14,
		DisablePatching = 0x1 << 15,
	}
}
