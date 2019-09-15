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
				var actualHashValue = FileAndPathUtils.ComputeHash(file.FullName, "SHA1");

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
