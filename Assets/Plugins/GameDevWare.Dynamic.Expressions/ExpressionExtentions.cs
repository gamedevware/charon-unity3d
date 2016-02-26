/*
	Copyright (c) 2016 Denis Zykov

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

using GameDevWare.Dynamic.Expressions;

// ReSharper disable once CheckNamespace
namespace System.Linq.Expressions
{
	public static class ExpressionExtentions
	{
		public static Func<TResult> CompileAot<TResult>(this Expression<Func<TResult>> expression, bool forceAot = false)
		{
			if (expression == null) throw new ArgumentNullException("expression");

			if (AotCompilation.AotRuntime || forceAot)
				return Executor.Prepare<TResult>(expression.Body, expression.Parameters);
			else
				return expression.Compile();
		}
		public static Func<TArg1, TResult> CompileAot<TArg1, TResult>(this Expression<Func<TArg1, TResult>> expression, bool forceAot = false)
		{
			if (expression == null) throw new ArgumentNullException("expression");

			if (AotCompilation.AotRuntime || forceAot)
				return Executor.Prepare<TArg1, TResult>(expression.Body, expression.Parameters);
			else
				return expression.Compile();
		}
		public static Func<TArg1, TArg2, TResult> CompileAot<TArg1, TArg2, TResult>(this Expression<Func<TArg1, TArg2, TResult>> expression, bool forceAot = false)
		{
			if (expression == null) throw new ArgumentNullException("expression");

			if (AotCompilation.AotRuntime || forceAot)
				return Executor.Prepare<TArg1, TArg2, TResult>(expression.Body, expression.Parameters);
			else
				return expression.Compile();
		}
		public static Func<TArg1, TArg2, TArg3, TResult> CompileAot<TArg1, TArg2, TArg3, TResult>(this Expression<Func<TArg1, TArg2, TArg3, TResult>> expression, bool forceAot = false)
		{
			if (expression == null) throw new ArgumentNullException("expression");

			if (AotCompilation.AotRuntime || forceAot)
				return Executor.Prepare<TArg1, TArg2, TArg3, TResult>(expression.Body, expression.Parameters);
			else
				return expression.Compile();
		}
		public static Func<TArg1, TArg2, TArg3, TArg4, TResult> CompileAot<TArg1, TArg2, TArg3, TArg4, TResult>(this Expression<Func<TArg1, TArg2, TArg3, TArg4, TResult>> expression, bool forceAot = false)
		{
			if (expression == null) throw new ArgumentNullException("expression");

			if (AotCompilation.AotRuntime || forceAot)
				return Executor.Prepare<TArg1, TArg2, TArg3, TArg4, TResult>(expression.Body, expression.Parameters);
			else
				return expression.Compile();
		}
	}
}
