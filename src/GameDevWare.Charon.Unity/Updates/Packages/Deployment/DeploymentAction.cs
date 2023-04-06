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
using System.Collections.Generic;
using System.IO;
using GameDevWare.Charon.Unity.Async;
using GameDevWare.Charon.Unity.Utils;

namespace GameDevWare.Charon.Unity.Updates.Packages.Deployment
{
	internal abstract class DeploymentAction
	{
		public const string ACTION_SKIP = Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_SKIP_ACTION;
		public const string ACTION_UPDATE = Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_UPDATE_ACTION;
		public const string ACTION_REPAIR = Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_REPAIR_ACTION;
		public const string ACTION_DOWNLOAD = Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_DOWNLOAD_ACTION;

		protected static readonly string[] NoChangedAssets = new string[0];

		public abstract IEnumerable<string> ChangedAssets { get; }

		public abstract Promise Prepare();
		public abstract Promise Complete();
		public abstract void CleanUp();

		protected static bool HasValidExecutableFiles(DirectoryInfo directoryInfo)
		{
			if (directoryInfo == null) throw new ArgumentNullException("directoryInfo");

			var checkedFiles = 0;
			foreach (var file in directoryInfo.GetFiles())
			{
				if (string.Equals(file.Extension, ".exe", StringComparison.OrdinalIgnoreCase) == false &&
					string.Equals(file.Extension, ".dll", StringComparison.OrdinalIgnoreCase) == false)
				{
					continue;
				}

				var hashFilePath = new FileInfo(file.FullName + ".sha1");
				if (hashFilePath.Exists == false)
				{
					return false; // no hash file
				}
				
				var expectedHashValue = File.ReadAllText(hashFilePath.FullName).Trim();
				var actualHashValue = FileHelper.ComputeHash(file.FullName, "SHA1");

				if (string.Equals(expectedHashValue, actualHashValue, StringComparison.OrdinalIgnoreCase) == false)
				{
					return false; // hash mismatch
				}

				checkedFiles++;
			}

			return checkedFiles > 0;
		}
	}
}
