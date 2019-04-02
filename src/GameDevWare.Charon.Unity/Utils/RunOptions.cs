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
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace GameDevWare.Charon.Unity.Utils
{
	public sealed class RunOptions
	{
		public bool RequireDotNetRuntime { get; set; }
		public bool CaptureStandardOutput { get; set; }
		public bool CaptureStandardError { get; set; }
		public bool WaitForExit { get; set; }
		public ProcessStartInfo StartInfo { get; private set; }
		public TimeSpan ExecutionTimeout { get; set; }
		public TimeSpan TerminationTimeout { get; set; }
		internal bool Schedule { get; set; }

		public RunOptions(string executablePath, params string[] arguments)
		{
			if (arguments == null) throw new ArgumentNullException("arguments");

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
			if (arguments == null) throw new ArgumentNullException("arguments");

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
			if (arguments == null) throw new ArgumentNullException("arguments");

			var flattenArguments = new ArrayList();
			IterateAndAddArguments(arguments, flattenArguments);
			return (string[])flattenArguments.ToArray(typeof(string));
		}
		private static void IterateAndAddArguments(IEnumerable arguments, ArrayList flattenArguments)
		{
			if (arguments == null) throw new ArgumentNullException("arguments");

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
