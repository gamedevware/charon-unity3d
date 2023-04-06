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
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using GameDevWare.Charon.Unity.Async;
using GameDevWare.Charon.Unity.Utils;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.Charon.Unity.Windows
{
	internal abstract class WebViewEditorWindow : EditorWindow
	{
		private bool syncingFocus;
		private int repeatedShow;
		private bool webViewHidden;

		[SerializeField]
		private ScriptableObject webView;

		protected Rect Padding { get; set; }

		protected WebViewEditorWindow()
		{
			this.titleContent = new GUIContent("View");
			this.minSize = new Vector2(100, 100);
		}

		protected bool WebViewExists { get { return ReferenceEquals(this.webView, null) == false && this.webView; } }

		protected virtual void OnDestroy()
		{
			if (ReferenceEquals(this.webView, null) == false)
				DestroyImmediate(this.webView);
			this.webView = null;
		}
		[SuppressMessage("ReSharper", "InconsistentNaming"), UsedImplicitly]
		protected virtual void OnGUI()
		{
			if (!this.WebViewExists)
				return;

			if (this.webViewHidden)
				return;

			if (this.repeatedShow-- > 0)
			{
				this.webView.Invoke("Hide");
				this.webView.Invoke("Show");
			}

			if (Event.current.type == EventType.Layout)
			{
				var engineAsm = typeof(UnityEngine.GUI).Assembly;
				var webViewRect = (Rect)engineAsm.GetType("UnityEngine.GUIClip", throwOnError: true).InvokeMember("Unclip", BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, null, null, new object[] { new Rect(0, 0, this.position.width, this.position.height) });
				this.webView.Invoke("SetSizeAndPosition", (int)(webViewRect.x + this.Padding.x), (int)(webViewRect.y + this.Padding.y), (int)(webViewRect.width - (this.Padding.width + this.Padding.x)), (int)(webViewRect.height - (this.Padding.height + this.Padding.y)));
			}
		}

		[SuppressMessage("ReSharper", "InconsistentNaming"), UsedImplicitly]
		protected void OnFocus()
		{
			this.SetFocus(true);
		}
		[SuppressMessage("ReSharper", "InconsistentNaming"), UsedImplicitly]
		protected void OnLostFocus()
		{
			this.SetFocus(false);
		}
		[UsedImplicitly]
		protected void OnBecameInvisible()
		{
			if (!this.WebViewExists) return;
			this.webView.Invoke("Hide");
			this.webView.Invoke("SetHostView", new object[] { null });
		}
		protected void SetFocus(bool focused)
		{
			if (this.syncingFocus)
				return;
			if (!this.WebViewExists)
				return;
			this.syncingFocus = true;

			if (focused)
			{
				var parent = this.GetFieldValue("m_Parent");
				if (ReferenceEquals(parent, null) == false)
					this.webView.Invoke("SetHostView", parent);

				if (!this.webViewHidden)
				{
					if (Application.platform != RuntimePlatform.WindowsEditor)
						this.repeatedShow = 5;
					else
						this.webView.Invoke("Show");
				}

			}

			this.webView.Invoke("SetFocus", focused);
			this.syncingFocus = false;
		}
		protected void SetWebViewVisibility(bool visible)
		{
			this.webViewHidden = !visible;

			if (!this.WebViewExists)
				return;

			if (this.webViewHidden)
				this.webView.Invoke("Hide");
			else
				this.SetFocus(true);
		}

		protected void LoadUrl(string url)
		{
			if (!this.webView)
			{
				var editorAsm = typeof(UnityEditor.SceneView).Assembly;
				var engineAsm = typeof(UnityEngine.GUI).Assembly;
				var webViewRect = (Rect)engineAsm.GetType("UnityEngine.GUIClip", throwOnError: true).InvokeMember("Unclip", BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, null, null, new object[] { new Rect(0, 0, this.position.width, this.position.height) });
				this.webView = CreateInstance(editorAsm.GetType("UnityEditor.WebView", throwOnError: true));
				var hostView = this.GetFieldValue("m_Parent");
				this.webView.Invoke("InitWebView", hostView, (int)(webViewRect.x + this.Padding.x), (int)(webViewRect.y + this.Padding.y), (int)(webViewRect.width - (this.Padding.width + this.Padding.x)), (int)(webViewRect.height - (this.Padding.height + this.Padding.y)), false);
				this.webView.SetPropertyValue("hideFlags", HideFlags.HideAndDontSave);

			}
			this.webView.Invoke("SetDelegateObject", this);
			this.webView.Invoke("LoadURL", url);

			if (Settings.Current.Verbose)
				Debug.Log("WebView is loading '" + url + "'.");

			Promise.Delayed(TimeSpan.FromSeconds(2)).ContinueWith(_ => this.Repaint());
		}
		protected void Reload()
		{
			if (!this.WebViewExists)
				return;

			this.webView.Invoke("Reload");
		}
		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		protected void ShowDevTools()
		{
			if (!this.WebViewExists)
				return;

			this.webView.Invoke("ShowDevTools");
		}
	}
}
