using System;
using System.Collections;
using GameDevWare.Charon.Unity.Async;
using GameDevWare.Charon.Unity.Utils;
using GameDevWare.Charon.Unity.Windows;
using JetBrains.Annotations;
using UnityEngine;

namespace GameDevWare.Charon.Unity.Routines
{
	[PublicAPI, UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public static class ExtractT4TemplatesRoutine
	{
		public static Promise Run(string extractionPath)
		{
			return new Async.Coroutine(ExtractT4Templates(extractionPath));
		}
		public static Promise Schedule(string extractionPath)
		{
			return CoroutineScheduler.Schedule(ExtractT4Templates(extractionPath));
		}

		private static IEnumerable ExtractT4Templates(string extractionPath)
		{
			var checkRequirements = CharonCli.CheckRequirementsAsync();
			yield return checkRequirements;

			switch (checkRequirements.GetResult())
			{
				case RequirementsCheckResult.MissingRuntime: yield return UpdateRuntimeWindow.ShowAsync(); break;
				case RequirementsCheckResult.WrongVersion:
				case RequirementsCheckResult.MissingExecutable: yield return CharonCli.DownloadCharon(ProgressUtils.ReportToLog(Resources.UI_UNITYPLUGIN_MENU_CHECK_UPDATES)); break;
				case RequirementsCheckResult.Ok: break;
				default: throw new InvalidOperationException("Unknown Tools check result.");
			}

			if (Settings.Current.Verbose) Debug.Log(string.Format("Extracting T4 Templates to '{0}'...", extractionPath));
			var dumpProcess = CharonCli.DumpTemplatesAsync(extractionPath);
			yield return dumpProcess;

			using (var dumpResult = dumpProcess.GetResult())
			{
				if (string.IsNullOrEmpty(dumpResult.GetErrorData()) == false)
					Debug.LogWarning(string.Format(Resources.UI_UNITYPLUGIN_T4_EXTRACTION_FAILED, dumpResult.GetErrorData()));
				else
					Debug.Log(Resources.UI_UNITYPLUGIN_T4_EXTRACTION_COMPLETE + "\r\n" + dumpResult.GetOutputData());
			}
		}
	}
}
