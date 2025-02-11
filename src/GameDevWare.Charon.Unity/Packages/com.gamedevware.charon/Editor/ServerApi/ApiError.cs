﻿/*
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

using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace GameDevWare.Charon.Editor.ServerApi
{
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal class ApiError
	{
		[DataMember(Name = "message")]
		public string Message { get; set; }
		[DataMember(Name = "code")]
		public string Code { get; set; }
	}
}
