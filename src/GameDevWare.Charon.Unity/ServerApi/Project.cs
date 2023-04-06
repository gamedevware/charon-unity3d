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

namespace GameDevWare.Charon.Unity.ServerApi
{
	[Serializable]
	internal class Project
	{
		[JsonMember("id")]
		public string Id;
		[JsonMember("name")]
		public string Name;
		[JsonMember("pictureUrl")]
		public string PictureUrl;
		[JsonMember("branches")]
		public Branch[] Branches;
	}
}
