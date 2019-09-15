using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameDevWare.Charon.Unity.Async;
using GameDevWare.Charon.Unity.Updates.Packages.Nuget;
using GameDevWare.Charon.Unity.Utils;
using UnityEngine;

namespace GameDevWare.Charon.Unity.Updates.Packages
{
	internal static class PackageManager
	{
		public const string NUGET_FEED_URL = "https://api.nuget.org/v3/index.json";
		public const string PRODUCT_EXPRESSIONS_ASSEMBLY = "GameDevWare.Dynamic.Expressions";

		private static readonly NugetFeed Feed = new NugetFeed(new Uri(NUGET_FEED_URL));

		public static readonly Dictionary<string, string> NugetPackagesByProduct = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
			{ProductInformation.PRODUCT_CHARON, "GameDevWare.Charon" },
			{ProductInformation.PRODUCT_CHARON_UNITY, "GameDevWare.Charon.Unity" },
			{ProductInformation.PRODUCT_EXPRESSIONS, "GameDevWare.Dynamic.Expressions" },
			{ProductInformation.PRODUCT_TEXT_TEMPLATES, "GameDevWare.TextTransform" },
		};

		public static Promise<PackageInfo[]> GetVersions(string product, Action<string, float> progressCallback = null)
		{
			if (string.IsNullOrEmpty(product)) throw new ArgumentException("Value cannot be null or empty.", "product");

			return new Coroutine<PackageInfo[]>(GetVersionsAsync(product, progressCallback));
		}
		private static IEnumerable GetVersionsAsync(string product, Action<string, float> progressCallback)
		{
			var packageId = GetPackageId(product);

			var getMetadataAsync = Feed.GetMetadata(packageId, progressCallback);
			yield return getMetadataAsync;

			var list = new List<PackageInfo>();
			foreach (var page in getMetadataAsync.GetResult().Pages)
			{
				foreach (var version in page.Versions)
				{
					if (version.CatalogEntry.IsListed == false)
					{
						continue;
					}
					var buildInfo = new PackageInfo
					{
						Id = version.PackageContentUrl,
						Version = version.CatalogEntry.Version
					};
					list.Add(buildInfo);
				}
			}
			yield return list.ToArray();
		}

		public static Promise<string> GetReleaseNotes(string product, SemanticVersion fromVersion, SemanticVersion toVersion, Action<string, float> progressCallback = null)
		{
			if (product == null) throw new ArgumentNullException("product");
			if (toVersion == null) throw new ArgumentNullException("toVersion");

			return new Coroutine<string>(GetReleaseNotesAsync(product, fromVersion, toVersion, progressCallback));
		}
		private static IEnumerable GetReleaseNotesAsync(string product, SemanticVersion fromVersion, SemanticVersion toVersion, Action<string, float> progressCallback)
		{
			var packageId = GetPackageId(product);

			var getMetadataAsync = Feed.GetMetadata(packageId, progressCallback);
			yield return getMetadataAsync;

			var releaseNotes = new SortedDictionary<SemanticVersion, Promise<string>>();
			foreach (var page in getMetadataAsync.GetResult().Pages)
			{
				foreach (var packageVersion in page.Versions)
				{
					var semVersion = packageVersion.CatalogEntry.Version;
					if (packageVersion.CatalogEntry.IsListed == false)
					{
						continue;
					}
					if (fromVersion != null && semVersion < fromVersion)
					{
						continue;
					}
					if (toVersion != null && semVersion > toVersion)
					{
						continue;
					}

					if (semVersion == fromVersion)
					{
						continue; // don't add current version to release notes
					}

					var getReleaseNotesAsync = Feed
						.GetSpecification(packageId, semVersion)
						.ContinueWith(p => p.HasErrors ? p.Error.Unwrap().ToString() : p.GetResult().ReleaseNotes);

					releaseNotes.Add(semVersion, getReleaseNotesAsync);
				}
			}

			if (releaseNotes.Count == 0)
			{
				yield return string.Empty;
				yield break;
			}

			yield return Promise.WhenAll(releaseNotes.Values.ToArray()).IgnoreFault();

			yield return string.Join(
				Environment.NewLine,
				releaseNotes.Select(kv => "<b>#" + kv.Key + "</b>" + Environment.NewLine + Environment.NewLine + kv.Value.GetResult() + Environment.NewLine).ToArray()
			);
		}

