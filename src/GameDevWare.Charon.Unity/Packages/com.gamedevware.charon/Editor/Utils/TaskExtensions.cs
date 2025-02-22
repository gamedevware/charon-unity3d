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
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GameDevWare.Charon.Editor.Utils
{
	internal static class TaskExtensions
	{
		public static Task<AsyncOperation> ToTask(this AsyncOperation asyncOperation)
		{
			if (asyncOperation == null) throw new ArgumentNullException(nameof(asyncOperation));

			var tcs = new TaskCompletionSource<AsyncOperation>();
			asyncOperation.completed += operation => tcs.TrySetResult(operation);
			return tcs.Task;
		}

		public static Task IgnoreFault(
			this Task task,
			TaskContinuationOptions options = TaskContinuationOptions.LazyCancellation | TaskContinuationOptions.ExecuteSynchronously,
			TaskScheduler scheduler = null)
		{
			if (task == null) throw new ArgumentNullException(nameof(task), "task != null");

			scheduler ??= TaskScheduler.Current ?? TaskScheduler.Default;

			if (task.IsCompleted)
			{
				ObserveException(task);
				if (task.IsCanceled)
					throw new TaskCanceledException();

				return Task.CompletedTask;
			}

			return task.ContinueWith(completedTask =>
			{
				ObserveException(completedTask);

				if (completedTask.IsCanceled)
					throw new TaskCanceledException();

			}, CancellationToken.None, options, scheduler);
		}

		public static Task<T> IgnoreFault<T>(
			this Task<T> task,
			T defaultResult = default,
			TaskContinuationOptions options = TaskContinuationOptions.LazyCancellation | TaskContinuationOptions.ExecuteSynchronously,
			TaskScheduler scheduler = null)
		{
			if (task == null) throw new ArgumentNullException(nameof(task), "task != null");

			scheduler ??= TaskScheduler.Current ?? TaskScheduler.Default;

			if (task.IsCompleted)
			{
				ObserveException(task);

				if (task.IsCanceled)
					throw new TaskCanceledException();

				return Task.FromResult<T>((task.Status == TaskStatus.RanToCompletion ? task.Result : defaultResult) ?? default(T)!);
			}

			return task.ContinueWith((completedTask, defaultResultObj) =>
			{
				ObserveException(completedTask);

				if (completedTask.IsCanceled) throw new TaskCanceledException();

				if (completedTask.Status == TaskStatus.RanToCompletion) return completedTask.Result;

				return (T)defaultResultObj!;
			}, defaultResult, CancellationToken.None, options, scheduler);
		}

		private static void ObserveException(Task task)
		{
			if (task == null)
				return;
			_ = task.Exception;
		}

		public static void LogFaultAsError(this Task task, string message = null, [CallerMemberName] string memberName = "Task", [CallerFilePath] string sourceFilePath = "<no file>", [CallerLineNumber] int sourceLineNumber = 0)
		{
			if (task == null) throw new ArgumentNullException(nameof(task));

			var sourceFileName = sourceFilePath != null ? Path.GetFileName(sourceFilePath) : "<no file>";
			task.ContinueWith(faultedTask =>
				{
					CharonEditorModule.Instance.Logger.Log(LogType.Error, $"[{sourceFileName}:{Convert.ToString(sourceLineNumber, CultureInfo.InvariantCulture)}, {memberName}] {message ?? "An error occurred while performing task"}.");
					CharonEditorModule.Instance.Logger.Log(LogType.Error, faultedTask.Exception?.Unwrap());
					CharonEditorMenu.FocusConsoleWindow();
				},
				CancellationToken.None,
				TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
				TaskScheduler.Current
			);
		}
	}
}
