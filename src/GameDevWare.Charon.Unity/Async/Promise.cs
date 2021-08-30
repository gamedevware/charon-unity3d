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
using System.Collections.Generic;
using System.Threading;

namespace GameDevWare.Charon.Unity.Async
{
	public partial class Promise : IAsyncResult, IDisposable
	{
		public static readonly Promise Fulfilled = new Promise { IsCompleted = true, cantBeDisposed = true };

		private readonly AsyncCallback asyncCallback;
		private readonly object asyncCallbackState;
		private readonly object promiseState;
		private bool cantBeDisposed;
		private ManualResetEvent completionEvent;
		private Continuation continuations;
		private AggregateException error;
		private readonly SynchronizationContext capturedSynchronizationContext;

		public bool IsDisposed { get; private set; }
		public bool HasErrors { get { return this.error != null; } }
		public Exception Error
		{
			get
			{
				this.IsErrorObserved = true; return this.error;
			}
		}
		public object PromiseState { get { return this.promiseState; } }
		public bool IsCompleted { get; private set; }
		WaitHandle IAsyncResult.AsyncWaitHandle { get { this.EnsureCompletionEvent(); return this.completionEvent; } }
		object IAsyncResult.AsyncState { get { return this.asyncCallbackState; } }
		bool IAsyncResult.CompletedSynchronously { get { return false; } }
		private bool ContinueOnCapturedContext { get { return SynchronizationContext.Current != this.capturedSynchronizationContext && this.capturedSynchronizationContext != null && this.capturedSynchronizationContext.GetType() != typeof(SynchronizationContext); } }
		internal bool IsErrorObserved { get; private set; }

		public Promise(AsyncCallback callback = null, object asyncCallbackState = null, object promiseState = null)
		{
			this.capturedSynchronizationContext = SynchronizationContext.Current;
			this.asyncCallback = callback;
			this.asyncCallbackState = asyncCallbackState;
			this.promiseState = promiseState;
		}

		void IDisposable.Dispose()
		{
			this.Dispose(true);
		}

		public void SetCompleted()
		{
			if (this.TrySetCompleted() == false)
			{
				this.ThrowIfDisposed();
				this.ThrowIfCompleted();
			}
		}
		public bool TrySetCompleted()
		{
			var complEvent = default(ManualResetEvent);
			lock (this)
			{
				if (this.IsCompleted || this.IsDisposed)
					return false;

				this.IsCompleted = true;
				complEvent = this.completionEvent;
				if (complEvent != null)
					complEvent.Set();
			}

			this.ExecuteContinuations();

			return true;
		}
		public void SetFailed(params Exception[] fault)
		{
			if (fault == null) throw new ArgumentNullException("fault");

			this.SetFailed(new AggregateException(fault));
		}
		public void SetFailed(IEnumerable<Exception> fault)
		{
			if (fault == null) throw new ArgumentNullException("fault");

			this.SetFailed(new AggregateException(fault));
		}
		public void SetFailed(Exception fault)
		{
			if (fault == null) throw new ArgumentNullException("fault");

			if (this.TrySetFailed(fault) == false)
			{
				this.ThrowIfDisposed();
				this.ThrowIfCompleted();
			}
		}
		public bool TrySetFailed(Exception fault)
		{
			if (fault == null) throw new ArgumentNullException("fault");

			if (fault is AggregateException == false)
				fault = new AggregateException(fault);

			lock (this)
			{
				if (this.IsCompleted || this.IsDisposed)
					return false;

				this.error = (AggregateException)fault;

				return this.TrySetCompleted();
			}
		}
		public void GetResult()
		{
			lock (this)
			{
				if (this.IsCompleted == false)
					throw new InvalidOperationException("Promise is not yet fulfilled.");

				if (this.HasErrors)
				{
					if (this.error.InnerExceptions.Count == 1 && this.Error.InnerException != null)
						throw this.Error.InnerException;
					throw this.Error;
				}
			}
		}

