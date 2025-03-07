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
