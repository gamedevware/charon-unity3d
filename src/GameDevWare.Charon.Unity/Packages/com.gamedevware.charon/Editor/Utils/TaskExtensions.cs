/*
	Copyright (c) 2025 Denis Zykov

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
				TaskContinuationOptions.OnlyOnFaulted,
				TaskScheduler.Current
			);
		}
	}
}
