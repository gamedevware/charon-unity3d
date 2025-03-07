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
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GameDevWare.Charon.Editor.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace GameDevWare.Charon.Editor.Utils
{
	internal static class HttpUtils
	{
		public static readonly string UserAgentHeaderValue = string.Format("{0}/{1} (OS: {2}, Unity Version: {3}, Product: {4}, )",
			typeof(HttpUtils).Assembly.GetName(false).Name, typeof(HttpUtils).Assembly.GetName(false).Version,
			Application.platform, Application.unityVersion, Application.productName);

		private const int BUFFER_SIZE = 32 * 1024;

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

			var request = default(UnityWebRequest);
			var requestStream = default(Stream);
			try
			{
				var logger = CharonEditorModule.Instance.Logger;

				cancellation.ThrowIfCancellationRequested();

				request = new UnityWebRequest();
				request.uri = url;
				request.method = method;
				request.useHttpContinue = false;
				request.disposeCertificateHandlerOnDispose = true;
				request.disposeDownloadHandlerOnDispose = true;
				request.disposeUploadHandlerOnDispose = true;

				if (timeout.Ticks > 0)
				{
					request.timeout = (int)timeout.TotalSeconds;
				}

				if (requestHeaders != null)
				{
					foreach (string header in requestHeaders.Keys)
					{
						foreach (var headerValue in requestHeaders.GetValues(header ?? "") ?? Enumerable.Empty<string>())
						{
							request.SetRequestHeader(header, headerValue);
						}
					}
				}

				if (requestHeaders == null || string.IsNullOrEmpty(requestHeaders["Accept"]))
				{
					request.SetRequestHeader("Accept", "*/*");
				}

				if (requestHeaders == null || string.IsNullOrEmpty(requestHeaders["User-Agent"]))
				{
					request.SetRequestHeader("User-Agent", UserAgentHeaderValue);
				}

				logger.Log(LogType.Assert, $"Staring new request to [{request.method}]'{request.uri}'.");

				if (uploadStream != Stream.Null && uploadStream.Length > 0)
				{
					if (requestHeaders == null || string.IsNullOrEmpty(requestHeaders["Content-Type"]))
					{
						request.SetRequestHeader("Content-Type", "application/octet-stream");
					}

					request.disposeUploadHandlerOnDispose = true;
					if (uploadStream is FileStream fileStream && fileStream.Position == 0)
					{
						request.uploadHandler = new UploadHandlerFile(fileStream.Name);
					}
					else
					{
						request.uploadHandler = new UploadHandlerRaw(ReadAllBytes(uploadStream));
					}
				}

				var downloadBuffer = new DownloadHandlerBuffer();
				request.downloadHandler = downloadBuffer;

				await request.SendWebRequest().ToTask();

				switch (request.result)
				{
					case UnityWebRequest.Result.Success:
						break;
					case UnityWebRequest.Result.ConnectionError:
						throw new WebException($"Connection failed for for request [{request.method}]'{url}'.", WebExceptionStatus.ConnectFailure);
					case UnityWebRequest.Result.ProtocolError:
						throw new WebException($"Protocol error for for request [{request.method}]'{url}'.", WebExceptionStatus.ProtocolError);
					case UnityWebRequest.Result.DataProcessingError:
						throw new WebException($"Data transfer error for for request [{request.method}]'{url}'.", WebExceptionStatus.ReceiveFailure);
					case UnityWebRequest.Result.InProgress:
					default:
						throw new ArgumentOutOfRangeException();
				}

				cancellation.ThrowIfCancellationRequested();


				logger.Log(LogType.Assert, string.Format("Got '{2}' response for [{0}]'{1}' request.", request.method, request.uri, request.responseCode));

				if (request.responseCode != (int)HttpStatusCode.OK)
				{
					throw new WebException($"An unexpected status code '{request.responseCode}' returned for request [{request.method}]'{url}'.");
				}

				downloadToStream.Write(downloadBuffer.data);
			}
			finally
			{
				if (!leaveOpen)
				{
					downloadToStream.Dispose();
					uploadStream.Dispose();
				}

				if (request != null)
				{
					try
					{
						request.Abort();
					}
					catch
					{
						/* ignore close errors*/
					}
				}

				if (requestStream != null)
				{
					try
					{
						requestStream.Dispose();
					}
					catch
					{
						/* ignore close errors*/
					}
				}
			}
		}

		private static byte[] ReadAllBytes(Stream uploadStream)
		{
			var bytes = new byte[uploadStream.Length - uploadStream.Position];
			var offset = 0;
			var read = 0;
			while ((read = uploadStream.Read(bytes, offset, bytes.Length - offset)) > 0 && offset < bytes.Length)
			{
				offset += read;
			}

			if (offset != bytes.Length) throw new InvalidOperationException("Failed to read whole stream into byte array.");

			return bytes;
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
