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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using GameDevWare.Charon.Utils;
using UnityEditor;

namespace GameDevWare.Charon
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

		public static void Watch(Process process)
		{
			if (process == null) throw new ArgumentNullException("process");

			process.Refresh();
			try
			{
				Watch(process.Id, process.ProcessName);
			}
			catch (Exception watchError)
			{
				try { process.Kill(); } catch { /*ignore*/}
				if (Settings.Current.Verbose)
					UnityEngine.Debug.LogWarning(string.Format("Failed to watch editor process. {0}{1}", Environment.NewLine, watchError));
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
