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
			this.baseDirectory = new DirectoryInfo(Settings.ToolBasePath);
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
			if (this.versionDirectory.Exists && HasValidExecutableFiles(this.versionDirectory))
			{
				yield break;
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

			this.versionDirectory.Refresh();
			if (this.versionDirectory.Exists == false)
			{
				this.versionDirectory.Create();
			}
			this.versionDirectory.Delete();

			this.downloadDirectory.MoveTo(this.versionDirectory.FullName);
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
						Debug.Log(string.Format("Removing old file '{0}' of '{1}' product.", fileToClean.FullName, ProductInformation.PRODUCT_CHARON));

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
					Debug.Log(string.Format("Copying target file '{0}' for product '{1}'.", file, ProductInformation.PRODUCT_CHARON));

				try
				{
					file.CopyTo(Path.Combine(Settings.ToolBasePath, file.Name), overwrite: true);
				}
				catch (Exception error)
				{
					Debug.LogWarning(error);
				}
			}

			// ensure .config or appsettings.json file
			if (this.versionToDeploy.Version <= CharonCli.LegacyToolsVersion)
			{
				var charonConfigPath = Settings.CharonExecutablePath + ".config";
				CreateAppConfig(charonConfigPath);
			}
			else
			{
				var charonDirectoryPath = Path.GetDirectoryName(Settings.CharonExecutablePath) ?? Settings.ToolBasePath;
				var charonAppSettingsPath = Path.Combine(charonDirectoryPath, "appsettings.json");
				CreateAppSettings(charonAppSettingsPath);
			}

			var currentVersion = default(SemanticVersion);
			if (File.Exists(Settings.CharonExecutablePath))
			{
				if (this.progressCallback != null) this.progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_CHECKING_TOOLS_VERSION, 0.95f);

				var checkToolsVersion = CharonCli.GetVersionAsync();
				yield return checkToolsVersion.IgnoreFault();
				currentVersion = checkToolsVersion.HasErrors ? default(SemanticVersion) : checkToolsVersion.GetResult();
			}

			Settings.Current.EditorVersion = currentVersion != null ? currentVersion.ToString() : null;
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
