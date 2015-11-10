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
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Assets.Unity.Charon.Editor.Tasks
{
	public class Coroutine : Coroutine<object>
	{
		public readonly static List<IUpdatable> UpdateList = new List<IUpdatable>();

		static Coroutine()
		{
			EditorApplication.update += () =>
			{
				for (var index = 0; index < UpdateList.Count; index++)
				{
					var error = default(Exception);
					var task = UpdateList[index];
					try { task.Update(); }
					catch (Exception exception) { error = exception; }

					if (task is IAsyncResult && ((IAsyncResult)task).IsCompleted)
					{
						if (task is Promise && ((Promise)task).HasErrors)
							error = ((Promise)task).Error;

						UpdateList.RemoveAt(index);
						index--;
					}

					if (error != null)
						Debug.LogError(task.GetType().Name + " was finished with error: " + error.Unwrap());
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
		internal static IEnumerable WaitTime(TimeSpan timeToWait)
		{
			var startTime = DateTime.UtcNow;
			while (DateTime.UtcNow - startTime < timeToWait)
				yield return null;
		}

	}
	public class Coroutine<T> : Promise<T>, IUpdatable
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

				using (this.current as IDisposable)
				{
					var promise = this.current as Promise;
					if (promise != null && promise.HasErrors)
						throw promise.Error;

					if (!this.coroutine.MoveNext())
						this.TrySetResult(this.lastResult);
					else
						this.current = this.coroutine.Current;

					if (this.current is T)
						this.lastResult = (T)this.current;
				}
			}
			catch (Exception e)
			{
				this.SetFailed(e);
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
				return string.Format("{0}, running", this.GetType().Name);
			if (this.HasErrors)
				return string.Format("{0}, error: " + this.Error.Message, this.GetType().Name);
			return string.Format("{0}, complete", this.GetType().Name);
		}
	}
}