		public static Promise WhenAny(Promise first, Promise second)
		{
			if (first == null) throw new ArgumentNullException("first");
			if (second == null) throw new ArgumentNullException("second");

			var result = new Promise();

			first.PropagateTo(result);
			second.PropagateTo(result);

			return result;
		}
		public static Promise WhenAny(params Promise[] promises)
		{
			if (promises == null) throw new ArgumentNullException("promises");
			if (promises.Length == 0) throw new ArgumentOutOfRangeException("promises");
			if (promises.Length == 1) return promises[0];

			var result = new Promise();

			foreach (var promise in promises)
				promise.PropagateTo(result);

			return result;
		}
		public static Promise WhenAll(Promise first, Promise second)
		{
			if (first == null) throw new ArgumentNullException("first");
			if (second == null) throw new ArgumentNullException("second");

			var result = new Promise();
			var resultsCount = 0;

			var continuation = (ActionContinuation)(_ =>
			{
				if (Interlocked.Increment(ref resultsCount) == 2)
				{
					if (first.HasErrors && second.HasErrors)
						result.SetFailed(new AggregateException(first.Error, second.Error).Flatten());
					else if (first.HasErrors)
						result.SetFailed(first.Error);
					else if (second.HasErrors)
						result.SetFailed(second.Error);
					else
						result.TrySetCompleted();
				}
			});

			first.ContinueWith(continuation);
			second.ContinueWith(continuation);

			return result;
		}
		public static Promise WhenAll(params Promise[] promises)
		{
			if (promises == null) throw new ArgumentNullException("promises");
			if (promises.Length == 0) throw new ArgumentOutOfRangeException("promises");

			var result = new Promise();
			var resultsCount = 0;

			var continuation = (ActionContinuation)(_ =>
			{
				if (Interlocked.Increment(ref resultsCount) == promises.Length)
				{
					var errorList = default(List<Exception>);
					foreach (var promise in promises)
					{
						if (promise.HasErrors == false)
							continue;
						if (errorList == null)
							errorList = new List<Exception>();

						if (errorList.Contains(promise.Error) == false)
							errorList.Add(promise.Error);
					}

					if (errorList != null)
						result.SetFailed(new AggregateException(errorList).Flatten());
					else
						result.TrySetCompleted();
				}
			});

			foreach (var promise in promises)
				promise.ContinueWith(continuation);

			return result;
		}
		public static Promise Delayed(TimeSpan timeSpan)
		{
			return new Coroutine(Coroutine.WaitTime(timeSpan), null, null);
		}

		public static Promise<T> FromResult<T>(T result)
		{
			var promise = new Promise<T>();
			promise.SetResult(result);
			return promise;
		}
		public static Promise<T> WhenAny<T>(Promise<T> first, Promise<T> second)
		{
			if (first == null) throw new ArgumentNullException("first");
			if (second == null) throw new ArgumentNullException("second");

			var result = new Promise<T>();

			first.PropagateTo(result);
			second.PropagateTo(result);

			return result;
		}
		public static Promise<T> WhenAny<T>(params Promise<T>[] promises)
		{
			if (promises == null) throw new ArgumentNullException("promises");
			if (promises.Length == 0) throw new ArgumentOutOfRangeException("promises");
			if (promises.Length == 1) return promises[0];

			var result = new Promise<T>();
			foreach (var promise in promises)
				promise.PropagateTo(result);

			return result;
		}
		public static Promise<T[]> WhenAll<T>(Promise<T> first, Promise<T> second)
		{
			if (first == null) throw new ArgumentNullException("first");
			if (second == null) throw new ArgumentNullException("second");

			var result = new Promise<T[]>();
			var resultsCount = 0;

			var continuation = (ActionContinuation)(_ =>
			{
				if (Interlocked.Increment(ref resultsCount) == 2)
				{
					if (first.HasErrors && second.HasErrors)
						result.SetFailed(new AggregateException(first.Error, second.Error).Flatten());
					else if (first.HasErrors)
						result.SetFailed(first.Error);
					else if (second.HasErrors)
						result.SetFailed(second.Error);
					else
						result.SetResult(new[] { first.GetResult(), second.GetResult() });
				}
			});

			first.ContinueWith(continuation);
			second.ContinueWith(continuation);

			return result;
		}
		public static Promise<T[]> WhenAll<T>(params Promise<T>[] promises)
		{
			if (promises == null) throw new ArgumentNullException("promises");
			if (promises.Length == 0) throw new ArgumentOutOfRangeException("promises");

			var result = new Promise<T[]>();
			var resultsCount = 0;

			var continuation = (ActionContinuation)(_ =>
			{
				if (Interlocked.Increment(ref resultsCount) == promises.Length)
				{
					var errorList = default(List<Exception>);
					foreach (var promise in promises)
					{
						if (promise.HasErrors == false)
							continue;
						if (errorList == null)
							errorList = new List<Exception>();

						if (errorList.Contains(promise.Error) == false)
							errorList.Add(promise.Error);
					}

					if (errorList != null)
						result.SetFailed(new AggregateException(errorList).Flatten());
					else
						result.SetResult(Array.ConvertAll(promises, pr => pr.GetResult()));
				}
			});

			foreach (var promise in promises)
				promise.ContinueWith(continuation);

			return result;
		}

		public Promise IgnoreFault()
		{
			if (this.IsCompleted)
				return Fulfilled;

			if (this is IUpdatable)
			{
				var result = new Coroutine(Coroutine.WaitForUpdatablePromise(this));
				return result;
			}

			return this.ContinueWith(_ => { });
		}

		public void PropagateTo(Promise promise)
		{
			if (promise == null) throw new ArgumentNullException("promise");

			this.ContinueWith(p =>
			{
				if (p.HasErrors)
					promise.TrySetFailed(p.Error);
				else
					promise.TrySetCompleted();
			});
		}

