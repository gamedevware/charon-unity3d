/*
	Copyright (c) 2025 Denis Zykov

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
using System.Threading;
using System.Threading.Tasks;
using GameDevWare.Charon.Editor.Cli;
using GameDevWare.Charon.Editor.Utils;
using JetBrains.Annotations;
using UnityEngine;

namespace GameDevWare.Charon.Editor.Routines
{
	[PublicAPI]
	public static class ExtractT4TemplatesRoutine
	{

		public static Task ScheduleAsync(string extractionPath, CancellationToken cancellation = default)
		{
			return CharonEditorModule.Instance.Routines.Schedule(() => RunAsync(extractionPath), cancellation);
		}

		public static Task RunAsync(string extractionPath)
		{
			var task = RunInternalAsync(extractionPath);
			task.LogFaultAsError();
			return task;
		}
		private static async Task RunInternalAsync(string extractionPath)
		{
			var logger = CharonEditorModule.Instance.Logger;
			logger.Log(LogType.Assert, $"Extracting T4 Templates to '{extractionPath}'...");

			try
			{
				await CharonCli.DumpTemplatesAsync(Path.GetFullPath(extractionPath));
			}
			catch (Exception dumpError)
			{
				logger.Log(LogType.Error, string.Format(Resources.UI_UNITYPLUGIN_T4_EXTRACTION_FAILED, dumpError.Unwrap().Message));
				logger.Log(LogType.Error, dumpError.Unwrap());
			}
			logger.Log(LogType.Assert, Resources.UI_UNITYPLUGIN_T4_EXTRACTION_COMPLETE);
		}
	}
}
