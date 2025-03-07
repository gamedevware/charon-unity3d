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
using System.Globalization;
using GameDevWare.Charon.Editor.Cli;
using GameDevWare.Charon.Editor.Utils;
using UnityEditor;

namespace GameDevWare.Charon.Editor.Services
{
	public sealed class CharonSettings
	{
		private const string DEFAULT_SERVER_ADDRESS = "https://charon.live/";

		private CharonLogLevel? cachedLogLevel;
		private TimeSpan? cachedIdleCloseTimeout;
		private CharonEditorApplication? cachedEditorApplication;
		private string cachedServerAddress;
		private string cachedCustomEditorApplicationPath;

		public CharonEditorApplication EditorApplication
		{
			get => EditorPrefsUtils.GetPrefsValue(nameof(this.EditorApplication), CharonEditorApplication.DefaultBrowser, ref this.cachedEditorApplication);
			set => EditorPrefsUtils.SetPrefsValue(nameof(this.EditorApplication), value, ref this.cachedEditorApplication);
		}
		public string CustomEditorApplicationPath
		{
			get => EditorPrefsUtils.GetPrefsValueRef(nameof(this.CustomEditorApplicationPath), string.Empty, ref this.cachedCustomEditorApplicationPath);
			set => EditorPrefsUtils.SetPrefsValueRef(nameof(this.CustomEditorApplicationPath), value, ref this.cachedCustomEditorApplicationPath);
		}
		public string ServerAddress
		{
			get => EditorPrefsUtils.GetPrefsValueRef(nameof(this.ServerAddress), DEFAULT_SERVER_ADDRESS, ref this.cachedServerAddress);
			set => EditorPrefsUtils.SetPrefsValueRef(nameof(this.ServerAddress), value, ref this.cachedServerAddress);
		}
		public TimeSpan IdleCloseTimeout
		{
			get => EditorPrefsUtils.GetPrefsValue(nameof(this.IdleCloseTimeout), TimeSpan.FromSeconds(60), ref this.cachedIdleCloseTimeout);
			set => EditorPrefsUtils.SetPrefsValue(nameof(this.IdleCloseTimeout), value, ref this.cachedIdleCloseTimeout);
		}
		public CharonLogLevel LogLevel
		{
			get => EditorPrefsUtils.GetPrefsValue(nameof(this.LogLevel), CharonLogLevel.Normal, ref this.cachedLogLevel);
			set => EditorPrefsUtils.SetPrefsValue(nameof(this.LogLevel), value, ref this.cachedLogLevel);
		}

		internal Uri GetServerAddressUrl()
		{
			var serverAddress = default(Uri);
			if (string.IsNullOrWhiteSpace(this.ServerAddress))
			{
				serverAddress = new Uri(DEFAULT_SERVER_ADDRESS);
			}
			else
			{
				serverAddress = new Uri(this.ServerAddress);
			}

			if (serverAddress.IsAbsoluteUri == false) throw new InvalidOperationException("Server address should be absolute URL.");
			return serverAddress;
		}

		public void Initialize()
		{
			// pre-load prefs

			_ = this.EditorApplication;
			_ = this.CustomEditorApplicationPath;
			_ = this.ServerAddress;
			_ = this.IdleCloseTimeout;
			_ = this.LogLevel;
		}
	}
}
