using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using GameDevWare.Charon.Unity.Async;
using GameDevWare.Charon.Unity.Utils;

namespace GameDevWare.Charon.Unity.Updates.Packages.Deployment
{
	internal sealed class LibraryDeploymentAction : DeploymentAction
	{
		private readonly string product;
		private readonly SemanticVersion versionToDeploy;
		private readonly Action<string, float> progressCallback;
		private readonly List<string> changedAssets;
		private readonly DirectoryInfo targetDirectory;
		private readonly DirectoryInfo downloadDirectory;

		public override IEnumerable<string> ChangedAssets { get { return this.changedAssets; } }

		public LibraryDeploymentAction(string product, SemanticVersion versionToDeploy, string location, Action<string, float> progressCallback)
		{
			this.product = product;
			this.versionToDeploy = versionToDeploy;
			this.targetDirectory = new DirectoryInfo(Path.GetDirectoryName(location));
			this.downloadDirectory = new DirectoryInfo(Path.Combine(Settings.TempPath, Path.GetRandomFileName()));
			this.changedAssets = new List<string>();
			this.progressCallback = progressCallback;
		}

		/// <inheritdoc />
		public override Promise Prepare()
		{
			return new Coroutine<object>(this.PrepareAsync());
		}
		private IEnumerable PrepareAsync()
		{
			var downloadAsync = PackageManager.DownloadAndUnpack(this.product, this.versionToDeploy, ArtifactKind.Library, this.downloadDirectory.FullName, this.progressCallback);
			yield return downloadAsync;
		}
		/// <inheritdoc />
		public override Promise Complete()
		{
			return new Coroutine<object>(this.CompleteAsync());
		}
		private IEnumerable CompleteAsync()
		{
			foreach (var file in this.downloadDirectory.GetFiles())
			{
				var targetFile = new FileInfo(Path.Combine(this.targetDirectory.FullName, file.Name));
				if (targetFile.Exists)
				{
					try { File.Replace(file.FullName, targetFile.FullName, null); }
					catch { /* ignore replace errors */ }
				}
				else
				{
					try { file.MoveTo(targetFile.FullName); }
					catch { /* ignore move errors */ }
				}
				this.changedAssets.Add(FileAndPathUtils.MakeProjectRelative(targetFile.FullName));
			}
			yield break;
		}
		/// <inheritdoc />
		public override void CleanUp()
		{
			this.changedAssets.Clear();
			if (this.downloadDirectory.Exists)
			{
				try { this.downloadDirectory.Delete(); }
				catch {/* ignore delete errors */}
			}
		}
	}
}
