using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GameDevWare.Charon.Editor.Utils
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
