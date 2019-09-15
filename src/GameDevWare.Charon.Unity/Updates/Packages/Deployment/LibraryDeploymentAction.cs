using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameDevWare.Charon.Unity.Async;
using GameDevWare.Charon.Unity.Utils;
using UnityEngine;

namespace GameDevWare.Charon.Unity.Updates.Packages.Deployment
{
	internal sealed class LibraryDeploymentAction : DeploymentAction
	{
		private readonly string product;
		private readonly SemanticVersion versionToDeploy;
		private readonly Action<string, float> progressCallback;
		private readonly List<string> changedAssets;
		private readonly DirectoryInfo targetDirectory;
		private readonly FileInfo targetFile;
		private readonly DirectoryInfo downloadDirectory;

		public override IEnumerable<string> ChangedAssets { get { return this.changedAssets; } }

		public LibraryDeploymentAction(string product, SemanticVersion versionToDeploy, string location, Action<string, float> progressCallback)
		{
			this.product = product;
			this.versionToDeploy = versionToDeploy;
			this.targetDirectory = new DirectoryInfo(Path.GetDirectoryName(location));
			this.downloadDirectory = new DirectoryInfo(Path.Combine(Settings.TempPath, Path.GetRandomFileName()));
			this.targetFile = new FileInfo(location);
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
			var downloadAsync = PackageManager.DownloadAndUnpack(this.product, this.versionToDeploy, ArtifactKind.Library, this.downloadDirectory, this.progressCallback);
			yield return downloadAsync;
		}
		/// <inheritdoc />
		public override Promise Complete()
		{
			return new Coroutine<object>(this.CompleteAsync());
		}
		private IEnumerable CompleteAsync()
		{
			var extensionsToClean = new[] { ".exe", ".dll", ".config", ".xml", ".pdb", ".mdb", ".sha1" };
			var filesToClean = extensionsToClean
				.Select(extension => new FileInfo(Path.ChangeExtension(this.targetFile.FullName, extension)))
				.Union(extensionsToClean.Select(extension => new FileInfo(this.targetFile.FullName + extension)));
			
			foreach (var fileToClean in filesToClean)
			{
				try
				{
					fileToClean.Refresh();
					if (!fileToClean.Exists)
						continue;

					if (Settings.Current.Verbose)
						Debug.Log(string.Format("Removing old file '{0}' of '{1}' product.", fileToClean.FullName, this.product));
					
					fileToClean.Delete();
				}
				catch (Exception error)
				{
					Debug.LogWarning(error);
				}
			}

			foreach (var file in this.downloadDirectory.GetFiles())
			{
				var targetFile = new FileInfo(Path.Combine(this.targetDirectory.FullName, file.Name));
				if (targetFile.Exists)
				{
					if (Settings.Current.Verbose)
						Debug.Log(string.Format("Replacing target file '{0}' with new content for product '{1}'.", targetFile, this.product));

					try { File.Replace(file.FullName, targetFile.FullName, null); }
					catch (Exception error)
					{
						Debug.LogWarning(error);
					}
				}
				else
				{
					if (Settings.Current.Verbose)
						Debug.Log(string.Format("Adding new file '{0}' for product '{1}'.", targetFile, this.product));

					try { file.MoveTo(targetFile.FullName); }
					catch (Exception error)
					{
						Debug.LogWarning(error);
					}
				}
				this.changedAssets.Add(FileAndPathUtils.MakeProjectRelative(targetFile.FullName));
			}
			yield break;
		}
		/// <inheritdoc />
		public override void CleanUp()
		{
			this.changedAssets.Clear();

			this.downloadDirectory.Refresh();
			if (this.downloadDirectory.Exists)
			{
				if (Settings.Current.Verbose)
					Debug.Log(string.Format("Cleaning up temporary directory '{0}' of product '{1}'.", this.downloadDirectory, this.product));

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
