/*
	Copyright (c) 2023 Denis Zykov

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
		public static Version GetVersion()
		{
			using (var ndpKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, "").OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\"))
			{
				if (ndpKey == null)
					return null;
				var release = ndpKey.GetValue("Release");
				if (release == null)
					return null;
				var releaseKey = Convert.ToInt32(release, CultureInfo.InvariantCulture);
				var version = default(Version);
				if (releaseKey >= 533320)
					version = new Version("4.8.1");
				else if (releaseKey >= 528040)
					version = new Version("4.8");
				else if (releaseKey >= 461808)
					version = new Version("4.7.2");
				else if (releaseKey >= 461308)
					version = new Version("4.7.1");
				else if (releaseKey >= 460798)
					version = new Version("4.7");
				else if (releaseKey >= 394802)
					version = new Version("4.6.2");
				else if (releaseKey >= 394254)
					version = new Version("4.6.1");
				else if (releaseKey >= 393295)
					version = new Version("4.6");
				else if (releaseKey >= 379893)
					version = new Version("4.5.2");
				else if (releaseKey >= 378675)
					version = new Version("4.5.1");
				else if (releaseKey >= 378389)
					version = new Version("4.5");
				
				return version;
			}
		}
	}
}
