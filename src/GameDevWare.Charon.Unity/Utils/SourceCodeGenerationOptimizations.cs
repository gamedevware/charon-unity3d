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
using JetBrains.Annotations;

namespace GameDevWare.Charon.Unity.Utils
{
	[Flags]
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public enum SourceCodeGenerationOptimizations
	{
		/// <summary>
		/// Enumeration value - eagerReferenceResolution.
		/// </summary>
		EagerReferenceResolution = 1 << 0,
		/// <summary>
		/// Enumeration value - rawReferences.
		/// </summary>
		RawReferences = 1 << 1,
		/// <summary>
		/// Enumeration value - rawLocalizedStrings.
		/// </summary>
		RawLocalizedStrings = 1 << 2,
		/// <summary>
		/// Enumeration value - disableStringOptimization.
		/// </summary>
		DisableStringOptimization = 1 << 3,
		/// <summary>
		/// Enumeration value - disableJsonSerialization.
		/// </summary>
		DisableJsonSerialization = 1 << 4,
		/// <summary>
		/// Enumeration value - disableMessagePackSerialization.
		/// </summary>
		DisableMessagePackSerialization = 1 << 5,
		/// <summary>
		/// Enumeration value - disablePatching.
		/// </summary>
		DisablePatching = 1 << 6,
		/// <summary>
		/// Enumeration value - disableFormulas.
		/// </summary>
		DisableFormulaCompilation = 1 << 7,
	}
}
