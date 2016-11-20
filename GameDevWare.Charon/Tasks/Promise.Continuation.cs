using System;
using System.Collections.Generic;

namespace GameDevWare.Charon.Tasks
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
			private readonly Delegate Callback;
			private readonly Promise ContinuationPromise;
			private readonly Continuation NextContinuation;

			public Continuation(Delegate callback, Promise continuationPromise, Continuation nextContinuation)
			{
				if (callback == null) throw new ArgumentNullException("callback");
				if (continuationPromise == null) throw new ArgumentNullException("continuationPromise");

				this.Callback = callback;
				this.ContinuationPromise = continuationPromise;
				this.NextContinuation = nextContinuation;

				if (nextContinuation != null)
					nextContinuation.CheckForCircularReference(this);
			}

			public override void Invoke(Promise completedPromise, out AggregateException errors)
			{
				var error = default(Exception);
				var continuationResult = default(ResultT);
				try
				{
					var actionContinuation = this.Callback as ActionContinuation;
					var actionContinuationT = this.Callback as ActionContinuation<PromiseT>;
					var funcContinuation = this.Callback as FuncContinuation<ResultT>;
					var funcContinuationT = this.Callback as FuncContinuation<PromiseT, ResultT>;

					if (actionContinuation != null)
						actionContinuation(completedPromise);
					else if (actionContinuationT != null)
						actionContinuationT((Promise<PromiseT>)completedPromise);
					else if (funcContinuation != null)
						continuationResult = funcContinuation(completedPromise);
					else if (funcContinuationT != null)
						continuationResult = funcContinuationT((Promise<PromiseT>)completedPromise);
					else
						throw new InvalidOperationException(string.Format("Unknown continuation function type '{0}'.", this.Callback.GetType()));
				}
				catch (Exception continuationError)
				{
					error = continuationError;
				}

				var error2 = default(Exception);
				try
				{
					if (error != null)
						this.ContinuationPromise.TrySetFailed(error);
					else if (this.ContinuationPromise is Promise<ResultT>)
						((Promise<ResultT>)this.ContinuationPromise).TrySetResult(continuationResult);
					else
						this.ContinuationPromise.TrySetCompleted();
				}
				catch (Exception setResultError)
				{
					error2 = setResultError;
				}

				errors = default(AggregateException);
				var error3 = default(Exception);
				try
				{
					if (this.NextContinuation != null)
						this.NextContinuation.Invoke(completedPromise, out errors);
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

				if (this.NextContinuation == null)
					return;

				if (this.NextContinuation == continuation)
					throw new InvalidOperationException("Continuation creates a circular chain of call.");

				this.NextContinuation.CheckForCircularReference(continuation);
			}
		}
	}
}
