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

namespace Assets.Unity.Charon.Editor.Tasks
{
	partial class Promise
	{
		private class Continuation
		{
			private readonly AsyncCallback Callback;
			private readonly Promise ContinuationPromise;
			private readonly Continuation NextContinuation;


			public Continuation(AsyncCallback callback, Promise continuationPromise, Continuation nextContinuation)
			{
				if (callback == null) throw new ArgumentNullException("callback");
				if (continuationPromise == null) throw new ArgumentNullException("continuationPromise");

				this.Callback = callback;
				this.ContinuationPromise = continuationPromise;
				this.NextContinuation = nextContinuation;

				if (nextContinuation != null)
					nextContinuation.CheckForCircularReference(this);
			}

			public AggregateException Invoke(Promise result)
			{
				var error = default(Exception);
				try { this.Callback(this.ContinuationPromise); }
				catch (Exception e) { error = e; }

				if (this.ContinuationPromise != null)
					this.ContinuationPromise.TrySetCompleted();

				var error2 = default(Exception);
				try
				{
					if (this.NextContinuation != null)
						error2 = this.NextContinuation.Invoke(result);
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
