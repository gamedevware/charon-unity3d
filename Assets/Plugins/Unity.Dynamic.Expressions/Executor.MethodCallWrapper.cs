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
	partial class Executor
	{
		private delegate object InvokeOperation(Closure closure, Func<Closure, object>[] argumentFns);

		private class MethodCallWrapper
		{
			private readonly Delegate fn;

			private MethodCallWrapper(Type delegateType, MethodInfo method)
			{
				if (delegateType == null) throw new ArgumentNullException("method");
				if (method == null) throw new ArgumentNullException("method");

				this.fn = Delegate.CreateDelegate(delegateType, method, true);
			}

			private object FuncInvoker<ResultT>(Closure closure, Func<Closure, object>[] argumentFns)
			{
				return ((Func<ResultT>)this.fn).Invoke();
			}
			private object FuncInvoker<Arg1T, ResultT>(Closure closure, Func<Closure, object>[] argumentFns)
			{
				var arg1 = closure.Unbox<Arg1T>(argumentFns[0](closure));
				return ((Func<Arg1T, ResultT>)this.fn).Invoke(arg1);
			}
			private object FuncInvoker<Arg1T, Arg2T, ResultT>(Closure closure, Func<Closure, object>[] argumentFns)
			{
				var arg1 = closure.Unbox<Arg1T>(argumentFns[0](closure));
				var arg2 = closure.Unbox<Arg2T>(argumentFns[1](closure));
				return ((Func<Arg1T, Arg2T, ResultT>)this.fn).Invoke(arg1, arg2);
			}
			private object FuncInvoker<Arg1T, Arg2T, Arg3T, ResultT>(Closure closure, Func<Closure, object>[] argumentFns)
			{
				var arg1 = closure.Unbox<Arg1T>(argumentFns[0](closure));
				var arg2 = closure.Unbox<Arg2T>(argumentFns[1](closure));
				var arg3 = closure.Unbox<Arg3T>(argumentFns[2](closure));
				return ((Func<Arg1T, Arg2T, Arg3T, ResultT>)this.fn).Invoke(arg1, arg2, arg3);
			}
			private object FuncInvoker<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>(Closure closure, Func<Closure, object>[] argumentFns)
			{
				var arg1 = closure.Unbox<Arg1T>(argumentFns[0](closure));
				var arg2 = closure.Unbox<Arg2T>(argumentFns[1](closure));
				var arg3 = closure.Unbox<Arg3T>(argumentFns[2](closure));
				var arg4 = closure.Unbox<Arg4T>(argumentFns[3](closure));
				return ((Func<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>)this.fn).Invoke(arg1, arg2, arg3, arg4);
			}

			public static InvokeOperation TryCreate(MethodInfo method)
			{
				if (method == null) throw new ArgumentNullException("method");

				if (!method.IsStatic)
					return null;

				// TODO caching for method wrappers

				var parameters = method.GetParameters();
				var invoker = default(InvokeOperation);
				// ReSharper disable once SwitchStatementMissingSomeCases
				switch (parameters.Length)
				{
					case 0:
						invoker =
							TryCreate<bool>(method, parameters) ??
							TryCreate<byte>(method, parameters) ??
							TryCreate<sbyte>(method, parameters) ??
							TryCreate<short>(method, parameters) ??
							TryCreate<ushort>(method, parameters) ??
							TryCreate<int>(method, parameters) ??
							TryCreate<uint>(method, parameters) ??
							TryCreate<long>(method, parameters) ??
							TryCreate<ulong>(method, parameters) ??
							TryCreate<float>(method, parameters) ??
							TryCreate<double>(method, parameters) ??
							TryCreate<decimal>(method, parameters) ??
							TryCreate<string>(method, parameters) ??
							TryCreate<object>(method, parameters) ??
							TryCreate<TimeSpan>(method, parameters) ??
							TryCreate<DateTime>(method, parameters);
						break;
					case 1:
						invoker =
							TryCreate<bool, bool>(method, parameters) ??
							TryCreate<byte, byte>(method, parameters) ??
							TryCreate<sbyte, sbyte>(method, parameters) ??
							TryCreate<short, short>(method, parameters) ??
							TryCreate<ushort, ushort>(method, parameters) ??
							TryCreate<int, int>(method, parameters) ??
							TryCreate<uint, long>(method, parameters) ??
							TryCreate<long, long>(method, parameters) ??
							TryCreate<ulong, ulong>(method, parameters) ??
							TryCreate<float, float>(method, parameters) ??
							TryCreate<double, double>(method, parameters) ??
							TryCreate<decimal, decimal>(method, parameters) ??
							TryCreate<string, string>(method, parameters) ??
							TryCreate<object, object>(method, parameters) ??
							TryCreate<TimeSpan, TimeSpan>(method, parameters) ??
							TryCreate<DateTime, TimeSpan>(method, parameters) ??
							TryCreate<byte, bool>(method, parameters) ??
							TryCreate<sbyte, bool>(method, parameters) ??
							TryCreate<short, bool>(method, parameters) ??
							TryCreate<ushort, bool>(method, parameters) ??
							TryCreate<int, bool>(method, parameters) ??
							TryCreate<uint, bool>(method, parameters) ??
							TryCreate<long, bool>(method, parameters) ??
							TryCreate<ulong, bool>(method, parameters) ??
							TryCreate<float, bool>(method, parameters) ??
							TryCreate<double, bool>(method, parameters) ??
							TryCreate<decimal, bool>(method, parameters) ??
							TryCreate<string, bool>(method, parameters) ??
							TryCreate<object, bool>(method, parameters) ??
							TryCreate<object, string>(method, parameters) ??
							TryCreate<TimeSpan, bool>(method, parameters) ??
							TryCreate<DateTime, bool>(method, parameters);
						break;
					case 2:
						invoker =
							TryCreate<bool, bool, bool>(method, parameters) ??
							TryCreate<byte, byte, byte>(method, parameters) ??
							TryCreate<sbyte, sbyte, sbyte>(method, parameters) ??
							TryCreate<short, short, short>(method, parameters) ??
							TryCreate<ushort, ushort, ushort>(method, parameters) ??
							TryCreate<int, int, int>(method, parameters) ??
							TryCreate<uint, uint, long>(method, parameters) ??
							TryCreate<long, long, long>(method, parameters) ??
							TryCreate<ulong, ulong, ulong>(method, parameters) ??
							TryCreate<float, float, float>(method, parameters) ??
							TryCreate<double, double, double>(method, parameters) ??
							TryCreate<decimal, decimal, decimal>(method, parameters) ??
							TryCreate<string, string, string>(method, parameters) ??
							TryCreate<object, object, object>(method, parameters) ??
							TryCreate<TimeSpan, TimeSpan, TimeSpan>(method, parameters) ??
							TryCreate<DateTime, DateTime, TimeSpan>(method, parameters) ??
							TryCreate<byte, byte, bool>(method, parameters) ??
							TryCreate<sbyte, sbyte, bool>(method, parameters) ??
							TryCreate<short, short, bool>(method, parameters) ??
							TryCreate<ushort, ushort, bool>(method, parameters) ??
							TryCreate<int, int, bool>(method, parameters) ??
							TryCreate<uint, uint, bool>(method, parameters) ??
							TryCreate<long, long, bool>(method, parameters) ??
							TryCreate<ulong, ulong, bool>(method, parameters) ??
							TryCreate<float, float, bool>(method, parameters) ??
							TryCreate<double, double, bool>(method, parameters) ??
							TryCreate<decimal, decimal, bool>(method, parameters) ??
							TryCreate<string, string, bool>(method, parameters) ??
							TryCreate<object, object, bool>(method, parameters) ??
							TryCreate<object, object, string>(method, parameters) ??
							TryCreate<TimeSpan, TimeSpan, bool>(method, parameters) ??
							TryCreate<DateTime, DateTime, bool>(method, parameters);
						break;
					case 3:
						invoker =
							TryCreate<bool, bool, bool, bool>(method, parameters) ??
							TryCreate<byte, byte, byte, byte>(method, parameters) ??
							TryCreate<sbyte, sbyte, sbyte, sbyte>(method, parameters) ??
							TryCreate<short, short, short, short>(method, parameters) ??
							TryCreate<ushort, ushort, ushort, ushort>(method, parameters) ??
							TryCreate<int, int, int, int>(method, parameters) ??
							TryCreate<uint, uint, uint, long>(method, parameters) ??
							TryCreate<long, long, long, long>(method, parameters) ??
							TryCreate<ulong, ulong, ulong, ulong>(method, parameters) ??
							TryCreate<float, float, float, float>(method, parameters) ??
							TryCreate<double, double, double, double>(method, parameters) ??
							TryCreate<decimal, decimal, decimal, decimal>(method, parameters) ??
							TryCreate<string, string, string, string>(method, parameters) ??
							TryCreate<object, object, object, object>(method, parameters) ??
							TryCreate<byte, byte, byte, bool>(method, parameters) ??
							TryCreate<sbyte, sbyte, sbyte, bool>(method, parameters) ??
							TryCreate<short, short, short, bool>(method, parameters) ??
							TryCreate<ushort, ushort, ushort, bool>(method, parameters) ??
							TryCreate<int, int, int, bool>(method, parameters) ??
							TryCreate<uint, uint, uint, bool>(method, parameters) ??
							TryCreate<long, long, long, bool>(method, parameters) ??
							TryCreate<ulong, ulong, ulong, bool>(method, parameters) ??
							TryCreate<float, float, float, bool>(method, parameters) ??
							TryCreate<double, double, double, bool>(method, parameters) ??
							TryCreate<decimal, decimal, decimal, bool>(method, parameters) ??
							TryCreate<string, string, string, bool>(method, parameters) ??
							TryCreate<object, object, object, bool>(method, parameters) ??
							TryCreate<object, object, object, string>(method, parameters);
						break;
					case 4:
						invoker =
							TryCreate<bool, bool, bool, bool, bool>(method, parameters) ??
							TryCreate<byte, byte, byte, byte, byte>(method, parameters) ??
							TryCreate<sbyte, sbyte, sbyte, sbyte, sbyte>(method, parameters) ??
							TryCreate<sbyte, short, short, short, short>(method, parameters) ??
							TryCreate<ushort, ushort, ushort, ushort, ushort>(method, parameters) ??
							TryCreate<int, int, int, int, int>(method, parameters) ??
							TryCreate<uint, uint, uint, uint, long>(method, parameters) ??
							TryCreate<long, long, long, long, long>(method, parameters) ??
							TryCreate<ulong, ulong, ulong, ulong, ulong>(method, parameters) ??
							TryCreate<float, float, float, float, float>(method, parameters) ??
							TryCreate<double, double, double, double, double>(method, parameters) ??
							TryCreate<decimal, decimal, decimal, decimal, decimal>(method, parameters) ??
							TryCreate<double, string, string, string, string>(method, parameters) ??
							TryCreate<object, object, object, object, object>(method, parameters) ??
							TryCreate<byte, byte, byte, byte, bool>(method, parameters) ??
							TryCreate<sbyte, sbyte, sbyte, sbyte, bool>(method, parameters) ??
							TryCreate<short, short, short, short, bool>(method, parameters) ??
							TryCreate<ushort, ushort, ushort, ushort, bool>(method, parameters) ??
							TryCreate<int, int, int, int, bool>(method, parameters) ??
							TryCreate<uint, uint, uint, uint, bool>(method, parameters) ??
							TryCreate<long, long, long, long, bool>(method, parameters) ??
							TryCreate<ulong, ulong, ulong, ulong, bool>(method, parameters) ??
							TryCreate<float, float, float, float, bool>(method, parameters) ??
							TryCreate<double, double, double, double, bool>(method, parameters) ??
							TryCreate<decimal, decimal, decimal, decimal, bool>(method, parameters) ??
							TryCreate<string, string, string, string, bool>(method, parameters) ??
							TryCreate<object, object, object, object, bool>(method, parameters) ??
							TryCreate<object, object, object, object, string>(method, parameters);
						break;
				}
				return invoker;
			}

			private static InvokeOperation TryCreate<ResultT>(MethodInfo method, ParameterInfo[] parameters)
			{
				if (method == null) throw new ArgumentNullException("method");
				if (parameters == null) throw new ArgumentNullException("parameters");

				if (parameters.Length != 0 || method.ReturnType != typeof(ResultT))
					return null;

				var wrapper = new MethodCallWrapper(typeof(Func<ResultT>), method);

				// never happens, just for AOT
#pragma warning disable 1720
				if (parameters.Length == int.MaxValue)
				{
					wrapper.FuncInvoker<ResultT>(null, null);
					((Func<ResultT>)null).Invoke();
				}
#pragma warning restore 1720

				return wrapper.FuncInvoker<ResultT>;
			}
			private static InvokeOperation TryCreate<Arg1T, ResultT>(MethodInfo method, ParameterInfo[] parameters)
			{
				if (method == null) throw new ArgumentNullException("method");
				if (parameters == null) throw new ArgumentNullException("parameters");

				if (parameters.Length != 1 || method.ReturnType != typeof(ResultT) || parameters[0].ParameterType != typeof(Arg1T))
					return null;

				var wrapper = new MethodCallWrapper(typeof(Func<Arg1T, ResultT>), method);

				// never happens, just for AOT
#pragma warning disable 1720
				if (parameters.Length == int.MaxValue)
				{
					wrapper.FuncInvoker<Arg1T, ResultT>(null, null);
					((Func<Arg1T, ResultT>)null).Invoke(default(Arg1T));
				}
#pragma warning restore 1720

				return wrapper.FuncInvoker<Arg1T, ResultT>;
			}
			private static InvokeOperation TryCreate<Arg1T, Arg2T, ResultT>(MethodInfo method, ParameterInfo[] parameters)
			{
				if (method == null) throw new ArgumentNullException("method");
				if (parameters == null) throw new ArgumentNullException("parameters");

				if (parameters.Length != 2 || method.ReturnType != typeof(ResultT) || parameters[0].ParameterType != typeof(Arg1T) ||
					parameters[1].ParameterType != typeof(Arg2T))
					return null;

				var wrapper = new MethodCallWrapper(typeof(Func<Arg1T, Arg2T, ResultT>), method);

				// never happens, just for AOT
#pragma warning disable 1720
				if (parameters.Length == int.MaxValue)
				{
					wrapper.FuncInvoker<Arg1T, Arg2T, ResultT>(null, null);
					((Func<Arg1T, Arg2T, ResultT>)null).Invoke(default(Arg1T), default(Arg2T));
				}
#pragma warning restore 1720

				return wrapper.FuncInvoker<Arg1T, Arg2T, ResultT>;
			}
			private static InvokeOperation TryCreate<Arg1T, Arg2T, Arg3T, ResultT>(MethodInfo method, ParameterInfo[] parameters)
			{
				if (method == null) throw new ArgumentNullException("method");
				if (parameters == null) throw new ArgumentNullException("parameters");

				if (parameters.Length != 3 || method.ReturnType != typeof(ResultT) || parameters[0].ParameterType != typeof(Arg1T) ||
					parameters[1].ParameterType != typeof(Arg2T) || parameters[2].ParameterType != typeof(Arg3T))
					return null;

				var wrapper = new MethodCallWrapper(typeof(Func<Arg1T, Arg2T, Arg3T, ResultT>), method);

				// never happens, just for AOT
#pragma warning disable 1720
				if (parameters.Length == int.MaxValue)
				{
					wrapper.FuncInvoker<Arg1T, Arg2T, Arg3T, ResultT>(null, null);
					((Func<Arg1T, Arg2T, Arg3T, ResultT>)null).Invoke(default(Arg1T), default(Arg2T), default(Arg3T));
				}
#pragma warning restore 1720

				return wrapper.FuncInvoker<Arg1T, Arg2T, Arg3T, ResultT>;
			}
			private static InvokeOperation TryCreate<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>(MethodInfo method, ParameterInfo[] parameters)
			{
				if (method == null) throw new ArgumentNullException("method");
				if (parameters == null) throw new ArgumentNullException("parameters");

				if (parameters.Length != 4 || method.ReturnType != typeof(ResultT) || parameters[0].ParameterType != typeof(Arg1T) ||
					parameters[1].ParameterType != typeof(Arg2T) || parameters[2].ParameterType != typeof(Arg3T) || parameters[3].ParameterType != typeof(Arg4T))
					return null;

				var wrapper = new MethodCallWrapper(typeof(Func<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>), method);

				// never happens, just for AOT
#pragma warning disable 1720
				if (parameters.Length == int.MaxValue)
				{
					wrapper.FuncInvoker<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>(null, null);
					((Func<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>)null).Invoke(default(Arg1T), default(Arg2T), default(Arg3T), default(Arg4T));
				}
#pragma warning restore 1720

				return wrapper.FuncInvoker<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>;
			}
		}
	}
}
