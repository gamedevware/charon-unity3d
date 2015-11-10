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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

// ReSharper disable InconsistentNaming

// ReSharper disable UnusedMember.Local
// ReSharper disable RedundantCast
// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable UnusedMethodReturnValue.Local

#pragma warning disable 0675

namespace Unity.Dynamic.Expressions
{
	partial class Compiler
	{
		private delegate object BinaryOperation(Closure cloj, object left, object right);
		private delegate object UnaryOperation(Closure cloj, object operand);

		private static UnaryOperation CreateUnaryOperationFn(MethodInfo method)
		{
			if (method == null) throw new ArgumentNullException("method");

			return (UnaryOperation)Delegate.CreateDelegate(typeof(UnaryOperation), method, true);
		}
		private static BinaryOperation CreateBinaryOperationFn(MethodInfo method)
		{
			if (method == null) throw new ArgumentNullException("method");

			return (BinaryOperation)Delegate.CreateDelegate(typeof(BinaryOperation), method, true);
		}
		private static UnaryOperation WrapUnaryOperation(Type type, string methodName)
		{
			if (type == null) throw new ArgumentNullException("type");
			if (methodName == null) throw new ArgumentNullException("methodName");

			var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
			if (method == null) return null;

			var invoker = MethodInvoker.TryCreate(method);
			if (invoker != null)
			{
				var argFns = new Func<Closure, object>[] { cloj => cloj.Locals[LOCAL_OPERAND1] };

				return (cloj, operand) =>
				{
					cloj.Locals[LOCAL_OPERAND1] = operand;

					var result = invoker(cloj, argFns);

					cloj.Locals[LOCAL_OPERAND1] = null;

					return result;
				};
			}
			else
			{
				return (cloj, operand) => { return method.Invoke(null, new object[] { operand }); };
			}
		}
		private static BinaryOperation WrapBinaryOperation(Type type, string methodName)
		{
			if (type == null) throw new ArgumentNullException("type");
			if (methodName == null) throw new ArgumentNullException("methodName");

			var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
			if (method == null) return null;

			var invoker = MethodInvoker.TryCreate(method);
			if (invoker != null)
			{
				var argFns = new Func<Closure, object>[] { cloj => cloj.Locals[LOCAL_OPERAND1], cloj => cloj.Locals[LOCAL_OPERAND2] };

				return (cloj, left, right) =>
				{
					cloj.Locals[LOCAL_OPERAND1] = left;
					cloj.Locals[LOCAL_OPERAND2] = right;

					var result = invoker(cloj, argFns);

					cloj.Locals[LOCAL_OPERAND1] = null;
					cloj.Locals[LOCAL_OPERAND2] = null;

					return result;
				};
			}
			else
			{
				return (cloj, left, right) => { return method.Invoke(null, new object[] { left, right }); };
			}
		}

		private static class NumericArithmetic
		{
			private static readonly ReadOnlyDictionary<Type, ReadOnlyDictionary<ExpressionType, Delegate>> Operations;

			static NumericArithmetic()
			{
				// AOT
				if (typeof(NumericArithmetic).Name == string.Empty)
				{
					op_Boolean.Not(default(Closure), default(Object));
					op_Byte.Negate(default(Closure), default(Object));
					op_SByte.Negate(default(Closure), default(Object));
					op_Int16.Negate(default(Closure), default(Object));
					op_UInt16.Negate(default(Closure), default(Object));
					op_Int32.Negate(default(Closure), default(Object));
#if !UNITY_WEBGL
					op_UInt32.Negate(default(Closure), default(Object));
					op_Int64.Negate(default(Closure), default(Object));
					op_UInt64.UnaryPlus(default(Closure), default(Object));
#endif
					op_Single.Negate(default(Closure), default(Object));
					op_Double.Negate(default(Closure), default(Object));
					op_Decimal.Negate(default(Closure), default(Object));
					op_Object.Equal(default(Closure), default(Object), default(Object));
				}

				Operations =
				(
					from opType in typeof(Compiler).GetNestedTypes(BindingFlags.NonPublic)
					where opType.Name.StartsWith("op_", StringComparison.Ordinal)
					from method in opType.GetMethods(BindingFlags.Public | BindingFlags.Static)
					let type = Type.GetType("System." + opType.Name.Substring(3), false)
					where type != null
					let expressionType = (ExpressionType)Enum.Parse(typeof(ExpressionType), method.Name)
					let fn = method.GetParameters().Length == 3
							? (Delegate)CreateBinaryOperationFn(method)
							: (Delegate)CreateUnaryOperationFn(method)
					select new { type, expressionType, fn }
				)
				.ToLookup(t => t.type)
				.ToDictionary
				(
					keySelector: k => k.Key,
					elementSelector: e => e.ToDictionary(b => b.expressionType, f => f.fn).AsReadOnly()
				).AsReadOnly();
			}

			public static object PerformBinaryOperation(Closure cloj, object left, object right,
				ExpressionType binaryOperationType, BinaryOperation userDefinedBinaryOperation)
			{
				if (cloj == null) throw new ArgumentNullException("cloj");

				var type = left != null ? left.GetType() : right != null ? right.GetType() : typeof(object);
				var dictionary = default(ReadOnlyDictionary<ExpressionType, Delegate>);
				var func = default(Delegate);

				if (Operations.TryGetValue(type, out dictionary) && dictionary.TryGetValue(binaryOperationType, out func))
					return ((BinaryOperation)func)(cloj, left, right);

				if (binaryOperationType == ExpressionType.Equal)
					userDefinedBinaryOperation = (BinaryOperation)Operations[typeof(object)][ExpressionType.Equal];
				else if (binaryOperationType == ExpressionType.NotEqual)
					userDefinedBinaryOperation = (BinaryOperation)Operations[typeof(object)][ExpressionType.NotEqual];

				if (userDefinedBinaryOperation == null)
					throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_COMPIL_NOBINARYOPONTYPE, binaryOperationType, type));

				return userDefinedBinaryOperation(cloj, left, right);
			}

