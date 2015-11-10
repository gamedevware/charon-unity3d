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
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

// ReSharper disable UnusedParameter.Local

namespace Unity.Dynamic.Expressions
{
	static partial class Compiler
	{
		public static readonly bool UseAotCompiler;

		private const int LOCAL_OPERAND1 = 0;
		private const int LOCAL_OPERAND2 = 1;
		private const int LOCAL_FIRST_PARAMETER = 2; // this is offset of first parameter in Closure locals

		private sealed class Closure
		{
			public readonly object[] Constants;
			public readonly object[] Locals; // first two locals is reserved, third and others is parameters

			public Closure(object[] constants, object[] locals)
			{
				if (constants == null) throw new ArgumentNullException("constants");
				if (locals == null) throw new ArgumentNullException("locals");
				this.Constants = constants;
				this.Locals = locals;
			}

			public object Box<T>(T value)
			{
				return value;
			}

			public T Unbox<T>(object boxed)
			{
				//if (boxed is StrongBox<T>)
				//	return ((StrongBox<T>)boxed).Value;
				//else if (boxed is IStrongBox)
				//	boxed = ((IStrongBox)boxed).Value;

				if (boxed is T)
					return (T)boxed;
				else
					return (T)Convert.ChangeType(boxed, typeof(T));
			}

			public bool Is<T>(object boxed)
			{
				return boxed is T;
			}
		}

		private sealed class ConstantsCollector : ExpressionVisitor
		{
			public readonly List<ConstantExpression> Constants = new List<ConstantExpression>();

			protected override Expression VisitConstant(ConstantExpression c)
			{
				this.Constants.Add(c);
				return c;
			}
		}

