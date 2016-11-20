using System;
using System.Diagnostics;
using System.IO;

namespace Assets.Editor.GameDevWare.Charon.Utils
{
	public sealed class ToolExecutionOptions
	{
		public bool RequireDotNetRuntime { get; set; }
		public bool CaptureStandartOutput { get; set; }
		public bool CaptureStandartError { get; set; }
		public bool WaitForExit { get; set; }
		public ProcessStartInfo StartInfo { get; private set; }
		public TimeSpan ExecutionTimeout { get; set; }
		public TimeSpan TerminationTimeout { get; set; }

		public ToolExecutionOptions(string executablePath, params string[] arguments)
		{
			this.StartInfo = new ProcessStartInfo(executablePath)
			{
				Arguments = ConcatArguments(arguments),
				UseShellExecute = false,
				WorkingDirectory = Path.GetFullPath("./"),
				EnvironmentVariables =
				{
					{ "UNITY_PROJECT_PATH", Path.GetFullPath("./").TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) }
				},
				CreateNoWindow = true
			};
			this.WaitForExit = true;
			this.TerminationTimeout = TimeSpan.FromSeconds(5);
		}

		public static string ConcatArguments(params string[] arguments)
		{
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

		public override string ToString()
		{
			return this.StartInfo.ToString();
		}
	}
}
