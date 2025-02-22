/*
	Copyright (c) 2025 Denis Zykov

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
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace GameDevWare.Charon.Editor.Utils
{
	public sealed class ToolRunOptions
	{
		public bool CaptureStandardOutput { get; set; }
		public bool CaptureStandardError { get; set; }
		public bool WaitForExit { get; set; }
		public ProcessStartInfo StartInfo { get; private set; }
		public TimeSpan ExecutionTimeout { get; set; }
		public TimeSpan TerminationTimeout { get; set; }

		public ToolRunOptions(string executablePath, params string[] arguments)
		{
			if (arguments == null) throw new ArgumentNullException(nameof(arguments));

			this.StartInfo = new ProcessStartInfo(executablePath)
			{
				Arguments = ConcatenateArguments(arguments),
				UseShellExecute = false,
				WorkingDirectory = Path.GetFullPath("./"),
				EnvironmentVariables =
				{
					{ "UNITY_PROJECT_PATH", Path.GetFullPath("./").TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar }
				},
				CreateNoWindow = true
			};
			this.WaitForExit = true;
			this.TerminationTimeout = TimeSpan.FromSeconds(5);
		}

		public static string ConcatenateArguments(params string[] arguments)
		{
			if (arguments == null) throw new ArgumentNullException(nameof(arguments));

			for (int i = 0; i < arguments.Length; i++)
			{
				var arg = arguments[i];
				if (string.IsNullOrEmpty(arg))
					continue;

				if (arg.IndexOfAny(new char[] { '"', ' ' }) != -1)
				{
					arguments[i] =
					"\"" + arg
						.Replace(@"\", @"\\")
						.Replace("\"", "\\\"") +
					"\"";
				}
			}
			return string.Join(" ", arguments);
		}

		public static string[] FlattenArguments(params object[] arguments)
		{
			if (arguments == null) throw new ArgumentNullException(nameof(arguments));

			var flattenArguments = new ArrayList();
			IterateAndAddArguments(arguments, flattenArguments);
			return (string[])flattenArguments.ToArray(typeof(string));
		}
		private static void IterateAndAddArguments(IEnumerable arguments, ArrayList flattenArguments)
		{
			if (arguments == null) throw new ArgumentNullException(nameof(arguments));

			foreach (var argument in arguments)
			{
				if (argument == null)
					continue;
				if (argument is string && string.IsNullOrEmpty((string)argument) == false)
					flattenArguments.Add((string)argument);
				else if (argument is IEnumerable)
					IterateAndAddArguments((IEnumerable)argument, flattenArguments);
				else
					flattenArguments.Add(Convert.ToString(argument, CultureInfo.InvariantCulture));
			}
		}

		public override string ToString()
		{
			return this.StartInfo.ToString();
		}
	}
}