		private void EnsureCompletionEvent()
		{
			lock (this)
			{
				this.ThrowIfDisposed();

				if (this.completionEvent == null)
				{
					this.completionEvent = new ManualResetEvent(this.IsCompleted);
				}
			}
		}

		private void ExecuteContinuations()
		{
			if (this.ContinueOnCapturedContext)
			{
				this.capturedSynchronizationContext.Post(_ => this.ExecuteContinuations(), null);
				return;
			}

			var errorList = default(HashSet<Exception>);

			var conts = Interlocked.Exchange(ref this.continuations, null);
			if (conts != null)
			{
				var contErrors = default(AggregateException);

				conts.Invoke(this, out contErrors);

				if (contErrors != null)
					errorList = new HashSet<Exception> { contErrors };
			}

			if (this.asyncCallback != null)
			{
				// ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
				foreach (AsyncCallback target in this.asyncCallback.GetInvocationList())
				{
					try
					{
						target(this);
					}
					catch (Exception e)
					{
						if (errorList == null) errorList = new HashSet<Exception>();
						errorList.Add(e);
					}
				}
			}

			if (errorList != null)
				throw new AggregateException(errorList).Flatten();
		}

		protected void ThrowIfCompleted()
		{
			if (this.IsCompleted) throw new InvalidOperationException("Promise is already fulfilled.");
		}
		protected void ThrowIfDisposed()
		{
			if (this.IsDisposed)
				throw new ObjectDisposedException(this.GetType().Name);
		}

		public Promise ContinueWith(ActionContinuation continuation)
		{
			return this.ContinueWithInternal<object, object>(continuation);
		}
		public Promise<RT> ContinueWith<RT>(FuncContinuation<RT> continuation)
		{
			return this.ContinueWithInternal<object, RT>(continuation);
		}

		protected Promise<RT> ContinueWithInternal<T, RT>(Delegate continuation)
		{
			if (continuation == null) throw new ArgumentNullException("continuation");

			var continuationPromise = new Promise<RT>();
			var newContinuation = default(Continuation);
			var curContinuations = default(Continuation);
			do
			{
				curContinuations = this.continuations;
				newContinuation = new Continuation<T, RT>(continuation, continuationPromise, curContinuations);
			} while (Interlocked.CompareExchange(ref this.continuations, newContinuation, curContinuations) != curContinuations);

			var errors = default(AggregateException);
			if (this.IsDisposed || this.IsCompleted)
				newContinuation.Invoke(this, out errors);

			return continuationPromise;
		}

		protected virtual void Dispose(bool disposed)
		{
			lock (this)
			{
				if (this.IsDisposed || this.cantBeDisposed) return;

				this.IsCompleted = true;
				this.IsDisposed = true;

				this.ExecuteContinuations();
			}
		}

		public override string ToString()
		{
			if (this.IsCompleted)
				return "Fulfilled promise.";
			return "Unfulfilled promise";
		}
	}

	public class Promise<T> : Promise
	{
		public static Promise<T> DefaultFulfilled = Promise.FromResult(default(T));

		private T value;

		public Promise()
		{
		}
		public Promise(AsyncCallback callback, object asyncCallbackState, object promiseState = null)
			: base(callback, asyncCallbackState, promiseState)
		{
		}

		public void SetResult(T result)
		{
			if (this.TrySetResult(result) == false)
			{
				this.ThrowIfDisposed();
				this.ThrowIfCompleted();
			}
		}
		public bool TrySetResult(T result)
		{
			lock (this)
			{
				if (this.IsCompleted || this.IsDisposed)
					return false;

				this.value = result;
				this.TrySetCompleted();
			}
			return true;
		}
		public T GetResult()
		{
			lock (this)
			{
				((Promise)this).GetResult();

				return this.value;
			}
		}

		public Promise ContinueWith(ActionContinuation<T> continuation)
		{
			return this.ContinueWithInternal<T, object>(continuation);
		}
		public Promise<RT> ContinueWith<RT>(FuncContinuation<T, RT> continuation)
		{
			return this.ContinueWithInternal<T, RT>(continuation);
		}
		public new Promise<T> IgnoreFault()
		{
			if (this.IsCompleted)
				return this.HasErrors ? DefaultFulfilled : this;

			if (this is IUpdatable)
			{
				var result = new Coroutine<T>(Coroutine.WaitForUpdatablePromise(this));
				return result;
			}

			return this.ContinueWith(p => p.HasErrors ? default(T) : p.GetResult());
		}

		public void PropagateTo(Promise<T> promise)
		{
			if (promise == null) throw new ArgumentNullException("promise");

			this.ContinueWith(p =>
			{
				if (p.HasErrors)
					promise.TrySetFailed(p.Error);
				else
					promise.TrySetResult(p.GetResult());
			});
		}

		public override string ToString()
		{
			if (this.IsCompleted)
				return string.Format("Fulfilled promise, result={0}.", this.value);
			return "Unfulfilled promise";
		}
	}
}
