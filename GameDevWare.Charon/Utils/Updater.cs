using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using GameDevWare.Charon.Json;
using GameDevWare.Charon.Windows;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.Charon.Utils
{
	public static class Updater
	{
		private static bool WillingToUpdate;

		public static IEnumerable CheckForUnityAssetUpdatesAsync(Action<string, float> progressCallback = null)
		{
			var assetPath = typeof(Settings).Assembly.Location;
			var currentVersion = Settings.GetCurrentAssetVersion();
			if (currentVersion == null || assetPath == null)
			{
				if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1.0f);

				Debug.LogWarning("This asset is build from sources and can't be updated.");
				yield break;
			}

			var toolName = Path.GetFileNameWithoutExtension(Path.GetFileName(assetPath));

			if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_GETTING_AVAILABLE_BUILDS, 0.10f);

			var updateServerAddress = Settings.Current.GetServerAddress();
			var getBuildsHeaders = new NameValueCollection { { "Accept", "application/json" } };
			var getBuildsUrl = new Uri(updateServerAddress, "Build?product=Charon_Unity");
			var getBuildsRequest = HttpUtils.GetJson<JsonValue>(getBuildsUrl, getBuildsHeaders);
			yield return getBuildsRequest.IgnoreFault();
			if (getBuildsRequest.HasErrors)
			{
				if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1.0f);
				Debug.LogError(String.Format("Unable to get builds list from server. Error: {0}", getBuildsRequest.Error.Unwrap().Message));
				yield break;
			}
			var response = getBuildsRequest.GetResult();
			if (response["error"] != null)
			{
				if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1.0f);
				Debug.LogError(String.Format("Request to '{0}' has failed with message from server: {1}.", getBuildsUrl, response["error"].Stringify()));
				yield break;
			}
			var builds = (JsonArray)response["result"];
			var buildsByVersion = builds.ToDictionary(b => new Version(b["Version"].As<string>()));
			var lastBuild =
			(
				from build in builds
				let buildObj = (JsonObject)build
				let version = new Version(buildObj["Version"].As<string>())
				orderby version descending
				select build
			).FirstOrDefault();
			var lastVersion = lastBuild != null ? new Version(lastBuild["Version"].As<string>()) : null;

			if (lastBuild == null)
			{
				if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1.0f);

				Debug.Log(String.Format("No builds of {0} are available."));
				yield break;
			}

			var isNotCorrupted = IsNotCorrupted(buildsByVersion, currentVersion, assetPath);
			if (isNotCorrupted && currentVersion >= lastVersion)
			{
				if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1.0f);

				Debug.Log(String.Format("{0} version '{1}' is up to date.", Path.GetFileName(assetPath), currentVersion));
				yield break;
			}

			var silent = isNotCorrupted == false;
			var availableBuildSize = lastBuild["Size"].As<long>();
			if (!silent && !AskForUpdate(currentVersion, toolName, lastVersion, availableBuildSize))
			{
				yield break;
			}

			if (progressCallback != null) progressCallback(String.Format(Resources.UI_UNITYPLUGIN_PROGRESS_DOWNLOADINGS, 0, 0), 0.10f);

			var downloadHeaders = new NameValueCollection { { "Accept", "application/octet-stream" } };
			var downloadUrl = new Uri(updateServerAddress, "Build?product=Charon_Unity&id=" + Uri.EscapeDataString(lastVersion.ToString()));
			var downloadPath = Path.GetTempFileName();
			yield return HttpUtils.DownloadToFile(downloadUrl, downloadPath, downloadHeaders, (read, total) =>
			{
				if (progressCallback == null || total == 0)
					return;
				progressCallback(String.Format(Resources.UI_UNITYPLUGIN_PROGRESS_DOWNLOADINGS, (float)read / 1024 / 1024, total / 1024 / 1024), 0.10f + (0.80f * Math.Min(1.0f, (float)read / total)));
			});

			GameDataEditorWindow.FindAllAndClose();
			foreach (var window in UnityEngine.Resources.FindObjectsOfTypeAll<AboutWindow>())
				window.Close();
			foreach (var window in UnityEngine.Resources.FindObjectsOfTypeAll<FeedbackWindow>())
				window.Close();
			foreach (var window in UnityEngine.Resources.FindObjectsOfTypeAll<UpdateRuntimeWindow>())
				window.Close();

			try
			{
				File.Delete(assetPath);
				File.Move(downloadPath, assetPath);
			}
			catch (Exception moveError)
			{
				Debug.LogWarning(String.Format("Failed to move downloaded file from '{0}' to {1}. {2}.", downloadPath, assetPath, moveError.Message));
				Debug.LogError(moveError);
			}
			finally
			{
				// ReSharper disable once EmptyGeneralCatchClause
				try { if (File.Exists(downloadPath)) File.Delete(downloadPath); }
				catch { }
			}

			Debug.Log(String.Format("{1} version is '{0}'. Update is complete.", lastVersion, Path.GetFileName(assetPath)));

			if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1.0f);

			AssetDatabase.ImportAsset(PathUtils.MakeProjectRelative(assetPath), ImportAssetOptions.ForceUpdate);
		}

		public static IEnumerable CheckForCharonUpdatesAsync(Action<string, float> progressCallback = null)
		{
			var checkRequirements = CharonCli.CheckRequirementsAsync();
			yield return checkRequirements;

			var checkResult = checkRequirements.GetResult();
			if (checkResult == RequirementsCheckResult.MissingRuntime)
				yield return UpdateRuntimeWindow.ShowAsync();

			var currentVersion = default(Version);
			var charonPath = Path.GetFullPath(Settings.CharonPath);
			var charonConfigPath = charonPath + ".config";
			var toolName = Path.GetFileNameWithoutExtension(Path.GetFileName(charonPath));

			if (File.Exists(charonPath))
			{
				if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_CHECKING_TOOLS_VERSION, 0.05f);

				var checkToolsVersion = CharonCli.GetVersionAsync();
				yield return checkToolsVersion.IgnoreFault();

				currentVersion = checkToolsVersion.HasErrors ? default(Version) : checkToolsVersion.GetResult();
			}

			if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_GETTING_AVAILABLE_BUILDS, 0.10f);

			var updateServerAddress = Settings.Current.GetServerAddress();
			var getBuildsHeaders = new NameValueCollection { { "Accept", "application/json" } };
			var getBuildsUrl = new Uri(updateServerAddress, "Build?product=Charon");
			var getBuildsRequest = HttpUtils.GetJson<JsonValue>(getBuildsUrl, getBuildsHeaders);
			yield return getBuildsRequest.IgnoreFault();
			if (getBuildsRequest.HasErrors)
			{
				if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1.0f);

				Debug.LogError(String.Format("Unable to get builds list from server. Error: {0}", getBuildsRequest.Error.Unwrap().Message));
				yield break;
			}
			var response = getBuildsRequest.GetResult();
			if (response["error"] != null)
			{
				if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1.0f);
				Debug.LogError(String.Format("Request to '{0}' has failed with message from server: {1}.", getBuildsUrl, response["error"].Stringify()));
				yield break;
			}
			var builds = (JsonArray)response["result"];
			var buildsByVersion = builds.ToDictionary(b => new Version(b["Version"].As<string>()));
			var lastBuild =
			(
				from buildKv in buildsByVersion
				orderby buildKv.Key descending
				select buildKv.Value
			).FirstOrDefault();

			if (lastBuild == null)
			{
				if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1.0f);

				Debug.Log(String.Format("No builds of {0} are available."));
				yield break;
			}
			var lastVersion = new Version(lastBuild["Version"].As<string>());
			var isNotCorrupted = IsNotCorrupted(buildsByVersion, currentVersion, charonPath);
			var versionInSettings = String.IsNullOrEmpty(Settings.Current.EditorVersion) ? default(Version) : new Version(Settings.Current.EditorVersion);

			if (isNotCorrupted && versionInSettings == currentVersion && versionInSettings == lastVersion)
			{
				if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1.0f);

				Debug.Log(String.Format("{0} version '{1}' is up to date.", toolName, currentVersion));
				yield break;
			}

			var silent = checkResult == RequirementsCheckResult.MissingExecutable || checkResult == RequirementsCheckResult.WrongVersion || isNotCorrupted == false;
			var versionToDownload = silent ? versionInSettings ?? lastVersion : lastVersion;

			if (buildsByVersion.ContainsKey(versionToDownload) == false)
			{
				if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1.0f);

				Debug.LogError(String.Format("Build of {0} with version '{1}' is not available to download.", toolName, versionToDownload));
				yield break;
			}

			var availableBuildSize = buildsByVersion[versionToDownload]["Size"].As<long>();
			if (!silent && !AskForUpdate(currentVersion, toolName, versionToDownload, availableBuildSize))
			{
				yield break;
			}

			if (progressCallback != null) progressCallback(String.Format(Resources.UI_UNITYPLUGIN_PROGRESS_DOWNLOADINGS, 0, 0), 0.10f);

			var downloadHeaders = new NameValueCollection { { "Accept", "application/octet-stream" } };
			var downloadUrl = new Uri(updateServerAddress, "Build?product=Charon&id=" + Uri.EscapeDataString(versionToDownload.ToString()));
			var downloadPath = Path.GetTempFileName();
			yield return HttpUtils.DownloadToFile(downloadUrl, downloadPath, downloadHeaders, (read, total) =>
			{
				if (progressCallback == null || total == 0)
					return;
				progressCallback(String.Format(Resources.UI_UNITYPLUGIN_PROGRESS_DOWNLOADINGS, (float)read / 1024 / 1024, total / 1024 / 1024), 0.10f + (0.80f * Math.Min(1.0f, (float)read / total)));
			});

			GameDataEditorWindow.FindAllAndClose();

			try
			{
				if (File.Exists(charonPath))
					File.Delete(charonPath);
				if (Directory.Exists(charonPath))
					Directory.Delete(charonPath);

				var toolsDirectory = Path.GetDirectoryName(charonPath);
				System.Diagnostics.Debug.Assert(toolsDirectory != null, "toolsDirectory != null");
				if (Directory.Exists(toolsDirectory) == false)
					Directory.CreateDirectory(toolsDirectory);

				File.Move(downloadPath, charonPath);

				// ensure config file
				if (File.Exists(charonConfigPath) == false)
				{
					var embeddedConfigStream = typeof(Menu).Assembly.GetManifestResourceStream("GameDevWare.Charon.Charon.exe.config");
					if (embeddedConfigStream != null)
					{
						using (embeddedConfigStream)
						using (var configFileStream = File.Create(charonConfigPath, 8 * 1024, FileOptions.SequentialScan))
						{
							var buffer = new byte[8 * 1024];
							var read = 0;
							while ((read = embeddedConfigStream.Read(buffer, 0, buffer.Length)) > 0)
								configFileStream.Write(buffer, 0, read);
						}
					}
				}
			}
			catch (Exception moveError)
			{
				Debug.LogWarning(String.Format("Failed to move downloaded file from '{0}' to {1}. {2}.", downloadPath, charonPath, moveError.Message));
				Debug.LogError(moveError);
			}
			finally
			{
				// ReSharper disable once EmptyGeneralCatchClause
				try { if (File.Exists(downloadPath)) File.Delete(downloadPath); }
				catch { }
			}

			if (File.Exists(charonPath))
			{
				if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_CHECKING_TOOLS_VERSION, 0.95f);

				var checkToolsVersion = CharonCli.GetVersionAsync();
				yield return checkToolsVersion.IgnoreFault();
				currentVersion = checkToolsVersion.HasErrors ? default(Version) : checkToolsVersion.GetResult();
			}

			Settings.Current.EditorVersion = currentVersion != null ? currentVersion.ToString() : null;
			Settings.Current.Save();

			Debug.Log(String.Format("{1} version is '{0}'. Update is complete.", currentVersion, Path.GetFileName(charonPath)));

			if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_DONE, 1.0f);
		}

		private static bool AskForUpdate(Version currentVersion, string toolName, Version availableVersion, long availableBuildSize)
		{
			if (Updater.WillingToUpdate)
				return true;

			return Updater.WillingToUpdate = EditorUtility.DisplayDialog(
				Resources.UI_UNITYPLUGIN_UPDATE_AVAILABLE_TITLE,
				String.Format(Resources.UI_UNITYPLUGIN_UPDATE_AVAILABLE_MESSAGE, currentVersion, availableVersion, toolName),
				String.Format(Resources.UI_UNITYPLUGIN_DOWNLOAD_BUTTON, availableBuildSize / 1024.0f / 1024.0f));
		}

		private static bool IsNotCorrupted(Dictionary<Version, JsonValue> buildByVersion, Version currentVersion, string filePath)
		{
			if (buildByVersion == null) throw new ArgumentNullException("buildByVersion");
			if (filePath == null) throw new ArgumentNullException("filePath");

			var currentBuild = currentVersion != null && buildByVersion.ContainsKey(currentVersion) ? buildByVersion[currentVersion] : default(JsonObject);
			if (currentBuild == null)
				return false;

			var hashAlgorithmName = (string)currentBuild["HashAlgorithm"];
			var hash = (string)currentBuild["Hash"];
			var fileHash = default(string);
			using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan))
			using (var hashAlgorithm = HashAlgorithm.Create(hashAlgorithmName) ?? MD5.Create())
				fileHash = BitConverter.ToString(hashAlgorithm.ComputeHash(file)).Replace("-", "");

			if (string.Equals(fileHash, hash, StringComparison.OrdinalIgnoreCase) == false)
			{
				if (Settings.Current.Verbose)
					Debug.Log(String.Format("File '{0}' hash is '{1}' while '{2}' is expected for version '{3}'.", filePath, fileHash, hash, currentVersion));

				return false;
			}

			return true;
		}
	}
}
