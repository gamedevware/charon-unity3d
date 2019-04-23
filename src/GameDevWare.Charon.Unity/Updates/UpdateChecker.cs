using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Text;
using GameDevWare.Charon.Unity.Async;
using GameDevWare.Charon.Unity.Packages;
using GameDevWare.Charon.Unity.Updates;
using GameDevWare.Charon.Unity.Windows;
using UnityEditor;
using UnityEngine;
using Coroutine = GameDevWare.Charon.Unity.Async.Coroutine;

namespace GameDevWare.Charon.Unity
{
	[InitializeOnLoad]
	internal static class UpdateChecker
	{
		private const string LAST_UPDATE_PREFS_KEY = Settings.PREF_PREFIX + "LastUpdateCheckTime";
		private static readonly TimeSpan UpdateCheckPeriod = TimeSpan.FromHours(8);
		private static readonly TimeSpan UpdateCheckCooldown = TimeSpan.FromMinutes(0.3);

		private static DateTime LastCheckTime;
		private static DateTime CheckCooldownTime;
		private static Promise CheckPromise;

		static UpdateChecker()
		{
			EditorApplication.update += Update;
			CheckCooldownTime = DateTime.UtcNow + UpdateCheckCooldown;
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
				if (lastVersion <= currentVersion)
				{
					continue; // no updates
				}

				releaseNotes.AppendFormat("<size=20>{0}</size>" + Environment.NewLine, product.Name);
				releaseNotes.AppendLine();

				var getReleaseNotesAsync = PackageManager.GetReleaseNotes(product.Id, currentVersion, lastVersion);
				yield return getReleaseNotesAsync;

				if (getReleaseNotesAsync.HasErrors)
				{
					continue; // has errors
				}

				releaseNotes.AppendLine(getReleaseNotesAsync.GetResult());
				releaseNotes.AppendLine();
			}

			var window = EditorWindow.GetWindow<UpdateAvailableWindow>(utility: true);
			window.ReleaseNotes = releaseNotes.ToString();
			
			SaveLastCheckTime(LastCheckTime = DateTime.UtcNow);
			CheckCooldownTime = DateTime.UtcNow + UpdateCheckCooldown;
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
	}
}