			public static object PerformUnaryOperation(Closure cloj, object operand, ExpressionType unaryOperationType,
				UnaryOperation userDefinedUnaryOperation)
			{
				if (cloj == null) throw new ArgumentNullException("cloj");

				var type = operand != null ? operand.GetType() : typeof(object);
				var dictionary = default(ReadOnlyDictionary<ExpressionType, Delegate>);
				var func = default(Delegate);

				if (Operations.TryGetValue(type, out dictionary) && dictionary.TryGetValue(unaryOperationType, out func))
					return ((UnaryOperation)func)(cloj, operand);

				if (userDefinedUnaryOperation == null)
					throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_COMPIL_NOUNARYOPONTYPE, unaryOperationType, type));

				return userDefinedUnaryOperation(cloj, operand);
			}
		}

		private static class op_Boolean
		{
			static op_Boolean()
			{
				// AOT
				if (typeof(op_Boolean).Name == string.Empty)
				{
					Not(default(Closure), default(Object));
					Equal(default(Closure), default(Object), default(Object));
					NotEqual(default(Closure), default(Object), default(Object));
				}
			}

			public static object Not(Closure cloj, object operand)
			{
				return cloj.Box(!cloj.Unbox<Boolean>(operand));
			}
			public static object Equal(Closure cloj, object left, object right)
			{
				return cloj.Box(Equals(cloj.Unbox<Boolean>(left), cloj.Unbox<Boolean>(right)));
			}
			public static object NotEqual(Closure cloj, object left, object right)
			{
				return cloj.Box(!Equals(cloj.Unbox<Boolean>(left), cloj.Unbox<Boolean>(right)));
			}
		}
		private static class op_Byte
		{
			static op_Byte()
			{
				// AOT
				if (typeof(op_Byte).Name == string.Empty)
				{
					Negate(default(Closure), default(Object));
					NegateChecked(default(Closure), default(Object));
					UnaryPlus(default(Closure), default(Object));
					Not(default(Closure), default(Object));
					Add(default(Closure), default(Object), default(Object));
					AddChecked(default(Closure), default(Object), default(Object));
					And(default(Closure), default(Object), default(Object));
					Divide(default(Closure), default(Object), default(Object));
					Equal(default(Closure), default(Object), default(Object));
					ExclusiveOr(default(Closure), default(Object), default(Object));
					GreaterThan(default(Closure), default(Object), default(Object));
					GreaterThanOrEqual(default(Closure), default(Object), default(Object));
					LeftShift(default(Closure), default(Object), default(Object));
					Power(default(Closure), default(Object), default(Object));
					RightShift(default(Closure), default(Object), default(Object));
					LessThan(default(Closure), default(Object), default(Object));
					LessThanOrEqual(default(Closure), default(Object), default(Object));
					Modulo(default(Closure), default(Object), default(Object));
					Multiply(default(Closure), default(Object), default(Object));
					MultiplyChecked(default(Closure), default(Object), default(Object));
					NotEqual(default(Closure), default(Object), default(Object));
					Or(default(Closure), default(Object), default(Object));
					Subtract(default(Closure), default(Object), default(Object));
					SubtractChecked(default(Closure), default(Object), default(Object));
				}
			}

			public static object Negate(Closure cloj, object operand)
			{
				return cloj.Box((Byte)unchecked(-cloj.Unbox<Byte>(operand)));
			}
			public static object NegateChecked(Closure cloj, object operand)
			{
				return cloj.Box((Byte)checked(-cloj.Unbox<Byte>(operand)));
			}
			public static object UnaryPlus(Closure cloj, object operand)
			{
				return cloj.Box((Byte)unchecked(+cloj.Unbox<Byte>(operand)));
			}
			public static object Not(Closure cloj, object operand)
			{
				return cloj.Box((Byte)~cloj.Unbox<Byte>(operand));
			}
			public static object Add(Closure cloj, object left, object right)
			{
				return cloj.Box((Byte)unchecked(cloj.Unbox<Byte>(left) + cloj.Unbox<Byte>(right)));
			}
			public static object AddChecked(Closure cloj, object left, object right)
			{
				return cloj.Box((Byte)checked(cloj.Unbox<Byte>(left) + cloj.Unbox<Byte>(right)));
			}
			public static object And(Closure cloj, object left, object right)
			{
				return cloj.Box((Byte)(cloj.Unbox<Byte>(left) & cloj.Unbox<Byte>(right)));
			}
			public static object Divide(Closure cloj, object left, object right)
			{
				return cloj.Box((Byte)(cloj.Unbox<Byte>(left) / cloj.Unbox<Byte>(right)));
			}
			public static object Equal(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Byte>(left) == cloj.Unbox<Byte>(right)));
			}
			public static object ExclusiveOr(Closure cloj, object left, object right)
			{
				return cloj.Box((Byte)(cloj.Unbox<Byte>(left) ^ cloj.Unbox<Byte>(right)));
			}
			public static object GreaterThan(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Byte>(left) > cloj.Unbox<Byte>(right)));
			}
			public static object GreaterThanOrEqual(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Byte>(left) >= cloj.Unbox<Byte>(right)));
			}
			public static object LeftShift(Closure cloj, object left, object right)
			{
				return cloj.Box((Byte)(cloj.Unbox<Byte>(left) << cloj.Unbox<Int32>(right)));
			}
			public static object Power(Closure cloj, object left, object right)
			{
				return cloj.Box((Byte)Math.Pow(cloj.Unbox<Byte>(left), cloj.Unbox<Double>(right)));
			}
			public static object RightShift(Closure cloj, object left, object right)
			{
				return cloj.Box((Byte)(cloj.Unbox<Byte>(left) >> cloj.Unbox<Int32>(right)));
			}
			public static object LessThan(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Byte>(left) < cloj.Unbox<Byte>(right)));
			}
			public static object LessThanOrEqual(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Byte>(left) <= cloj.Unbox<Byte>(right)));
			}
			public static object Modulo(Closure cloj, object left, object right)
			{
				return cloj.Box((Byte)(cloj.Unbox<Byte>(left) % cloj.Unbox<Byte>(right)));
			}
			public static object Multiply(Closure cloj, object left, object right)
			{
				return cloj.Box((Byte)unchecked(cloj.Unbox<Byte>(left) * cloj.Unbox<Byte>(right)));
			}
			public static object MultiplyChecked(Closure cloj, object left, object right)
			{
				return cloj.Box((Byte)checked(cloj.Unbox<Byte>(left) * cloj.Unbox<Byte>(right)));
			}
			public static object NotEqual(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Byte>(left) != cloj.Unbox<Byte>(right)));
			}
			public static object Or(Closure cloj, object left, object right)
			{
				return cloj.Box((Byte)(cloj.Unbox<Byte>(left) | cloj.Unbox<Byte>(right)));
			}
			public static object Subtract(Closure cloj, object left, object right)
			{
				return cloj.Box((Byte)unchecked(cloj.Unbox<Byte>(left) - cloj.Unbox<Byte>(right)));
			}
			public static object SubtractChecked(Closure cloj, object left, object right)
			{
				return cloj.Box((Byte)checked(cloj.Unbox<Byte>(left) - cloj.Unbox<Byte>(right)));
			}
		}
		private static class op_SByte
		{
			static op_SByte()
			{
				// AOT
				if (typeof(op_SByte).Name == string.Empty)
				{
					Negate(default(Closure), default(Object));
					NegateChecked(default(Closure), default(Object));
					UnaryPlus(default(Closure), default(Object));
					Not(default(Closure), default(Object));
					Add(default(Closure), default(Object), default(Object));
					AddChecked(default(Closure), default(Object), default(Object));
					And(default(Closure), default(Object), default(Object));
					Divide(default(Closure), default(Object), default(Object));
					Equal(default(Closure), default(Object), default(Object));
					ExclusiveOr(default(Closure), default(Object), default(Object));
					GreaterThan(default(Closure), default(Object), default(Object));
					GreaterThanOrEqual(default(Closure), default(Object), default(Object));
					LeftShift(default(Closure), default(Object), default(Object));
					Power(default(Closure), default(Object), default(Object));
					RightShift(default(Closure), default(Object), default(Object));
					LessThan(default(Closure), default(Object), default(Object));
					LessThanOrEqual(default(Closure), default(Object), default(Object));
					Modulo(default(Closure), default(Object), default(Object));
					Multiply(default(Closure), default(Object), default(Object));
					MultiplyChecked(default(Closure), default(Object), default(Object));
					NotEqual(default(Closure), default(Object), default(Object));
					Or(default(Closure), default(Object), default(Object));
					Subtract(default(Closure), default(Object), default(Object));
					SubtractChecked(default(Closure), default(Object), default(Object));
				}
			}

			public static object Negate(Closure cloj, object operand)
			{
				return cloj.Box((SByte)unchecked(-cloj.Unbox<SByte>(operand)));
			}
			public static object NegateChecked(Closure cloj, object operand)
			{
				return cloj.Box((SByte)checked(-cloj.Unbox<SByte>(operand)));
			}
			public static object UnaryPlus(Closure cloj, object operand)
			{
				return cloj.Box((SByte)unchecked(+cloj.Unbox<SByte>(operand)));
			}
			public static object Not(Closure cloj, object operand)
			{
				return cloj.Box((SByte)~cloj.Unbox<SByte>(operand));
			}
			public static object Add(Closure cloj, object left, object right)
			{
				return cloj.Box((SByte)unchecked(cloj.Unbox<SByte>(left) + cloj.Unbox<SByte>(right)));
			}
			public static object AddChecked(Closure cloj, object left, object right)
			{
				return cloj.Box((SByte)checked(cloj.Unbox<SByte>(left) + cloj.Unbox<SByte>(right)));
			}
			public static object And(Closure cloj, object left, object right)
			{
				return cloj.Box((SByte)(cloj.Unbox<SByte>(left) & cloj.Unbox<SByte>(right)));
			}
			public static object Divide(Closure cloj, object left, object right)
			{
				return cloj.Box((SByte)(cloj.Unbox<SByte>(left) / cloj.Unbox<SByte>(right)));
			}
			public static object Equal(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<SByte>(left) == cloj.Unbox<SByte>(right)));
			}
			public static object ExclusiveOr(Closure cloj, object left, object right)
			{
				return cloj.Box((SByte)(cloj.Unbox<SByte>(left) ^ cloj.Unbox<SByte>(right)));
			}
			public static object GreaterThan(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<SByte>(left) > cloj.Unbox<SByte>(right)));
			}
			public static object GreaterThanOrEqual(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<SByte>(left) >= cloj.Unbox<SByte>(right)));
			}
			public static object LeftShift(Closure cloj, object left, object right)
			{
				return cloj.Box((SByte)(cloj.Unbox<SByte>(left) << cloj.Unbox<Int32>(right)));
			}
			public static object Power(Closure cloj, object left, object right)
			{
				return cloj.Box((SByte)Math.Pow(cloj.Unbox<SByte>(left), cloj.Unbox<Double>(right)));
			}
			public static object RightShift(Closure cloj, object left, object right)
			{
				return cloj.Box((SByte)(cloj.Unbox<SByte>(left) >> cloj.Unbox<Int32>(right)));
			}
			public static object LessThan(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<SByte>(left) < cloj.Unbox<SByte>(right)));
			}
			public static object LessThanOrEqual(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<SByte>(left) <= cloj.Unbox<SByte>(right)));
			}
			public static object Modulo(Closure cloj, object left, object right)
			{
				return cloj.Box((SByte)(cloj.Unbox<SByte>(left) % cloj.Unbox<SByte>(right)));
			}
			public static object Multiply(Closure cloj, object left, object right)
			{
				return cloj.Box((SByte)unchecked(cloj.Unbox<SByte>(left) * cloj.Unbox<SByte>(right)));
			}
			public static object MultiplyChecked(Closure cloj, object left, object right)
			{
				return cloj.Box((SByte)checked(cloj.Unbox<SByte>(left) * cloj.Unbox<SByte>(right)));
			}
			public static object NotEqual(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<SByte>(left) != cloj.Unbox<SByte>(right)));
			}
			public static object Or(Closure cloj, object left, object right)
			{
				return cloj.Box((SByte)(cloj.Unbox<SByte>(left) | cloj.Unbox<SByte>(right)));
			}
			public static object Subtract(Closure cloj, object left, object right)
			{
				return cloj.Box((SByte)unchecked(cloj.Unbox<SByte>(left) - cloj.Unbox<SByte>(right)));
			}
			public static object SubtractChecked(Closure cloj, object left, object right)
			{
				return cloj.Box((SByte)checked(cloj.Unbox<SByte>(left) - cloj.Unbox<SByte>(right)));
			}
		}
		private static class op_Int16
		{
			static op_Int16()
			{
				// AOT
				if (typeof(op_Int16).Name == string.Empty)
				{
					Negate(default(Closure), default(Object));
					NegateChecked(default(Closure), default(Object));
					UnaryPlus(default(Closure), default(Object));
					Not(default(Closure), default(Object));
					Add(default(Closure), default(Object), default(Object));
					AddChecked(default(Closure), default(Object), default(Object));
					And(default(Closure), default(Object), default(Object));
					Divide(default(Closure), default(Object), default(Object));
					Equal(default(Closure), default(Object), default(Object));
					ExclusiveOr(default(Closure), default(Object), default(Object));
					GreaterThan(default(Closure), default(Object), default(Object));
					GreaterThanOrEqual(default(Closure), default(Object), default(Object));
					LeftShift(default(Closure), default(Object), default(Object));
					Power(default(Closure), default(Object), default(Object));
					RightShift(default(Closure), default(Object), default(Object));
					LessThan(default(Closure), default(Object), default(Object));
					LessThanOrEqual(default(Closure), default(Object), default(Object));
					Modulo(default(Closure), default(Object), default(Object));
					Multiply(default(Closure), default(Object), default(Object));
					MultiplyChecked(default(Closure), default(Object), default(Object));
					NotEqual(default(Closure), default(Object), default(Object));
					Or(default(Closure), default(Object), default(Object));
					Subtract(default(Closure), default(Object), default(Object));
					SubtractChecked(default(Closure), default(Object), default(Object));
				}
			}

			public static object Negate(Closure cloj, object operand)
			{
				return cloj.Box((Int16)unchecked(-cloj.Unbox<Int16>(operand)));
			}
			public static object NegateChecked(Closure cloj, object operand)
			{
				return cloj.Box((Int16)checked(-cloj.Unbox<Int16>(operand)));
			}
			public static object UnaryPlus(Closure cloj, object operand)
			{
				return cloj.Box((Int16)unchecked(+cloj.Unbox<Int16>(operand)));
			}
			public static object Not(Closure cloj, object operand)
			{
				return cloj.Box((Int16)~cloj.Unbox<Int16>(operand));
			}
			public static object Add(Closure cloj, object left, object right)
			{
				return cloj.Box((Int16)unchecked(cloj.Unbox<Int16>(left) + cloj.Unbox<Int16>(right)));
			}
			public static object AddChecked(Closure cloj, object left, object right)
			{
				return cloj.Box((Int16)checked(cloj.Unbox<Int16>(left) + cloj.Unbox<Int16>(right)));
			}
			public static object And(Closure cloj, object left, object right)
			{
				return cloj.Box((Int16)(cloj.Unbox<Int16>(left) & cloj.Unbox<Int16>(right)));
			}
			public static object Divide(Closure cloj, object left, object right)
			{
				return cloj.Box((Int16)(cloj.Unbox<Int16>(left) / cloj.Unbox<Int16>(right)));
			}
			public static object Equal(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Int16>(left) == cloj.Unbox<Int16>(right)));
			}
			public static object ExclusiveOr(Closure cloj, object left, object right)
			{
				return cloj.Box((Int16)(cloj.Unbox<Int16>(left) ^ cloj.Unbox<Int16>(right)));
			}
			public static object GreaterThan(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Int16>(left) > cloj.Unbox<Int16>(right)));
			}
			public static object GreaterThanOrEqual(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Int16>(left) >= cloj.Unbox<Int16>(right)));
			}
			public static object LeftShift(Closure cloj, object left, object right)
			{
				return cloj.Box((Int16)(cloj.Unbox<Int16>(left) << cloj.Unbox<Int32>(right)));
			}
			public static object Power(Closure cloj, object left, object right)
			{
				return cloj.Box((Int16)Math.Pow(cloj.Unbox<Int16>(left), cloj.Unbox<Double>(right)));
			}
			public static object RightShift(Closure cloj, object left, object right)
			{
				return cloj.Box((Int16)(cloj.Unbox<Int16>(left) >> cloj.Unbox<Int32>(right)));
			}
			public static object LessThan(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Int16>(left) < cloj.Unbox<Int16>(right)));
			}
			public static object LessThanOrEqual(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Int16>(left) <= cloj.Unbox<Int16>(right)));
			}
			public static object Modulo(Closure cloj, object left, object right)
			{
				return cloj.Box((Int16)(cloj.Unbox<Int16>(left) % cloj.Unbox<Int16>(right)));
			}
			public static object Multiply(Closure cloj, object left, object right)
			{
				return cloj.Box((Int16)unchecked(cloj.Unbox<Int16>(left) * cloj.Unbox<Int16>(right)));
			}
			public static object MultiplyChecked(Closure cloj, object left, object right)
			{
				return cloj.Box((Int16)checked(cloj.Unbox<Int16>(left) * cloj.Unbox<Int16>(right)));
			}
			public static object NotEqual(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Int16>(left) != cloj.Unbox<Int16>(right)));
			}
			public static object Or(Closure cloj, object left, object right)
			{
				return cloj.Box((Int16)(cloj.Unbox<Int16>(left) | cloj.Unbox<Int16>(right)));
			}
			public static object Subtract(Closure cloj, object left, object right)
			{
				return cloj.Box((Int16)unchecked(cloj.Unbox<Int16>(left) - cloj.Unbox<Int16>(right)));
			}
			public static object SubtractChecked(Closure cloj, object left, object right)
			{
				return cloj.Box((Int16)checked(cloj.Unbox<Int16>(left) - cloj.Unbox<Int16>(right)));
			}
		}
		private static class op_UInt16
		{
			static op_UInt16()
			{
				// AOT
				if (typeof(op_UInt16).Name == string.Empty)
				{
					Negate(default(Closure), default(Object));
					NegateChecked(default(Closure), default(Object));
					UnaryPlus(default(Closure), default(Object));
					Not(default(Closure), default(Object));
					Add(default(Closure), default(Object), default(Object));
					AddChecked(default(Closure), default(Object), default(Object));
					And(default(Closure), default(Object), default(Object));
					Divide(default(Closure), default(Object), default(Object));
					Equal(default(Closure), default(Object), default(Object));
					ExclusiveOr(default(Closure), default(Object), default(Object));
					GreaterThan(default(Closure), default(Object), default(Object));
					GreaterThanOrEqual(default(Closure), default(Object), default(Object));
					LeftShift(default(Closure), default(Object), default(Object));
					Power(default(Closure), default(Object), default(Object));
					RightShift(default(Closure), default(Object), default(Object));
					LessThan(default(Closure), default(Object), default(Object));
					LessThanOrEqual(default(Closure), default(Object), default(Object));
					Modulo(default(Closure), default(Object), default(Object));
					Multiply(default(Closure), default(Object), default(Object));
					MultiplyChecked(default(Closure), default(Object), default(Object));
					NotEqual(default(Closure), default(Object), default(Object));
					Or(default(Closure), default(Object), default(Object));
					Subtract(default(Closure), default(Object), default(Object));
					SubtractChecked(default(Closure), default(Object), default(Object));
				}
			}

			public static object Negate(Closure cloj, object operand)
			{
				return cloj.Box((UInt16)unchecked(-cloj.Unbox<UInt16>(operand)));
			}
			public static object NegateChecked(Closure cloj, object operand)
			{
				return cloj.Box((UInt16)checked(-cloj.Unbox<UInt16>(operand)));
			}
			public static object UnaryPlus(Closure cloj, object operand)
			{
				return cloj.Box((UInt16)unchecked(+cloj.Unbox<UInt16>(operand)));
			}
			public static object Not(Closure cloj, object operand)
			{
				return cloj.Box((UInt16)~cloj.Unbox<UInt16>(operand));
			}
			public static object Add(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt16)unchecked(cloj.Unbox<UInt16>(left) + cloj.Unbox<UInt16>(right)));
			}
			public static object AddChecked(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt16)checked(cloj.Unbox<UInt16>(left) + cloj.Unbox<UInt16>(right)));
			}
			public static object And(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt16)(cloj.Unbox<UInt16>(left) & cloj.Unbox<UInt16>(right)));
			}
			public static object Divide(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt16)(cloj.Unbox<UInt16>(left) / cloj.Unbox<UInt16>(right)));
			}
			public static object Equal(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<UInt16>(left) == cloj.Unbox<UInt16>(right)));
			}
			public static object ExclusiveOr(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt16)(cloj.Unbox<UInt16>(left) ^ cloj.Unbox<UInt16>(right)));
			}
			public static object GreaterThan(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<UInt16>(left) > cloj.Unbox<UInt16>(right)));
			}
			public static object GreaterThanOrEqual(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<UInt16>(left) >= cloj.Unbox<UInt16>(right)));
			}
			public static object LeftShift(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt16)(cloj.Unbox<UInt16>(left) << cloj.Unbox<Int32>(right)));
			}
			public static object Power(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt16)Math.Pow(cloj.Unbox<UInt16>(left), cloj.Unbox<Double>(right)));
			}
			public static object RightShift(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt16)(cloj.Unbox<UInt16>(left) >> cloj.Unbox<Int32>(right)));
			}
			public static object LessThan(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<UInt16>(left) < cloj.Unbox<UInt16>(right)));
			}
			public static object LessThanOrEqual(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<UInt16>(left) <= cloj.Unbox<UInt16>(right)));
			}
			public static object Modulo(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt16)(cloj.Unbox<UInt16>(left) % cloj.Unbox<UInt16>(right)));
			}
			public static object Multiply(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt16)unchecked(cloj.Unbox<UInt16>(left) * cloj.Unbox<UInt16>(right)));
			}
			public static object MultiplyChecked(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt16)checked(cloj.Unbox<UInt16>(left) * cloj.Unbox<UInt16>(right)));
			}
			public static object NotEqual(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<UInt16>(left) != cloj.Unbox<UInt16>(right)));
			}
			public static object Or(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt16)(cloj.Unbox<UInt16>(left) | cloj.Unbox<UInt16>(right)));
			}
			public static object Subtract(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt16)unchecked(cloj.Unbox<UInt16>(left) - cloj.Unbox<UInt16>(right)));
			}
			public static object SubtractChecked(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt16)checked(cloj.Unbox<UInt16>(left) - cloj.Unbox<UInt16>(right)));
			}
		}
		private static class op_Int32
		{
			static op_Int32()
			{
				// AOT
				if (typeof(op_Int32).Name == string.Empty)
				{
					Negate(default(Closure), default(Object));
					NegateChecked(default(Closure), default(Object));
					UnaryPlus(default(Closure), default(Object));
					Not(default(Closure), default(Object));
					Add(default(Closure), default(Object), default(Object));
					AddChecked(default(Closure), default(Object), default(Object));
					And(default(Closure), default(Object), default(Object));
					Divide(default(Closure), default(Object), default(Object));
					Equal(default(Closure), default(Object), default(Object));
					ExclusiveOr(default(Closure), default(Object), default(Object));
					GreaterThan(default(Closure), default(Object), default(Object));
					GreaterThanOrEqual(default(Closure), default(Object), default(Object));
					LeftShift(default(Closure), default(Object), default(Object));
					Power(default(Closure), default(Object), default(Object));
					RightShift(default(Closure), default(Object), default(Object));
					LessThan(default(Closure), default(Object), default(Object));
					LessThanOrEqual(default(Closure), default(Object), default(Object));
					Modulo(default(Closure), default(Object), default(Object));
					Multiply(default(Closure), default(Object), default(Object));
					MultiplyChecked(default(Closure), default(Object), default(Object));
					NotEqual(default(Closure), default(Object), default(Object));
					Or(default(Closure), default(Object), default(Object));
					Subtract(default(Closure), default(Object), default(Object));
					SubtractChecked(default(Closure), default(Object), default(Object));
				}
			}

			public static object Negate(Closure cloj, object operand)
			{
				return cloj.Box((Int32)unchecked(-cloj.Unbox<Int32>(operand)));
			}
			public static object NegateChecked(Closure cloj, object operand)
			{
				return cloj.Box((Int32)checked(-cloj.Unbox<Int32>(operand)));
			}
			public static object UnaryPlus(Closure cloj, object operand)
			{
				return cloj.Box((Int32)unchecked(+cloj.Unbox<Int32>(operand)));
			}
			public static object Not(Closure cloj, object operand)
			{
				return cloj.Box((Int32)~cloj.Unbox<Int32>(operand));
			}
			public static object Add(Closure cloj, object left, object right)
			{
				return cloj.Box((Int32)unchecked(cloj.Unbox<Int32>(left) + cloj.Unbox<Int32>(right)));
			}
			public static object AddChecked(Closure cloj, object left, object right)
			{
				return cloj.Box((Int32)checked(cloj.Unbox<Int32>(left) + cloj.Unbox<Int32>(right)));
			}
			public static object And(Closure cloj, object left, object right)
			{
				return cloj.Box((Int32)(cloj.Unbox<Int32>(left) & cloj.Unbox<Int32>(right)));
			}
			public static object Divide(Closure cloj, object left, object right)
			{
				return cloj.Box((Int32)(cloj.Unbox<Int32>(left) / cloj.Unbox<Int32>(right)));
			}
			public static object Equal(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Int32>(left) == cloj.Unbox<Int32>(right)));
			}
			public static object ExclusiveOr(Closure cloj, object left, object right)
			{
				return cloj.Box((Int32)(cloj.Unbox<Int32>(left) ^ cloj.Unbox<Int32>(right)));
			}
			public static object GreaterThan(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Int32>(left) > cloj.Unbox<Int32>(right)));
			}
			public static object GreaterThanOrEqual(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Int32>(left) >= cloj.Unbox<Int32>(right)));
			}
			public static object LeftShift(Closure cloj, object left, object right)
			{
				return cloj.Box((Int32)(cloj.Unbox<Int32>(left) << cloj.Unbox<Int32>(right)));
			}
			public static object Power(Closure cloj, object left, object right)
			{
				return cloj.Box((Int32)Math.Pow(cloj.Unbox<Int32>(left), cloj.Unbox<Double>(right)));
			}
			public static object RightShift(Closure cloj, object left, object right)
			{
				return cloj.Box((Int32)(cloj.Unbox<Int32>(left) >> cloj.Unbox<Int32>(right)));
			}
			public static object LessThan(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Int32>(left) < cloj.Unbox<Int32>(right)));
			}
			public static object LessThanOrEqual(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Int32>(left) <= cloj.Unbox<Int32>(right)));
			}
			public static object Modulo(Closure cloj, object left, object right)
			{
				return cloj.Box((Int32)(cloj.Unbox<Int32>(left) % cloj.Unbox<Int32>(right)));
			}
			public static object Multiply(Closure cloj, object left, object right)
			{
				return cloj.Box((Int32)unchecked(cloj.Unbox<Int32>(left) * cloj.Unbox<Int32>(right)));
			}
			public static object MultiplyChecked(Closure cloj, object left, object right)
			{
				return cloj.Box((Int32)checked(cloj.Unbox<Int32>(left) * cloj.Unbox<Int32>(right)));
			}
			public static object NotEqual(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Int32>(left) != cloj.Unbox<Int32>(right)));
			}
			public static object Or(Closure cloj, object left, object right)
			{
				return cloj.Box((Int32)(cloj.Unbox<Int32>(left) | cloj.Unbox<Int32>(right)));
			}
			public static object Subtract(Closure cloj, object left, object right)
			{
				return cloj.Box((Int32)unchecked(cloj.Unbox<Int32>(left) - cloj.Unbox<Int32>(right)));
			}
			public static object SubtractChecked(Closure cloj, object left, object right)
			{
				return cloj.Box((Int32)checked(cloj.Unbox<Int32>(left) - cloj.Unbox<Int32>(right)));
			}
		}
