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
using System.IO;
using System.Text;
using GameDevWare.Charon.Unity.Async;
using GameDevWare.Charon.Unity.Json;
using JetBrains.Annotations;

namespace GameDevWare.Charon.Unity.Utils
{
	[PublicAPI, UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class CommandOutput
	{
		public const string FORMAT_JSON = "json";
		public const string FORMAT_BSON = "bson";
		public const string FORMAT_MESSAGE_PACK = "msgpack";
		public const string FORMAT_XML = "xml";
		public const string FORMAT_XLSX = "xlsx";
		public const string FORMAT_XLIFF = "xliff";

		private const string TARGET_STANDARD_OUTPUT = "out";
		private const string TARGET_STANDARD_ERROR = "err";
		private const string TARGET_NULL = "null";

		public static readonly string[] NoOptions = new string[0];

		private bool captureOutput;
		private string capturedOutput;

		public string Target { get; private set; }
		public string Format { get; private set; }
		public string[] FormattingOptions { get; private set; }

		internal Promise<RunResult> Capture(Promise<RunResult> runTask)
		{
			if (runTask == null) throw new ArgumentNullException("runTask");

			if (this.captureOutput == false)
				return runTask;

			return runTask.ContinueWith(t =>
			{
				var result = t.GetResult();
				if (this.Target == TARGET_STANDARD_OUTPUT)
					this.capturedOutput = result.GetOutputData();
				else if (this.Target == TARGET_STANDARD_ERROR)
					this.capturedOutput = result.GetErrorData();
				return result;
			});
		}

		public static CommandOutput Null()
		{
			return new CommandOutput
			{
				captureOutput = true,
				Target = TARGET_NULL,
				Format = FORMAT_JSON,
				FormattingOptions = NoOptions
			};
		}
		public static CommandOutput File(string filePath, string format = FORMAT_JSON, string[] formattingOptions = null)
		{
			if (filePath == null) throw new ArgumentNullException("filePath");
			if (format == null) throw new ArgumentNullException("format");
			if (formattingOptions == null) formattingOptions = NoOptions;

			return new CommandOutput
			{
				Target = filePath,
				Format = format,
				FormattingOptions = formattingOptions
			};
		}
		public static CommandOutput StandardOutput(string format = FORMAT_JSON, string[] formattingOptions = null)
		{
			if (format == null) throw new ArgumentNullException("format");
			if (formattingOptions == null) formattingOptions = NoOptions;

			return new CommandOutput
			{
				Target = TARGET_STANDARD_OUTPUT,
				Format = format,
				FormattingOptions = formattingOptions
			};
		}
		public static CommandOutput StandardError(string format = FORMAT_JSON, string[] formattingOptions = null)
		{
			if (format == null) throw new ArgumentNullException("format");
			if (formattingOptions == null) formattingOptions = NoOptions;

			return new CommandOutput
			{
				Target = TARGET_STANDARD_ERROR,
				Format = format,
				FormattingOptions = formattingOptions
			};
		}
		public static CommandOutput CaptureJson()
		{
			return new CommandOutput
			{
				captureOutput = true,
				Target = TARGET_STANDARD_OUTPUT,
				Format = FORMAT_JSON,
				FormattingOptions = NoOptions
			};
		}

		public T ReadJsonAs<T>()
		{
			var output = default(string);
			if (this.captureOutput)
			{
				output = this.capturedOutput;
				if (output == null)
					throw new InvalidOperationException("No output captured during command execution.");
			}
			else if (this.Target == TARGET_NULL)
				throw new InvalidOperationException("Command execution is discarded to null device.");
			else
				output = System.IO.File.ReadAllText(this.Target, Encoding.UTF8);

			var jsonObject = JsonValue.Load(new StringReader(output));

			if (jsonObject is T)
				return (T)(object)jsonObject;
			else
				return jsonObject.As<T>();
		}
	}
}