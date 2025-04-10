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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace GameDevWare.Charon.Editor
{
	/// <summary>
	/// An event argument for asset synchronization process.
	/// </summary>
	[PublicAPI]
	public class GameDataSynchronizationEventArgs : EventArgs
	{
		/// <summary>
		/// List of additional tasks to perform during asset synchronization.
		/// </summary>
		public readonly List<Func<CancellationToken, Task>> Tasks;
		/// <summary>
		/// Path to published game data file which would be imported into `.asset` file.
		/// </summary>
		public readonly string PublishedGameDataFilePath;

		public GameDataSynchronizationEventArgs(string publishedGameDataFilePath)
		{
			this.Tasks = new List<Func<CancellationToken, Task>>();
			this.PublishedGameDataFilePath = publishedGameDataFilePath;
		}

		internal async Task RunAsync(CancellationToken cancellationToken, ILogger logger, string operationName)
		{
			// ReSharper disable once ForCanBeConvertedToForeach
			for (var index = 0; index < this.Tasks.Count; index++)
			{
				var task = this.Tasks[index];

				try
				{
					await task(cancellationToken).ConfigureAwait(false);
				}
				catch (Exception error)
				{
					logger.Log(LogType.Error, $"An error occurred while running extension for '{operationName}' operation.");
					logger.Log(LogType.Error, error);
				}
			}
		}
	}
}
