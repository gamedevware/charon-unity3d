/*
	Copyright (c) 2025 Denis Zykov

	This is part of "Charon: Game Data Editor" Unity Plugin.

	Charon Game Data Editor Unity Plugin is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see http://www.gnu.org/licenses.
*/

using System;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using GameDevWare.Charon.Editor.Utils;

namespace GameDevWare.Charon.Editor.ServerApi
{
	internal sealed class ServerApiClient
	{
		private readonly Uri baseAddress;
		private readonly NameValueCollection requestHeaders;

		public Uri BaseAddress => this.baseAddress;

		public ServerApiClient(Uri baseAddress)
		{
			if (baseAddress == null) throw new ArgumentNullException(nameof(baseAddress));

			this.requestHeaders = new NameValueCollection();
			this.baseAddress = baseAddress;
		}

		public Uri GetApiKeysUrl()
		{
			return new Uri(this.baseAddress, "view/user/me/profile/api-keys");
		}
		public void UseApiKey(string apiKey)
		{
			if (apiKey == null) throw new ArgumentNullException(nameof(apiKey));

			this.requestHeaders["Authorization"] = "Bearer " + apiKey;
		}
		public async Task<Project[]> GetMyProjectsAsync()
		{
			var getMyProjectsAddress = new Uri(this.baseAddress, "api/v1/project/my/");
			var myProjects = await HttpUtils.GetJsonAsync<ApiResponse<Project[]>>(getMyProjectsAddress, this.requestHeaders);
			return myProjects.GetResponseResultOrError();
		}
		public Task DownloadDataSourceAsync
		(
			string branchId,
			GameDataFormat storeFormat,
			string downloadPath,
			Action<long, long> downloadProgressCallback,
			CancellationToken cancellation = default)
		{
			if (branchId == null) throw new ArgumentNullException(nameof(branchId));
			if (downloadPath == null) throw new ArgumentNullException(nameof(downloadPath));

			var requestHeaders = new NameValueCollection(this.requestHeaders);

			switch (storeFormat)
			{
				case GameDataFormat.Json:
					requestHeaders.Add("Accept", "application/json");
					break;
				case GameDataFormat.MessagePack:
					requestHeaders.Add("Accept", "application/x-msgpack");
					break;
				default:
					throw new InvalidOperationException($"Unknown storage format '{storeFormat}'.");
			}

			var downloadParams = "?" +
				"exportMode=publication&" +
				"schemas=%2A&" +
				"properties=%2A&" +
				"languages=%2A&" +
				"download=true";

			var downloadDataSourceAddress = new Uri(this.baseAddress, $"api/v1/datasource/{branchId}/collections/raw/{downloadParams}");
			var downloadDataSourceAsync = HttpUtils.DownloadToFileAsync(
				downloadDataSourceAddress,
				downloadPath,
				requestHeaders,
				downloadProgressCallback,
				cancellation: cancellation);
			return downloadDataSourceAsync;
		}
		public Task UploadDataSourceAsync
		(
			string branchId,
			GameDataFormat storeFormat,
			string uploadPath,
			Action<long, long> uploadProgressCallback,
			CancellationToken cancellation = default)
		{
			if (branchId == null) throw new ArgumentNullException(nameof(branchId));
			if (uploadPath == null) throw new ArgumentNullException(nameof(uploadPath));

			var requestHeaders = new NameValueCollection(this.requestHeaders);
			requestHeaders.Add("Accept", "*/*");

			switch (storeFormat)
			{
				case GameDataFormat.Json:
					requestHeaders.Add("Content-Type", "application/json");
					break;
				case GameDataFormat.MessagePack:
					requestHeaders.Add("Content-Type", "application/x-msgpack");
					break;
				default:
					throw new InvalidOperationException($"Unknown storage format '{storeFormat}'.");
			}

			var uploadDataSourceAddress = new Uri(this.baseAddress, $"api/v1/datasource/{branchId}");
			var uploadDataSourceAsync = HttpUtils.UploadFromFileAsync(
				"PUT",
				uploadDataSourceAddress,
				uploadPath,
				requestHeaders,
				uploadProgressCallback,
				cancellation: cancellation);
			return uploadDataSourceAsync;
		}

		public async Task<string> GetLoginCodeAsync(string apiKey)
		{
			if (apiKey == null) throw new ArgumentNullException(nameof(apiKey));

			var requestHeaders = new NameValueCollection(this.requestHeaders);
			requestHeaders.Add("Accept", "application/json");
			requestHeaders.Add("Content-Type", "application/json");

			var request = new ApiKeyAuthenticateRequest {
				ApiKey = apiKey
			};
			var beginApiKeyAuthFlow = new Uri(this.baseAddress, "api/v1/auth/flow/api-key/");
			var apiKeyAuthFlow = await HttpUtils.PostJsonAsync<ApiKeyAuthenticateRequest, ApiResponse<AuthenticationFlowStage>>(
				beginApiKeyAuthFlow, request, requestHeaders);
			return apiKeyAuthFlow.GetResponseResultOrError().AuthorizationCode;
		}
	}
}
