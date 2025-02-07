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
using System.Collections.Generic;
using System.Reflection;

namespace GameDevWare.Charon.Editor.Utils
{
	internal static class ExceptionUtils
    {
        public static Exception Unwrap(this Exception exception)
        {
			if (exception is AggregateException aggregateException)
                return Unwrap(aggregateException.InnerException);
            else if (exception is TargetInvocationException tie)
                return Unwrap(tie.InnerException);
            else
                return exception;
        }
        public static IEnumerable<Exception> Iterate(this Exception exception)
        {
            if (exception == null)
                yield break;

			if (exception is AggregateException aggregateException)
            {
                foreach (var innerException in aggregateException.InnerExceptions)
                {
                    foreach (var innerInnerException in Iterate(innerException))
                        yield return innerInnerException;
                }
            }
            else if (exception is TargetInvocationException tie)
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
