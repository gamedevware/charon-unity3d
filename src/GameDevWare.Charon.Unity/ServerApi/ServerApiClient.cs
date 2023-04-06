using GameDevWare.Charon.Unity.Utils;
using System;
using System.Collections.Specialized;
using GameDevWare.Charon.Unity.Async;

namespace GameDevWare.Charon.Unity.ServerApi
{
	internal sealed class ServerApiClient
	{
		private readonly Uri baseAddress;
		private readonly NameValueCollection requestHeaders;

		public Uri BaseAddress { get { return this.baseAddress; } }

		public ServerApiClient(Uri baseAddress)
		{
			if (baseAddress == null) throw new ArgumentNullException("baseAddress");

			this.requestHeaders = new NameValueCollection();
			this.baseAddress = baseAddress;
		}

		public Uri GetApiKeysUrl()
		{
			return new Uri(this.baseAddress, "view/user/me/profile/api-keys");
		}
		public void UseApiKey(string apiKey)
		{
			if (apiKey == null) throw new ArgumentNullException("apiKey");

			this.requestHeaders["Authorization"] = "Bearer " + apiKey;
		}
		public Promise<Project[]> GetMyProjectsAsync()
		{
			var getMyProjectsAddress = new Uri(this.baseAddress, "api/v1/project/my/");
			var getMyProjectsAsync = HttpUtils.GetJson<ApiResponse<Project[]>>(getMyProjectsAddress, this.requestHeaders);
			return getMyProjectsAsync.ContinueWith(result => result.GetResult().GetResponseResultOrError());
		}
		public Promise DownloadDataSourceAsync(string branchId, GameDataStoreFormat storeFormat, string downloadPath, Action<long, long> downloadProgressCallback)
		{
			if (branchId == null) throw new ArgumentNullException("branchId");
			if (downloadPath == null) throw new ArgumentNullException("downloadPath");

			var requestHeaders = new NameValueCollection(this.requestHeaders);

			switch (storeFormat)
			{
				case GameDataStoreFormat.Json:
					requestHeaders.Add("Accept", "application/json");
					break;
				case GameDataStoreFormat.MessagePack:
					requestHeaders.Add("Accept", "application/x-msgpack");
					break;
				default:
					throw new InvalidOperationException(string.Format("Unknown storage format '{0}'.", storeFormat));
			}

			var downloadParams = "?" +
				"exportMode=publication&" +
				"entities=%2A&" +
				"attributes=%2A&" +
				"languages=%2A&" +
				"download=true";

			var downloadDataSourceAddress = new Uri(this.baseAddress, string.Format("api/v1/datasource/{0}/collections/raw/{1}", branchId, downloadParams));
			var downloadDataSourceAsync = HttpUtils.DownloadToFile(
				downloadDataSourceAddress,
				downloadPath,
				requestHeaders,
				downloadProgressCallback);
			return downloadDataSourceAsync;
		}
	}
}
