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

using GameDevWare.Charon.Unity.Async;
using System;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.Charon.Unity.Utils
{
	internal static class ProgressUtils
	{
		public static Action<string, float> ShowProgressBar(string title, float from = 0.0f, float to = 1.0f)
		{
			return (t, p) => EditorUtility.DisplayProgressBar(title, t, Mathf.Clamp(from + (to - from) * Mathf.Clamp(p, 0.0f, 1.0f), 0.0f, 1.0f));
		}
		public static Action<string, float> ShowCancellableProgressBar(string title, float from = 0.0f, float to = 1.0f, Promise cancellation = null)
		{
			return (t, p) =>
			{
				var isCancelled = EditorUtility.DisplayCancelableProgressBar(title, t, Mathf.Clamp(from + (to - from) * Mathf.Clamp(p, 0.0f, 1.0f), 0.0f, 1.0f));
				if (isCancelled && cancellation != null)
				{
					cancellation.TrySetCompleted();
				}
			};
		}
		public static void HideProgressBar(object state = null)
		{
			EditorUtility.ClearProgressBar();
		}
		public static Action<string, float> ReportToLog(string prefix)
		{
			return (t, p) => Debug.Log(prefix + t);
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
