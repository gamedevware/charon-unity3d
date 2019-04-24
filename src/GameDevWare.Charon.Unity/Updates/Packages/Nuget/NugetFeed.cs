using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using GameDevWare.Charon.Unity.Async;
using GameDevWare.Charon.Unity.Updates.Packages.Zip;
using GameDevWare.Charon.Unity.Utils;

namespace GameDevWare.Charon.Unity.Updates.Packages.Nuget
{
	internal sealed class NugetFeed
	{
		private readonly Uri feedUri;
		private Uri metadataServiceUri;
		private Uri packageServiceUri;

		public NugetFeed(Uri feedUri)
		{
			this.feedUri = feedUri;
		}

		public Promise<PackageSpecification> GetSpecification(string packageId, SemanticVersion version, Action<string, float> progressCallback = null)
		{
			if (packageId == null) throw new ArgumentNullException("packageId");
			if (version == null) throw new ArgumentNullException("version");

			return new Coroutine<PackageSpecification>(this.GetSpecificationAsync(packageId, version, progressCallback));
		}
		private IEnumerable GetSpecificationAsync(string packageId, SemanticVersion version, Action<string, float> progressCallback)
		{
			yield return new Coroutine(this.LocateServices(progressCallback.Sub(0, 0.1f)));

			var specificationAddress = new Uri(this.packageServiceUri, packageId.ToLowerInvariant() + "/" +
				version.ToString().ToLowerInvariant() + "/" + packageId.ToLowerInvariant() + ".nuspec");

			var getMetadataAsync = HttpUtils.GetStream(specificationAddress,
				downloadProgressCallback: progressCallback.Sub(0, 0.1f).ToDownloadProgress(packageId.ToLowerInvariant() + ".nuspec"),
				timeout: TimeSpan.FromSeconds(10));
			yield return getMetadataAsync;


			using (var textReader = new StreamReader(getMetadataAsync.GetResult(), Encoding.UTF8))
			using (var xmlReader = new XmlTextReader(textReader)
			{
				EntityHandling = EntityHandling.ExpandCharEntities,
				ProhibitDtd = true,
				WhitespaceHandling = WhitespaceHandling.None,
				Normalization = true
			})
			{
				xmlReader.Namespaces = false;

				if (xmlReader.ReadToFollowing("metadata") == false)
				{
					throw new InvalidOperationException("Unable to locate <metadata> element in package specification.");
				}

				var serializer = new XmlSerializer(typeof(PackageSpecification));
				yield return serializer.Deserialize(xmlReader);
			}
		}

		public Promise<PackageMetadata> GetMetadata(string packageId, Action<string, float> progressCallback = null)
		{
			if (packageId == null) throw new ArgumentNullException("packageId");

			return new Coroutine<PackageMetadata>(this.GetMetadataAsync(packageId, progressCallback));
		}
		private IEnumerable GetMetadataAsync(string packageId, Action<string, float> progressCallback)
		{
			yield return new Coroutine(this.LocateServices(progressCallback.Sub(0, 0.1f)));

			var metadataUrl = new Uri(this.metadataServiceUri, packageId.ToLowerInvariant() + "/index.json");

			var getMetadataAsync = HttpUtils.GetJson<PackageMetadata>(metadataUrl,
				downloadProgressCallback: progressCallback.Sub(0, 0.1f).ToDownloadProgress(metadataUrl.AbsoluteUri));

			yield return getMetadataAsync;
			yield return getMetadataAsync.GetResult();
		}

