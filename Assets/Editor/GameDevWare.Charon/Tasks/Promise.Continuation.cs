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

namespace Assets.Editor.GameDevWare.Charon.Tasks
{
	partial class Promise
	{
		protected class Continuation
		{
			private readonly Delegate Callback;
			private readonly object CallbackState;
			private readonly Promise ContinuationPromise;
			private readonly Continuation NextContinuation;


			public Continuation(Delegate callback, object state, Promise continuationPromise, Continuation nextContinuation)
			{
				if (callback == null) throw new ArgumentNullException("callback");
				if (continuationPromise == null) throw new ArgumentNullException("continuationPromise");

				this.Callback = callback;
				this.CallbackState = state;
				this.ContinuationPromise = continuationPromise;
				this.NextContinuation = nextContinuation;

				if (nextContinuation != null)
					nextContinuation.CheckForCircularReference(this);
			}


			public AggregateException Invoke<T>(Promise<T> result)
			{
				return this.InvokeInternal<T>(result);
			}
			public AggregateException Invoke(Promise result)
			{
				return this.InvokeInternal<object>(result);
			}
			private AggregateException InvokeInternal<T>(Promise result)
			{
				var error = default(Exception);
				try
				{
					var noStateCallback = this.Callback as Action<Promise>;
					var stateCallback = this.Callback as Action<Promise, object>;
					var noStateTypedCallback = this.Callback as Action<Promise<T>>;
					var stateTypedCallback = this.Callback as Action<Promise<T>, object>;
					if (noStateCallback != null)
						noStateCallback(result);
					else if (stateCallback != null)
						stateCallback(result, this.CallbackState);
					else if (noStateTypedCallback != null)
						noStateTypedCallback((Promise<T>)result);
					else if (stateTypedCallback != null)
						stateTypedCallback((Promise<T>)result, this.CallbackState);
					else
						throw new InvalidOperationException("Invalid continuation callback type.");
				}
				catch (Exception e) { error = e; }

				if (this.ContinuationPromise != null)
					this.ContinuationPromise.TrySetCompleted();

				var error2 = default(Exception);
				try
				{
					if (this.NextContinuation != null)
						error2 = this.NextContinuation.InvokeInternal<T>(result);
				}
				catch (Exception e) { error2 = e; }

				if (error == null && error2 == null)
					return null;
				else if (error != null && error2 != null)
					return new AggregateException(error, error2);
				else
					return new AggregateException(error);
			}

			private void CheckForCircularReference(Continuation continuation)
			{
				if (continuation == null) throw new ArgumentNullException("continuation");

				if (this.NextContinuation == null)
					return;

				if (this.NextContinuation == continuation)
					throw new InvalidOperationException("Continuation creates a circular chain of call.");

				this.NextContinuation.CheckForCircularReference(continuation);
			}
		}
	}
}
