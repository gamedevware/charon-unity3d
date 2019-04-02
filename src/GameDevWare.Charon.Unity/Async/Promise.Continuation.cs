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

namespace GameDevWare.Charon.Unity.Async
{
	partial class Promise
	{
		private abstract class Continuation
		{
			public abstract void Invoke(Promise completedPromise, out AggregateException errors);
			public abstract void CheckForCircularReference(Continuation continuation);
		}
		private class Continuation<PromiseT, ResultT> : Continuation
		{
			private readonly Delegate callback;
			private readonly Promise continuationPromise;
			private readonly Continuation nextContinuation;

			public Continuation(Delegate callback, Promise continuationPromise, Continuation nextContinuation)
			{
				if (callback == null) throw new ArgumentNullException("callback");
				if (continuationPromise == null) throw new ArgumentNullException("continuationPromise");

				this.callback = callback;
				this.continuationPromise = continuationPromise;
				this.nextContinuation = nextContinuation;

				if (nextContinuation != null)
					nextContinuation.CheckForCircularReference(this);
			}

			public override void Invoke(Promise completedPromise, out AggregateException errors)
			{
				var error = default(Exception);
				var continuationResult = default(ResultT);
				try
				{
					var actionContinuation = this.callback as ActionContinuation;
					var actionContinuationT = this.callback as ActionContinuation<PromiseT>;
					var funcContinuation = this.callback as FuncContinuation<ResultT>;
					var funcContinuationT = this.callback as FuncContinuation<PromiseT, ResultT>;

					if (actionContinuation != null)
						actionContinuation(completedPromise);
					else if (actionContinuationT != null)
						actionContinuationT((Promise<PromiseT>)completedPromise);
					else if (funcContinuation != null)
						continuationResult = funcContinuation(completedPromise);
					else if (funcContinuationT != null)
						continuationResult = funcContinuationT((Promise<PromiseT>)completedPromise);
					else
						throw new InvalidOperationException(string.Format("Unknown continuation function type '{0}'.", this.callback.GetType()));
				}
				catch (Exception continuationError)
				{
					error = continuationError;
				}

				var error2 = default(Exception);
				try
				{
					if (error != null)
						this.continuationPromise.TrySetFailed(error);
					else if (this.continuationPromise is Promise<ResultT>)
						((Promise<ResultT>)this.continuationPromise).TrySetResult(continuationResult);
					else
						this.continuationPromise.TrySetCompleted();
				}
				catch (Exception setResultError)
				{
					error2 = setResultError;
				}

				errors = default(AggregateException);
				var error3 = default(Exception);
				try
				{
					if (this.nextContinuation != null)
						this.nextContinuation.Invoke(completedPromise, out errors);
				}
				catch (Exception chainInvokeError) { error3 = chainInvokeError; }

				if (error != null || error2 != null || error3 != null || errors != null)
				{
					var errorList = new List<Exception>();
					if (error != null) errorList.Add(error);
					if (error2 != null) errorList.Add(error2);
					if (error3 != null) errorList.Add(error3);
					if (errors != null) errorList.AddRange(errors.InnerExceptions);

					errors = new AggregateException(errorList);
				}
			}

			public override void CheckForCircularReference(Continuation continuation)
			{
				if (continuation == null) throw new ArgumentNullException("continuation");

				if (this.nextContinuation == null)
					return;

				if (this.nextContinuation == continuation)
					throw new InvalidOperationException("Continuation creates a circular chain of call.");

				this.nextContinuation.CheckForCircularReference(continuation);
			}
		}
	}
}
