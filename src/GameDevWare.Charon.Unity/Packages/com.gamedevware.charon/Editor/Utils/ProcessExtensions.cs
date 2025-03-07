/*
	Copyright (c) 2025 GameDevWare, Denis Zykov

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
using System.Diagnostics;
using System.Threading;
using JetBrains.Annotations;
using UnityEngine;

namespace GameDevWare.Charon.Editor.Utils
{
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal static class ProcessExtensions
	{
		public static void EndGracefully(int processId)
		{
			try
			{
				using var process = Process.GetProcessById(processId);
				EndGracefully(process);
			}
			catch (ArgumentException)
			{
				// ignore
			}
			catch (InvalidOperationException)
			{
				// ignore
			}
		}
		public static void EndGracefully(this Process process)
		{
			if (process == null) throw new ArgumentNullException(nameof(process));

			try
			{
				if (process.HasExited)
					return;

				//Debug.Log(string.Format("Trying to kill process with id '{0}'.", process.Id));

				process.CloseMainWindow();
				if (WaitForExit(process, TimeSpan.FromSeconds(5)))
					return;

				process.Kill();

				if (WaitForExit(process, TimeSpan.FromSeconds(10)))
					return;

				throw new InvalidOperationException("Process doesn't respond to Kill signal.");
			}
			catch (Exception error)
			{
				CharonEditorModule.Instance.Logger.Log(LogType.Warning, $"Failed to kill process with id {process.Id} because of error: {Environment.NewLine}{error.Message}");
			}
		}

		private static bool WaitForExit(Process process, TimeSpan timeout)
		{
			var hasExited = false;
			var dt = DateTime.UtcNow;
			while (DateTime.UtcNow - dt < timeout)
			{
				try
				{
					process.Refresh();
					hasExited = process.HasExited;
				}
				catch (InvalidOperationException)
				{
					// ignored
				}
				catch (System.ComponentModel.Win32Exception)
				{
					// ignored
				}
				Thread.Sleep(0);
			}
			return hasExited;
		}
	}
}
