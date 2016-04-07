using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts;
using UnityEngine;
using UnityEditor;

namespace Assets.Editor.GameDevWare.Charon.Tasks
{
	public class Coroutine : Coroutine<object>
	{
		private static readonly List<IUpdatable> UpdateList = new List<IUpdatable>();

		static Coroutine()
		{
			EditorApplication.update += UpdateCoroutines;
		}
		public Coroutine(IEnumerable coroutine, AsyncCallback callback = null, object asyncCallbackState = null, object promiseState = null)
			: base(coroutine, callback, asyncCallbackState, promiseState)
		{
		}

		private static void UpdateCoroutines()
		{
			for (var index = 0; index < UpdateList.Count; index++)
			{
				var error = default(Exception);
				var task = UpdateList[index];

				try { task.Update(); }
				catch (Exception exception) { error = exception; }

				if (task is IAsyncResult && ((IAsyncResult)task).IsCompleted)
				{
					UpdateList.RemoveAt(index);
					index--;
				}

				var promise = task as Promise;
				if (promise != null && promise.HasErrors && !promise.IsErrorObserved)
					error = promise.Error;

				if (error != null)
					Debug.LogWarning(task.GetType().Name + " was finished with error: " + error.Unwrap());
			}
		}
		public static void AddToUpdateList(IUpdatable updatable)
		{
			if (updatable == null) throw new ArgumentNullException("updatable");
			UpdateList.Add(updatable);
		}
		public static void RemoveFromUpdateList(IUpdatable updatable)
		{
			if (updatable == null) throw new ArgumentNullException("updatable");

			UpdateList.Remove(updatable);
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

			Coroutine.AddToUpdateList(this);
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
			catch (Exception error)
			{
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
				return string.Format("{0}, running", coroutine);
			if (this.HasErrors)
				return string.Format("{0}, error: " + this.Error.Message, coroutine);
			return string.Format("{0}, complete", coroutine);
		}
	}
}
