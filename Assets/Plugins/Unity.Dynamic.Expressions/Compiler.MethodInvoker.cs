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
using System.Reflection;
using System.Runtime.CompilerServices;

// ReSharper disable PossibleNullReferenceException

namespace Unity.Dynamic.Expressions
{
	partial class Compiler
	{
		private delegate object InvokeOperation(Closure cloj, Func<Closure, object>[] argumentFns);

		private class MethodInvoker
		{
			private readonly Delegate fn;

			private MethodInvoker(Type delegateType, MethodInfo method)
			{
				if (delegateType == null) throw new ArgumentNullException("method");
				if (method == null) throw new ArgumentNullException("method");

				this.fn = Delegate.CreateDelegate(delegateType, method, true);
			}

			private object FuncInvoker<ResultT>(Closure cloj, Func<Closure, object>[] argumentFns)
			{
				return ((Func<ResultT>)this.fn).Invoke();
			}
			private object FuncInvoker<Arg1T, ResultT>(Closure cloj, Func<Closure, object>[] argumentFns)
			{
				var arg1 = cloj.Unbox<Arg1T>(argumentFns[0](cloj));
				return ((Func<Arg1T, ResultT>)this.fn).Invoke(arg1);
			}
			private object FuncInvoker<Arg1T, Arg2T, ResultT>(Closure cloj, Func<Closure, object>[] argumentFns)
			{
				var arg1 = cloj.Unbox<Arg1T>(argumentFns[0](cloj));
				var arg2 = cloj.Unbox<Arg2T>(argumentFns[1](cloj));
				return ((Func<Arg1T, Arg2T, ResultT>)this.fn).Invoke(arg1, arg2);
			}
			private object FuncInvoker<Arg1T, Arg2T, Arg3T, ResultT>(Closure cloj, Func<Closure, object>[] argumentFns)
			{
				var arg1 = cloj.Unbox<Arg1T>(argumentFns[0](cloj));
				var arg2 = cloj.Unbox<Arg2T>(argumentFns[1](cloj));
				var arg3 = cloj.Unbox<Arg3T>(argumentFns[2](cloj));
				return ((Func<Arg1T, Arg2T, Arg3T, ResultT>)this.fn).Invoke(arg1, arg2, arg3);
			}

