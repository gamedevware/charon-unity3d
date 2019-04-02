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

using System.IO;
using UnityEditor;

namespace GameDevWare.Charon.Unity.Utils
{
	internal static class MonoRuntimeInformation
	{
		private const string MONO_PATH_EDITOR_PREFS_KEY = "CHARON::MONOPATH";

		public static readonly string MonoExecutableName;
		public static readonly string MonoDefaultLocation;

		public static string MonoPath { get { return EditorPrefs.GetString(MONO_PATH_EDITOR_PREFS_KEY); } set { EditorPrefs.SetString(MONO_PATH_EDITOR_PREFS_KEY, value); } }

		static MonoRuntimeInformation()
		{
			if (RuntimeInformation.IsWindows)
			{
				MonoExecutableName = "mono.exe";
				MonoDefaultLocation = Path.Combine(FileAndPathUtils.GetProgramFilesx86(), @"Mono\bin");
			}
			else if (RuntimeInformation.IsOsx)
			{
				MonoExecutableName = "mono";
				MonoDefaultLocation = @"/Library/Frameworks/Mono.framework/Commands";
			}
			else
			{
				MonoExecutableName = "mono";
				MonoDefaultLocation = @"/usr/bin";
			}
		}
	}
}
