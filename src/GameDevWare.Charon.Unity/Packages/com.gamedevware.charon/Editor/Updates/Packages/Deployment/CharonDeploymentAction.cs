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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameDevWare.Charon.Unity.Async;
using GameDevWare.Charon.Unity.Utils;
using GameDevWare.Charon.Unity.Windows;
using UnityEngine;

namespace GameDevWare.Charon.Unity.Updates.Packages.Deployment
{
	internal sealed class CharonDeploymentAction : DeploymentAction
	{
		private readonly SemanticVersion versionToDeploy;
		private readonly Action<string, float> progressCallback;
		private readonly DirectoryInfo baseDirectory;
		private readonly string[] changedAssets;
		private readonly DirectoryInfo downloadDirectory;
		private readonly DirectoryInfo versionDirectory;

		public override IEnumerable<string> ChangedAssets { get { return this.changedAssets; } }

		public CharonDeploymentAction(SemanticVersion versionToDeploy, Action<string, float> progressCallback)
		{
			this.versionToDeploy = versionToDeploy;
			this.progressCallback = progressCallback;
			this.changedAssets = NoChangedAssets;
			this.baseDirectory = new DirectoryInfo(Settings.LibraryCharonPath);
			this.versionDirectory = new DirectoryInfo(Path.Combine(this.baseDirectory.FullName, this.versionToDeploy.ToNormalizedString().ToLowerInvariant()));
			this.downloadDirectory = new DirectoryInfo(Path.Combine(Settings.TempPath, Path.GetRandomFileName()));
		}
		/// <inheritdoc />
		public override Promise Prepare()
		{
			return new Coroutine<object>(this.PrepareAsync());
		}
		private IEnumerable PrepareAsync()
		{
			this.versionDirectory.Refresh();

			if (Settings.Current.Verbose)
			{
				Debug.Log(string.Format("Preparing version '{0}' of '{1}' from directory '{2}'.", this.versionToDeploy, ProductInformation.PRODUCT_CHARON, this.downloadDirectory));
			}

			if (this.versionDirectory.Exists && HasValidExecutableFiles(this.versionDirectory))
			{
				if (Settings.Current.Verbose)
				{
					Debug.Log(string.Format("Product '{0}' of version '{1}' has valid executable in '{2}'.", ProductInformation.PRODUCT_CHARON, this.versionToDeploy, this.versionDirectory));
				}
				SyncSettingsVersion(this.versionToDeploy);
				yield break;
			}

			if (Settings.Current.Verbose)
			{
				Debug.Log(string.Format("Downloading '{0}' of version '{1}' into temporary directory '{2}'.", ProductInformation.PRODUCT_CHARON, this.versionToDeploy, this.downloadDirectory));
			}

			var downloadAsync = PackageManager.DownloadAndUnpack(ProductInformation.PRODUCT_CHARON, this.versionToDeploy, ArtifactKind.Tool, this.downloadDirectory, this.progressCallback);
			yield return downloadAsync;

			this.downloadDirectory.Refresh();
			if (this.downloadDirectory.Exists == false)
			{
				Debug.LogError(string.Format("Failed to download version '{0}' of '{1}' because target directory '{2}' is empty or doesn't exists.",
					this.versionDirectory.Name, ProductInformation.PRODUCT_CHARON, this.downloadDirectory.FullName));
				yield break;
			}

			if (Settings.Current.Verbose)
			{
				Debug.Log(string.Format("Cleaning directory '{2}' for '{1}' version of '{0}'.", ProductInformation.PRODUCT_CHARON, this.versionToDeploy, this.downloadDirectory));
			}

			this.versionDirectory.Refresh();
			if (this.versionDirectory.Exists == false)
			{
				this.versionDirectory.Create();
			}
			this.versionDirectory.Delete(recursive: true);

			if (Settings.Current.Verbose)
			{
				Debug.Log(string.Format("Moving temporary directory '{3}' into '{2}' for '{1}' version of '{0}'.", ProductInformation.PRODUCT_CHARON, this.versionToDeploy, this.versionDirectory, this.downloadDirectory));
			}

			Directory.Move(this.downloadDirectory.FullName, this.versionDirectory.FullName);

			this.downloadDirectory.Refresh();
			this.versionDirectory.Refresh();
		}
		/// <inheritdoc />
		public override Promise Complete()
		{
			return new Coroutine<object>(this.CompleteAsync());
		}
		private IEnumerable CompleteAsync()
		{
			GameDataEditorWindow.FindAllAndClose();

			this.versionDirectory.Refresh();
			if (this.versionDirectory.Exists == false)
			{
				Debug.LogError(string.Format("Unable to complete deployment because directory '{0}' with '{1}' product doesn't exists.", this.versionDirectory.Name, ProductInformation.PRODUCT_CHARON));
				yield break;
			}

			var extensionsToClean = new[] { ".exe", ".dll", ".json", ".config", ".xml", ".pdb", ".mdb", ".sha1" };
			foreach (var fileToClean in this.baseDirectory.GetFiles().Where(file => extensionsToClean.Contains(file.Extension, StringComparer.OrdinalIgnoreCase)))
			{
				try
				{
					if (Settings.Current.Verbose)
					{
						Debug.Log(string.Format("Removing old file '{0}' of '{1}' product.", fileToClean.FullName, ProductInformation.PRODUCT_CHARON));
					}

					fileToClean.Delete();
				}
				catch (Exception error)
				{
					Debug.LogWarning(error);
				}
			}

			this.versionDirectory.Refresh();
			foreach (var file in this.versionDirectory.GetFiles())
			{
				if (Settings.Current.Verbose)
				{
					Debug.Log(string.Format("Copying target file '{0}' into '{2}' for product '{1}'.", file.FullName, ProductInformation.PRODUCT_CHARON, Settings.LibraryCharonPath));
				}

				try
				{
					file.CopyTo(Path.Combine(Settings.LibraryCharonPath, file.Name), overwrite: true);
				}
				catch (Exception error)
				{
					Debug.LogWarning(error);
				}
			}

			// ensure .config or appsettings.json file
			if (this.versionToDeploy <= CharonCli.LegacyToolsVersion)
			{
				var charonConfigPath = Settings.CharonExePath + ".config";
				CreateAppConfig(charonConfigPath);

				if (Settings.Current.Verbose)
				{
					Debug.Log(string.Format("Copying configuration file '{0}' into '{2}' for product '{1}'.", charonConfigPath, ProductInformation.PRODUCT_CHARON, Settings.LibraryCharonPath));
				}
			}
			else
			{
				var charonDirectoryPath = Path.GetDirectoryName(Settings.CharonExePath) ?? Settings.LibraryCharonPath;
				var charonAppSettingsPath = Path.Combine(charonDirectoryPath, "appsettings.json");
				CreateAppSettings(charonAppSettingsPath);

				if (Settings.Current.Verbose)
				{
					Debug.Log(string.Format("Copying appsettings file '{0}' into '{2}' for product '{1}'.", charonAppSettingsPath, ProductInformation.PRODUCT_CHARON, Settings.LibraryCharonPath));
				}
			}

			var currentVersion = default(SemanticVersion);
			if (File.Exists(Settings.CharonExePath))
			{
				if (this.progressCallback != null) this.progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_CHECKING_TOOLS_VERSION, 0.95f);

				var checkToolsVersion = CharonCli.GetVersionAsync();
				yield return checkToolsVersion.IgnoreFault();
				currentVersion = checkToolsVersion.HasErrors ? default(SemanticVersion) : checkToolsVersion.GetResult();

				if (Settings.Current.Verbose)
				{
					Debug.Log(string.Format("Current deployed version of '{0}' is {1}.", ProductInformation.PRODUCT_CHARON, checkToolsVersion.GetResult()));
				}
			}

