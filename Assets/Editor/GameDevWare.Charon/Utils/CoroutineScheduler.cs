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
using System.Collections.Generic;
using Assets.Editor.GameDevWare.Charon.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor.GameDevWare.Charon.Utils
{
	public static class CoroutineScheduler
	{
		private static IAsyncResult current;

		private static readonly Queue<string> ids = new Queue<string>();
		private static readonly Dictionary<string, Promise> coroutineById = new Dictionary<string, Promise>();

		public static bool IsRunning { get { return current != null && current.IsCompleted == false; } }

		static CoroutineScheduler()
		{
			EditorApplication.update += Update;
		}

		private static void Update()
		{
			if (IsRunning || ids.Count <= 0)
				return;
			var id = ids.Dequeue();

			coroutineById[id].SetCompleted(); // start coroutine
		}

		public static Coroutine<object> Schedule(IEnumerable coroutine)
		{
			return Schedule(coroutine, Guid.NewGuid().ToString());
		}
		public static Coroutine<object> Schedule(IEnumerable coroutine, string id)
		{
			return Schedule<object>(coroutine, id);
		}
		public static Coroutine<T> Schedule<T>(IEnumerable coroutine)
		{
			return Schedule<T>(coroutine, Guid.NewGuid().ToString());
		}
		public static Coroutine<T> Schedule<T>(IEnumerable coroutine, string id)
		{
			if (coroutine == null) throw new ArgumentNullException("coroutine");

			var coroutineName = coroutine.GetType().Name;
			if (coroutineName.IndexOf('<') >= 0 && coroutineName.IndexOf('>') > coroutineName.IndexOf('<'))
				coroutineName = coroutineName.Substring(coroutineName.IndexOf('<') + 1, coroutineName.IndexOf('>') - coroutineName.IndexOf('<') - 1);

			if (string.IsNullOrEmpty(id))
				id = coroutineName + "_" + Guid.NewGuid().ToString().Replace("-", "");

			var startPromise = default(Promise);
			if (coroutineById.TryGetValue(id, out startPromise))
				return (Coroutine<T>)((object[])startPromise.PromiseState)[0];

			if (Settings.Current.Verbose)
				Debug.Log("Sheduling new coroutine " + coroutineName + " with id '" + id + "'.");

			startPromise = new Promise(null, null, new object[1]);
			ids.Enqueue(id);
			coroutineById.Add(id, startPromise);
			var result = new Coroutine<T>(Start<T>(coroutine, id, startPromise));
			((object[])startPromise.PromiseState)[0] = result;

			return result;
		}

		private static IEnumerable Start<T>(IEnumerable coroutine, string id, Promise startPromise)
		{
			yield return startPromise;
			var innerCoroutine = new Coroutine<T>(coroutine);
			current = innerCoroutine;
			yield return innerCoroutine;
			coroutineById.Remove(id);
			yield return innerCoroutine.GetResult();
		}
	}
}
