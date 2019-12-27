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
using System.IO;
using GameDevWare.Charon.Unity.Async;
using GameDevWare.Charon.Unity.Json;
using UnityEngine;
using IOFile = System.IO.File;

namespace GameDevWare.Charon.Unity.Utils
{
	public class CommandInput : IDisposable
	{
		public const string FORMAT_JSON = CommandOutput.FORMAT_JSON;
		public const string FORMAT_BSON = CommandOutput.FORMAT_BSON;
		public const string FORMAT_MESSAGE_PACK = CommandOutput.FORMAT_MESSAGE_PACK;
		public const string FORMAT_XML = CommandOutput.FORMAT_XML;
		public const string FORMAT_XLSX = CommandOutput.FORMAT_XLSX;
		public const string FORMAT_XLIFF = CommandOutput.FORMAT_XLIFF;

		private const string SOURCE_STANDARD_INPUT = "in";

		public static readonly string[] NoOptions = new string[0];

		private string temporaryFile;

		public string Source { get; private set; }
		public string Format { get; private set; }
		public string[] FormattingOptions { get; private set; }

		/// <inheritdoc />
		~CommandInput()
		{
			this.ReleaseUnmanagedResources();
		}

		public static CommandInput JsonString(string jsonString)
		{
			if (string.IsNullOrEmpty(jsonString)) throw new ArgumentException("Value cannot be null or empty.", "jsonString");

			var temporaryFile = Path.Combine(Settings.TempPath, Path.GetTempFileName());
			IOFile.WriteAllText(temporaryFile, jsonString);

			return new CommandInput
			{
				temporaryFile = temporaryFile,
				Source = temporaryFile,
				Format = FORMAT_JSON,
				FormattingOptions = NoOptions
			};
		}
		public static CommandInput JsonFile(string jsonFilePath, bool deleteAfterUse = false)
		{
			if (string.IsNullOrEmpty(jsonFilePath)) throw new ArgumentException("Value cannot be null or empty.", "jsonFilePath");

			if (IOFile.Exists(jsonFilePath) == false) throw new IOException(string.Format("File '{0}' doesn't exists.", jsonFilePath));

			return new CommandInput
			{
				temporaryFile = deleteAfterUse ? jsonFilePath : null,
				Source = jsonFilePath,
				Format = FORMAT_JSON,
				FormattingOptions = NoOptions
			};
		}
		public static CommandInput Json<T>(T value)
		{
			var jsonValue = JsonObject.From(value);
			var temporaryFile = Path.Combine(Settings.TempPath, Path.GetTempFileName());
			using (var temporaryFileStream = new FileStream(temporaryFile, FileMode.Create, FileAccess.Write, FileShare.None))
				jsonValue.Save(temporaryFileStream);

			return new CommandInput
			{
				temporaryFile = temporaryFile,
				Source = temporaryFile,
				Format = FORMAT_JSON,
				FormattingOptions = NoOptions
			};
		}
		public static CommandInput File(string filePath, string format = FORMAT_JSON, string[] formattingOptions = null, bool deleteAfterUse = false)
		{
			if (filePath == null) throw new ArgumentNullException("filePath");
			if (format == null) throw new ArgumentNullException("format");

			if (IOFile.Exists(filePath) == false) throw new IOException(string.Format("File '{0}' doesn't exists.", filePath));

			return new CommandInput
			{
				temporaryFile = deleteAfterUse ? filePath : null,
				Source = filePath,
				Format = format,
				FormattingOptions = formattingOptions ?? NoOptions
			};
		}

		internal Promise<RunResult> StickWith(Promise<RunResult> runTask)
		{
			if (runTask == null) throw new ArgumentNullException("runTask");

			runTask.ContinueWith(t =>
			{
				this.Dispose();
			});

			return runTask;
		}

		private void ReleaseUnmanagedResources()
		{
			if (this.temporaryFile != null && IOFile.Exists(this.temporaryFile))
			{
				if (Settings.Current.Verbose)
					Debug.Log(string.Format("Removing temporary input file '{0}'.", this.temporaryFile));
				IOFile.Delete(this.temporaryFile);
			}

			this.temporaryFile = null;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			this.ReleaseUnmanagedResources();
			GC.SuppressFinalize(this);
		}

	}
}