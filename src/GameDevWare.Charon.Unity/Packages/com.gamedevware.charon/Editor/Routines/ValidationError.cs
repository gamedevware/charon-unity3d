/*
	Copyright (c) 2025 GameDevWare, Denis Zykov

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Reflection;

namespace GameDevWare.Charon.Editor.Routines
{
	internal class ValidationError : Exception
	{
		private static readonly FieldInfo StackTraceField = typeof(Exception).GetField("stack_trace", BindingFlags.NonPublic | BindingFlags.Instance) ??
			typeof(Exception).GetField("_stackTraceString", BindingFlags.NonPublic | BindingFlags.Instance);

		private static readonly Dictionary<int, string> ReferenceByExceptionId = new Dictionary<int, string>();
		private static int LastExceptionId = 1;

		public ValidationError(string gameDataPath, string projectId, string branchId, string id, string schemaName, string path, string errorMessage)
			: base($"{schemaName}->{id}, {errorMessage}")
		{

			var exceptionId = LastExceptionId++;

			if (StackTraceField != null) // Trick Unity into thinking that error inside game data file by replacing StackTrace of this Exception
				StackTraceField.SetValue(this, path + " (<double-click to open>) (at " + gameDataPath + ":" + exceptionId + ")");

			var reference = $"view/data/{projectId}/{branchId}/form/{schemaName}/{id}";
			ReferenceByExceptionId.Add(exceptionId, reference);
		}

		public static string GetReference(int exceptionId)
		{
			ReferenceByExceptionId.TryGetValue(exceptionId, out var reference);
			return reference;
		}

	}
}
