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
using System.Collections.Generic;
using System.Reflection;

namespace GameDevWare.Charon.Unity
{
	internal class ValidationError : Exception
	{
		private static readonly FieldInfo StackTraceField = typeof(Exception).GetField("stack_trace", BindingFlags.NonPublic | BindingFlags.Instance) ??
			typeof(Exception).GetField("_stackTraceString", BindingFlags.NonPublic | BindingFlags.Instance);

		private static readonly Dictionary<int, string> ReferenceByExceptionId = new Dictionary<int, string>();
		private static int LastExceptionId = 1;

		public ValidationError(string gameDataPath, string projectId, string branchId, string id, string schemaName, string path, string errorMessage)
			: base(string.Format("{0}->{1}, {2}", schemaName, id, errorMessage))
		{

			var exceptionId = LastExceptionId++;

			if (StackTraceField != null) // Trick Unity into thinking that error inside game data file by replacing StackTrace of this Exception
				StackTraceField.SetValue(this, path + " (<double-click to open>) (at " + gameDataPath + ":" + exceptionId + ")");

			var reference = string.Format("view/data/{0}/{1}/form/{2}/{3}", projectId, branchId, schemaName, id);
			ReferenceByExceptionId.Add(exceptionId, reference);
		}

		public static string GetReference(int exceptionId)
		{
			var reference = default(string);
			ReferenceByExceptionId.TryGetValue(exceptionId, out reference);
			return reference;
		}

	}
}