		public static Promise<FileInfo[]> DownloadAndUnpack(string product, SemanticVersion version, ArtifactKind kind, DirectoryInfo destinationDirectory, Action<string, float> progressCallback = null)
		{
			if (product == null) throw new ArgumentNullException("product");
			if (version == null) throw new ArgumentNullException("version");
			if (destinationDirectory == null) throw new ArgumentNullException("destinationDirectory");

			return new Coroutine<FileInfo[]>(DownloadAndUnpackAsync(product, version, kind, destinationDirectory, progressCallback));
		}
		private static IEnumerable DownloadAndUnpackAsync(string product, SemanticVersion version, ArtifactKind kind, DirectoryInfo destinationDirectory, Action<string, float> progressCallback = null)
		{
			var packageId = GetPackageId(product);

			var getContentAsync = Feed.GetPackageContent(packageId, version, progressCallback);
			yield return getContentAsync;

			var touchedFiles = new List<FileInfo>();
			var packageDirectory = getContentAsync.GetResult();
			var toolsDirectory = packageDirectory.GetDirectories().FirstOrDefault(d => string.Equals(d.Name, "tools", StringComparison.OrdinalIgnoreCase));
			var libDirectory = packageDirectory.GetDirectories().FirstOrDefault(d => string.Equals(d.Name, "lib", StringComparison.OrdinalIgnoreCase));

			if (kind == ArtifactKind.Library && libDirectory != null && libDirectory.Exists)
			{
				var prefTargets = default(string[]);
				if (Environment.Version > new Version(4, 0))
				{
					prefTargets = new string[] { "net46", "net452", "net451", "net45", "net403", "net40", "net35", "net20", "net11" };
				}
				else
				{
					prefTargets = new string[] { "net35", "net20", "net11" };

				}

				var targetFrameworkDirectories = libDirectory.GetDirectories().Where(d => Array.IndexOf(prefTargets, d.Name) >= 0).ToArray();
				Array.Sort(targetFrameworkDirectories, (x, y) => Array.IndexOf(prefTargets, x.Name).CompareTo(Array.IndexOf(prefTargets, y.Name)));

				foreach (var targetFrameworkDirectory in targetFrameworkDirectories)
				{
					destinationDirectory.Refresh();
					if (destinationDirectory.Exists == false)
					{
						destinationDirectory.Create();
					}

					if (Settings.Current.Verbose)
					{
						Debug.Log(string.Format("Copying 'Library' artifacts from '{0}' package into '{1}' directory.", packageId, destinationDirectory.FullName));
					}

					foreach (var file in targetFrameworkDirectory.GetFiles())
					{
						touchedFiles.Add(DeployAtPath(file, destinationDirectory));
					}

					yield return touchedFiles.ToArray();
					yield break;
				}
			}
			else if (kind == ArtifactKind.Tool && toolsDirectory != null && toolsDirectory.Exists)
			{
				destinationDirectory.Refresh();
				if (destinationDirectory.Exists == false)
				{
					destinationDirectory.Create();
				}

				if (Settings.Current.Verbose)
				{
					Debug.Log(string.Format("Copying 'Tools' artifacts from '{0}' package into '{1}' directory.", packageId, destinationDirectory.FullName));
				}

				foreach (var file in toolsDirectory.GetFiles())
				{
					touchedFiles.Add(DeployAtPath(file, destinationDirectory));
				}

				yield return touchedFiles.ToArray();
				yield break;
			}
			throw new InvalidOperationException("Package '{0}' doesn't contains libraries or tools to deploy.");
		}
		private static FileInfo DeployAtPath(FileInfo file, DirectoryInfo destinationDirectory)
		{
			if (destinationDirectory == null) throw new ArgumentNullException("destinationDirectory");
			if (file == null) throw new ArgumentNullException("file");

			destinationDirectory.Refresh();

			var targetPath = Path.Combine(destinationDirectory.FullName, file.Name);

			if (Settings.Current.Verbose)
			{
				Debug.Log(string.Format("Copying '{0}' file to '{1}' directory.", file.Name, destinationDirectory.FullName));
			}

			file.CopyTo(targetPath, overwrite: true);
			if (string.Equals(file.Extension, ".exe", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(file.Extension, ".dll", StringComparison.OrdinalIgnoreCase))
			{
				if (Settings.Current.Verbose)
				{
					Debug.Log(string.Format("Computing SHA1 hash for '{0}' file in '{1}' directory.", file.Name, destinationDirectory.FullName));
				}

				var targetPathHash = targetPath + ".sha1";
				File.WriteAllText(targetPathHash, FileAndPathUtils.ComputeHash(targetPath, "SHA1"));
			}
			return new FileInfo(targetPath);
		}

		private static string GetPackageId(string product)
		{
			var packageId = default(string);
			if (NugetPackagesByProduct.TryGetValue(product, out packageId) == false)
			{
				throw new InvalidOperationException(string.Format("Unable to find package id for product '{0}'.", product));
			}

			return packageId;
		}
	}
}