#if !UNITY_WEBGL
		private static class op_UInt32
		{
			static op_UInt32()
			{
				// AOT
				if (typeof(op_UInt32).Name == string.Empty)
				{
					Negate(default(Closure), default(Object));
					NegateChecked(default(Closure), default(Object));
					UnaryPlus(default(Closure), default(Object));
					Not(default(Closure), default(Object));
					Add(default(Closure), default(Object), default(Object));
					AddChecked(default(Closure), default(Object), default(Object));
					And(default(Closure), default(Object), default(Object));
					Divide(default(Closure), default(Object), default(Object));
					Equal(default(Closure), default(Object), default(Object));
					ExclusiveOr(default(Closure), default(Object), default(Object));
					GreaterThan(default(Closure), default(Object), default(Object));
					GreaterThanOrEqual(default(Closure), default(Object), default(Object));
					LeftShift(default(Closure), default(Object), default(Object));
					Power(default(Closure), default(Object), default(Object));
					RightShift(default(Closure), default(Object), default(Object));
					LessThan(default(Closure), default(Object), default(Object));
					LessThanOrEqual(default(Closure), default(Object), default(Object));
					Modulo(default(Closure), default(Object), default(Object));
					Multiply(default(Closure), default(Object), default(Object));
					MultiplyChecked(default(Closure), default(Object), default(Object));
					NotEqual(default(Closure), default(Object), default(Object));
					Or(default(Closure), default(Object), default(Object));
					Subtract(default(Closure), default(Object), default(Object));
					SubtractChecked(default(Closure), default(Object), default(Object));
				}
			}

			public static object Negate(Closure cloj, object operand)
			{
				return cloj.Box((UInt32)unchecked(-cloj.Unbox<UInt32>(operand)));
			}
			public static object NegateChecked(Closure cloj, object operand)
			{
				return cloj.Box((UInt32)checked(-cloj.Unbox<UInt32>(operand)));
			}
			public static object UnaryPlus(Closure cloj, object operand)
			{
				return cloj.Box((UInt32)unchecked(+cloj.Unbox<UInt32>(operand)));
			}
			public static object Not(Closure cloj, object operand)
			{
				return cloj.Box((UInt32)~cloj.Unbox<UInt32>(operand));
			}
			public static object Add(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt32)unchecked(cloj.Unbox<UInt32>(left) + cloj.Unbox<UInt32>(right)));
			}
			public static object AddChecked(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt32)checked(cloj.Unbox<UInt32>(left) + cloj.Unbox<UInt32>(right)));
			}
			public static object And(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt32)(cloj.Unbox<UInt32>(left) & cloj.Unbox<UInt32>(right)));
			}
			public static object Divide(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt32)(cloj.Unbox<UInt32>(left) / cloj.Unbox<UInt32>(right)));
			}
			public static object Equal(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<UInt32>(left) == cloj.Unbox<UInt32>(right)));
			}
			public static object ExclusiveOr(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt32)(cloj.Unbox<UInt32>(left) ^ cloj.Unbox<UInt32>(right)));
			}
			public static object GreaterThan(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<UInt32>(left) > cloj.Unbox<UInt32>(right)));
			}
			public static object GreaterThanOrEqual(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<UInt32>(left) >= cloj.Unbox<UInt32>(right)));
			}
			public static object LeftShift(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt32)(cloj.Unbox<UInt32>(left) << cloj.Unbox<Int32>(right)));
			}
			public static object Power(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt32)Math.Pow(cloj.Unbox<UInt32>(left), cloj.Unbox<Double>(right)));
			}
			public static object RightShift(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt32)(cloj.Unbox<UInt32>(left) >> cloj.Unbox<Int32>(right)));
			}
			public static object LessThan(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<UInt32>(left) < cloj.Unbox<UInt32>(right)));
			}
			public static object LessThanOrEqual(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<UInt32>(left) <= cloj.Unbox<UInt32>(right)));
			}
			public static object Modulo(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt32)(cloj.Unbox<UInt32>(left) % cloj.Unbox<UInt32>(right)));
			}
			public static object Multiply(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt32)unchecked(cloj.Unbox<UInt32>(left) * cloj.Unbox<UInt32>(right)));
			}
			public static object MultiplyChecked(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt32)checked(cloj.Unbox<UInt32>(left) * cloj.Unbox<UInt32>(right)));
			}
			public static object NotEqual(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<UInt32>(left) != cloj.Unbox<UInt32>(right)));
			}
			public static object Or(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt32)(cloj.Unbox<UInt32>(left) | cloj.Unbox<UInt32>(right)));
			}
			public static object Subtract(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt32)unchecked(cloj.Unbox<UInt32>(left) - cloj.Unbox<UInt32>(right)));
			}
			public static object SubtractChecked(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt32)checked(cloj.Unbox<UInt32>(left) - cloj.Unbox<UInt32>(right)));
			}
		}
		private static class op_Int64
		{
			static op_Int64()
			{
				// AOT
				if (typeof(op_Int64).Name == string.Empty)
				{
					Negate(default(Closure), default(Object));
					NegateChecked(default(Closure), default(Object));
					UnaryPlus(default(Closure), default(Object));
					Not(default(Closure), default(Object));
					Add(default(Closure), default(Object), default(Object));
					AddChecked(default(Closure), default(Object), default(Object));
					And(default(Closure), default(Object), default(Object));
					Divide(default(Closure), default(Object), default(Object));
					Equal(default(Closure), default(Object), default(Object));
					ExclusiveOr(default(Closure), default(Object), default(Object));
					GreaterThan(default(Closure), default(Object), default(Object));
					GreaterThanOrEqual(default(Closure), default(Object), default(Object));
					LeftShift(default(Closure), default(Object), default(Object));
					Power(default(Closure), default(Object), default(Object));
					RightShift(default(Closure), default(Object), default(Object));
					LessThan(default(Closure), default(Object), default(Object));
					LessThanOrEqual(default(Closure), default(Object), default(Object));
					Modulo(default(Closure), default(Object), default(Object));
					Multiply(default(Closure), default(Object), default(Object));
					MultiplyChecked(default(Closure), default(Object), default(Object));
					NotEqual(default(Closure), default(Object), default(Object));
					Or(default(Closure), default(Object), default(Object));
					Subtract(default(Closure), default(Object), default(Object));
					SubtractChecked(default(Closure), default(Object), default(Object));
				}
			}

			public static object Negate(Closure cloj, object operand)
			{
				return cloj.Box((Int64)unchecked(-cloj.Unbox<Int64>(operand)));
			}
			public static object NegateChecked(Closure cloj, object operand)
			{
				return cloj.Box((Int64)checked(-cloj.Unbox<Int64>(operand)));
			}
			public static object UnaryPlus(Closure cloj, object operand)
			{
				return cloj.Box((Int64)unchecked(+cloj.Unbox<Int64>(operand)));
			}
			public static object Not(Closure cloj, object operand)
			{
				return cloj.Box((Int64)~cloj.Unbox<Int64>(operand));
			}
			public static object Add(Closure cloj, object left, object right)
			{
				return cloj.Box((Int64)unchecked(cloj.Unbox<Int64>(left) + cloj.Unbox<Int64>(right)));
			}
			public static object AddChecked(Closure cloj, object left, object right)
			{
				return cloj.Box((Int64)checked(cloj.Unbox<Int64>(left) + cloj.Unbox<Int64>(right)));
			}
			public static object And(Closure cloj, object left, object right)
			{
				return cloj.Box((Int64)(cloj.Unbox<Int64>(left) & cloj.Unbox<Int64>(right)));
			}
			public static object Divide(Closure cloj, object left, object right)
			{
				return cloj.Box((Int64)(cloj.Unbox<Int64>(left) / cloj.Unbox<Int64>(right)));
			}
			public static object Equal(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Int64>(left) == cloj.Unbox<Int64>(right)));
			}
			public static object ExclusiveOr(Closure cloj, object left, object right)
			{
				return cloj.Box((Int64)(cloj.Unbox<Int64>(left) ^ cloj.Unbox<Int64>(right)));
			}
			public static object GreaterThan(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Int64>(left) > cloj.Unbox<Int64>(right)));
			}
			public static object GreaterThanOrEqual(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Int64>(left) >= cloj.Unbox<Int64>(right)));
			}
			public static object LeftShift(Closure cloj, object left, object right)
			{
				return cloj.Box((Int64)(cloj.Unbox<Int64>(left) << cloj.Unbox<Int32>(right)));
			}
			public static object Power(Closure cloj, object left, object right)
			{
				return cloj.Box((Int64)Math.Pow(cloj.Unbox<Int64>(left), cloj.Unbox<Double>(right)));
			}
			public static object RightShift(Closure cloj, object left, object right)
			{
				return cloj.Box((Int64)(cloj.Unbox<Int64>(left) >> cloj.Unbox<Int32>(right)));
			}
			public static object LessThan(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Int64>(left) < cloj.Unbox<Int64>(right)));
			}
			public static object LessThanOrEqual(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Int64>(left) <= cloj.Unbox<Int64>(right)));
			}
			public static object Modulo(Closure cloj, object left, object right)
			{
				return cloj.Box((Int64)(cloj.Unbox<Int64>(left) % cloj.Unbox<Int64>(right)));
			}
			public static object Multiply(Closure cloj, object left, object right)
			{
				return cloj.Box((Int64)unchecked(cloj.Unbox<Int64>(left) * cloj.Unbox<Int64>(right)));
			}
			public static object MultiplyChecked(Closure cloj, object left, object right)
			{
				return cloj.Box((Int64)checked(cloj.Unbox<Int64>(left) * cloj.Unbox<Int64>(right)));
			}
			public static object NotEqual(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Int64>(left) != cloj.Unbox<Int64>(right)));
			}
			public static object Or(Closure cloj, object left, object right)
			{
				return cloj.Box((Int64)(cloj.Unbox<Int64>(left) | cloj.Unbox<Int64>(right)));
			}
			public static object Subtract(Closure cloj, object left, object right)
			{
				return cloj.Box((Int64)unchecked(cloj.Unbox<Int64>(left) - cloj.Unbox<Int64>(right)));
			}
			public static object SubtractChecked(Closure cloj, object left, object right)
			{
				return cloj.Box((Int64)checked(cloj.Unbox<Int64>(left) - cloj.Unbox<Int64>(right)));
			}
		}
		private static class op_UInt64
		{
			static op_UInt64()
			{
				// AOT
				if (typeof(op_UInt64).Name == string.Empty)
				{
					UnaryPlus(default(Closure), default(Object));
					Not(default(Closure), default(Object));
					Add(default(Closure), default(Object), default(Object));
					AddChecked(default(Closure), default(Object), default(Object));
					And(default(Closure), default(Object), default(Object));
					Divide(default(Closure), default(Object), default(Object));
					Equal(default(Closure), default(Object), default(Object));
					ExclusiveOr(default(Closure), default(Object), default(Object));
					GreaterThan(default(Closure), default(Object), default(Object));
					GreaterThanOrEqual(default(Closure), default(Object), default(Object));
					LeftShift(default(Closure), default(Object), default(Object));
					Power(default(Closure), default(Object), default(Object));
					RightShift(default(Closure), default(Object), default(Object));
					LessThan(default(Closure), default(Object), default(Object));
					LessThanOrEqual(default(Closure), default(Object), default(Object));
					Modulo(default(Closure), default(Object), default(Object));
					Multiply(default(Closure), default(Object), default(Object));
					MultiplyChecked(default(Closure), default(Object), default(Object));
					NotEqual(default(Closure), default(Object), default(Object));
					Or(default(Closure), default(Object), default(Object));
					Subtract(default(Closure), default(Object), default(Object));
					SubtractChecked(default(Closure), default(Object), default(Object));
				}
			}

			public static object UnaryPlus(Closure cloj, object operand)
			{
				return cloj.Box((UInt64)unchecked(+cloj.Unbox<UInt64>(operand)));
			}
			public static object Not(Closure cloj, object operand)
			{
				return cloj.Box((UInt64)~cloj.Unbox<UInt64>(operand));
			}
			public static object Add(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt64)unchecked(cloj.Unbox<UInt64>(left) + cloj.Unbox<UInt64>(right)));
			}
			public static object AddChecked(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt64)checked(cloj.Unbox<UInt64>(left) + cloj.Unbox<UInt64>(right)));
			}
			public static object And(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt64)(cloj.Unbox<UInt64>(left) & cloj.Unbox<UInt64>(right)));
			}
			public static object Divide(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt64)(cloj.Unbox<UInt64>(left) / cloj.Unbox<UInt64>(right)));
			}
			public static object Equal(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<UInt64>(left) == cloj.Unbox<UInt64>(right)));
			}
			public static object ExclusiveOr(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt64)(cloj.Unbox<UInt64>(left) ^ cloj.Unbox<UInt64>(right)));
			}
			public static object GreaterThan(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<UInt64>(left) > cloj.Unbox<UInt64>(right)));
			}
			public static object GreaterThanOrEqual(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<UInt64>(left) >= cloj.Unbox<UInt64>(right)));
			}
			public static object LeftShift(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt64)(cloj.Unbox<UInt64>(left) << cloj.Unbox<Int32>(right)));
			}
			public static object Power(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt64)Math.Pow(cloj.Unbox<UInt64>(left), cloj.Unbox<Double>(right)));
			}
			public static object RightShift(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt64)(cloj.Unbox<UInt64>(left) >> cloj.Unbox<Int32>(right)));
			}
			public static object LessThan(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<UInt64>(left) < cloj.Unbox<UInt64>(right)));
			}
			public static object LessThanOrEqual(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<UInt64>(left) <= cloj.Unbox<UInt64>(right)));
			}
			public static object Modulo(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt64)(cloj.Unbox<UInt64>(left) % cloj.Unbox<UInt64>(right)));
			}
			public static object Multiply(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt64)unchecked(cloj.Unbox<UInt64>(left) * cloj.Unbox<UInt64>(right)));
			}
			public static object MultiplyChecked(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt64)checked(cloj.Unbox<UInt64>(left) * cloj.Unbox<UInt64>(right)));
			}
			public static object NotEqual(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<UInt64>(left) != cloj.Unbox<UInt64>(right)));
			}
			public static object Or(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt64)(cloj.Unbox<UInt64>(left) | cloj.Unbox<UInt64>(right)));
			}
			public static object Subtract(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt64)unchecked(cloj.Unbox<UInt64>(left) - cloj.Unbox<UInt64>(right)));
			}
			public static object SubtractChecked(Closure cloj, object left, object right)
			{
				return cloj.Box((UInt64)checked(cloj.Unbox<UInt64>(left) - cloj.Unbox<UInt64>(right)));
			}
		}