			SyncSettingsVersion(currentVersion);
		}

		private static void SyncSettingsVersion(SemanticVersion currentVersion)
		{
			if (currentVersion == null || currentVersion.ToString() == Settings.Current.EditorVersion)
			{
				return;
			}

			Settings.Current.EditorVersion = currentVersion.ToString();
			Settings.Current.Save();
		}

		private static void CreateAppConfig(string charonConfigPath)
		{
			if (charonConfigPath == null) throw new ArgumentNullException("charonConfigPath");

			CopyEmbeddedResource("GameDevWare.Charon.Unity.Charon.exe.config", new FileInfo(charonConfigPath));
		}
		private static void CreateAppSettings(string charonAppSettingsPath)
		{
			if (charonAppSettingsPath == null) throw new ArgumentNullException("charonAppSettingsPath");

			CopyEmbeddedResource("GameDevWare.Charon.Unity.appsettings.json", new FileInfo(charonAppSettingsPath));
		}

		private static void CopyEmbeddedResource(string embeddedResourceName, FileInfo filePath)
		{
			if (embeddedResourceName == null) throw new ArgumentNullException("embeddedResourceName");
			if (filePath == null) throw new ArgumentNullException("filePath");

			if (filePath.Exists)
			{
				try
				{
					filePath.Delete();
				}
				catch (Exception error)
				{
					Debug.LogWarning(error);
				}
			}

			var manifestResourceStream = typeof(Menu).Assembly.GetManifestResourceStream(embeddedResourceName);
			if (manifestResourceStream == null)
				throw new InvalidOperationException(string.Format("Unable to find embedded resource at '{0}'.", embeddedResourceName));

			using (manifestResourceStream)
			using (var fileStream = filePath.Create())
			{
				var buffer = new byte[8 * 1024];
				var read = 0;
				while ((read = manifestResourceStream.Read(buffer, 0, buffer.Length)) > 0)
					fileStream.Write(buffer, 0, read);
			}
		}

		/// <inheritdoc />
		public override void CleanUp()
		{
			this.downloadDirectory.Refresh();
			if (this.downloadDirectory.Exists)
			{
				if (Settings.Current.Verbose)
				{
					Debug.Log(string.Format("Deleting temporary download directory '{0}'.", this.downloadDirectory));
				}

				try { this.downloadDirectory.Delete(recursive: true); }
				catch (Exception error)
				{
					if (Settings.Current.Verbose)
						Debug.LogWarning(error);
				}
			}
		}
	}
}
