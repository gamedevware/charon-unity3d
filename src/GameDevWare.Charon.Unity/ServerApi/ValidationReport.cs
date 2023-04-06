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
using GameDevWare.Charon.Unity.Json;
using JetBrains.Annotations;

namespace GameDevWare.Charon.Unity.ServerApi
{
	[Serializable, UsedImplicitly(ImplicitUseTargetFlags.WithMembers), PublicAPI]
	public class ValidationReport
	{
		[JsonMember("records")]
		public ValidationRecord[] Records;

		public bool HasErrors
		{
			get { return this.Records != null && this.Records.Length > 0 && this.Records.Any(r => !r.HasErrors); }
		}

		public static ValidationReport CreateErrorReport(string message)
		{
			return new ValidationReport
			{
				Records = new ValidationRecord[] {
					new ValidationRecord {
						Errors = new ValidationError[] {
							new ValidationError {
								Message = message
							}
						}
					}
				}
			};
		}
	}
}
