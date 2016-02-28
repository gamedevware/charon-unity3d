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
using System.IO;
using System.Linq;
using System.Net;
using Assets.Unity.Charon.Editor.Json;
using UnityEngine;

namespace Assets.Unity.Charon.Editor.Tasks
{
	public class GetRequest<ResultT> : Coroutine<ResultT>
	{
		public Uri Url { get; private set; }
		public bool TrustSSLCertificates { get; set; }

		public GetRequest(Uri url, NameValueCollection requestHeader = null)
			: base(DoRequestAsync(url, requestHeader))
		{
			if (url == null) throw new ArgumentNullException("url");

			this.Url = url;
		}

		private static IEnumerable DoRequestAsync(Uri url, NameValueCollection requestHeader)
		{
			if (url == null) throw new ArgumentNullException("url");

			if (Settings.Current.Verbose)
				Debug.Log(string.Format("Starting new get request to '{0}'.", url));

			var request = (HttpWebRequest)WebRequest.Create(url);
			request.Accept = "*/*";
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

			request.UserAgent = typeof(GetRequest<ResultT>).Assembly.GetName().Name;
			var getResponseAsync = request.BeginGetResponse(null, null);
			yield return getResponseAsync;
			using (var response = (HttpWebResponse)request.EndGetResponse(getResponseAsync))
			{
				using (var responseStream = response.GetResponseStream())
				{
					if (Settings.Current.Verbose)
						Debug.Log(string.Format("Got [{1}] response for '{0}' request.", url, response.StatusCode));

					if (response.StatusCode != HttpStatusCode.OK)
						throw new WebException(string.Format("An unexpected status code '{0}' returned for request '{1}'.", response.StatusCode, url));

					var bufferSize = 4 * 1024;
					var memoryStream = new MemoryStream((int)Math.Max(bufferSize, response.ContentLength));
					var buffer = new byte[bufferSize];
					var read = 0;
					do
					{
						var readAsync = responseStream.BeginRead(buffer, 0, buffer.Length, null, null);
						yield return readAsync;
						read = responseStream.EndRead(readAsync);
						if (read <= 0) continue;

						var writeAsync = memoryStream.BeginWrite(buffer, 0, read, null, null);
						yield return writeAsync;
					} while (read != 0);

					memoryStream.Position = 0;
					if (typeof(JsonValue) == typeof(ResultT))
						yield return (ResultT)(object)JsonValue.Load(memoryStream);
					else
						yield return JsonValue.Load(memoryStream).As<ResultT>();
				}
			}
		}
	}
}
