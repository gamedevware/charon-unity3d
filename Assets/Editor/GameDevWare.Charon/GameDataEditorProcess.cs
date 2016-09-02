using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEditor;

using Debug = UnityEngine.Debug;

namespace Assets.Editor.GameDevWare.Charon
{
	internal static class GameDataEditorProcess
	{
		private const string PREFS_PROCESSID_KEY = Settings.PREF_PREFIX + "EditorProcessId";
		private const string PREFS_IMAGENAME_KEY = Settings.PREF_PREFIX + "EditorImageName";

		private static Process Process;
		private static bool IsInitialized;

		public static bool IsRunning { get { return Process != null && Process.HasExited == false; } }

		public static void Watch(int processId, string imageName)
		{
			Initialize();

			//Debug.Log(string.Format("Watching for GameData's Editor process '{0}' with executable '{1}'.", processId, imageName));

			Kill();
			try
			{
				Process = Process.GetProcessById(processId);
				if (Process.Modules.Count > 0 && Path.GetFileName(Process.MainModule.FileName) != Path.GetFileName(imageName))
					throw new InvalidOperationException(string.Format("Wrong executable of watched process '{0}' while '{1}' is expected.", Process.MainModule.FileName, imageName));

				// saving current process id to editor prefs
				EditorPrefs.SetString(PREFS_PROCESSID_KEY, Process.Id.ToString());
				EditorPrefs.SetString(PREFS_IMAGENAME_KEY, imageName);
			}
			catch
			{
				//Debug.LogWarning(string.Format("Failed to find editor process with id {0}.{1}{2}", processId, Environment.NewLine, e));

				using (Process)
					Process = null;
			}
		}
		public static void Kill()
		{
			Initialize();

			if (IsRunning == false)
				return;

			using (Process)
			{
				Kill(Process);
				Process = null;
			}
		}

		public static void Kill(int processId)
		{
			Initialize();

			try
			{
				using (var process = Process.GetProcessById(processId))
					Kill(process);
			}
			catch (ArgumentException)
			{
				// ignore
			}
			catch (InvalidOperationException)
			{
				// ignore
			}
		}
		public static void Kill(Process process)
		{
			Initialize();

			if (process == null) throw new ArgumentNullException("process");

			if (process.HasExited)
				return;

			try
			{
				process.Refresh();
				//Debug.Log(string.Format("Trying to kill process with id '{0}'.", process.Id));

				process.CloseMainWindow();
				if (process.WaitForExit(500))
					return;

				process.Kill();
				if (process.WaitForExit(500))
					return;

				throw new InvalidOperationException("Process doesn't respond to Kill signal.");
			}
			catch (Exception error)
			{
				Debug.LogWarning(string.Format("Failed to kill process with id {0} because of error: {1}{2}", process.Id, Environment.NewLine, error.Message));
			}
		}

		// ReSharper disable once UnusedMember.Local
		private static void Initialize()
		{
			if (IsInitialized)
				return;
			IsInitialized = true;

			var processIdStr = EditorPrefs.GetString(PREFS_PROCESSID_KEY) ?? "";
			var imageNameStr = EditorPrefs.GetString(PREFS_IMAGENAME_KEY) ?? "";
			var processId = 0;
			if (int.TryParse(processIdStr, out processId))
				Watch(processId, imageNameStr);
		}
	}
}
