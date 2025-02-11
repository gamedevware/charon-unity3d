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
		public readonly List<Func<CancellationToken, Task>> Tasks = new List<Func<CancellationToken, Task>>();

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
