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
using System.Collections.Generic;
using System.Reflection;

namespace GameDevWare.Charon.Unity
{
	internal class ValidationException : Exception
	{
		private static readonly FieldInfo StackTraceField = typeof(Exception).GetField("stack_trace", BindingFlags.NonPublic | BindingFlags.Instance);
		private static readonly Dictionary<int, string> ReferenceByExceptionId = new Dictionary<int, string>();
		private static int LastExceptionId = 1;

		public ValidationException(string gameDataPath, string id, string entityName, string path, string msg)
			: base(string.Format("{0}:{1} {2}", entityName, id, msg))
		{

			var exceptionId = LastExceptionId++;
			StackTraceField.SetValue(this, path + "() (at " + gameDataPath + ":" + exceptionId + ")");

			var reference = "#edit/" + entityName + "/" + id;
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
