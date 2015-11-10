/*
	Copyright (c) 2015 Denis Zykov

	This is part of Charon Game Data Editor Unity Plugin.

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
using System.Diagnostics;
using System.IO;
using System.Net;

namespace Assets.Unity.Charon.Editor.Tasks
{
	public class FileDownloadTask  : Task
	{
		private readonly Uri url;
		private readonly string filePath;

		public FileDownloadTask(Uri url, string filePath)
		{
			if (url == null) throw new ArgumentNullException("url");
			if (filePath == null) throw new ArgumentNullException("filePath");

			this.url = url;
			this.filePath = filePath;

			if (Directory.Exists(Path.GetDirectoryName(this.filePath)) == false)
				Directory.CreateDirectory(Path.GetDirectoryName(this.filePath));
		}

		protected override IEnumerable InitAsync()
		{
			yield return StartedEvent;

			foreach (var item in this.DownloadAsync())
				yield return item;
		}
		private IEnumerable DownloadAsync()
		{
			var request = (HttpWebRequest)WebRequest.Create(url);
			request.Accept = "*/*";
			request.UserAgent = typeof(FileDownloadTask).Assembly.GetName().Name;
			var getResponseAsync = request.BeginGetResponse(null, null);
			yield return getResponseAsync;
			var response = (HttpWebResponse)request.EndGetResponse(getResponseAsync);
			var responseStream = response.GetResponseStream();
			Debug.Assert(responseStream != null, "responseStream != null");

			if (response.StatusCode != HttpStatusCode.OK)
				throw new WebException(string.Format("An unexpected status code '{0}' returned for request '{1}'.", response.StatusCode, this.url));

			var bufferSize = 512 * 1024;
			using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, FileOptions.Asynchronous))
			{
				var buffer = new byte[bufferSize];
				var read = 0;
				do
				{
					var readAsync = responseStream.BeginRead(buffer, 0, buffer.Length, null, null);
					yield return readAsync;
					read = responseStream.EndRead(readAsync);
					if (read <= 0) continue;

					var writeAsync = fs.BeginWrite(buffer, 0, read, null, null);
					yield return writeAsync;
				} while (read != 0);
			}
		}
	}
}
