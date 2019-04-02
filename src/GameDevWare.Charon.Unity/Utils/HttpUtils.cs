/*
	Copyright (c) 2017 Denis Zykov

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

		public static Promise DownloadToFile(Uri url, string downloadToFilePath, NameValueCollection requestHeader = null, Action<long, long> downloadProgressCallback = null, TimeSpan timeout = default(TimeSpan))
		{
			if (url == null) throw new ArgumentNullException("url");
			if (downloadToFilePath == null) throw new ArgumentNullException("downloadToFilePath");

			var downloadDir = Path.GetDirectoryName(downloadToFilePath);
			if (string.IsNullOrEmpty(downloadDir) == false && Directory.Exists(downloadDir) == false)
				Directory.CreateDirectory(downloadDir);

			const bool LEAVE_OPEN = false;
			var downloadToStream = new FileStream(downloadToFilePath, FileMode.Create, FileAccess.Write, FileShare.None, BUFFER_SIZE, FileOptions.None);
			var downloadCoroutine = new Coroutine<long>(DownloadToAsync(url, downloadToStream, LEAVE_OPEN, requestHeader, downloadProgressCallback, timeout));
			return downloadCoroutine;
		}
		public static Promise DownloadTo(Stream downloadToStream, Uri url, NameValueCollection requestHeader = null, Action<long, long> downloadProgressCallback = null, TimeSpan timeout = default(TimeSpan))
		{
			var downloadCoroutine = new Coroutine<long>(DownloadToAsync(url, downloadToStream, true, requestHeader, downloadProgressCallback, timeout));
			return downloadCoroutine;
		}
		public static Promise<T> GetJson<T>(Uri url, NameValueCollection requestHeader = null, Action<long, long> downloadProgressCallback = null, TimeSpan timeout = default(TimeSpan))
		{
			var memoryStream = new MemoryStream();
			const bool LEAVE_OPEN = true;
			return new Coroutine<long>(DownloadToAsync(url, memoryStream, LEAVE_OPEN, requestHeader, downloadProgressCallback, timeout)).ContinueWith(new FuncContinuation<T>(p =>
			{
				if (p.HasErrors)
					throw p.Error.Unwrap();

				memoryStream.Position = 0;
				if (typeof(JsonValue) == typeof(T))
					return (T)(object)JsonValue.Load(memoryStream);
				else
					return JsonValue.Load(memoryStream).As<T>();
			}));
		}
		public static Promise<MemoryStream> GetStream(Uri url, NameValueCollection requestHeader = null, Action<long, long> downloadProgressCallback = null, TimeSpan timeout = default(TimeSpan))
		{
			var memoryStream = new MemoryStream();
			return DownloadTo(memoryStream, url, requestHeader, downloadProgressCallback).ContinueWith(p =>
			{
				if (p.HasErrors)
					throw p.Error.Unwrap();

				memoryStream.Position = 0;
				return memoryStream;
			});
		}

		private static IEnumerable DownloadToAsync(Uri url, Stream downloadToStream, bool leaveOpen, NameValueCollection requestHeader, Action<long, long> downloadProgressCallback, TimeSpan timeout)
		{
			if (url == null) throw new ArgumentNullException("url");
			if (downloadToStream == null) throw new ArgumentNullException("downloadToStream");

			using (new NoCertificateValidationContext())
			{
				var request = (HttpWebRequest)WebRequest.Create(url);
				request.Accept = "*/*";
				request.UserAgent = typeof(HttpUtils).Assembly.GetName().FullName;
				request.AutomaticDecompression = DecompressionMethods.None;
				if (timeout.Ticks > 0)
					request.Timeout = (int)timeout.TotalMilliseconds;

				if (requestHeader != null)
				{
					foreach (string header in requestHeader.Keys)
					{
						foreach (var headerValue in requestHeader.GetValues(header ?? "") ?? Enumerable.Empty<string>())
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
				var responseStream = response.GetResponseStream();
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
				var writen = 0L;
				try
				{
					var buffer = new byte[BUFFER_SIZE];
					var read = 0;
					do
					{
						if (UnityEditor.EditorApplication.isCompiling)
							throw new InvalidOperationException("Download has been canceled due pending compilation.");

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

						writen += read;

						if (downloadProgressCallback != null && (writen - lastReported) > (totalLength / 200.0f))
							downloadProgressCallback(lastReported = writen, totalLength);
					} while (read != 0);

					downloadToStream.Flush();
				}
				finally
				{
					if (!leaveOpen)
						downloadToStream.Dispose();
				}

				if (downloadProgressCallback != null) downloadProgressCallback(totalLength, totalLength);

				yield return writen;
			}
		}
	}
}
