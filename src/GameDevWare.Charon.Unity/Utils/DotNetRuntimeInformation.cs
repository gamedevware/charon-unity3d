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
using System.Globalization;
using Microsoft.Win32;

namespace GameDevWare.Charon.Unity.Utils
{
	internal static class DotNetRuntimeInformation
	{
		// ReSharper disable once InconsistentNaming
		public static string GetVersion()
		{
			using (var ndpKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, "").OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\"))
			{
				if (ndpKey == null)
					return null;
				var release = ndpKey.GetValue("Release");
				if (release == null)
					return null;
				var releaseKey = Convert.ToInt32(release, CultureInfo.InvariantCulture);
				if (releaseKey >= 393295)
					return "4.6";
				if ((releaseKey >= 379893))
					return "4.5.2";
				if ((releaseKey >= 378675))
					return "4.5.1";
				if ((releaseKey >= 378389))
					return "4.5";

				return null;
			}
		}
	}
}
