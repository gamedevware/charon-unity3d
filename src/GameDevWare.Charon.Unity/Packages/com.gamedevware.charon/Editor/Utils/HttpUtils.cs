/*
	Copyright (c) 2025 GameDevWare, Denis Zykov

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
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GameDevWare.Charon.Editor.Json;
using UnityEngine;

namespace GameDevWare.Charon.Editor.Utils
{
	internal static class HttpUtils
	{
		public static readonly string UserAgentHeaderValue = string.Format("{0}/{1} (OS: {2}, Unity Version: {3}, Product: {4})",
			typeof(HttpUtils).Assembly.GetName(false).Name, typeof(HttpUtils).Assembly.GetName(false).Version,
			Application.platform, Application.unityVersion, Application.productName);

		private static readonly HashSet<string> ContentHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"Allow",
			"Content-Disposition",
			"Content-Encoding",
			"Content-Language",
			"Content-Length",
			"Content-Location",
			"Content-MD5",
			"Content-Range",
			"Content-Type",
			"Expires",
			"Last-Modified",
		};

		private const int BUFFER_SIZE = 32 * 1024;

		private static readonly HttpClient SharedHttpClient = new HttpClient();

		public static async Task UploadFromFileAsync
		(
			string method,
			Uri url,
			string uploadFilePath,
			NameValueCollection requestHeaders = null,
			Action<long, long> uploadProgressCallback = null,
			TimeSpan timeout = default,
			CancellationToken cancellation = default)
		{
			if (url == null) throw new ArgumentNullException(nameof(url));
			if (uploadFilePath == null) throw new ArgumentNullException(nameof(uploadFilePath));

			const bool LEAVE_OPEN = false;
			var uploadStream = new FileStream(uploadFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, BUFFER_SIZE, FileOptions.SequentialScan);
			if (requestHeaders == null) requestHeaders = new NameValueCollection();
			try
			{
				await RequestToAsync(method, url, Stream.Null, uploadStream, LEAVE_OPEN, requestHeaders, uploadProgressCallback, timeout, cancellation);
			}
			catch (Exception requestError)
			{
				throw EnrichWebError(requestError, url, requestHeaders, timeout);
			}
		}
		public static async Task DownloadToFileAsync
		(
			Uri url,
			string downloadToFilePath,
			NameValueCollection requestHeaders = null,
			Action<long, long> downloadProgressCallback = null,
			TimeSpan timeout = default,
			CancellationToken cancellation = default)
		{
			if (url == null) throw new ArgumentNullException(nameof(url));
			if (downloadToFilePath == null) throw new ArgumentNullException(nameof(downloadToFilePath));

			var downloadDir = Path.GetDirectoryName(downloadToFilePath);
			if (string.IsNullOrEmpty(downloadDir) == false && Directory.Exists(downloadDir) == false)
				Directory.CreateDirectory(downloadDir);

			const bool LEAVE_OPEN = false;
			var downloadToStream = new FileStream(downloadToFilePath, FileMode.Create, FileAccess.Write, FileShare.None, BUFFER_SIZE, FileOptions.None);
			try
			{
				await RequestToAsync("GET", url, downloadToStream, Stream.Null, LEAVE_OPEN, requestHeaders, downloadProgressCallback, timeout, cancellation);
			}
			catch (Exception requestError)
			{
				throw EnrichWebError(requestError, url, requestHeaders, timeout);
			}
		}
		public static async Task DownloadToAsync
		(
			Stream downloadToStream,
			Uri url,
			NameValueCollection requestHeaders = null,
			Action<long, long> downloadProgressCallback = null,
			TimeSpan timeout = default,
			CancellationToken cancellation = default)
		{
			const bool LEAVE_OPEN = true;
			try
			{
				await RequestToAsync("GET", url, downloadToStream, Stream.Null, LEAVE_OPEN, requestHeaders, downloadProgressCallback, timeout, cancellation);
			}
			catch (Exception requestError)
			{
				throw EnrichWebError(requestError, url, requestHeaders, timeout);
			}
		}
		public static async Task<T> GetJsonAsync<T>
		(
			Uri url,
			NameValueCollection requestHeaders = null,
			Action<long, long> downloadProgressCallback = null,
			TimeSpan timeout = default,
			CancellationToken cancellation = default)
		{
			using var responseStream = new MemoryStream();
			const bool LEAVE_OPEN = true;

			try
			{
				if (requestHeaders == null || string.IsNullOrEmpty(requestHeaders["Accept"]))
				{
					requestHeaders = requestHeaders != null ? new NameValueCollection(requestHeaders) : new NameValueCollection();
					requestHeaders.Add("Accept", "application/json");
				}

				await RequestToAsync("GET", url, responseStream, Stream.Null, LEAVE_OPEN, requestHeaders, downloadProgressCallback, timeout, cancellation);

				responseStream.Position = 0;
				if (typeof(JsonValue) == typeof(T))
					return (T)(object)JsonValue.Load(responseStream);
				else
					return JsonValue.Load(responseStream).ToObject<T>();
			}
			catch (Exception requestError)
			{
				throw EnrichWebError(requestError, url, requestHeaders, timeout);
			}
		}
		public static async Task<ResponseT> PostJsonAsync<RequestT, ResponseT>
		(
			Uri url,
			RequestT request,
			NameValueCollection requestHeaders = null,
			Action<long, long> downloadProgressCallback = null,
			TimeSpan timeout = default,
			CancellationToken cancellation = default)
		{
			var requestStream = new MemoryStream(Encoding.UTF8.GetBytes(JsonObject.From(request).ToString()));
			var responseStream = new MemoryStream();
			const bool LEAVE_OPEN = true;
			try
			{
				if (requestHeaders == null || string.IsNullOrEmpty(requestHeaders["Content-Type"]))
				{
					requestHeaders = requestHeaders != null ? new NameValueCollection(requestHeaders) : new NameValueCollection();
					requestHeaders.Add("Content-Type", "application/json");
				}

				await RequestToAsync("POST", url, responseStream, requestStream, LEAVE_OPEN, requestHeaders, downloadProgressCallback, timeout, cancellation);

				responseStream.Position = 0;
				if (typeof(JsonValue) == typeof(ResponseT))
					return (ResponseT)(object)JsonValue.Load(responseStream);
				else
					return JsonValue.Load(responseStream).ToObject<ResponseT>();
			}
			catch (Exception requestError)
			{
				throw EnrichWebError(requestError, url, requestHeaders, timeout);
			}
		}
		public static async Task<Stream> GetStreamAsync
		(
			Uri url,
			NameValueCollection requestHeaders = null,
			Action<long, long> downloadProgressCallback = null,
			TimeSpan timeout = default,
			CancellationToken cancellation = default)
		{
			var memoryStream = new MemoryStream();
			try
			{
				await DownloadToAsync(memoryStream, url, requestHeaders, downloadProgressCallback, timeout, cancellation);

				memoryStream.Position = 0;
				return memoryStream;
			}
			catch (Exception requestError)
			{
				throw EnrichWebError(requestError, url, requestHeaders, timeout);
			}
		}

		private static async Task RequestToAsync
		(
			string method,
			Uri url,
			Stream downloadToStream,
			Stream uploadStream,
			bool leaveOpen,
			NameValueCollection requestHeaders,
			Action<long, long> progressCallback,
			TimeSpan timeout,
			CancellationToken cancellation)
		{
			if (url == null) throw new ArgumentNullException(nameof(url));
			if (downloadToStream == null) throw new ArgumentNullException(nameof(downloadToStream));

			var logger = CharonEditorModule.Instance.Logger;

			cancellation.ThrowIfCancellationRequested();

			using var requestMessage = new HttpRequestMessage(new HttpMethod(method), url);

			if (requestHeaders == null || string.IsNullOrEmpty(requestHeaders["Accept"]))
				requestMessage.Headers.TryAddWithoutValidation("Accept", "*/*");

			if (requestHeaders == null || string.IsNullOrEmpty(requestHeaders["User-Agent"]))
				requestMessage.Headers.TryAddWithoutValidation("User-Agent", UserAgentHeaderValue);

			var pendingContentHeaders = new NameValueCollection();
			if (requestHeaders != null)
			{
				foreach (string header in requestHeaders.Keys)
				{
					foreach (var headerValue in requestHeaders.GetValues(header ?? "") ?? Array.Empty<string>())
					{
						if (ContentHeaders.Contains(header))
							pendingContentHeaders.Add(header, headerValue);
						else
							requestMessage.Headers.TryAddWithoutValidation(header, headerValue);
					}
				}
			}

			if (uploadStream != Stream.Null && uploadStream.Length > 0)
			{
				var content = new StreamContent(uploadStream, BUFFER_SIZE);
				var hasContentType = false;

					foreach (string header in pendingContentHeaders.Keys)
					{
						foreach (var headerValue in pendingContentHeaders.GetValues(header ?? "") ?? Array.Empty<string>())
						{
							content.Headers.TryAddWithoutValidation(header, headerValue);
							hasContentType |= string.Equals(header, "Content-Type", StringComparison.OrdinalIgnoreCase);
						}
					}

				if (!hasContentType)
					content.Headers.TryAddWithoutValidation("Content-Type", "application/octet-stream");
				requestMessage.Content = content;
			}

			logger.Log(LogType.Assert, $"Staring new request to [{method}]'{url}'.");

			CancellationTokenSource cts = null;
			var token = cancellation;
			if (timeout.Ticks > 0)
			{
				cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
				cts.CancelAfter(timeout);
				token = cts.Token;
			}

			try
			{
				using var response = await SharedHttpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, token);

				cancellation.ThrowIfCancellationRequested();

				logger.Log(LogType.Assert, $"Got '{(int)response.StatusCode}' response for [{method}]'{url}' request.");

				if (response.StatusCode != HttpStatusCode.OK)
					throw new WebException($"An unexpected status code '{(int)response.StatusCode}' returned for request [{method}]'{url}'.");

				using var responseStream = await response.Content.ReadAsStreamAsync();
				var totalBytes = response.Content.Headers.ContentLength ?? -1L;
				var bytesRead = 0L;
				var buffer = new byte[BUFFER_SIZE];
				int read;
				while ((read = await responseStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
				{
					await downloadToStream.WriteAsync(buffer, 0, read, token);
					bytesRead += read;
					progressCallback?.Invoke(bytesRead, totalBytes);
				}
			}
			finally
			{
				cts?.Dispose();
				if (!leaveOpen)
				{
					downloadToStream.Dispose();
					uploadStream.Dispose();
				}
			}
		}

		private static Exception EnrichWebError(Exception error, Uri requestUrl, NameValueCollection requestHeaders, TimeSpan timeout)
		{
			if (error == null) throw new ArgumentNullException(nameof(error));
			if (requestUrl == null) throw new ArgumentNullException(nameof(requestUrl));

			error.Data["requestUrl"] = requestUrl;
			if (requestHeaders != null)
				error.Data["requestHeaders"] = requestHeaders;
			error.Data["timeout"] = timeout;

			return error;
		}
	}
}
