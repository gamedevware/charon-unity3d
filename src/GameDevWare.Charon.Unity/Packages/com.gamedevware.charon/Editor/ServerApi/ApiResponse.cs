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
using System.Linq;
using System.Net;
using GameDevWare.Charon.Unity.Json;
using JetBrains.Annotations;

namespace GameDevWare.Charon.Unity.ServerApi
{
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal class ApiResponse<T>
	{
		[JsonMember("result")]
		public T Result;

		[JsonMember("errors")]
		public ApiError[] Errors;

		public T GetResponseResultOrError()
		{
			if (this.Errors != null)
			{
				throw new WebException(Resources.UI_UNITYPLUGIN_SERVER_ERROR +
					Environment.NewLine + string.Join(Environment.NewLine, this.Errors.Select(error => error.ToString()).ToArray()),
					WebExceptionStatus.UnknownError);
			}

			return this.Result;
		}
	}
}
