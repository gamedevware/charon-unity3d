/*
	Copyright (c) 2017 Denis Zykov

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
using GameDevWare.Charon.Unity.Utils;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.Charon.Unity.Async
{
	internal class Coroutine : Coroutine<object>
	{
		public static readonly List<IUpdatable> UpdateList = new List<IUpdatable>();
		public static readonly HashSet<Exception> ReportedExceptions = new HashSet<Exception>();

		static Coroutine()
		{
			EditorApplication.update += () =>
			{
				ReportedExceptions.Clear();

				for (var index = 0; index < UpdateList.Count; index++)
				{
					var error = default(Exception);
					var task = UpdateList[index];
					try { task.Update(); }
					catch (Exception exception) { error = exception.Unwrap(); }

					if (task is IAsyncResult && ((IAsyncResult)task).IsCompleted)
					{
						UpdateList.RemoveAt(index);
						index--;
					}

					if (error != null && ReportedExceptions.Contains(error) == false)
					{
						ReportedExceptions.Add(error);
						if (Settings.Current.Verbose)
							Debug.LogError(error);
					}
				}
			};
		}
		public Coroutine(IEnumerable coroutine, AsyncCallback callback = null, object asyncCallbackState = null, object promiseState = null)
			: base(coroutine, callback, asyncCallbackState, promiseState)
		{
		}

		internal static IEnumerable WaitForUpdatablePromise(Promise promise)
		{
			do
			{
				var upd = promise as IUpdatable;
				if (upd != null)
					upd.Update();

				yield return null;
			} while (promise.IsCompleted == false);
		}
		internal static IEnumerable WaitForUpdatablePromise<T>(Promise<T> promise)
		{
			do
			{
				var upd = promise as IUpdatable;
				if (upd != null)
					upd.Update();

				yield return null;
			} while (promise.IsCompleted == false);

			if (promise.HasErrors)
				yield return default(T);
			else
				yield return promise.GetResult();
		}
		internal static IEnumerable WaitTime(TimeSpan timeToWait)
		{
			var startTime = DateTime.UtcNow;
			while (DateTime.UtcNow - startTime < timeToWait)
				yield return null;
		}

	}
	internal class Coroutine<T> : Promise<T>, IUpdatable
	{
		private readonly IEnumerator coroutine;
		private object current;
		private T lastResult;

		public Coroutine(IEnumerable coroutine, AsyncCallback callback = null, object asyncCallbackState = null, object promiseState = null)
			: base(callback, asyncCallbackState, promiseState)
		{
			if (coroutine == null) throw new ArgumentNullException("coroutine");

			this.coroutine = coroutine.GetEnumerator();

			Coroutine.UpdateList.Add(this);
		}

		public void Update()
		{
			if (this.IsCompleted)
				return;

			try
			{
				if (this.current is IUpdatable)
					(this.current as IUpdatable).Update();

				if (this.current is IAsyncResult && !((IAsyncResult)this.current).IsCompleted)
					return;

				var stateToDispose = this.current as IDisposable;
				try
				{
					var promise = this.current as Promise;
					if (promise != null && promise.HasErrors)
						throw promise.Error;

					if (this.current is T)
						this.lastResult = (T)this.current;

					if (!this.coroutine.MoveNext())
					{
						if (ReferenceEquals(stateToDispose, this.lastResult))
							stateToDispose = null;
						this.TrySetResult(this.lastResult);
					}
					else
						this.current = this.coroutine.Current;
				}
				finally
				{
					if (stateToDispose != null)
						stateToDispose.Dispose();
				}
			}
			catch (Exception exception)
			{
				var error = exception.Unwrap();
				if (Coroutine.ReportedExceptions.Contains(error) == false)
				{
					if (Settings.Current.Verbose)
						Debug.LogError(error);
					Coroutine.ReportedExceptions.Add(error);
				}

				this.TrySetFailed(error);
			}
		}

		protected override void Dispose(bool disposed)
		{
			if (this.coroutine is IDisposable)
				(this.coroutine as IDisposable).Dispose();

			base.Dispose(disposed);
		}

		public override string ToString()
		{
			if (!this.IsCompleted)
				return string.Format("{0}, running", this.coroutine);
			if (this.HasErrors)
				return string.Format("{0}, error: " + this.Error.Message, this.coroutine);
			return string.Format("{0}, complete", this.coroutine);
		}
	}
}
