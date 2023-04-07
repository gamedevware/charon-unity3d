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
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.Charon.Unity.Async
{
	internal static class CoroutineScheduler
	{
		private static Promise Current;

		private static readonly Queue<Action> WaitQueue = new Queue<Action>();
		private static readonly Dictionary<string, Promise> CoroutineById = new Dictionary<string, Promise>();

		public static bool IsRunning { get { return Current != null && Current.IsCompleted == false; } }

		public static string CurrentId;

		static CoroutineScheduler()
		{
			EditorApplication.update += Update;
		}

		private static void Update()
		{
			if (IsRunning || WaitQueue.Count <= 0)
				return;

			var start = WaitQueue.Dequeue();
			start();
		}

		public static Promise Schedule(IEnumerable coroutine)
		{
			return Schedule(coroutine, null);
		}
		public static Promise Schedule(IEnumerable coroutine, string coroutineId)
		{
			return Schedule<object>(coroutine, coroutineId);
		}
		public static Promise<T> Schedule<T>(IEnumerable coroutine)
		{
			return Schedule<T>(coroutine, Guid.NewGuid().ToString());
		}
		public static Promise<T> Schedule<T>(IEnumerable coroutine, string coroutineId)
		{
			if (coroutine == null) throw new ArgumentNullException("coroutine");



			var coroutineName = coroutine.GetType().Name;
			if (coroutineName.IndexOf('<') >= 0 && coroutineName.IndexOf('>') > coroutineName.IndexOf('<'))
				coroutineName = coroutineName.Substring(coroutineName.IndexOf('<') + 1, coroutineName.IndexOf('>') - coroutineName.IndexOf('<') - 1);

			if (string.IsNullOrEmpty(coroutineId))
				coroutineId = coroutineName + "_" + Guid.NewGuid().ToString().Replace("-", "");

			var existingPromise = default(Promise);
			if (CoroutineById.TryGetValue(coroutineId, out existingPromise) && !existingPromise.IsCompleted)
			{
				return (Promise<T>)existingPromise;
			}

			if (Settings.Current.Verbose)
				Debug.Log("Scheduling new coroutine " + coroutineName + " with id '" + coroutineId + "'.");

			var resultPromise = new Promise<T>();
			WaitQueue.Enqueue(() =>
			{
				var originalId = CurrentId;

				Current = new Coroutine<T>(coroutine).ContinueWith(completed =>
				{
					completed.PropagateTo(resultPromise);
					CurrentId = originalId;
					CoroutineById.Remove(coroutineId);
				});
			});

			CoroutineById[coroutineId] = resultPromise;
			return resultPromise;
		}

	}
}
