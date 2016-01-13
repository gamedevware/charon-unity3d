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
using System.Threading;

namespace Assets.Unity.Charon.Editor.Tasks
{
	public abstract class Task : IAsyncResult
	{
		private readonly Coroutine internalCoroutine;

		protected Promise StartedEvent = new Promise();

		public bool IsStarted { get { return this.StartedEvent.IsCompleted; } }

		protected Task()
		{
			this.internalCoroutine = new Coroutine(this.InitAsync());
		}

		protected abstract IEnumerable InitAsync();
		protected virtual void OnStop() { }

		public void Start()
		{
			if (this.StartedEvent.TrySetCompleted() == false)
				throw new InvalidOperationException("Task is not stopped.");
		}
		public void Stop()
		{
			var ev = this.StartedEvent;
			var newEv = new Promise();
			var oldEv = ev;
			while (ev.IsCompleted && (oldEv = Interlocked.CompareExchange(ref this.StartedEvent, newEv, ev)) != ev)
				ev = oldEv;

			if (ev.IsCompleted == false)
				throw new InvalidOperationException("Task is not started.");

			this.OnStop();
		}

		public Promise IgnoreFault()
		{
			return this.internalCoroutine.IgnoreFault();
		}

		object IAsyncResult.AsyncState
		{
			get { return (this.internalCoroutine as IAsyncResult).AsyncState; }
		}
		WaitHandle IAsyncResult.AsyncWaitHandle
		{
			get { return (this.internalCoroutine as IAsyncResult).AsyncWaitHandle; }
		}
		bool IAsyncResult.CompletedSynchronously
		{
			get { return (this.internalCoroutine as IAsyncResult).CompletedSynchronously; }
		}
		bool IAsyncResult.IsCompleted
		{
			get { return (this.internalCoroutine as IAsyncResult).IsCompleted; }
		}
	}
}
