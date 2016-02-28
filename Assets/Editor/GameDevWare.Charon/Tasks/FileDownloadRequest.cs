/*
	Copyright (c) 2016 Denis Zykov

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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;

namespace Assets.Editor.GameDevWare.Charon.Tasks
{
	public class FileDownloadRequest : Coroutine
	{
		public string FilePath { get; private set; }
		public Uri Url { get; private set; }

		public FileDownloadRequest(Uri url, string filePath, NameValueCollection requestHeader = null, Action<long, long> downloadProgressCallback = null)
			: base(DownloadAsync(url, filePath, requestHeader, downloadProgressCallback))
		{
			if (url == null) throw new ArgumentNullException("url");
			if (filePath == null) throw new ArgumentNullException("filePath");

			this.Url = url;
			this.FilePath = filePath;
		}

		private static IEnumerable DownloadAsync(Uri url, string filePath, NameValueCollection requestHeader, Action<long, long> downloadProgressCallback)
		{
			if (url == null) throw new ArgumentNullException("url");
			if (filePath == null) throw new ArgumentNullException("filePath");

			if (Directory.Exists(Path.GetDirectoryName(filePath)) == false)
				Directory.CreateDirectory(Path.GetDirectoryName(filePath));

			var request = (HttpWebRequest)WebRequest.Create(url);
			request.Accept = "*/*";
			request.UserAgent = typeof(FileDownloadRequest).Assembly.GetName().Name;
			request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
			request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip");
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
			var bufferSize = 32 * 1024;
			var writen = 0;
			using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, FileOptions.None))
			{
				var buffer = new byte[bufferSize];
				var read = 0;
				do
				{
					var readAsync = responseStream.BeginRead(buffer, 0, buffer.Length, ar => responseStream.EndRead(ar), null);
					yield return readAsync;
					read = responseStream.EndRead(readAsync);
					if (read <= 0) continue;

					var writeAsync = fs.BeginWrite(buffer, 0, read, fs.EndWrite, null);
					yield return writeAsync;
					writen += read;

					if (downloadProgressCallback != null) downloadProgressCallback(writen, totalLength);
				} while (read != 0);

				fs.Flush();
			}
			if (downloadProgressCallback != null) downloadProgressCallback(totalLength, totalLength);
		}
	}
}
