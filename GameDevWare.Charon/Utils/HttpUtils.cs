using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using GameDevWare.Charon.Json;
using GameDevWare.Charon.Tasks;

namespace GameDevWare.Charon.Utils
{
	internal static class HttpUtils
	{
		private const int BufferSize = 32 * 1024;

		public static Promise DownloadToFile(Uri url, string downloadToFilePath, NameValueCollection requestHeader = null, Action<long, long> downloadProgressCallback = null, TimeSpan timeout = default(TimeSpan))
		{
			if (url == null) throw new ArgumentNullException("url");
			if (downloadToFilePath == null) throw new ArgumentNullException("downloadToFilePath");

			var downloadDir = Path.GetDirectoryName(downloadToFilePath);
			if (string.IsNullOrEmpty(downloadDir) == false && Directory.Exists(downloadDir) == false)
				Directory.CreateDirectory(downloadDir);

			const bool leaveOpen = false;
			var downloadToStream = new FileStream(downloadToFilePath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, FileOptions.None);
			var downloadCoroutine = new Coroutine<long>(DownloadToAsync(url, downloadToStream, leaveOpen, requestHeader, downloadProgressCallback, timeout));
			return downloadCoroutine;
		}
		public static Promise<FileStream> Download(Uri url, NameValueCollection requestHeader = null, Action<long, long> downloadProgressCallback = null, TimeSpan timeout = default(TimeSpan))
		{
			var tmpFilePath = Path.GetTempFileName();
			return DownloadToFile(url, tmpFilePath, requestHeader, downloadProgressCallback).ContinueWith(p =>
			{
				if (p.HasErrors)
					throw p.Error.Unwrap();

				return File.OpenRead(tmpFilePath);
			});
		}
		public static Promise<T> GetJson<T>(Uri url, NameValueCollection requestHeader = null, Action<long, long> downloadProgressCallback = null, TimeSpan timeout = default(TimeSpan))
		{
			var memoryStream = new MemoryStream();
			const bool leaveOpen = true;
			return new Coroutine<long>(DownloadToAsync(url, memoryStream, leaveOpen, requestHeader, downloadProgressCallback, timeout)).ContinueWith(new FuncContinuation<T>(p =>
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

		private static IEnumerable DownloadToAsync(Uri url, Stream downloadToStream, bool leaveOpen, NameValueCollection requestHeader, Action<long, long> downloadProgressCallback, TimeSpan timeout)
		{
			if (url == null) throw new ArgumentNullException("url");
			if (downloadToStream == null) throw new ArgumentNullException("downloadToStream");


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
							case "Accept": request.Accept = headerValue; break;
							case "Connection": request.Connection = headerValue; break;
							case "Content-Type": request.ContentType = headerValue; break;
							case "Content-Length": request.ContentLength = long.Parse(headerValue); break;
							case "Expect": request.Expect = headerValue; break;
							case "Referer": request.Referer = headerValue; break;
							case "Transfer-Encoding": request.TransferEncoding = headerValue; break;
							case "User-Agent": request.UserAgent = headerValue; break;
							default: request.Headers.Add(header, headerValue); break;
						}
					}
				}
			}

			var getResponseAsync = request.BeginGetResponse(null, null);
			yield return getResponseAsync;
			var response = (HttpWebResponse)request.EndGetResponse(getResponseAsync);
			var responseStream = response.GetResponseStream();
			Debug.Assert(responseStream != null, "responseStream != null");

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
			var writen = 0L;
			try
			{
				var buffer = new byte[BufferSize];
				var read = 0;
				do
				{
					var readAsync = responseStream.BeginRead(buffer, 0, buffer.Length, ar => responseStream.EndRead(ar), null);
					yield return readAsync;
					read = responseStream.EndRead(readAsync);
					if (read <= 0) continue;

					var writeAsync = downloadToStream.BeginWrite(buffer, 0, read, downloadToStream.EndWrite, null);
					yield return writeAsync;
					writen += read;

					if (downloadProgressCallback != null) downloadProgressCallback(writen, totalLength);
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
