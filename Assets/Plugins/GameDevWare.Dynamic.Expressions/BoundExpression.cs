/*
	Copyright (c) 2015 Denis Zykov

	This is part of Charon Game Data Editor Unity Plugin.

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
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

// ReSharper disable ArrangeStaticMemberQualifier
// ReSharper disable ConvertPropertyToExpressionBody
// ReSharper disable ConvertIfStatementToConditionalTernaryExpression
// ReSharper disable StaticMemberInGenericType

namespace GameDevWare.Dynamic.Expressions
{
	public abstract class BoundExpression : Expression
	{
		public static bool AotRuntime;
		public static readonly ExpressionType BoundExpressionType = (ExpressionType)101;
		public static readonly ReadOnlyCollection<ParameterExpression> EmptyParameters = new ReadOnlyCollection<ParameterExpression>(new ParameterExpression[0]);

		public abstract Expression Body { get; }
		public abstract ReadOnlyCollection<ParameterExpression> Parameters { get; }

		static BoundExpression()
		{
			try { Lambda<Func<bool>>(Constant(true)).Compile(); }
			catch (Exception) { AotRuntime = true; }
		}
		protected BoundExpression(Type resultType)
			: base(BoundExpressionType, resultType)
		{

		}

		public abstract Delegate DynamicCompile();
		public abstract object DynamicExecute(params object[] args);

		protected static T BindParameter<T>(object[] args, ReadOnlyCollection<ParameterExpression> parameters, int paramIndex)
		{
			if (parameters == null) throw new ArgumentNullException("parameters");
			if (paramIndex >= parameters.Count || paramIndex < 0) throw new ArgumentOutOfRangeException("paramIndex");
			if (args == null) throw new ArgumentNullException("args");
			if (args.Length != parameters.Count) throw new InvalidOperationException(Properties.Resources.EXCEPTION_BOUNDEXPR_ARGSDOESNTMATCHPARAMS);

			var parameterName = parameters[paramIndex].Name;
			var value = args[paramIndex];
			var paramType = typeof(T);
			if (value is T || (value == null && paramType.IsValueType == false) || (value == null && paramType.IsValueType && paramType.IsGenericType && paramType.GetGenericTypeDefinition() == typeof(Nullable<>)))
				return (T)value;

			throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BOUNDEXPR_CANTCONVERTARG, parameterName, paramType, value ?? "<null>"));
		}

		public override bool Equals(object obj)
		{
			var other = obj as BoundExpression;
			if (other == null) return false;

			return this.ToString() == other.ToString();
		}
		public override int GetHashCode()
		{
			return this.ToString().GetHashCode();
		}

		public override string ToString()
		{
			return this.Body.ToString() + ", params: " +
				   string.Join(", ", Array.ConvertAll(this.Parameters.ToArray(), p => p.ToString()));
		}
	}

	public class BoundExpression<ResultT> : BoundExpression
	{
		private Func<ResultT> compiledExpression;
		private readonly Expression bodyExpression;
		private readonly ReadOnlyCollection<ParameterExpression> parameterExpressions;

		public override Expression Body { get { return this.bodyExpression; } }
		public override ReadOnlyCollection<ParameterExpression> Parameters { get { return this.parameterExpressions; } }

		public BoundExpression(Expression body)
			: this(body, EmptyParameters)
		{

		}
		public BoundExpression(Expression body, ReadOnlyCollection<ParameterExpression> parameters)
			: base(typeof(ResultT))
		{
			if (parameters == null) throw new ArgumentNullException("parameters");
			if (body == null) throw new ArgumentNullException("body");
			if (parameters.Count != 0) throw new ArgumentException(Properties.Resources.EXCEPTION_BOUNDEXPR_WRONGNUMPARAMS, "parameters");
			if (body.Type != typeof(ResultT)) throw new ArgumentException(Properties.Resources.EXCEPTION_BOUNDEXPR_BODYRESULTDOESNTMATCHRESULTTYPE, "body");

			this.bodyExpression = body;
			this.parameterExpressions = parameters;
		}

		public Func<ResultT> Compile()
		{
			if (this.compiledExpression != null)
				return this.compiledExpression;

			if (AotRuntime)
				this.compiledExpression = Executor.Prepare<ResultT>(this.Body, this.Parameters);
			else
				this.compiledExpression = Lambda<Func<ResultT>>(this.Body, this.Parameters.ToArray()).Compile();

			return this.compiledExpression;
		}
		public ResultT Execute()
		{
			var fn = this.Compile();
			return fn();
		}
		public override object DynamicExecute(params object[] args)
		{
			return this.Execute();
		}
		public override Delegate DynamicCompile()
		{
			return this.Compile();
		}
	}
	public class BoundExpression<Arg1T, ResultT> : BoundExpression
	{
		private Func<Arg1T, ResultT> compiledExpression;
		private readonly Expression bodyExpression;
		private readonly ReadOnlyCollection<ParameterExpression> parameterExpressions;

		public override Expression Body { get { return this.bodyExpression; } }
		public override ReadOnlyCollection<ParameterExpression> Parameters { get { return this.parameterExpressions; } }

		public BoundExpression(Expression body, ReadOnlyCollection<ParameterExpression> parameters)
			: base(typeof(ResultT))
		{
			if (parameters == null) throw new ArgumentNullException("parameters");
			if (body == null) throw new ArgumentNullException("body");
			if (parameters.Count != 1) throw new ArgumentException(Properties.Resources.EXCEPTION_BOUNDEXPR_WRONGNUMPARAMS, "parameters");
			if (parameters[0].Type != typeof(Arg1T)) throw new ArgumentException(Properties.Resources.EXCEPTION_BOUNDEXPR_WRONGPARAMETERTYPE, "parameters");
			if (body.Type != typeof(ResultT)) throw new ArgumentException(Properties.Resources.EXCEPTION_BOUNDEXPR_BODYRESULTDOESNTMATCHRESULTTYPE, "body");

			this.bodyExpression = body;
			this.parameterExpressions = parameters;
		}

		public Func<Arg1T, ResultT> Compile()
		{
			if (this.compiledExpression != null)
				return this.compiledExpression;

			if (AotRuntime)
				this.compiledExpression = Executor.Prepare<Arg1T, ResultT>(this.Body, this.Parameters);
			else
				this.compiledExpression = Lambda<Func<Arg1T, ResultT>>(this.Body, this.Parameters.ToArray()).Compile();

			return this.compiledExpression;
		}
		public ResultT Execute(Arg1T arg1)
		{
			var fn = this.Compile();
			return fn(arg1);
		}
		public override object DynamicExecute(params object[] args)
		{
			return this.Execute
			(
				arg1: BindParameter<Arg1T>(args, this.parameterExpressions, 0)
			);
		}
		public override Delegate DynamicCompile()
		{
			return this.Compile();
		}
	}
	public class BoundExpression<Arg1T, Arg2T, ResultT> : BoundExpression
	{

		private Func<Arg1T, Arg2T, ResultT> compiledExpression;
		private readonly Expression bodyExpression;
		private readonly ReadOnlyCollection<ParameterExpression> parameterExpressions;

		public override Expression Body { get { return this.bodyExpression; } }
		public override ReadOnlyCollection<ParameterExpression> Parameters { get { return this.parameterExpressions; } }

		public BoundExpression(Expression body, ReadOnlyCollection<ParameterExpression> parameters)
			: base(typeof(ResultT))
		{
			if (parameters == null) throw new ArgumentNullException("parameters");
			if (body == null) throw new ArgumentNullException("body");
			if (parameters.Count != 2) throw new ArgumentException(Properties.Resources.EXCEPTION_BOUNDEXPR_WRONGNUMPARAMS, "parameters");
			if (parameters[0].Type != typeof(Arg1T)) throw new ArgumentException(Properties.Resources.EXCEPTION_BOUNDEXPR_WRONGPARAMETERTYPE, "parameters");
			if (parameters[1].Type != typeof(Arg2T)) throw new ArgumentException(Properties.Resources.EXCEPTION_BOUNDEXPR_WRONGPARAMETERTYPE, "parameters");
			if (body.Type != typeof(ResultT)) throw new ArgumentException(Properties.Resources.EXCEPTION_BOUNDEXPR_BODYRESULTDOESNTMATCHRESULTTYPE, "body");

			this.bodyExpression = body;
			this.parameterExpressions = parameters;
		}

		public Func<Arg1T, Arg2T, ResultT> Compile()
		{
			if (this.compiledExpression != null)
				return this.compiledExpression;

			if (AotRuntime)
				this.compiledExpression = Executor.Prepare<Arg1T, Arg2T, ResultT>(this.Body, this.Parameters);
			else
				this.compiledExpression = Lambda<Func<Arg1T, Arg2T, ResultT>>(this.Body, this.Parameters.ToArray()).Compile();

			return this.compiledExpression;
		}
		public ResultT Execute(Arg1T arg1, Arg2T arg2)
		{
			var fn = this.Compile();
			return fn(arg1, arg2);
		}
		public override object DynamicExecute(params object[] args)
		{
			return this.Execute
			(
				arg1: BindParameter<Arg1T>(args, this.parameterExpressions, 0),
				arg2: BindParameter<Arg2T>(args, this.parameterExpressions, 1)
			);
		}
		public override Delegate DynamicCompile()
		{
			return this.Compile();
		}
	}
	public class BoundExpression<Arg1T, Arg2T, Arg3T, ResultT> : BoundExpression
	{
		private Func<Arg1T, Arg2T, Arg3T, ResultT> compiledExpression;
		private readonly Expression bodyExpression;
		private readonly ReadOnlyCollection<ParameterExpression> parameterExpressions;

		public override Expression Body { get { return this.bodyExpression; } }
		public override ReadOnlyCollection<ParameterExpression> Parameters { get { return this.parameterExpressions; } }

		public BoundExpression(Expression body, ReadOnlyCollection<ParameterExpression> parameters)
			: base(typeof(ResultT))
		{
			if (parameters == null) throw new ArgumentNullException("parameters");
			if (body == null) throw new ArgumentNullException("body");
			if (parameters.Count != 3) throw new ArgumentException(Properties.Resources.EXCEPTION_BOUNDEXPR_WRONGNUMPARAMS, "parameters");
			if (parameters[0].Type != typeof(Arg1T)) throw new ArgumentException(Properties.Resources.EXCEPTION_BOUNDEXPR_WRONGPARAMETERTYPE, "parameters");
			if (parameters[1].Type != typeof(Arg2T)) throw new ArgumentException(Properties.Resources.EXCEPTION_BOUNDEXPR_WRONGPARAMETERTYPE, "parameters");
			if (parameters[2].Type != typeof(Arg3T)) throw new ArgumentException(Properties.Resources.EXCEPTION_BOUNDEXPR_WRONGPARAMETERTYPE, "parameters");
			if (body.Type != typeof(ResultT)) throw new ArgumentException(Properties.Resources.EXCEPTION_BOUNDEXPR_BODYRESULTDOESNTMATCHRESULTTYPE, "body");

			this.bodyExpression = body;
			this.parameterExpressions = parameters;
		}

		public Func<Arg1T, Arg2T, Arg3T, ResultT> Compile()
		{
			if (this.compiledExpression != null)
				return this.compiledExpression;

			if (AotRuntime)
				this.compiledExpression = Executor.Prepare<Arg1T, Arg2T, Arg3T, ResultT>(this.Body, this.Parameters);
			else
				this.compiledExpression = Lambda<Func<Arg1T, Arg2T, Arg3T, ResultT>>(this.Body, this.Parameters.ToArray()).Compile();

			return this.compiledExpression;
		}
		public ResultT Execute(Arg1T arg1, Arg2T arg2, Arg3T arg3)
		{
			var fn = this.Compile();
			return fn(arg1, arg2, arg3);
		}
		public override object DynamicExecute(params object[] args)
		{
			return this.Execute
			(
				arg1: BindParameter<Arg1T>(args, this.parameterExpressions, 0),
				arg2: BindParameter<Arg2T>(args, this.parameterExpressions, 1),
				arg3: BindParameter<Arg3T>(args, this.parameterExpressions, 2)
			);
		}
		public override Delegate DynamicCompile()
		{
			return this.Compile();
		}
	}
	public class BoundExpression<Arg1T, Arg2T, Arg3T, Arg4T, ResultT> : BoundExpression
	{
		private Func<Arg1T, Arg2T, Arg3T, Arg4T, ResultT> compiledExpression;
		private readonly Expression bodyExpression;
		private readonly ReadOnlyCollection<ParameterExpression> parameterExpressions;

		public override Expression Body { get { return this.bodyExpression; } }
		public override ReadOnlyCollection<ParameterExpression> Parameters { get { return this.parameterExpressions; } }

		public BoundExpression(Expression body, ReadOnlyCollection<ParameterExpression> parameters)
			: base(typeof(ResultT))
		{
			if (parameters == null) throw new ArgumentNullException("parameters");
			if (body == null) throw new ArgumentNullException("body");
			if (parameters.Count != 3) throw new ArgumentException(Properties.Resources.EXCEPTION_BOUNDEXPR_WRONGNUMPARAMS, "parameters");
			if (parameters[0].Type != typeof(Arg1T)) throw new ArgumentException(Properties.Resources.EXCEPTION_BOUNDEXPR_WRONGPARAMETERTYPE, "parameters");
			if (parameters[1].Type != typeof(Arg2T)) throw new ArgumentException(Properties.Resources.EXCEPTION_BOUNDEXPR_WRONGPARAMETERTYPE, "parameters");
			if (parameters[2].Type != typeof(Arg3T)) throw new ArgumentException(Properties.Resources.EXCEPTION_BOUNDEXPR_WRONGPARAMETERTYPE, "parameters");
			if (parameters[3].Type != typeof(Arg4T)) throw new ArgumentException(Properties.Resources.EXCEPTION_BOUNDEXPR_WRONGPARAMETERTYPE, "parameters");
			if (body.Type != typeof(ResultT)) throw new ArgumentException(Properties.Resources.EXCEPTION_BOUNDEXPR_BODYRESULTDOESNTMATCHRESULTTYPE, "body");

			this.bodyExpression = body;
			this.parameterExpressions = parameters;
		}

		public Func<Arg1T, Arg2T, Arg3T, Arg4T, ResultT> Compile()
		{
			if (this.compiledExpression != null)
				return this.compiledExpression;

			if (AotRuntime)
				this.compiledExpression = Executor.Prepare<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>(this.Body, this.Parameters);
			else
				this.compiledExpression = Lambda<Func<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>>(this.Body, this.Parameters.ToArray()).Compile();

			return this.compiledExpression;
		}
		public ResultT Execute(Arg1T arg1, Arg2T arg2, Arg3T arg3, Arg4T arg4)
		{
			var fn = this.Compile();
			return fn(arg1, arg2, arg3, arg4);
		}
		public override object DynamicExecute(params object[] args)
		{
			return this.Execute
			(
				arg1: BindParameter<Arg1T>(args, this.parameterExpressions, 0),
				arg2: BindParameter<Arg2T>(args, this.parameterExpressions, 1),
				arg3: BindParameter<Arg3T>(args, this.parameterExpressions, 2),
				arg4: BindParameter<Arg4T>(args, this.parameterExpressions, 3)
			);
		}
		public override Delegate DynamicCompile()
		{
			return this.Compile();
		}
	}
}
