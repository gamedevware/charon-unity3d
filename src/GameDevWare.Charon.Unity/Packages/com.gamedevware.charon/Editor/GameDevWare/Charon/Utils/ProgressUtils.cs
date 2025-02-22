/*
	Copyright (c) 2025 Denis Zykov

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
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Editor.GameDevWare.Charon.Utils
{
	internal static class ProgressUtils
	{
		public static Action<string, float> ShowProgressBar(string title, float from = 0.0f, float to = 1.0f)
		{
			return (t, p) => EditorUtility.DisplayProgressBar(title, t, Mathf.Clamp(from + (to - from) * Mathf.Clamp(p, 0.0f, 1.0f), 0.0f, 1.0f));
		}
		public static Action<string, float> ShowCancellableProgressBar(string title, float from = 0.0f, float to = 1.0f, CancellationTokenSource cancellationSource = null)
		{
			return (t, p) =>
			{
				var isCancelled = EditorUtility.DisplayCancelableProgressBar(title, t, Mathf.Clamp(from + (to - from) * Mathf.Clamp(p, 0.0f, 1.0f), 0.0f, 1.0f));
				if (isCancelled && cancellationSource != null)
				{
					cancellationSource.Cancel();
				}
			};
		}
		public static void HideProgressBar(object state = null)
		{
			EditorUtility.ClearProgressBar();
		}
		public static void ContinueWithHideProgressBar(this Task task, CancellationToken cancellationToken = default)
		{
			task.ContinueWith(HideProgressBar, cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);
		}
		public static Action<string, float> ReportToLog(string prefix)
		{
			return (t, _) => CharonEditorModule.Instance.Logger.Log(LogType.Log, prefix + t);
		}
		public static Action<long, long> ToDownloadProgress(this Action<string, float> progress, string fileName)
		{
			if (progress == null)
			{
				return null;
			}

			return (read, total) =>
			{
				if (total == 0)
					return;

				progress(
					string.Format(
							Resources.UI_UNITYPLUGIN_PROGRESS_DOWNLOADING,
							(float)read / 1024 / 1024,
							(float)total / 1024 / 1024,
							fileName ?? ""),
					Mathf.Clamp01((float)read / total));
			};
		}
		public static Action<long, long> ToUploadProgress(this Action<string, float> progress, string fileName)
		{
			if (progress == null)
			{
				return null;
			}

			return (read, total) =>
			{
				if (total == 0)
					return;

				progress(
					string.Format(
						Resources.UI_UNITYPLUGIN_PROGRESS_UPLOADING,
						(float)read / 1024 / 1024,
						(float)total / 1024 / 1024,
						fileName ?? ""),
					Mathf.Clamp01((float)read / total));
			};
		}
		public static Action<string, float> Sub(this Action<string, float> progress, float from = 0.0f, float to = 1.0f)
		{
			if (progress == null)
			{
				return null;
			}

			to = Mathf.Clamp01(to);
			from = Mathf.Clamp(from, 0, to);
			return (t, p) => progress(t, from + (to - from) * Mathf.Clamp01(p));
		}

	}
}
