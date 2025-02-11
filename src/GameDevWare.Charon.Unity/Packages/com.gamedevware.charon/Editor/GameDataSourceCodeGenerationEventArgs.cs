using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace GameDevWare.Charon.Editor
{
	/// <summary>
	/// An event argument for source code generation process.
	/// </summary>
	[PublicAPI]
	public class GameDataSourceCodeGenerationEventArgs : EventArgs
	{
		/// <summary>
		/// Directory where source code would be saved.
		/// </summary>
		public readonly string SourceCodeDirectory;
		/// <summary>
		/// List of additional tasks to perform during source code generation.
		/// </summary>
		public readonly List<Func<CancellationToken, Task>> Tasks;

		/// <summary>
		/// Constructor of <see cref="GameDataSourceCodeGenerationEventArgs"/>.
		/// </summary>
		public GameDataSourceCodeGenerationEventArgs(string sourceCodeDirectory)
		{
			this.SourceCodeDirectory = sourceCodeDirectory;
			this.Tasks = new List<Func<CancellationToken, Task>>();
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
