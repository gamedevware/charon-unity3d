using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using GameDevWare.Charon.Tasks;
using Microsoft.Win32;
using UnityEditor;
using UnityEngine;

using Coroutine = GameDevWare.Charon.Tasks.Coroutine;

namespace GameDevWare.Charon.Utils
{
	public static class ToolsRunner
	{
		public static string ToolShadowCopyPath = FileUtil.GetUniqueTempPathInProject();

		// ReSharper disable once IdentifierTypo
		private const string MONO_PATH_EDITORPREFS_KEY = "CHARON::MONOPATH";

		public static string MonoPath { get { return EditorPrefs.GetString(MONO_PATH_EDITORPREFS_KEY); } set { EditorPrefs.SetString(MONO_PATH_EDITORPREFS_KEY, value); } }
		public static bool IsWindows { get { return Application.platform == RuntimePlatform.WindowsEditor; } }
		public static bool IsOsx { get { return Application.platform == RuntimePlatform.OSXEditor; } }

		public static CharonCheckResult CheckCharon()
		{
			if (string.IsNullOrEmpty(Settings.Current.ToolsPath) || !File.Exists(Settings.Current.ToolsPath))
				return CharonCheckResult.MissingExecutable;

			if (IsWindows)
			{
				if (Get45or451FromRegistry() == null && (string.IsNullOrEmpty(MonoPath) || File.Exists(MonoPath) == false))
					return CharonCheckResult.MissingRuntime;
			}
			else
			{
				if (string.IsNullOrEmpty(MonoPath) || File.Exists(MonoPath) == false)
					return CharonCheckResult.MissingRuntime;
			}
			return CharonCheckResult.Ok;
		}

		public static Promise<ToolExecutionResult> Run(string executablePath, params string[] arguments)
		{
			if (executablePath == null) throw new ArgumentNullException("executablePath");

			return Run(new ToolExecutionOptions(executablePath, arguments));
		}
		public static Promise<ToolExecutionResult> Run(ToolExecutionOptions options)
		{
			if (options == null) throw new ArgumentNullException("options");

			return new Coroutine<ToolExecutionResult>(RunAsync(options));
		}
		public static Promise<ToolExecutionResult> RunCharonAsTool(params string[] arguments)
		{
			var toolsPath = Settings.Current.ToolsPath;
			if (string.IsNullOrEmpty(toolsPath))
				throw new InvalidOperationException("Unable to launch Charon.exe tool because path to it is null or empty.");

			return Run(new ToolExecutionOptions(toolsPath, arguments)
			{
				CaptureStandartOutput = true,
				CaptureStandartError = true,
				ExecutionTimeout = TimeSpan.FromSeconds(30),
				RequireDotNetRuntime = true,
				WaitForExit = true,
				StartInfo =
				{
					EnvironmentVariables =
					{
						{ "CHARON_APP_DATA", Settings.GetAppDataPath() },
						{ "CHARON_LICENSE_SERVER", Settings.Current.LicenseServerAddress },
					}
				}
			});
		}
		private static IEnumerable RunAsync(ToolExecutionOptions options)
		{
			yield return null;

			var isDotNetInstalled = false;

			if (IsWindows)
			{
				isDotNetInstalled = Get45or451FromRegistry() != null;
			}

			if (options.RequireDotNetRuntime && isDotNetInstalled == false)
			{
				if (string.IsNullOrEmpty(MonoPath))
					throw new InvalidOperationException("No .NET runtime found on machine.");

				options.StartInfo.Arguments = ToolExecutionOptions.ConcatArguments(options.StartInfo.FileName) + " " + options.StartInfo.Arguments;
				options.StartInfo.FileName = MonoPath;
			}

			if (options.CaptureStandartError)
				options.StartInfo.RedirectStandardError = true;
			if (options.CaptureStandartOutput)
				options.StartInfo.RedirectStandardOutput = true;

			if (Settings.Current.Verbose)
				UnityEngine.Debug.Log(string.Format("Starting process '{0}' at '{1}' with arguments '{2}' and environment variables '{3}'.", options.StartInfo.FileName, options.StartInfo.WorkingDirectory, options.StartInfo.Arguments, ConcatDictionaryValues(options.StartInfo.EnvironmentVariables)));

			var processStarted = DateTime.UtcNow;
			var timeout = options.ExecutionTimeout;
			if (timeout <= TimeSpan.Zero)
				timeout = TimeSpan.MaxValue;

			var process = Process.Start(options.StartInfo);
			if (process == null)
				throw new InvalidOperationException("Unknown process start error.");

			var result = new ToolExecutionResult(options, process);
			if (options.WaitForExit == false)
			{
				//if (Settings.Current.Verbose)
				//	UnityEngine.Debug.Log(string.Format("Yielding started process '{0}' at '{1}' with arguments '{2}'.", options.StartInfo.FileName, options.StartInfo.WorkingDirectory, options.StartInfo.Arguments));
				yield return result;
				yield break;
			}

			var hasExited = false;
			while (hasExited == false)
			{
				if (DateTime.UtcNow - processStarted > timeout)
					throw new TimeoutException();

				try
				{
					process.Refresh();
					hasExited = process.HasExited;
				}
				catch (InvalidOperationException)
				{
					// ignored
				}
				catch (System.ComponentModel.Win32Exception)
				{
					// ignored
				}
				yield return Promise.Delayed(TimeSpan.FromMilliseconds(50));
			}

			processStarted = DateTime.UtcNow;
			timeout = options.TerminationTimeout;
			if (timeout <= TimeSpan.Zero)
				timeout = TimeSpan.MaxValue;

			while (result.HasPendingData)
			{
				if (DateTime.UtcNow - processStarted > timeout)
					throw new TimeoutException();

				yield return Promise.Delayed(TimeSpan.FromMilliseconds(50));
			}

			result.ExitCode = process.ExitCode;

			if (Settings.Current.Verbose)
				UnityEngine.Debug.Log(string.Format("Process #{1} '{0}' has exited with code {2}.", options.StartInfo.FileName, result.ProcessId, result.ExitCode));

			yield return result;
		}


		public static string Get45or451FromRegistry()
		{
			using (RegistryKey ndpKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, "").OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\"))
			{
				if (ndpKey == null)
					return null;
				var release = ndpKey.GetValue("Release");
				if (release == null)
					return null;
				var releaseKey = Convert.ToInt32(release, CultureInfo.InvariantCulture);
				if (releaseKey >= 393295)
					return "4.6 or later";
				if ((releaseKey >= 379893))
					return "4.5.2 or later";
				if ((releaseKey >= 378675))
					return "4.5.1 or later";
				if ((releaseKey >= 378389))
					return "4.5 or later";

				return null;
			}
		}

		public static Promise UpdateCharonExecutable(Action<string, float> progressCallback = null)
		{
			return new Coroutine(Menu.CheckForUpdatesAsync(progressCallback));
		}

		private static string ConcatDictionaryValues(StringDictionary dictionary)
		{
			if (dictionary.Count == 0)
				return "";

			var sb = new StringBuilder();
			foreach (string key in dictionary.Keys)
				sb.Append(key).Append("=").Append(dictionary[key]).Append(", ");
			if (sb.Length > 2)
				sb.Length -= 2;
			return sb.ToString();
		}

		// ReSharper disable once IdentifierTypo
		internal static string GetProgramFilesx86()
		{
			if (8 == IntPtr.Size || (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
			{
				return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
			}

			return Environment.GetEnvironmentVariable("ProgramFiles");
		}
	}
}
