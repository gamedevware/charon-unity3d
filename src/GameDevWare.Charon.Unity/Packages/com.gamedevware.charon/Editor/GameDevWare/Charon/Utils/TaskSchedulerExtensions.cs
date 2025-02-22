/*
	Copyright (c) 2025 Denis Zykov

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Editor.GameDevWare.Charon.Utils
{
	public static class TaskSchedulerExtensions
	{
		public static SwitchToAwaiter SwitchTo(this TaskScheduler taskScheduler)
		{
			if (taskScheduler == null) throw new ArgumentNullException(nameof(taskScheduler));

			return new SwitchToAwaiter(taskScheduler);
		}

		public struct SwitchToAwaiter : INotifyCompletion
		{
			private readonly TaskScheduler taskScheduler;
			private Action continuation;
			private Task task;

			public SwitchToAwaiter(TaskScheduler taskScheduler)
			{
				if (taskScheduler == null) throw new ArgumentNullException(nameof(taskScheduler));

				this.taskScheduler = taskScheduler;
				this.task = Task.CompletedTask;
				this.continuation = null;
			}

			public SwitchToAwaiter GetAwaiter() { return this; }

			public bool IsCompleted => this.continuation != null;

			public void OnCompleted(Action continuation)
			{
				this.continuation = continuation;
				this.task =  Task.Factory.StartNew(continuation, CancellationToken.None, TaskCreationOptions.PreferFairness, this.taskScheduler);
			}

			public void GetResult()
			{
				this.task.GetAwaiter().GetResult();
			}
		}
	}
}
