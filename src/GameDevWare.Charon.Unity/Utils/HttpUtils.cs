/*
	Copyright (c) 2023 Denis Zykov

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
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using GameDevWare.Charon.Unity.Async;
using GameDevWare.Charon.Unity.Json;

namespace GameDevWare.Charon.Unity.Utils
{
	internal static class HttpUtils
	{
		private const int BUFFER_SIZE = 32 * 1024;

		public static Promise DownloadToFile(Uri url, string downloadToFilePath, NameValueCollection requestHeaders = null, Action<long, long> downloadProgressCallback = null, TimeSpan timeout = default(TimeSpan), Promise cancellation = null)
		{
			if (url == null) throw new ArgumentNullException("url");
			if (downloadToFilePath == null) throw new ArgumentNullException("downloadToFilePath");

			var downloadDir = Path.GetDirectoryName(downloadToFilePath);
			if (string.IsNullOrEmpty(downloadDir) == false && Directory.Exists(downloadDir) == false)
				Directory.CreateDirectory(downloadDir);


			const bool LEAVE_OPEN = false;
			var downloadToStream = new FileStream(downloadToFilePath, FileMode.Create, FileAccess.Write, FileShare.None, BUFFER_SIZE, FileOptions.None);
			var downloadCoroutine = new Coroutine<long>(DownloadToAsync(url, downloadToStream, LEAVE_OPEN, requestHeaders, downloadProgressCallback, timeout, cancellation)).ContinueWith(new FuncContinuation<long, long>(p =>
			{
				if (p.HasErrors)
				{
					throw EnrichWebError(p.Error, url, requestHeaders, timeout);
				}

				return p.GetResult();
			}));
			return downloadCoroutine;
		}
		public static Promise DownloadTo(Stream downloadToStream, Uri url, NameValueCollection requestHeaders = null, Action<long, long> downloadProgressCallback = null, TimeSpan timeout = default(TimeSpan), Promise cancellation = null)
		{
			var downloadCoroutine = new Coroutine<long>(DownloadToAsync(url, downloadToStream, true, requestHeaders, downloadProgressCallback, timeout, cancellation)).ContinueWith(new FuncContinuation<long, long>(p =>
			{
				if (p.HasErrors)
				{
					throw EnrichWebError(p.Error, url, requestHeaders, timeout);
				}

				return p.GetResult();
			}));
			return downloadCoroutine;
		}
		public static Promise<T> GetJson<T>(Uri url, NameValueCollection requestHeaders = null, Action<long, long> downloadProgressCallback = null, TimeSpan timeout = default(TimeSpan), Promise cancellation = null)
		{
			var memoryStream = new MemoryStream();
			const bool LEAVE_OPEN = true;
			return new Coroutine<long>(DownloadToAsync(url, memoryStream, LEAVE_OPEN, requestHeaders, downloadProgressCallback, timeout, cancellation)).ContinueWith(new FuncContinuation<T>(p =>
			{
				if (p.HasErrors)
				{
					throw EnrichWebError(p.Error, url, requestHeaders, timeout);
				}

				memoryStream.Position = 0;
				if (typeof(JsonValue) == typeof(T))
					return (T)(object)JsonValue.Load(memoryStream);
				else
					return JsonValue.Load(memoryStream).As<T>();
			}));
		}

		public static Promise<MemoryStream> GetStream(Uri url, NameValueCollection requestHeaders = null, Action<long, long> downloadProgressCallback = null, TimeSpan timeout = default(TimeSpan), Promise cancellation = null)
		{
			var memoryStream = new MemoryStream();
			return DownloadTo(memoryStream, url, requestHeaders, downloadProgressCallback, cancellation: cancellation).ContinueWith(p =>
			{
				if (p.HasErrors)
				{
					throw EnrichWebError(p.Error, url, requestHeaders, timeout);
				}

				memoryStream.Position = 0;
				return memoryStream;
			});
		}

		private static IEnumerable DownloadToAsync(Uri url, Stream downloadToStream, bool leaveOpen, NameValueCollection requestHeaders, Action<long, long> downloadProgressCallback, TimeSpan timeout, Promise cancellation)
		{
			if (url == null) throw new ArgumentNullException("url");
			if (downloadToStream == null) throw new ArgumentNullException("downloadToStream");

			var noCertificateValidationContext = new NoCertificateValidationContext();
			var written = 0L;
			var request = default(HttpWebRequest);
			var responseStream = default(Stream);
			try
			{
				cancellation.ThrowIfCancellationRequested();

				request = (HttpWebRequest)WebRequest.Create(url);
				request.Accept = "*/*";
				request.UserAgent = typeof(HttpUtils).Assembly.GetName().FullName;
				request.AutomaticDecompression = DecompressionMethods.None;

				if (timeout.Ticks > 0)
					request.Timeout = (int)timeout.TotalMilliseconds;

				if (requestHeaders != null)
				{
					foreach (string header in requestHeaders.Keys)
					{
						foreach (var headerValue in requestHeaders.GetValues(header ?? "") ?? Enumerable.Empty<string>())
						{
							switch (header)
							{
								case "Accept":
									request.Accept = headerValue;
									break;
								case "Connection":
									request.Connection = headerValue;
									break;
								case "Content-Type":
									request.ContentType = headerValue;
									break;
								case "Content-Length":
									request.ContentLength = long.Parse(headerValue);
									break;
								case "Expect":
									request.Expect = headerValue;
									break;
								case "Referer":
									request.Referer = headerValue;
									break;
								case "Transfer-Encoding":
									request.TransferEncoding = headerValue;
									break;
								case "User-Agent":
									request.UserAgent = headerValue;
									break;
								default:
									request.Headers.Add(header, headerValue);
									break;
							}
						}
					}
				}

				if (string.IsNullOrEmpty(request.UserAgent))
				{
					request.UserAgent = RuntimeInformation.UserAgentHeaderValue;
				}

				if (Settings.Current.Verbose)
					UnityEngine.Debug.Log(string.Format("Staring new request to [{0}]'{1}'.", request.Method, request.RequestUri));

				var getResponseAsync = request.BeginGetResponse(ar =>
				{
					try
					{
						request.EndGetResponse(ar);
					}
					catch
					{
						/* ignore */
					}
				}, null);
				yield return getResponseAsync;

				var response = (HttpWebResponse)request.EndGetResponse(getResponseAsync);
				responseStream = response.GetResponseStream();

				cancellation.ThrowIfCancellationRequested();

				if (Settings.Current.Verbose)
					UnityEngine.Debug.Log(string.Format("Got '{2}' response for [{0}]'{1}' request.", request.Method, request.RequestUri, response.StatusCode));

				if (response.StatusCode != HttpStatusCode.OK)
					throw new WebException(string.Format("An unexpected status code '{0}' returned for request '{1}'.", response.StatusCode, url));

				var totalLength = response.ContentLength;
				if (!string.IsNullOrEmpty(response.Headers["Content-Disposition"]))
				{
					var contentDisposition = new ContentDisposition(response.Headers["Content-Disposition"]);
					if (contentDisposition.Size > 0)
						totalLength = contentDisposition.Size;
				}

				if (downloadProgressCallback != null) downloadProgressCallback(0, totalLength);
				var lastReported = 0L;

				var buffer = new byte[BUFFER_SIZE];
				var read = 0;
				do
				{
					if (UnityEditor.EditorApplication.isCompiling)
						throw new InvalidOperationException("Download has been canceled due pending compilation.");

					cancellation.ThrowIfCancellationRequested();
					
					var readAsync = responseStream.BeginRead(buffer, 0, buffer.Length, ar =>
					{
						try
						{
							responseStream.EndRead(ar);
						}
						catch
						{
							/* ignore */
						}
					}, null);
					yield return readAsync;

					if (UnityEditor.EditorApplication.isCompiling)
						throw new InvalidOperationException("Download has been canceled due pending compilation.");

					read = responseStream.EndRead(readAsync);
					if (read <= 0) continue;

					var writeAsync = downloadToStream.BeginWrite(buffer, 0, read, ar =>
					{
						try
						{
							downloadToStream.EndWrite(ar);
						}
						catch
						{
							/* ignore */
						}
					}, null);
					yield return writeAsync;

					written += read;

					if (downloadProgressCallback != null && (written - lastReported) > (totalLength / 200.0f))
						downloadProgressCallback(lastReported = written, totalLength);
				} while (read != 0);

				downloadToStream.Flush();

				if (downloadProgressCallback != null) downloadProgressCallback(totalLength, totalLength);
			}
			finally
			{
				noCertificateValidationContext.Dispose();

				if (!leaveOpen)
					downloadToStream.Dispose();

				if (responseStream != null)
					responseStream.Dispose();
				
				if (request != null)
					request.Abort();
			}

			yield return written;

		}

		private static Exception EnrichWebError(Exception error, Uri requestUrl, NameValueCollection requestHeaders, TimeSpan timeout)
		{
			if (error == null) throw new ArgumentNullException("error");
			if (requestUrl == null) throw new ArgumentNullException("requestUrl");

			error.Data["requestUrl"] = requestUrl;
			if (requestHeaders != null)
				error.Data["requestHeaders"] = requestHeaders;
			error.Data["timeout"] = timeout;

			return error;
		}
	}
}