		static Compiler()
		{
			try { Expression.Lambda<Func<bool>>(Expression.Constant(true)).Compile(); }
			catch (Exception) { UseAotCompiler = true; }

			// AOT
			if (typeof(Compiler).Name == string.Empty)
			{
				CompileExpression(default(Expression), default(ConstantExpression[]), default(ParameterExpression[]));
				CompileConditional(default(ConditionalExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				CompileConstant(default(ConstantExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				CompileInvocation(default(InvocationExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				CompileLambda(default(LambdaExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				CompileListInit(default(ListInitExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				CompileMemberAccess(default(MemberExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				CompileMemberInit(default(MemberInitExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				CompileMethodCall(default(MethodCallExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				CompileNew(default(NewExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				CompileNewArray(default(NewArrayExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				CompileParameter(default(ParameterExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				CompileTypeIs(default(TypeBinaryExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				CompileUnary(default(UnaryExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				CompileBinary(default(BinaryExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				CreateUnaryOperationFn(default(MethodInfo));
				CreateBinaryOperationFn(default(MethodInfo));
				WrapUnaryOperation(default(Type), default(String));
				WrapBinaryOperation(default(Type), default(String));
			}
		}

		public static Func<ResultT> Compile<ResultT>(Expression body, ReadOnlyCollection<ParameterExpression> parameters)
		{
			if (body == null) throw new ArgumentNullException("body");

			var collector = new ConstantsCollector();
			collector.Visit(body);

			var constantsExprs = collector.Constants.ToArray();
			var localsExprs = parameters.ToArray();
			var compiledFn = CompileExpression(body, constantsExprs, localsExprs);

			return (() =>
			{
				var constants = Array.ConvertAll(constantsExprs, c => c.Value);
				var locals = new object[] { null, null };
				var closure = new Closure(constants, locals);

				return (ResultT)compiledFn(closure);
			});
		}
		public static Func<Arg1T, ResultT> Compile<Arg1T, ResultT>(Expression body, ReadOnlyCollection<ParameterExpression> parameters)
		{
			if (body == null) throw new ArgumentNullException("body");

			var collector = new ConstantsCollector();
			collector.Visit(body);

			var constantsExprs = collector.Constants.ToArray();
			var localsExprs = parameters.ToArray();
			var compiledFn = CompileExpression(body, constantsExprs, localsExprs);
			var constants = Array.ConvertAll(constantsExprs, c => c.Value);

			return (arg1 =>
			{
				var locals = new object[] { null, null, arg1 };
				var closure = new Closure(constants, locals);

				return (ResultT)compiledFn(closure);
			});
		}
		public static Func<Arg1T, Arg2T, ResultT> Compile<Arg1T, Arg2T, ResultT>(Expression body, ReadOnlyCollection<ParameterExpression> parameters)
		{
			if (body == null) throw new ArgumentNullException("body");

			var collector = new ConstantsCollector();
			collector.Visit(body);

			var constantsExprs = collector.Constants.ToArray();
			var localsExprs = parameters.ToArray();
			var compiledFn = CompileExpression(body, constantsExprs, localsExprs);

			return ((arg1, arg2) =>
			{
				var constants = Array.ConvertAll(constantsExprs, c => c.Value);
				var locals = new object[] { null, null, arg1, arg2 };
				var closure = new Closure(constants, locals);

				return (ResultT)compiledFn(closure);
			});
		}
		public static Func<Arg1T, Arg2T, Arg3T, ResultT> Compile<Arg1T, Arg2T, Arg3T, ResultT>(Expression body, ReadOnlyCollection<ParameterExpression> parameters)
		{
			if (body == null) throw new ArgumentNullException("body");

			var collector = new ConstantsCollector();
			collector.Visit(body);

			var constantsExprs = collector.Constants.ToArray();
			var localsExprs = parameters.ToArray();
			var compiledFn = CompileExpression(body, constantsExprs, localsExprs);

			return ((arg1, arg2, arg3) =>
			{
				var constants = Array.ConvertAll(constantsExprs, c => c.Value);
				var locals = new object[] { null, null, arg1, arg2, arg3 };
				var closure = new Closure(constants, locals);

				return (ResultT)compiledFn(closure);
			});
		}
		public static Func<Arg1T, Arg2T, Arg3T, Arg4T, ResultT> Compile<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>(Expression body, ReadOnlyCollection<ParameterExpression> parameters)
		{
			if (body == null) throw new ArgumentNullException("body");

			var collector = new ConstantsCollector();
			collector.Visit(body);

			var constantsExprs = collector.Constants.ToArray();
			var localsExprs = parameters.ToArray();
			var compiledFn = CompileExpression(body, constantsExprs, localsExprs);

			return ((arg1, arg2, arg3, arg4) =>
			{
				var constants = Array.ConvertAll(constantsExprs, c => c.Value);
				var locals = new object[] { null, null, arg1, arg2, arg3, arg4 };
				var closure = new Closure(constants, locals);

				return (ResultT)compiledFn(closure);
			});
		}

		private static Func<Closure, object> CompileExpression(Expression exp, ConstantExpression[] constantsExprs,
			ParameterExpression[] localsExprs)
		{
			if (exp == null)
				return (cloj => null);

			switch (exp.NodeType)
			{
				case ExpressionType.Add:
				case ExpressionType.AddChecked:
				case ExpressionType.And:
				case ExpressionType.AndAlso:
				case ExpressionType.ArrayIndex:
				case ExpressionType.Coalesce:
				case ExpressionType.Divide:
				case ExpressionType.Equal:
				case ExpressionType.ExclusiveOr:
				case ExpressionType.GreaterThan:
				case ExpressionType.GreaterThanOrEqual:
				case ExpressionType.LeftShift:
				case ExpressionType.LessThan:
				case ExpressionType.LessThanOrEqual:
				case ExpressionType.Modulo:
				case ExpressionType.Multiply:
				case ExpressionType.MultiplyChecked:
				case ExpressionType.NotEqual:
				case ExpressionType.Or:
				case ExpressionType.OrElse:
				case ExpressionType.Power:
				case ExpressionType.RightShift:
				case ExpressionType.Subtract:
				case ExpressionType.SubtractChecked:
					return CompileBinary((BinaryExpression)exp, constantsExprs, localsExprs);

				case ExpressionType.ArrayLength:
				case ExpressionType.Convert:
				case ExpressionType.ConvertChecked:
				case ExpressionType.Negate:
				case ExpressionType.UnaryPlus:
				case ExpressionType.NegateChecked:
				case ExpressionType.Not:
				case ExpressionType.Quote:
				case ExpressionType.TypeAs:
					return CompileUnary((UnaryExpression)exp, constantsExprs, localsExprs);

				case ExpressionType.Call:
					return CompileMethodCall((MethodCallExpression)exp, constantsExprs, localsExprs);

				case ExpressionType.Conditional:
					return CompileConditional((ConditionalExpression)exp, constantsExprs, localsExprs);

				case ExpressionType.Constant:
					return CompileConstant((ConstantExpression)exp, constantsExprs, localsExprs);

				case ExpressionType.Invoke:
					return CompileInvocation((InvocationExpression)exp, constantsExprs, localsExprs);

				case ExpressionType.Lambda:
					return CompileLambda((LambdaExpression)exp, constantsExprs, localsExprs);

				case ExpressionType.ListInit:
					return CompileListInit((ListInitExpression)exp, constantsExprs, localsExprs);

				case ExpressionType.MemberAccess:
					return CompileMemberAccess((MemberExpression)exp, constantsExprs, localsExprs);

				case ExpressionType.MemberInit:
					return CompileMemberInit((MemberInitExpression)exp, constantsExprs, localsExprs);

				case ExpressionType.New:
					return CompileNew((NewExpression)exp, constantsExprs, localsExprs);

				case ExpressionType.NewArrayInit:
				case ExpressionType.NewArrayBounds:
					return CompileNewArray((NewArrayExpression)exp, constantsExprs, localsExprs);

				case ExpressionType.Parameter:
					return CompileParameter((ParameterExpression)exp, constantsExprs, localsExprs);

				case ExpressionType.TypeIs:
					return CompileTypeIs((TypeBinaryExpression)exp, constantsExprs, localsExprs);
			}
			throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_COMPIL_UNKNOWNEXPRTYPE, exp.Type));
		}

		private static Func<Closure, object> CompileConditional(ConditionalExpression conditionalExpression,
			ConstantExpression[] constantsExprs, ParameterExpression[] localsExprs)
		{
			var trueFn = CompileExpression(conditionalExpression.IfTrue, constantsExprs, localsExprs);
			var falseFn = CompileExpression(conditionalExpression.IfFalse, constantsExprs, localsExprs);
			var testFn = CompileExpression(conditionalExpression.Test, constantsExprs, localsExprs);

			return cloj => cloj.Unbox<bool>(testFn(cloj)) ? falseFn(cloj) : trueFn(cloj);
		}

		private static Func<Closure, object> CompileConstant(ConstantExpression constantExpression,
			ConstantExpression[] constantsExprs, ParameterExpression[] localsExprs)
		{
			return cloj => cloj.Constants[Array.IndexOf(constantsExprs, constantExpression)];
		}

		private static Func<Closure, object> CompileInvocation(InvocationExpression invocationExpression,
			ConstantExpression[] constantsExprs, ParameterExpression[] localsExprs)
		{
			var valueFn = CompileExpression(invocationExpression.Expression, constantsExprs, localsExprs);
			var valuesFns =
				invocationExpression.Arguments.Select(e => CompileExpression(e, constantsExprs, localsExprs)).ToArray();

			return cloj =>
			{
				var dlg = (Delegate)valueFn(cloj);
				var args = new object[valuesFns.Length];
				for (var i = 0; i < args.Length; i++)
					args[i] = valuesFns[i](cloj);

				return dlg.DynamicInvoke(args);
			};
		}

		private static Func<Closure, object> CompileLambda(LambdaExpression lambdaExpression,
			ConstantExpression[] constantsExprs, ParameterExpression[] localsExprs)
		{
			throw new NotSupportedException();
		}

		private static Func<Closure, object> CompileListInit(ListInitExpression listInitExpression,
			ConstantExpression[] constantsExprs, ParameterExpression[] localsExprs)
		{
			throw new NotSupportedException();
		}

		private static Func<Closure, object> CompileMemberAccess(MemberExpression memberExpression,
			ConstantExpression[] constantsExprs, ParameterExpression[] localsExprs)
		{
			var valueFn = CompileExpression(memberExpression.Expression, constantsExprs, localsExprs);

			return cloj =>
			{
				var value = valueFn(cloj);
				var member = memberExpression.Member;
				if (member is FieldInfo)
					return ((FieldInfo)member).GetValue(value);
				else
					return ((PropertyInfo)member).GetValue(value, null);
			};
		}

		private static Func<Closure, object> CompileMemberInit(MemberInitExpression memberInitExpression,
			ConstantExpression[] constantsExprs, ParameterExpression[] localsExprs)
		{
			throw new NotSupportedException();
		}

		private static Func<Closure, object> CompileMethodCall(MethodCallExpression methodCallExpression,
			ConstantExpression[] constantsExprs, ParameterExpression[] localsExprs)
		{
			var valueFn = CompileExpression(methodCallExpression.Object, constantsExprs, localsExprs);
			var valuesFns =
				methodCallExpression.Arguments.Select(e => CompileExpression(e, constantsExprs, localsExprs)).ToArray();
			var invokeFn = MethodInvoker.TryCreate(methodCallExpression.Method);

			if (invokeFn != null)
			{
				return cloj => { return invokeFn(cloj, valuesFns); };
			}
			else
			{
				return cloj =>
				{
					var target = valueFn(cloj);
					var parameters = new object[valuesFns.Length];
					for (var i = 0; i < parameters.Length; i++)
						parameters[i] = valuesFns[i](cloj);

					return methodCallExpression.Method.Invoke(target, parameters);
				};
			}
		}

		private static Func<Closure, object> CompileNew(NewExpression newExpression, ConstantExpression[] constantsExprs,
			ParameterExpression[] localsExprs)
		{
			var valuesFns = newExpression.Arguments.Select(e => CompileExpression(e, constantsExprs, localsExprs)).ToArray();

			return cloj =>
			{
				var source = new object[valuesFns.Length];
				for (var i = 0; i < source.Length; i++)
					source[i] = valuesFns[i](cloj);

				var args = source.Take(newExpression.Constructor.GetParameters().Length).ToArray();
				var instance = Activator.CreateInstance(newExpression.Type, args);

				for (var j = 0; j < newExpression.Members.Count; j++)
				{
					var member = newExpression.Members[j];
					if (member is FieldInfo)
						((FieldInfo)member).SetValue(instance, source[args.Length + j]);
					else
						((PropertyInfo)member).SetValue(instance, source[args.Length + j], null);
				}
				return instance;
			};
		}

		private static Func<Closure, object> CompileNewArray(NewArrayExpression newArrayExpression,
			ConstantExpression[] constantsExprs, ParameterExpression[] localsExprs)
		{
			if (newArrayExpression.NodeType == ExpressionType.NewArrayBounds)
			{
				var lengthFns =
					newArrayExpression.Expressions.Select(e => CompileExpression(e, constantsExprs, localsExprs)).ToArray();

				return cloj =>
				{
					var lengths = new int[lengthFns.Length];
					for (var i = 0; i < lengthFns.Length; i++)
						lengths[i] = cloj.Unbox<int>(lengthFns[i](cloj));

					// ReSharper disable once AssignNullToNotNullAttribute
					var array = Array.CreateInstance(newArrayExpression.Type.GetElementType(), lengths);
					return array;
				};
			}
			else
			{
				var valuesFns =
					newArrayExpression.Expressions.Select(e => CompileExpression(e, constantsExprs, localsExprs)).ToArray();

				return cloj =>
				{
					// ReSharper disable once AssignNullToNotNullAttribute
					var array = Array.CreateInstance(newArrayExpression.Type.GetElementType(), valuesFns.Length);
					for (var i = 0; i < valuesFns.Length; i++)
						array.SetValue(valuesFns[i](cloj), i);

					return array;
				};
			}
		}

		private static Func<Closure, object> CompileParameter(ParameterExpression parameterExpression,
			ConstantExpression[] constantsExprs, ParameterExpression[] localsExprs)
		{
			return
				cloj => cloj.Locals[LOCAL_FIRST_PARAMETER + Array.IndexOf(localsExprs, parameterExpression)];
		}

		private static Func<Closure, object> CompileTypeIs(TypeBinaryExpression typeBinaryExpression,
			ConstantExpression[] constantsExprs, ParameterExpression[] localsExprs)
		{
			var valueFn = CompileExpression(typeBinaryExpression.Expression, constantsExprs, localsExprs);

			return cloj =>
			{
				var value = valueFn(cloj);
				if (value == null) return false;

				return value.GetType().IsAssignableFrom(typeBinaryExpression.TypeOperand);
			};
		}

		private static Func<Closure, object> CompileUnary(UnaryExpression unaryExpression, ConstantExpression[] constantsExprs,
			ParameterExpression[] localsExprs)
		{
			var valueFn = CompileExpression(unaryExpression.Operand, constantsExprs, localsExprs);
			var opUnaryNegation = WrapUnaryOperation(unaryExpression.Operand.Type, "op_UnaryNegation");
			var opUnaryPlus = WrapUnaryOperation(unaryExpression.Operand.Type, "op_UnaryPlus");
			var opOnesComplement = WrapUnaryOperation(unaryExpression.Operand.Type, "op_OnesComplement");

			return cloj =>
			{
				var operand = valueFn(cloj);
				switch (unaryExpression.NodeType)
				{
					case ExpressionType.Negate:
					case ExpressionType.NegateChecked:
						return NumericArithmetic.PerformUnaryOperation(cloj, operand, unaryExpression.NodeType, opUnaryNegation);
					case ExpressionType.UnaryPlus:
						return NumericArithmetic.PerformUnaryOperation(cloj, operand, unaryExpression.NodeType, opUnaryPlus);
					case ExpressionType.Not:
						return NumericArithmetic.PerformUnaryOperation(cloj, operand, unaryExpression.NodeType, opOnesComplement);
					case ExpressionType.Convert:
					case ExpressionType.ConvertChecked:
						// TODO: this is wrong!
						var value = cloj.Unbox<object>(operand);
						if (value == null)
							return unaryExpression.Type.IsValueType ? Activator.CreateInstance(unaryExpression.Type) : null;
						if (value is IConvertible)
							return Convert.ChangeType(value, unaryExpression.Type, CultureInfo.InvariantCulture);
						else if (unaryExpression.Type.IsInstanceOfType(value))
							return value;
						else
							throw new InvalidCastException();
					case ExpressionType.ArrayLength:
						return cloj.Unbox<Array>(operand).Length;
					case ExpressionType.TypeAs:
					case ExpressionType.Quote:
						return operand;
				}

				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_COMPIL_UNKNOWNUNARYEXPRTYPE, unaryExpression.Type));
			};
		}

		private static Func<Closure, object> CompileBinary(BinaryExpression binaryExpression,
			ConstantExpression[] constantsExprs, ParameterExpression[] localsExprs)
		{
			var leftFn = CompileExpression(binaryExpression.Left, constantsExprs, localsExprs);
			var rightFn = CompileExpression(binaryExpression.Right, constantsExprs, localsExprs);
			var opAddition = WrapBinaryOperation(binaryExpression.Left.Type, "op_Addition");
			var opBitwiseAnd = WrapBinaryOperation(binaryExpression.Left.Type, "op_BitwiseAnd");
			var opDivision = WrapBinaryOperation(binaryExpression.Left.Type, "op_Division");
			var opEquality = WrapBinaryOperation(binaryExpression.Left.Type, "op_Equality");
			var opExclusiveOr = WrapBinaryOperation(binaryExpression.Left.Type, "op_ExclusiveOr");
			var opGreaterThan = WrapBinaryOperation(binaryExpression.Left.Type, "op_GreaterThan");
			var opGreaterThanOrEqual = WrapBinaryOperation(binaryExpression.Left.Type, "op_GreaterThanOrEqual");
			var opLessThan = WrapBinaryOperation(binaryExpression.Left.Type, "op_LessThan");
			var opLessThanOrEqual = WrapBinaryOperation(binaryExpression.Left.Type, "op_LessThanOrEqual");
			var opModulus = WrapBinaryOperation(binaryExpression.Left.Type, "op_Modulus");
			var opMultiply = WrapBinaryOperation(binaryExpression.Left.Type, "op_Multiply");
			var opBitwiseOr = WrapBinaryOperation(binaryExpression.Left.Type, "op_BitwiseOr");
			var opSubtraction = WrapBinaryOperation(binaryExpression.Left.Type, "op_Subtraction");

			return (cloj =>
			{
				switch (binaryExpression.NodeType)
				{
					case ExpressionType.AndAlso:
						return cloj.Unbox<bool>(leftFn(cloj)) && cloj.Unbox<bool>(rightFn(cloj));
					case ExpressionType.OrElse:
						return cloj.Unbox<bool>(leftFn(cloj)) || cloj.Unbox<bool>(rightFn(cloj));
				}

				var left = leftFn(cloj);
				var right = rightFn(cloj);
				switch (binaryExpression.NodeType)
				{
					case ExpressionType.Add:
					case ExpressionType.AddChecked:
						return NumericArithmetic.PerformBinaryOperation(cloj, left, right, binaryExpression.NodeType, opAddition);
					case ExpressionType.And:
						return NumericArithmetic.PerformBinaryOperation(cloj, left, right, binaryExpression.NodeType, opBitwiseAnd);
					case ExpressionType.ArrayIndex:
						return cloj.Is<int[]>(right)
							? cloj.Unbox<Array>(left).GetValue(cloj.Unbox<int[]>(right))
							: cloj.Unbox<Array>(left).GetValue(cloj.Unbox<int>(right));
					case ExpressionType.Coalesce:
						return left ?? right;
					case ExpressionType.Divide:
						return NumericArithmetic.PerformBinaryOperation(cloj, left, right, binaryExpression.NodeType, opDivision);
					case ExpressionType.Equal:
						return NumericArithmetic.PerformBinaryOperation(cloj, left, right, binaryExpression.NodeType, opEquality);
					case ExpressionType.ExclusiveOr:
						return NumericArithmetic.PerformBinaryOperation(cloj, left, right, binaryExpression.NodeType, opExclusiveOr);
					case ExpressionType.GreaterThan:
						return NumericArithmetic.PerformBinaryOperation(cloj, left, right, binaryExpression.NodeType, opGreaterThan);
					case ExpressionType.GreaterThanOrEqual:
						return NumericArithmetic.PerformBinaryOperation(cloj, left, right, binaryExpression.NodeType,
							opGreaterThanOrEqual);
					case ExpressionType.LeftShift:
					case ExpressionType.Power:
					case ExpressionType.RightShift:
						return NumericArithmetic.PerformBinaryOperation(cloj, left, right, binaryExpression.NodeType, null);
					case ExpressionType.LessThan:
						return NumericArithmetic.PerformBinaryOperation(cloj, left, right, binaryExpression.NodeType, opLessThan);
					case ExpressionType.LessThanOrEqual:
						return NumericArithmetic.PerformBinaryOperation(cloj, left, right, binaryExpression.NodeType, opLessThanOrEqual);
					case ExpressionType.Modulo:
						return NumericArithmetic.PerformBinaryOperation(cloj, left, right, binaryExpression.NodeType, opModulus);
					case ExpressionType.Multiply:
					case ExpressionType.MultiplyChecked:
						return NumericArithmetic.PerformBinaryOperation(cloj, left, right, binaryExpression.NodeType, opMultiply);
					case ExpressionType.NotEqual:
						return !((bool)NumericArithmetic.PerformBinaryOperation(cloj, left, right, ExpressionType.Equal, opEquality));
					case ExpressionType.Or:
						return NumericArithmetic.PerformBinaryOperation(cloj, left, right, binaryExpression.NodeType, opBitwiseOr);
					case ExpressionType.Subtract:
					case ExpressionType.SubtractChecked:
						return NumericArithmetic.PerformBinaryOperation(cloj, left, right, binaryExpression.NodeType, opSubtraction);
				}

				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_COMPIL_UNKNOWNBINARYEXPRTYPE, binaryExpression.Type));
			});
		}
	}
}
