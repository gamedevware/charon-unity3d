using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Assets.Editor.GameDevWare.Charon.Utils;
using UnityEditor;

namespace Assets.Editor.GameDevWare.Charon
{
	internal static class GameDataEditorProcess
	{
		private const string PREFS_PROCESSID_KEY = Settings.PREF_PREFIX + "EditorProcessId";
		private const string PREFS_PROCESSTITLE_KEY = Settings.PREF_PREFIX + "EditorProcessTitle";

		private static Process Process;
		private static bool IsInitialized;

		public static bool IsRunning
		{
			[MethodImpl(MethodImplOptions.Synchronized)]
			get
			{
				Initialize();
				var process = Process;
				if (process == null) return false;
				process.Refresh();
				return !process.HasExited;
			}
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public static void Watch(int processId, string title)
		{
			Initialize();

			UnityEngine.Debug.Log(string.Format("Watching for GameData's Editor process '{0}' with title '{1}'.", processId, title));

			EndGracefully();
			var process = default(Process);
			try
			{
				process = Process.GetProcessById(processId);
				if (process.ProcessName != title)
					throw new InvalidOperationException(string.Format("Wrong process title '{0}' while '{1}' is expected.", process.ProcessName, title));

				// saving current process id to editor prefs
				EditorPrefs.SetString(PREFS_PROCESSID_KEY, process.Id.ToString());
				EditorPrefs.SetString(PREFS_PROCESSTITLE_KEY, title);

				Process = process;
			}
			catch (Exception exception)
			{
				UnityEngine.Debug.LogWarning(string.Format("Failed to find editor process with id {0}.{1}{2}", processId, Environment.NewLine, exception));

				using (process)
					Process = null;
				process = null;

				// saving current process id to editor prefs
				EditorPrefs.DeleteKey(PREFS_PROCESSID_KEY);
				EditorPrefs.DeleteKey(PREFS_PROCESSTITLE_KEY);
			}
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public static void EndGracefully()
		{
			Initialize();

			if (IsRunning == false)
				return;

			var process = Process;
			Process = null;

			using (process)
				process.EndGracefully();
		}

		// ReSharper disable once UnusedMember.Local
		[MethodImpl(MethodImplOptions.Synchronized)]
		private static void Initialize()
		{
			if (IsInitialized)
				return;
			IsInitialized = true;

			var processIdStr = EditorPrefs.GetString(PREFS_PROCESSID_KEY) ?? "";
			var imageNameStr = EditorPrefs.GetString(PREFS_PROCESSTITLE_KEY) ?? "";
			var processId = 0;
			if (int.TryParse(processIdStr, out processId))
				Watch(processId, imageNameStr);
		}


	}
}
