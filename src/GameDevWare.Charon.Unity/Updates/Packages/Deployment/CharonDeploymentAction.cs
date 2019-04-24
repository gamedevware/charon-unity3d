using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameDevWare.Charon.Unity.Async;
using GameDevWare.Charon.Unity.Utils;
using GameDevWare.Charon.Unity.Windows;

namespace GameDevWare.Charon.Unity.Updates.Packages.Deployment
{
	internal sealed class CharonDeploymentAction : DeploymentAction
	{
		private readonly SemanticVersion versionToDeploy;
		private readonly string location;
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
			if (this.versionDirectory.Exists && HasValidExecutableFiles(this.versionDirectory))
			{
				yield break;
			}

			var downloadAsync = PackageManager.DownloadAndUnpack(ProductInformation.PRODUCT_CHARON, this.versionToDeploy, ArtifactKind.Tool, this.downloadDirectory.FullName, this.progressCallback);
			yield return downloadAsync;

			if (this.versionDirectory.Exists)
			{
				this.versionDirectory.Delete();
			}
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

			var extensionsToClean = new[] { ".exe", ".dll", ".config", ".xml", ".pdb", ".mdb" };
			foreach (var fileToClean in this.baseDirectory.GetFiles().Where(file => extensionsToClean.Contains(file.Extension, StringComparer.OrdinalIgnoreCase)))
			{
				try
				{
					fileToClean.Delete();
				}
				catch
				{
					/* ignore delete errors */
				}
			}

			foreach (var file in this.versionDirectory.GetFiles())
			{
				try { file.CopyTo(Path.Combine(Settings.ToolBasePath, file.Name)); }
				catch { /* ignore copy errors */ }
			}

			// ensure config file
			var charonConfigPath = new FileInfo(Settings.CharonExecutablePath + ".config");
			if (charonConfigPath.Exists)
			{
				try { charonConfigPath.Delete(); }
				catch { /* ignore copy errors */ }
			}
			var embeddedConfigStream = typeof(Menu).Assembly.GetManifestResourceStream("GameDevWare.Charon.Unity.Charon.exe.config");
			if (embeddedConfigStream != null)
			{
				using (embeddedConfigStream)
				using (var configFileStream = charonConfigPath.Create())
				{
					var buffer = new byte[8 * 1024];
					var read = 0;
					while ((read = embeddedConfigStream.Read(buffer, 0, buffer.Length)) > 0)
						configFileStream.Write(buffer, 0, read);
				}
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
		/// <inheritdoc />
		public override void CleanUp()
		{
			if (this.downloadDirectory.Exists)
			{
				try { this.downloadDirectory.Delete(); }
				catch {/* ignore delete errors */}
			}
		}
	}
}
