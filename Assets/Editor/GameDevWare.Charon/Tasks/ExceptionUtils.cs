using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Editor.GameDevWare.Charon.Tasks;

// ReSharper disable once CheckNamespace
namespace Assets.Scripts
{
	public static class ExceptionUtils
	{
		public static Exception Unwrap(this Exception exception)
		{
			var aggr = exception as AggregateException;
			var tie = exception as TargetInvocationException;
			if (aggr != null)
				return Unwrap(aggr.InnerException);
			else if (tie != null)
				return Unwrap(tie.InnerException);
			else
				return exception;
		}
		public static IEnumerable<Exception> Iterate(this Exception exception)
		{
			if (exception == null)
				yield break;

			var aggr = exception as AggregateException;
			var tie = exception as TargetInvocationException;
			if (aggr != null)
			{
				foreach (var innerException in aggr.InnerExceptions)
				{
					foreach (var innerInnerException in Iterate(innerException))
						yield return innerInnerException;
				}
			}
			else if (tie != null)
			{
				foreach (var innerInnerException in Iterate(tie.InnerException))
					yield return innerInnerException;
			}
			else
			{
				yield return exception;
			}
		}
	}
}
