using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using GameDevWare.Charon.Unity.Async;
using GameDevWare.Charon.Unity.Updates.Packages;
using GameDevWare.Charon.Unity.Utils;
using GameDevWare.Charon.Unity.Windows;
using UnityEditor;
using UnityEngine;
using Coroutine = GameDevWare.Charon.Unity.Async.Coroutine;

namespace GameDevWare.Charon.Unity.Updates
{
	[InitializeOnLoad]
	internal static class UpdateChecker
	{
		private const string SKIPPED_UPDATE_PREFS_KEY = Settings.PREF_PREFIX + "SkippedUpdateHash";
		private const string LAST_UPDATE_PREFS_KEY = Settings.PREF_PREFIX + "LastUpdateCheckTime";
		private const string LAST_VERSION_TEMPLATE_PREFS_KEY = Settings.PREF_PREFIX + "Last{0}Version";
		private static readonly TimeSpan UpdateCheckPeriod = TimeSpan.FromHours(8);
		private static readonly TimeSpan UpdateCheckCooldown = TimeSpan.FromMinutes(0.3);

		private static DateTime LastCheckTime;
		private static DateTime CheckCooldownTime;
		private static readonly Dictionary<string, SemanticVersion> LastVersionByProductId;
		private static Promise CheckPromise;

		static UpdateChecker()
		{
			EditorApplication.update += Update;
			CheckCooldownTime = DateTime.UtcNow + UpdateCheckCooldown;
			LastVersionByProductId = new Dictionary<string, SemanticVersion>(StringComparer.OrdinalIgnoreCase);
		}

		private static void Update()
		{
			if (EditorApplication.isCompiling)
				return;

			if (LastCheckTime == default(DateTime))
			{
				LastCheckTime = LoadLastCheckTime();
			}

			if (DateTime.UtcNow - LastCheckTime > UpdateCheckPeriod &&
				DateTime.UtcNow > CheckCooldownTime &&
				(CheckPromise == null || CheckPromise.IsCompleted))
			{
				CheckPromise = new Coroutine(CheckForUpdates());
			}
		}
		private static IEnumerable CheckForUpdates()
		{
			if (Settings.Current.Verbose)
			{
				Debug.Log("Checking for product updates.");
			}

			SaveLastCheckTime(LastCheckTime = DateTime.UtcNow);
			CheckCooldownTime = DateTime.UtcNow + UpdateCheckCooldown;

			var checkHadErrors = false;
			var products = ProductInformation.GetKnownProducts();

			yield return Promise.WhenAll(products.Select(p => p.AllBuilds).ToArray());
			yield return Promise.WhenAll(products.Select(p => p.CurrentVersion).ToArray());

			var releaseNotes = new StringBuilder();
			foreach (var product in products)
			{
				if (product.CurrentVersion.HasErrors || product.AllBuilds.HasErrors)
				{
					continue; // has errors
				}

				var currentVersion = (product.ExpectedVersion ?? product.CurrentVersion.GetResult());
				var lastVersion = product.AllBuilds.GetResult().Select(p => p.Version).Max();

				SaveLastVersion(product.Id, lastVersion);

				if (lastVersion == null || lastVersion <= currentVersion)
				{
					continue; // no updates
				}

				if (Settings.Current.Verbose)
				{
					Debug.Log(string.Format("Product '{0}' current version is '{1}', last version is '{2}'.", product.Name, currentVersion, lastVersion));
				}

				releaseNotes.AppendFormat("<size=20>{0}</size>" + Environment.NewLine, product.Name);
				releaseNotes.AppendLine();

				var getReleaseNotesAsync = PackageManager.GetReleaseNotes(product.Id, currentVersion, lastVersion);
				yield return getReleaseNotesAsync;

				if (getReleaseNotesAsync.HasErrors)
				{
					checkHadErrors = true;
					continue; // has errors
				}

				releaseNotes.AppendLine(getReleaseNotesAsync.GetResult());
				releaseNotes.AppendLine();
			}

			var releaseNotesStr = releaseNotes.ToString();

			if (releaseNotesStr.Length > 0 && IsUpdateSkipped(releaseNotesStr) == false)
			{
				var window = EditorWindow.GetWindow<UpdateAvailableWindow>(utility: true);
				window.ReleaseNotes = releaseNotesStr;
			}
			else
			{
				if (Settings.Current.Verbose)
				{
					Debug.Log("No updates are found or current update is skipped.");
				}
			}

			if (checkHadErrors)
			{
				CheckCooldownTime = DateTime.UtcNow + UpdateCheckPeriod;
			}
		}

		private static DateTime LoadLastCheckTime()
		{
			try
			{
				var time = EditorPrefs.GetString(LAST_UPDATE_PREFS_KEY);
				if (string.IsNullOrEmpty(time))
					return default(DateTime) + TimeSpan.FromTicks(1);
				else
					return new DateTime(long.Parse(time, CultureInfo.InvariantCulture), DateTimeKind.Utc);
			}
			catch
			{
				return default(DateTime) + TimeSpan.FromTicks(1);
			}
		}
		private static void SaveLastCheckTime(DateTime time)
		{
			EditorPrefs.SetString(LAST_UPDATE_PREFS_KEY, time.Ticks.ToString());
		}
		private static SemanticVersion LoadLastVersion(string productId)
		{
			try
			{
				var lastVersion = EditorPrefs.GetString(string.Format(LAST_VERSION_TEMPLATE_PREFS_KEY, productId));
				if (string.IsNullOrEmpty(lastVersion))
					return null;
				else
					return SemanticVersion.Parse(lastVersion);
			}
			catch
			{
				return null;
			}
		}
		private static void SaveLastVersion(string productId, SemanticVersion lastVersion)
		{
			if (Settings.Current.Verbose && lastVersion != null)
			{
				Debug.Log(string.Format("Saving information about last version '{0}' of product '{1}' in EditorPrefs.", lastVersion, productId));
			}

			EditorPrefs.SetString(string.Format(LAST_VERSION_TEMPLATE_PREFS_KEY, productId), lastVersion != null ? lastVersion.ToString() : "");
			lock (LastVersionByProductId)
				LastVersionByProductId[productId] = lastVersion;
		}
		private static bool IsUpdateSkipped(string releaseNotes)
		{
			try
			{
				var skippedUpdateHash = EditorPrefs.GetString(SKIPPED_UPDATE_PREFS_KEY);
				if (string.IsNullOrEmpty(skippedUpdateHash) || string.IsNullOrEmpty(releaseNotes))
					return false;

				return string.Equals(skippedUpdateHash, FileAndPathUtils.ComputeNameHash(releaseNotes, "SHA1"));
			}
			catch
			{
				return false;
			}
		}
		public static void SkipUpdates(string releaseNotes)
		{
			EditorPrefs.SetString(SKIPPED_UPDATE_PREFS_KEY, FileAndPathUtils.ComputeNameHash(releaseNotes, "SHA1"));
		}
		public static SemanticVersion GetLastCharonVersion()
		{
			const string PRODUCT_ID = ProductInformation.PRODUCT_CHARON;

			lock (LastVersionByProductId)
			{
				if (!LastVersionByProductId.ContainsKey(PRODUCT_ID))
				{
					LastVersionByProductId[PRODUCT_ID] = LoadLastVersion(PRODUCT_ID);
				}
				return LastVersionByProductId[PRODUCT_ID];
			}
		}

	}
}
