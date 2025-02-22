/*
	Copyright (c) 2025 Denis Zykov

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

using System;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using Editor.GameDevWare.Charon.Utils;
using GameDevWare.Charon;

namespace Editor.GameDevWare.Charon.Services.ServerApi
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

			var downloadParams = "?download=true";

			var downloadDataSourceAddress = new Uri(this.baseAddress, $"api/v1/datasource/{branchId}/raw/{downloadParams}");
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