		public Promise<DirectoryInfo> GetPackageContent(string packageId, SemanticVersion version, Action<string, float> progressCallback = null)
		{
			if (packageId == null) throw new ArgumentNullException("packageId");
			if (version == null) throw new ArgumentNullException("version");

			return new Coroutine<DirectoryInfo>(this.GetPackageContentAsync(packageId, version, progressCallback));
		}
		public Promise<DirectoryInfo> GetPackageContent(Uri packageContentUrl, Action<string, float> progressCallback)
		{
			if (packageContentUrl == null) throw new ArgumentNullException("packageContentUrl");

			return new Coroutine<DirectoryInfo>(this.GetPackageContentAsync(packageContentUrl, progressCallback));
		}
		private IEnumerable GetPackageContentAsync(string packageId, SemanticVersion version, Action<string, float> progressCallback)
		{
			var getMetadataAsync = this.GetMetadata(packageId, progressCallback.Sub(0, 0.1f));
			yield return getMetadataAsync;

			foreach (var page in getMetadataAsync.GetResult().Pages)
			{
				foreach (var packageVersion in page.Versions)
				{
					if (packageVersion.CatalogEntry.Version.Equals(version))
					{
						foreach (var step in this.GetPackageContentAsync(new Uri(packageVersion.PackageContentUrl), progressCallback.Sub(0.1f)))
						{
							yield return step;
						}

						yield break;
					}
				}
			}
			throw new InvalidOperationException(string.Format("Unable to find '{1}' version of package '{0}'.", packageId, version));
		}
		private IEnumerable GetPackageContentAsync(Uri packageContentUrl, Action<string, float> progressCallback)
		{
			var fileName = Path.GetFileName(packageContentUrl.AbsolutePath);
			var downloadDirectory = Path.Combine(Settings.TempPath, Path.GetRandomFileName());
			var downloadPath = Path.Combine(downloadDirectory, fileName);
			var downloadAsync = HttpUtils.DownloadToFile(packageContentUrl,
				downloadPath,
				downloadProgressCallback: progressCallback.Sub(0, 0.5f).ToDownloadProgress(fileName));

			yield return downloadAsync;

			var unpackDirectoryName = Path.Combine(downloadDirectory, Path.GetFileNameWithoutExtension(fileName));
			var unpackDirectory = new DirectoryInfo(unpackDirectoryName);
			if (unpackDirectory.Exists == false)
				unpackDirectory.Create();

			var progressValue = 0.5f;
			if (progressCallback != null)
				progressCallback.Invoke(string.Format(Resources.UI_UNITYPLUGIN_PROGRESS_UNPACKING, fileName), progressValue);

			var buffer = new byte[4096];
			using (var zipArchive = new ZipInputStream(new FileStream(downloadPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4 * 1024,
				FileOptions.SequentialScan | FileOptions.DeleteOnClose)))
			{
				var zipEntry = zipArchive.GetNextEntry();
				while (zipEntry != null)
				{
					var entryFileName = zipEntry.Name;

					var fullZipToPath = Path.Combine(unpackDirectory.FullName, entryFileName);
					var directoryName = Path.GetDirectoryName(fullZipToPath);
					if (string.IsNullOrEmpty(directoryName) == false)
						Directory.CreateDirectory(directoryName);

					var zipFileName = Path.GetFileName(fullZipToPath);
					if (string.IsNullOrEmpty(zipFileName))
					{
						zipEntry = zipArchive.GetNextEntry();
						continue;
					}

					if (progressCallback != null)
					{
						progressCallback.Invoke(string.Format(Resources.UI_UNITYPLUGIN_PROGRESS_UNPACKING, zipFileName), progressValue);
					}

					using (var streamWriter = new FileStream(fullZipToPath, FileMode.Create, FileAccess.Write, FileShare.None, 4 * 1024,
						FileOptions.SequentialScan))
					{
						StreamUtils.Copy(zipArchive, streamWriter, buffer);
					}
					progressValue += 0.05f;

					zipEntry = zipArchive.GetNextEntry();
				}
			}

			yield return unpackDirectory;
		}

		private IEnumerable LocateServices(Action<string, float> progressCallback)
		{
			if (this.metadataServiceUri != null)
			{
				yield break;
			}

			var getIndexAsync = HttpUtils.GetJson<ServiceIndex>(this.feedUri,
				downloadProgressCallback: progressCallback.ToDownloadProgress(this.feedUri.AbsoluteUri),
				timeout: TimeSpan.FromSeconds(10));

			yield return getIndexAsync;
			var serviceIndex = getIndexAsync.GetResult();

			foreach (var service in serviceIndex.Services)
			{
				if (service.Type.StartsWith("PackageBaseAddress", StringComparison.OrdinalIgnoreCase))
				{
					this.packageServiceUri = new Uri(service.Id);
				}
				if (string.Equals(service.Type, "RegistrationsBaseUrl", StringComparison.OrdinalIgnoreCase))
				{
					this.metadataServiceUri = new Uri(service.Id);
				}
			}

			if (this.metadataServiceUri == null)
			{
				throw new InvalidOperationException(string.Format("Unable to locate metadata service on '{0}' feed.", this.feedUri));
			}

			if (this.packageServiceUri == null)
			{
				throw new InvalidOperationException(string.Format("Unable to locate package service on '{0}' feed.", this.feedUri));
			}
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return "Nuget feed: " + this.feedUri;
		}
	}
}
