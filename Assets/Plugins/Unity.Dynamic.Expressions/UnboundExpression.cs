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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using SyntaxTree = System.Collections.Generic.IDictionary<string, object>;

namespace Unity.Dynamic.Expressions
{
	public sealed class UnboundExpression : Expression
	{
		public const ExpressionType UnboundExpressionType = (ExpressionType)102;

		private readonly Dictionary<InvokationParameters, Expression> compiledExpressions;

		private readonly ExpressionTree expressionTree;
		public ExpressionTree ExpressionTree { get { return this.expressionTree; } }

		public UnboundExpression(SyntaxTree node)
			: base(UnboundExpressionType, typeof(object))
		{
			if (node == null) throw new ArgumentNullException("node");

			this.compiledExpressions = new Dictionary<InvokationParameters, Expression>();
			this.expressionTree = node is ExpressionTree ? (ExpressionTree)node : new ExpressionTree(node);
		}

		public Func<ResultT> Bind<ResultT>()
		{
			var key = new InvokationParameters(typeof(ResultT));
			var expression = default(Expression);
			lock (this.compiledExpressions)
			{
				if (!this.compiledExpressions.TryGetValue(key, out expression))
				{
					var parameters = BoundExpression.EmptyParameters;
					var builder = new ExpressionBuilder(parameters);
					expression = new BoundExpression<ResultT>(builder.Build(this.ExpressionTree), parameters);
					this.compiledExpressions.Add(key, expression);
				}
			}

			return ((BoundExpression<ResultT>)expression).Compile();
		}
		public Func<Arg1T, ResultT> Bind<Arg1T, ResultT>(string arg1Name = null)
		{
			var key = new InvokationParameters(typeof(Arg1T), arg1Name ?? "arg1", typeof(ResultT));
			var expression = default(Expression);
			lock (this.compiledExpressions)
			{
				if (!this.compiledExpressions.TryGetValue(key, out expression))
				{
					var parameters = CreateParameters(new[] { typeof(Arg1T) }, new[] { arg1Name ?? "arg1" });
					var builder = new ExpressionBuilder(parameters);
					expression = new BoundExpression<ResultT>(builder.Build(this.ExpressionTree), parameters);
					this.compiledExpressions.Add(key, expression);
				}
			}

			return ((BoundExpression<Arg1T, ResultT>)expression).Compile();
		}
		public Func<Arg1T, Arg2T, ResultT> Bind<Arg1T, Arg2T, ResultT>(string arg1Name = null, string arg2Name = null)
		{
			var key = new InvokationParameters(typeof(Arg1T), arg1Name ?? "arg1", typeof(Arg2T), arg2Name ?? "arg2", typeof(ResultT));
			var expression = default(Expression);
			lock (this.compiledExpressions)
			{
				if (!this.compiledExpressions.TryGetValue(key, out expression))
				{
					var parameters = CreateParameters(new[] { typeof(Arg1T), typeof(Arg2T) }, new[] { arg1Name ?? "arg1", arg2Name ?? "arg2" });
					var builder = new ExpressionBuilder(parameters);
					expression = new BoundExpression<ResultT>(builder.Build(this.ExpressionTree), parameters);
					this.compiledExpressions.Add(key, expression);
				}
			}

			return ((BoundExpression<Arg1T, Arg2T, ResultT>)expression).Compile();
		}
		public Func<Arg1T, Arg2T, Arg3T, ResultT> Bind<Arg1T, Arg2T, Arg3T, ResultT>(string arg1Name = null, string arg2Name = null, string arg3Name = null)
		{
			var key = new InvokationParameters(typeof(Arg1T), arg1Name ?? "arg1", typeof(Arg2T), arg2Name ?? "arg2", typeof(Arg3T), arg3Name ?? "arg3", typeof(ResultT));
			var expression = default(Expression);
			lock (this.compiledExpressions)
			{
				if (!this.compiledExpressions.TryGetValue(key, out expression))
				{
					var parameters = CreateParameters(new[] { typeof(Arg1T), typeof(Arg2T), typeof(Arg3T) }, new[] { arg1Name ?? "arg1", arg2Name ?? "arg2", arg3Name ?? "arg3" });
					var builder = new ExpressionBuilder(parameters);
					expression = new BoundExpression<ResultT>(builder.Build(this.ExpressionTree), parameters);
					this.compiledExpressions.Add(key, expression);
				}
			}
			return ((BoundExpression<Arg1T, Arg2T, Arg3T, ResultT>)expression).Compile();
		}
		public Func<Arg1T, Arg2T, Arg3T, Arg4T, ResultT> Bind<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>(string arg1Name = null, string arg2Name = null, string arg3Name = null, string arg4Name = null)
		{
			var key = new InvokationParameters(typeof(Arg1T), arg1Name ?? "arg1", typeof(Arg2T), arg2Name ?? "arg2", typeof(Arg3T), arg3Name ?? "arg3", typeof(Arg4T), arg4Name ?? "arg4", typeof(ResultT));
			var expression = default(Expression);
			lock (this.compiledExpressions)
			{
				if (!this.compiledExpressions.TryGetValue(key, out expression))
				{
					var parameters = CreateParameters(new[] { typeof(Arg1T), typeof(Arg2T), typeof(Arg3T), typeof(Arg4T) }, new[] { arg1Name ?? "arg1", arg2Name ?? "arg2", arg3Name ?? "arg3", arg4Name ?? "arg4" });
					var builder = new ExpressionBuilder(parameters);
					expression = new BoundExpression<ResultT>(builder.Build(this.ExpressionTree), parameters);
					this.compiledExpressions.Add(key, expression);
				}
			}
			return ((BoundExpression<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>)expression).Compile();
		}

		private static ReadOnlyCollection<ParameterExpression> CreateParameters(Type[] types, string[] names)
		{
			if (types == null) throw new ArgumentNullException("types");
			if (names == null) throw new ArgumentNullException("names");
			if (types.Length != names.Length) throw new ArgumentException(Properties.Resources.EXCEPTION_UNBOUNDEXPR_TYPESDOESNTMATCHNAMES, "types");

			var parameters = new ParameterExpression[types.Length];
			for (var i = 0; i < parameters.Length; i++)
			{
				if (Array.IndexOf(names, names[i]) != i) throw new ArgumentException(string.Format(Properties.Resources.EXCEPTION_UNBOUNDEXPR_DUPLICATEPARAMNAME, names[i]), "names");

				parameters[i] = Parameter(types[i], names[i]);
			}
			return new ReadOnlyCollection<ParameterExpression>(parameters);
		}

		public override bool Equals(object obj)
		{
			var other = obj as UnboundExpression;
			if (other == null) return false;

			return this.expressionTree.SequenceEqual(other.ExpressionTree);
		}
		public override int GetHashCode()
		{
			return this.expressionTree.GetHashCode();
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			lock (this.compiledExpressions)
			{
				foreach (var compiled in this.compiledExpressions)
					sb.Append(compiled.Key).Append(": ").Append(compiled.Value).AppendLine();
			}
			return sb.ToString();
		}
	}
}
