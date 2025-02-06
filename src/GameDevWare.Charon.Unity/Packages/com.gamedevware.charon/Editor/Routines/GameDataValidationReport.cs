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
using GameDevWare.Charon.Unity.ServerApi;

namespace GameDevWare.Charon.Unity.Routines
{
	public class GameDataValidationReport
	{
		public readonly string GameDataPath;
		public readonly ValidationReport Report;

		public GameDataValidationReport(string gameDataPath, ValidationReport report)
		{
			if (gameDataPath == null) throw new ArgumentNullException("gameDataPath");
			if (report == null) throw new ArgumentNullException("report");

			this.GameDataPath = gameDataPath;
			this.Report = report;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return string.Format("{0}, Has Errors: {1}", this.GameDataPath, this.Report.HasErrors);
		}
	}
}
