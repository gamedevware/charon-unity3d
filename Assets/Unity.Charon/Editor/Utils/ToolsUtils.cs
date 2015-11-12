
using System.IO;
using UnityEditor;

namespace Assets.Unity.Charon.Editor.Utils
{
	public static class ToolsUtils
	{
		// ReSharper disable once IdentifierTypo
		private const string MONO_PATH_EDITORPREFS_KEY = "CHARON::MONOPATH";

		public static string MonoPath { get { return EditorPrefs.GetString(MONO_PATH_EDITORPREFS_KEY); } set { EditorPrefs.SetString(MONO_PATH_EDITORPREFS_KEY, value); } }

		public static ToolsCheckResult CheckTools()
		{
			if (!File.Exists(Settings.Current.ToolsPath))
				return ToolsCheckResult.MissingTools;
#if UNITY_EDITOR_WIN
			if (!File.Exists(ToolsUtils.MonoPath))
				return ToolsCheckResult.MissingMono;
#endif
			return ToolsCheckResult.Ok;
		}
	}
}
