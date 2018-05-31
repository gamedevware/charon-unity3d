using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using GameDevWare.Charon.Async;
using GameDevWare.Charon.Json;
using UnityEngine;

namespace GameDevWare.Charon.Utils
{
	public static class UpdateServerCli
	{
		public const string PRODUCT_CHARON = "Charon";
		public const string PRODUCT_CHARON_UNITY = "Charon_Unity";
		public const string PRODUCT_CHARON_UNITY_ASSEMBLY = "GameDevWare.Charon";
		public const string PRODUCT_EXPRESSIONS = "Expressions";
		public const string PRODUCT_EXPRESSIONS_ASSEMBLY = "GameDevWare.Dynamic.Expressions";
		public const string PRODUCT_TEXT_TEMPLATES = "TextTransform";
		public const string PRODUCT_TEXT_TEMPLATES_ASSEMBLY = "GameDevWare.TextTransform";


		public static Promise<BuildInfo[]> GetBuilds(string product)
		{
			if (string.IsNullOrEmpty(product)) throw new ArgumentException("Value cannot be null or empty.", "product");

			return new Coroutine<BuildInfo[]>(GetBuildsAsync(product));
		}
		private static IEnumerable GetBuildsAsync(string product)
		{
			var updateServerAddress = Settings.Current.GetServerAddress();
			var getBuildsHeaders = new NameValueCollection { { "Accept", "application/json" } };
			var getBuildsUrl = new Uri(updateServerAddress, "Build?product=" + Uri.EscapeDataString(product));
			var getBuildsRequest = HttpUtils.GetJson<JsonValue>(getBuildsUrl, getBuildsHeaders, timeout: TimeSpan.FromSeconds(10));
			yield return getBuildsRequest.IgnoreFault();

			if (getBuildsRequest.HasErrors)
				throw new Exception(String.Format("Unable to get builds list from server. Error: {0}", getBuildsRequest.Error.Unwrap().Message));

			var response = getBuildsRequest.GetResult();
			if (response["error"] != null)
				throw new Exception(String.Format("Request to '{0}' has failed with message from server: {1}.", getBuildsUrl, response["error"].Stringify()));

			var jsonArray = (JsonArray)response["result"];
			var builds = new List<BuildInfo>();
			foreach (var build in jsonArray)
			{
				try
				{
					builds.Add(build.As<BuildInfo>());
				}
				catch (Exception readError)
				{
					if (Settings.Current.Verbose)
						Debug.LogWarning("Failed to read build information from JSON response: \r\n" + build.Stringify() + "\r\n" + readError);
				}
			}
			yield return builds.ToArray();
		}

		public static Promise DownloadBuild(string product, Version version, string destinationFilePath, Action<string, float> progressCallback = null)
		{
			var updateServerAddress = Settings.Current.GetServerAddress();
			var downloadHeaders = new NameValueCollection { { "Accept", "application/octet-stream" } };
			var downloadUrl = new Uri(updateServerAddress, "Build?product=" + Uri.EscapeDataString(product) + "&id=" + Uri.EscapeDataString(version.ToString()));
			
			return HttpUtils.DownloadToFile(downloadUrl, destinationFilePath, downloadHeaders, (read, total) =>
			{
				if (progressCallback == null || total == 0)
					return;

				progressCallback(String.Format(Resources.UI_UNITYPLUGIN_PROGRESS_DOWNLOADINGS, (float)read / 1024 / 1024, (float)total / 1024 / 1024), 0.10f + (0.80f * Math.Min(1.0f, (float)read / total)));

			}, timeout: TimeSpan.FromSeconds(30));
		}
	}
}