			public static InvokeOperation TryCreate(MethodInfo method)
			{
				if (method == null) throw new ArgumentNullException("method");

				if (!method.IsStatic)
					return null;

				var parameters = method.GetParameters();
				switch (parameters.Length)
				{
					case 0:
						return
							TryCreate<Boolean>(method, parameters) ??
							TryCreate<Byte>(method, parameters) ??
							TryCreate<SByte>(method, parameters) ??
							TryCreate<Int16>(method, parameters) ??
							TryCreate<UInt16>(method, parameters) ??
							TryCreate<Int32>(method, parameters) ??
							TryCreate<UInt32>(method, parameters) ??
							TryCreate<Int64>(method, parameters) ??
							TryCreate<UInt64>(method, parameters) ??
							TryCreate<Single>(method, parameters) ??
							TryCreate<Double>(method, parameters) ??
							TryCreate<Decimal>(method, parameters) ??
							TryCreate<String>(method, parameters) ??
							TryCreate<Object>(method, parameters) ??
							TryCreate<TimeSpan>(method, parameters) ??
							TryCreate<DateTime>(method, parameters);
					case 1:
						return
							TryCreate<Boolean, Boolean>(method, parameters) ??
							TryCreate<Byte, Byte>(method, parameters) ??
							TryCreate<SByte, SByte>(method, parameters) ??
							TryCreate<Int16, Int16>(method, parameters) ??
							TryCreate<UInt16, UInt16>(method, parameters) ??
							TryCreate<Int32, Int32>(method, parameters) ??
							TryCreate<UInt32, UInt32>(method, parameters) ??
							TryCreate<Int64, Int64>(method, parameters) ??
							TryCreate<UInt64, UInt64>(method, parameters) ??
							TryCreate<Single, Single>(method, parameters) ??
							TryCreate<Double, Double>(method, parameters) ??
							TryCreate<Decimal, Decimal>(method, parameters) ??
							TryCreate<String, String>(method, parameters) ??
							TryCreate<Object, Object>(method, parameters) ??
							TryCreate<TimeSpan, TimeSpan>(method, parameters) ??
							TryCreate<DateTime, DateTime>(method, parameters) ??
							TryCreate<Byte, Boolean>(method, parameters) ??
							TryCreate<SByte, Boolean>(method, parameters) ??
							TryCreate<Int16, Boolean>(method, parameters) ??
							TryCreate<UInt16, Boolean>(method, parameters) ??
							TryCreate<Int32, Boolean>(method, parameters) ??
							TryCreate<UInt32, Boolean>(method, parameters) ??
							TryCreate<Int64, Boolean>(method, parameters) ??
							TryCreate<UInt64, Boolean>(method, parameters) ??
							TryCreate<Single, Boolean>(method, parameters) ??
							TryCreate<Double, Boolean>(method, parameters) ??
							TryCreate<Decimal, Boolean>(method, parameters) ??
							TryCreate<String, Boolean>(method, parameters) ??
							TryCreate<TimeSpan, Boolean>(method, parameters) ??
							TryCreate<DateTime, Boolean>(method, parameters) ??
							TryCreate<Object, Boolean>(method, parameters);

					case 2:
						return
							TryCreate<Boolean, Boolean, Boolean>(method, parameters) ??
							TryCreate<Byte, Byte, Byte>(method, parameters) ??
							TryCreate<SByte, SByte, SByte>(method, parameters) ??
							TryCreate<Int16, Int16, Int16>(method, parameters) ??
							TryCreate<UInt16, UInt16, UInt16>(method, parameters) ??
							TryCreate<Int32, Int32, Int32>(method, parameters) ??
							TryCreate<UInt32, UInt32, UInt32>(method, parameters) ??
							TryCreate<Int64, Int64, Int64>(method, parameters) ??
							TryCreate<UInt64, UInt64, UInt64>(method, parameters) ??
							TryCreate<Single, Single, Single>(method, parameters) ??
							TryCreate<Double, Double, Double>(method, parameters) ??
							TryCreate<Decimal, Decimal, Decimal>(method, parameters) ??
							TryCreate<String, String, String>(method, parameters) ??
							TryCreate<Object, Object, Object>(method, parameters) ??
							TryCreate<TimeSpan, TimeSpan, TimeSpan>(method, parameters) ??
							TryCreate<DateTime, DateTime, DateTime>(method, parameters) ??
							TryCreate<Byte, Byte, Boolean>(method, parameters) ??
							TryCreate<SByte, SByte, Boolean>(method, parameters) ??
							TryCreate<Int16, Int16, Boolean>(method, parameters) ??
							TryCreate<UInt16, UInt16, Boolean>(method, parameters) ??
							TryCreate<Int32, Int32, Boolean>(method, parameters) ??
							TryCreate<UInt32, UInt32, Boolean>(method, parameters) ??
							TryCreate<Int64, Int64, Boolean>(method, parameters) ??
							TryCreate<UInt64, UInt64, Boolean>(method, parameters) ??
							TryCreate<Single, Single, Boolean>(method, parameters) ??
							TryCreate<Double, Double, Boolean>(method, parameters) ??
							TryCreate<Decimal, Decimal, Boolean>(method, parameters) ??
							TryCreate<String, String, Boolean>(method, parameters) ??
							TryCreate<Object, Object, Boolean>(method, parameters) ??
							TryCreate<TimeSpan, TimeSpan, Boolean>(method, parameters) ??
							TryCreate<DateTime, DateTime, Boolean>(method, parameters); ;

					case 3:
						return
							TryCreate<Boolean, Boolean, Boolean, Boolean>(method, parameters) ??
							TryCreate<Byte, Byte, Byte, Byte>(method, parameters) ??
							TryCreate<SByte, SByte, SByte>(method, parameters) ??
							TryCreate<SByte, Int16, Int16, Int16>(method, parameters) ??
							TryCreate<UInt16, UInt16, UInt16>(method, parameters) ??
							TryCreate<UInt16, Int32, Int32, Int32>(method, parameters) ??
							TryCreate<UInt32, UInt32, UInt32>(method, parameters) ??
							TryCreate<UInt32, Int64, Int64, Int64>(method, parameters) ??
							TryCreate<UInt64, UInt64, UInt64>(method, parameters) ??
							TryCreate<UInt64, Single, Single, Single>(method, parameters) ??
							TryCreate<Double, Double, Double, Double>(method, parameters) ??
							TryCreate<Decimal, Decimal, Decimal, Decimal>(method, parameters) ??
							TryCreate<Double, String, String, String>(method, parameters) ??
							TryCreate<Decimal, String, String, String>(method, parameters) ??
							TryCreate<Object, Object, Object, Object>(method, parameters) ??
							TryCreate<TimeSpan, TimeSpan, TimeSpan, TimeSpan>(method, parameters) ??
							TryCreate<DateTime, DateTime, DateTime, DateTime>(method, parameters) ??
							TryCreate<Byte, Byte, Byte, Boolean>(method, parameters) ??
							TryCreate<SByte, SByte, SByte, Boolean>(method, parameters) ??
							TryCreate<SByte, Int16, Int16, Boolean>(method, parameters) ??
							TryCreate<UInt16, UInt16, Boolean>(method, parameters) ??
							TryCreate<UInt16, Int32, Int32, Boolean>(method, parameters) ??
							TryCreate<UInt32, UInt32, UInt32, Boolean>(method, parameters) ??
							TryCreate<UInt32, Int64, Int64, Boolean>(method, parameters) ??
							TryCreate<UInt64, UInt64, UInt64, Boolean>(method, parameters) ??
							TryCreate<Single, Single, Single, Boolean>(method, parameters) ??
							TryCreate<Double, Double, Double, Boolean>(method, parameters) ??
							TryCreate<Decimal, Decimal, Decimal, Boolean>(method, parameters) ??
							TryCreate<String, String, String, Boolean>(method, parameters) ??
							TryCreate<Object, Object, Object, Boolean>(method, parameters) ??
							TryCreate<TimeSpan, TimeSpan, TimeSpan, Boolean>(method, parameters) ??
							TryCreate<DateTime, DateTime, DateTime, Boolean>(method, parameters);
					default:
						return null;
				}
			}

