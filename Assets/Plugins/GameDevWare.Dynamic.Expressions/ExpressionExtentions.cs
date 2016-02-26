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
