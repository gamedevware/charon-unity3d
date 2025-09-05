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
using System.Diagnostics;
using System.Runtime.Serialization;
using UnityEngine;

namespace GameDevWare.Charon.Editor.Services.ResourceServerApi
{
	[DataContract]
	public class ResourceServerInfo
	{
		[DataMember(Name = "title")]
		public string Title;
		[DataMember(Name = "version")]
		public string Version;
		[DataMember(Name = "apiVersion")]
		public string ApiVersion;
		[DataMember(Name = "platformName")]
		public string PlatformName;
		[DataMember(Name = "platformVersion")]
		public string PlatformVersion;

		public static ResourceServerInfo Gather()
		{
			var title = GetProductTitle();

			return new ResourceServerInfo {
				Title = title,
				Version = typeof(ResourceServerInfo).Assembly.GetName(false).Version.ToString(),
				ApiVersion = "1",
				PlatformVersion = Application.unityVersion,
				PlatformName = "Unity",
			};
		}

		private static string GetProductTitle()
		{
			var currentProcess = Process.GetCurrentProcess();
			var title = "Unity";
			try { title = $"{currentProcess.MainWindowTitle} [pid: {currentProcess.Id}]"; }
			catch { title = $"{Application.productName} [pid: {currentProcess.Id}]"; }

			return title;
		}
	}
}
