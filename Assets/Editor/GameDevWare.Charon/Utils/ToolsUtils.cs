using System;
using System.Globalization;
using System.IO;
using Assets.Editor.GameDevWare.Charon.Tasks;
using Microsoft.Win32;
using UnityEditor;

namespace Assets.Editor.GameDevWare.Charon.Utils
{
	public static class ToolsUtils
	{
		public static readonly string ToolShadowCopyPath = FileUtil.GetUniqueTempPathInProject();

		// ReSharper disable once IdentifierTypo
		private const string MONO_PATH_EDITORPREFS_KEY = "CHARON::MONOPATH";

		public static string MonoPath { get { return EditorPrefs.GetString(MONO_PATH_EDITORPREFS_KEY); } set { EditorPrefs.SetString(MONO_PATH_EDITORPREFS_KEY, value); } }

		public static ToolsCheckResult CheckTools()
		{
			if (!File.Exists(Settings.Current.ToolsPath))
				return ToolsCheckResult.MissingTools;
#if UNITY_EDITOR_WIN
			if (Get45or451FromRegistry() == null && File.Exists(MonoPath) == false)
				return ToolsCheckResult.MissingRuntime;
#else
			if (File.Exists(MonoPath) == false)
				return ToolsCheckResult.MissingRuntime;
#endif
			return ToolsCheckResult.Ok;
		}

#if UNITY_EDITOR_WIN
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
#endif
		internal static Promise UpdateTools(Action<string, float> progressCallback = null)
		{
			return new Coroutine(Menu.CheckForUpdatesAsync(progressCallback));
		}
	}
}