#endif
		private static class op_Single
		{
			static op_Single()
			{
				// AOT
				if (typeof(op_Single).Name == string.Empty)
				{
					Negate(default(Closure), default(Object));
					NegateChecked(default(Closure), default(Object));
					UnaryPlus(default(Closure), default(Object));
					Add(default(Closure), default(Object), default(Object));
					AddChecked(default(Closure), default(Object), default(Object));
					Divide(default(Closure), default(Object), default(Object));
					Equal(default(Closure), default(Object), default(Object));
					GreaterThan(default(Closure), default(Object), default(Object));
					GreaterThanOrEqual(default(Closure), default(Object), default(Object));
					Power(default(Closure), default(Object), default(Object));
					LessThan(default(Closure), default(Object), default(Object));
					LessThanOrEqual(default(Closure), default(Object), default(Object));
					Modulo(default(Closure), default(Object), default(Object));
					Multiply(default(Closure), default(Object), default(Object));
					MultiplyChecked(default(Closure), default(Object), default(Object));
					NotEqual(default(Closure), default(Object), default(Object));
					Subtract(default(Closure), default(Object), default(Object));
					SubtractChecked(default(Closure), default(Object), default(Object));
				}
			}

			public static object Negate(Closure cloj, object operand)
			{
				return cloj.Box((Single)unchecked(-cloj.Unbox<Single>(operand)));
			}
			public static object NegateChecked(Closure cloj, object operand)
			{
				return cloj.Box((Single)checked(-cloj.Unbox<Single>(operand)));
			}
			public static object UnaryPlus(Closure cloj, object operand)
			{
				return cloj.Box((Single)unchecked(+cloj.Unbox<Single>(operand)));
			}
			public static object Add(Closure cloj, object left, object right)
			{
				return cloj.Box((Single)unchecked(cloj.Unbox<Single>(left) + cloj.Unbox<Single>(right)));
			}
			public static object AddChecked(Closure cloj, object left, object right)
			{
				return cloj.Box((Single)checked(cloj.Unbox<Single>(left) + cloj.Unbox<Single>(right)));
			}
			public static object Divide(Closure cloj, object left, object right)
			{
				return cloj.Box((Single)(cloj.Unbox<Single>(left) / cloj.Unbox<Single>(right)));
			}
			public static object Equal(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Single>(left) == cloj.Unbox<Single>(right)));
			}
			public static object GreaterThan(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Single>(left) > cloj.Unbox<Single>(right)));
			}
			public static object GreaterThanOrEqual(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Single>(left) >= cloj.Unbox<Single>(right)));
			}
			public static object Power(Closure cloj, object left, object right)
			{
				return cloj.Box((Single)Math.Pow(cloj.Unbox<Single>(left), cloj.Unbox<Double>(right)));
			}
			public static object LessThan(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Single>(left) < cloj.Unbox<Single>(right)));
			}
			public static object LessThanOrEqual(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Single>(left) <= cloj.Unbox<Single>(right)));
			}
			public static object Modulo(Closure cloj, object left, object right)
			{
				return cloj.Box((Single)(cloj.Unbox<Single>(left) % cloj.Unbox<Single>(right)));
			}
			public static object Multiply(Closure cloj, object left, object right)
			{
				return cloj.Box((Single)unchecked(cloj.Unbox<Single>(left) * cloj.Unbox<Single>(right)));
			}
			public static object MultiplyChecked(Closure cloj, object left, object right)
			{
				return cloj.Box((Single)checked(cloj.Unbox<Single>(left) * cloj.Unbox<Single>(right)));
			}
			public static object NotEqual(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Single>(left) != cloj.Unbox<Single>(right)));
			}
			public static object Subtract(Closure cloj, object left, object right)
			{
				return cloj.Box((Single)unchecked(cloj.Unbox<Single>(left) - cloj.Unbox<Single>(right)));
			}
			public static object SubtractChecked(Closure cloj, object left, object right)
			{
				return cloj.Box((Single)checked(cloj.Unbox<Single>(left) - cloj.Unbox<Single>(right)));
			}
		}
		private static class op_Double
		{
			static op_Double()
			{
				// AOT
				if (typeof(op_Double).Name == string.Empty)
				{
					Negate(default(Closure), default(Object));
					NegateChecked(default(Closure), default(Object));
					UnaryPlus(default(Closure), default(Object));
					Add(default(Closure), default(Object), default(Object));
					AddChecked(default(Closure), default(Object), default(Object));
					Divide(default(Closure), default(Object), default(Object));
					Equal(default(Closure), default(Object), default(Object));
					GreaterThan(default(Closure), default(Object), default(Object));
					GreaterThanOrEqual(default(Closure), default(Object), default(Object));
					Power(default(Closure), default(Object), default(Object));
					LessThan(default(Closure), default(Object), default(Object));
					LessThanOrEqual(default(Closure), default(Object), default(Object));
					Modulo(default(Closure), default(Object), default(Object));
					Multiply(default(Closure), default(Object), default(Object));
					MultiplyChecked(default(Closure), default(Object), default(Object));
					NotEqual(default(Closure), default(Object), default(Object));
					Subtract(default(Closure), default(Object), default(Object));
					SubtractChecked(default(Closure), default(Object), default(Object));
				}
			}

			public static object Negate(Closure cloj, object operand)
			{
				return cloj.Box((Double)unchecked(-cloj.Unbox<Double>(operand)));
			}
			public static object NegateChecked(Closure cloj, object operand)
			{
				return cloj.Box((Double)checked(-cloj.Unbox<Double>(operand)));
			}
			public static object UnaryPlus(Closure cloj, object operand)
			{
				return cloj.Box((Double)unchecked(+cloj.Unbox<Double>(operand)));
			}
			public static object Add(Closure cloj, object left, object right)
			{
				return cloj.Box((Double)unchecked(cloj.Unbox<Double>(left) + cloj.Unbox<Double>(right)));
			}
			public static object AddChecked(Closure cloj, object left, object right)
			{
				return cloj.Box((Double)checked(cloj.Unbox<Double>(left) + cloj.Unbox<Double>(right)));
			}
			public static object Divide(Closure cloj, object left, object right)
			{
				return cloj.Box((Double)(cloj.Unbox<Double>(left) / cloj.Unbox<Double>(right)));
			}
			public static object Equal(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Double>(left) == cloj.Unbox<Double>(right)));
			}
			public static object GreaterThan(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Double>(left) > cloj.Unbox<Double>(right)));
			}
			public static object GreaterThanOrEqual(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Double>(left) >= cloj.Unbox<Double>(right)));
			}
			public static object Power(Closure cloj, object left, object right)
			{
				return cloj.Box((Double)Math.Pow(cloj.Unbox<Double>(left), cloj.Unbox<Double>(right)));
			}
			public static object LessThan(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Double>(left) < cloj.Unbox<Double>(right)));
			}
			public static object LessThanOrEqual(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Double>(left) <= cloj.Unbox<Double>(right)));
			}
			public static object Modulo(Closure cloj, object left, object right)
			{
				return cloj.Box((Double)(cloj.Unbox<Double>(left) % cloj.Unbox<Double>(right)));
			}
			public static object Multiply(Closure cloj, object left, object right)
			{
				return cloj.Box((Double)unchecked(cloj.Unbox<Double>(left) * cloj.Unbox<Double>(right)));
			}
			public static object MultiplyChecked(Closure cloj, object left, object right)
			{
				return cloj.Box((Double)checked(cloj.Unbox<Double>(left) * cloj.Unbox<Double>(right)));
			}
			public static object NotEqual(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Double>(left) != cloj.Unbox<Double>(right)));
			}
			public static object Subtract(Closure cloj, object left, object right)
			{
				return cloj.Box((Double)unchecked(cloj.Unbox<Double>(left) - cloj.Unbox<Double>(right)));
			}
			public static object SubtractChecked(Closure cloj, object left, object right)
			{
				return cloj.Box((Double)checked(cloj.Unbox<Double>(left) - cloj.Unbox<Double>(right)));
			}
		}
		private static class op_Decimal
		{
			static op_Decimal()
			{
				// AOT
				if (typeof(op_Decimal).Name == string.Empty)
				{
					Negate(default(Closure), default(Object));
					NegateChecked(default(Closure), default(Object));
					UnaryPlus(default(Closure), default(Object));
					Add(default(Closure), default(Object), default(Object));
					AddChecked(default(Closure), default(Object), default(Object));
					Divide(default(Closure), default(Object), default(Object));
					Equal(default(Closure), default(Object), default(Object));
					GreaterThan(default(Closure), default(Object), default(Object));
					GreaterThanOrEqual(default(Closure), default(Object), default(Object));
					Power(default(Closure), default(Object), default(Object));
					LessThan(default(Closure), default(Object), default(Object));
					LessThanOrEqual(default(Closure), default(Object), default(Object));
					Modulo(default(Closure), default(Object), default(Object));
					Multiply(default(Closure), default(Object), default(Object));
					MultiplyChecked(default(Closure), default(Object), default(Object));
					NotEqual(default(Closure), default(Object), default(Object));
					Subtract(default(Closure), default(Object), default(Object));
					SubtractChecked(default(Closure), default(Object), default(Object));
				}
			}

			public static object Negate(Closure cloj, object operand)
			{
				return cloj.Box((Decimal)unchecked(-cloj.Unbox<Decimal>(operand)));
			}
			public static object NegateChecked(Closure cloj, object operand)
			{
				return cloj.Box((Decimal)checked(-cloj.Unbox<Decimal>(operand)));
			}
			public static object UnaryPlus(Closure cloj, object operand)
			{
				return cloj.Box((Decimal)unchecked(+cloj.Unbox<Decimal>(operand)));
			}
			public static object Add(Closure cloj, object left, object right)
			{
				return cloj.Box((Decimal)unchecked(cloj.Unbox<Decimal>(left) + cloj.Unbox<Decimal>(right)));
			}
			public static object AddChecked(Closure cloj, object left, object right)
			{
				return cloj.Box((Decimal)checked(cloj.Unbox<Decimal>(left) + cloj.Unbox<Decimal>(right)));
			}
			public static object Divide(Closure cloj, object left, object right)
			{
				return cloj.Box((Decimal)(cloj.Unbox<Decimal>(left) / cloj.Unbox<Decimal>(right)));
			}
			public static object Equal(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Decimal>(left) == cloj.Unbox<Decimal>(right)));
			}
			public static object GreaterThan(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Decimal>(left) > cloj.Unbox<Decimal>(right)));
			}
			public static object GreaterThanOrEqual(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Decimal>(left) >= cloj.Unbox<Decimal>(right)));
			}
			public static object Power(Closure cloj, object left, object right)
			{
				return cloj.Box((Decimal)Math.Pow((double)cloj.Unbox<Decimal>(left), cloj.Unbox<Double>(right)));
			}
			public static object LessThan(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Decimal>(left) < cloj.Unbox<Decimal>(right)));
			}
			public static object LessThanOrEqual(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Decimal>(left) <= cloj.Unbox<Decimal>(right)));
			}
			public static object Modulo(Closure cloj, object left, object right)
			{
				return cloj.Box((Decimal)(cloj.Unbox<Decimal>(left) % cloj.Unbox<Decimal>(right)));
			}
			public static object Multiply(Closure cloj, object left, object right)
			{
				return cloj.Box((Decimal)unchecked(cloj.Unbox<Decimal>(left) * cloj.Unbox<Decimal>(right)));
			}
			public static object MultiplyChecked(Closure cloj, object left, object right)
			{
				return cloj.Box((Decimal)checked(cloj.Unbox<Decimal>(left) * cloj.Unbox<Decimal>(right)));
			}
			public static object NotEqual(Closure cloj, object left, object right)
			{
				return cloj.Box((Boolean)(cloj.Unbox<Decimal>(left) != cloj.Unbox<Decimal>(right)));
			}
			public static object Subtract(Closure cloj, object left, object right)
			{
				return cloj.Box((Decimal)unchecked(cloj.Unbox<Decimal>(left) - cloj.Unbox<Decimal>(right)));
			}
			public static object SubtractChecked(Closure cloj, object left, object right)
			{
				return cloj.Box((Decimal)checked(cloj.Unbox<Decimal>(left) - cloj.Unbox<Decimal>(right)));
			}
		}
		private static class op_Object
		{
			static op_Object()
			{
				// AOT
				if (typeof(op_Object).Name == string.Empty)
				{
					Equal(default(Closure), default(Object), default(Object));
					NotEqual(default(Closure), default(Object), default(Object));
				}
			}

			public static object Equal(Closure cloj, object left, object right)
			{
				return cloj.Box(Equals(cloj.Unbox<Object>(left), cloj.Unbox<Object>(right)));
			}
			public static object NotEqual(Closure cloj, object left, object right)
			{
				return cloj.Box(!Equals(cloj.Unbox<Object>(left), cloj.Unbox<Object>(right)));
			}
		}
	}
}
