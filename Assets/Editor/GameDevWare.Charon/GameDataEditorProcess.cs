using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

using Debug = UnityEngine.Debug;

namespace Assets.Editor.GameDevWare.Charon
{
	[InitializeOnLoad, Serializable]
	internal sealed class GameDataEditorProcess : ScriptableObject
	{
		private const string PREFS_PROCESSID_KEY = Settings.PREF_PREFIX + "EditorProcessId";
		private const string PREFS_IMAGENAME_KEY = Settings.PREF_PREFIX + "EditorImageName";

		public static GameDataEditorProcess Instance;
		private Process process;

		public bool IsRunning { get { return this.process != null && this.process.HasExited == false; } }

		static GameDataEditorProcess()
		{
			Instance = ScriptableObject.CreateInstance<GameDataEditorProcess>();
		}

		public void Watch(int processId, string imageName)
		{
			//Debug.Log(string.Format("Watching for GameData's Editor process '{0}' with executable '{1}'.", processId, imageName));

			this.Kill();
			try
			{
				this.process = Process.GetProcessById(processId);
				if (this.process.Modules.Count > 0 && Path.GetFileName(this.process.MainModule.FileName) != Path.GetFileName(imageName))
					throw new InvalidOperationException(string.Format("Wrong executable of watched process '{0}' while '{1}' is expected.", this.process.MainModule.FileName, imageName));

				// saving current process id to editor prefs
				EditorPrefs.SetString(PREFS_PROCESSID_KEY, this.process.Id.ToString());
				EditorPrefs.SetString(PREFS_IMAGENAME_KEY, imageName);
			}
			catch
			{
				//Debug.LogWarning(string.Format("Failed to find editor process with id {0}.{1}{2}", processId, Environment.NewLine, e));

				using (this.process)
					this.process = null;
			}
		}
		public void Kill()
		{
			if (this.IsRunning == false)
				return;

			using (this.process)
			{
				Kill(this.process);
				this.process = null;
			}
		}

		public static void Kill(int processId)
		{
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
		private void Awake()
		{
			var processIdStr = EditorPrefs.GetString(PREFS_PROCESSID_KEY) ?? "";
			var imageNameStr = EditorPrefs.GetString(PREFS_IMAGENAME_KEY) ?? "";
			var processId = 0;
			if (int.TryParse(processIdStr, out processId))
				Watch(processId, imageNameStr);
		}
	}
}