			[MethodImpl(MethodImplOptions.NoOptimization)]
			private static InvokeOperation TryCreate<ResultT>(MethodInfo method, ParameterInfo[] parameters)
			{
				if (method == null) throw new ArgumentNullException("method");
				if (parameters == null) throw new ArgumentNullException("parameters");

				if (parameters.Length != 0 || method.ReturnType != typeof(ResultT))
					return null;

				var inv = new MethodInvoker(typeof(Func<ResultT>), method);

                // never happens, just for AOT
#pragma warning disable 1720
                if (parameters.Length == int.MaxValue)
				{
					inv.FuncInvoker<ResultT>(null, null);
					((Func<ResultT>)null).Invoke();
				}
#pragma warning restore 1720

                return inv.FuncInvoker<ResultT>;
			}
			[MethodImpl(MethodImplOptions.NoOptimization)]
			private static InvokeOperation TryCreate<Arg1T, ResultT>(MethodInfo method, ParameterInfo[] parameters)
			{
				if (method == null) throw new ArgumentNullException("method");
				if (parameters == null) throw new ArgumentNullException("parameters");

				if (parameters.Length != 1 || method.ReturnType != typeof(ResultT) || parameters[0].ParameterType != typeof(Arg1T))
					return null;

				var inv = new MethodInvoker(typeof(Func<Arg1T, ResultT>), method);

                // never happens, just for AOT
#pragma warning disable 1720
                if (parameters.Length == int.MaxValue)
				{
					inv.FuncInvoker<Arg1T, ResultT>(null, null);
					((Func<Arg1T, ResultT>)null).Invoke(default(Arg1T));
				}
#pragma warning restore 1720

                return inv.FuncInvoker<Arg1T, ResultT>;
			}
			[MethodImpl(MethodImplOptions.NoOptimization)]
			private static InvokeOperation TryCreate<Arg1T, Arg2T, ResultT>(MethodInfo method, ParameterInfo[] parameters)
			{
				if (method == null) throw new ArgumentNullException("method");
				if (parameters == null) throw new ArgumentNullException("parameters");

				if (parameters.Length != 2 || method.ReturnType != typeof(ResultT) || parameters[0].ParameterType != typeof(Arg1T) ||
					parameters[1].ParameterType != typeof(Arg2T))
					return null;

				var inv = new MethodInvoker(typeof(Func<Arg1T, Arg2T, ResultT>), method);

                // never happens, just for AOT
#pragma warning disable 1720
                if (parameters.Length == int.MaxValue)
				{
					inv.FuncInvoker<Arg1T, Arg2T, ResultT>(null, null);
					((Func<Arg1T, Arg2T, ResultT>)null).Invoke(default(Arg1T), default(Arg2T));
				}
#pragma warning restore 1720

                return inv.FuncInvoker<Arg1T, Arg2T, ResultT>;
			}
			[MethodImpl(MethodImplOptions.NoOptimization)]
			private static InvokeOperation TryCreate<Arg1T, Arg2T, Arg3T, ResultT>(MethodInfo method, ParameterInfo[] parameters)
			{
				if (method == null) throw new ArgumentNullException("method");
				if (parameters == null) throw new ArgumentNullException("parameters");

				if (parameters.Length != 3 || method.ReturnType != typeof(ResultT) || parameters[0].ParameterType != typeof(Arg1T) ||
					parameters[1].ParameterType != typeof(Arg2T) || parameters[2].ParameterType != typeof(Arg3T))
					return null;

				var inv = new MethodInvoker(typeof(Func<Arg1T, Arg2T, Arg3T, ResultT>), method);

                // never happens, just for AOT
#pragma warning disable 1720
                if (parameters.Length == int.MaxValue)
				{
					inv.FuncInvoker<Arg1T, Arg2T, Arg3T, ResultT>(null, null);
					((Func<Arg1T, Arg2T, Arg3T, ResultT>)null).Invoke(default(Arg1T), default(Arg2T), default(Arg3T));
				}
#pragma warning restore 1720

                return inv.FuncInvoker<Arg1T, Arg2T, Arg3T, ResultT>;
			}
		}
	}
}
