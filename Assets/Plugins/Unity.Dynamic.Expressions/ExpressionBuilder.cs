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

namespace Unity.Dynamic.Expressions
{
	public class ExpressionBuilder
	{
		private static readonly CultureInfo Format = CultureInfo.InvariantCulture;
		private static readonly IDictionary<string, object> EmptyArguments = ReadOnlyDictionary<string, object>.Empty;
		private static readonly ILookup<string, MethodInfo> ExpressionConstructors;
		private static readonly string[] NumericPromotionOperations;
		private static readonly TypeCode[] SignedIntegerTypes;
		private static readonly TypeCode[] Numeric;

		private readonly Dictionary<string, Type[]> knownTypes;
		private readonly ReadOnlyCollection<ParameterExpression> parameters;
		private readonly Type contextType;

		public ReadOnlyCollection<ParameterExpression> Parameters { get { return this.parameters; } }
		public Type ContextType { get { return this.contextType; } }

		static ExpressionBuilder()
		{
			ExpressionConstructors = typeof(Expression)
				.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.Where(m => typeof(Expression).IsAssignableFrom(m.ReturnType))
				.ToLookup(m => m.Name);

			NumericPromotionOperations = new[]
			{
				"Add", "AddChecked", "And", "Coalesce", "Condition", "Divide", "ExclusiveOr", "Equal",  "GreaterThan", "GreaterThanOrEqual",
				"LessThan", "LessThanOrEqual", "Modulo", "Multiply", "MultiplyChecked", "NotEqual", "Or", "Subtract", "SubtractChecked"
			};
			Array.Sort(NumericPromotionOperations);
			SignedIntegerTypes = new[] { TypeCode.SByte, TypeCode.Int16, TypeCode.Int32, TypeCode.Int64 };
			Numeric = new[]
			{
				TypeCode.SByte, TypeCode.Int16, TypeCode.Int32, TypeCode.Int64,
				TypeCode.Byte, TypeCode.UInt16, TypeCode.UInt32, TypeCode.UInt64,
				TypeCode.Single, TypeCode.Double, TypeCode.Decimal,
			};
			Array.Sort(Numeric);
		}
		public ExpressionBuilder(ReadOnlyCollection<ParameterExpression> parameters, Type contextType = null)
		{
			if (parameters == null) throw new ArgumentNullException("parameters");

			var exposedTypes = new Type[parameters.Count];
			for (var i = 0; i < parameters.Count; i++)
				exposedTypes[i] = parameters[i].Type;

			this.parameters = parameters;
			this.contextType = contextType;
			this.knownTypes = GetKnownTypes(exposedTypes)
				.ToLookup(t => t.FullName)
				.ToDictionary(kv => kv.Key, kv => kv.ToArray());
		}

		public Expression Build(ExpressionTree node, Expression context = null)
		{
			if (node == null) throw new ArgumentNullException("node");

			var expressionTypeObj = default(object);
			if (node.TryGetValue(ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE, out expressionTypeObj) == false || expressionTypeObj is string == false)
				throw new InvalidOperationException(string.Format(node.Position + " " + Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE));

			var expression = default(Expression);
			var expressionType = (string)expressionTypeObj;
			switch (expressionType)
			{
				case "Invoke": expression = BuildInvoke(node, context); break;
				case "Index": expression = BuildIndex(node, context); break;
				case "Enclose":
				case "Group": expression = BuildGroup(node, context); break;
				case "Constant": expression = BuildConstant(node); break;
				case "PropertyOrField": expression = BuildPropertyOrField(node, context); break;
				default: expression = BuildByType(node, context); break;
			}

			return expression;
		}
		private Expression BuildByType(ExpressionTree node, Expression context)
		{
			if (node == null) throw new ArgumentNullException("node");

			var expressionType = (string)node[ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE];
			if (expressionType == "Complement")
				expressionType = "Not";

			if (ExpressionConstructors.Contains(expressionType) == false)
				throw new InvalidOperationException(string.Format(node.Position + " " + Properties.Resources.EXCEPTION_BUILD_UNKNOWNEXPRTYPE, expressionType));

			var argumentNames = new HashSet<string>(node.Keys, StringComparer.Ordinal);
			argumentNames.Remove(ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE);
			argumentNames.RemoveWhere(e => e.StartsWith("$", StringComparison.Ordinal));
			foreach (var method in ExpressionConstructors[expressionType].OrderBy(m => m.GetParameters().Length))
			{
				var parameterNames = new HashSet<string>(method.GetParameters().Select(p => p.Name), StringComparer.Ordinal);
				if (argumentNames.IsSubsetOf(parameterNames) == false)
					continue;

				var methodParameters = method.GetParameters();
				var methodArguments = new object[methodParameters.Length];
				var index = 0;
				foreach (var methodParameter in methodParameters)
				{
					var argument = default(object);
					if (node.TryGetValue(methodParameter.Name, out argument))
					{
						var typeName = default(string);
						if (argument != null && methodParameter.ParameterType == typeof(Type) && TryGetTypeName(argument, out typeName))
							argument = ResolveType(typeName);
						else if (argument is ExpressionTree)
							argument = Build((ExpressionTree)argument, context);
						else if (argument != null)
							argument = ChangeType(argument, methodParameter.ParameterType);
						else if (methodParameter.ParameterType.IsValueType)
							argument = GetDefaultValue(methodParameter.ParameterType);

						methodArguments[index] = argument;
					}
					else
					{
						methodArguments[index] = GetDefaultValue(methodParameter.ParameterType);
					}

					index++;
				}

				if (Array.BinarySearch(NumericPromotionOperations, expressionType) >= 0)
					PromoteNumeric(method, methodArguments);

				try
				{
					if (methodArguments.Length == 2 &&
						(((Expression)methodArguments[0]).Type == typeof(string) ||
						((Expression)methodArguments[1]).Type == typeof(string)) &&
						(expressionType == "Add" || expressionType == "AddChecked"))
					{
						var concatArguments = new Expression[]
						{
							Expression.Convert((Expression)methodArguments[0], typeof(object)),
							Expression.Convert((Expression)methodArguments[1], typeof(object))
						};
						return Expression.Call(typeof(string), "Concat", Type.EmptyTypes, concatArguments);
					}
					else
					{
						return (Expression)method.Invoke(null, methodArguments);
					}
				}
				catch (TargetInvocationException te) { throw te.InnerException; }
			}
			throw new InvalidOperationException(string.Format(node.Position + " " + Properties.Resources.EXCEPTION_BUILD_UNABLETOCREATEEXPRWITHPARAMS, expressionType, string.Join(", ", argumentNames.ToArray())));
		}

		private Expression BuildGroup(ExpressionTree node, Expression context)
		{
			if (node == null) throw new ArgumentNullException("node");

			var expressionObj = default(object);
			if (node.TryGetValue(ExpressionTree.EXPRESSION_ATTRIBUTE, out expressionObj) && expressionObj != null && expressionObj is ExpressionTree == false)
				throw new InvalidOperationException(string.Format(node.Position + " " + Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.EXPRESSION_ATTRIBUTE, "PropertyOrField"));

			return Build((ExpressionTree)expressionObj, context);
		}
		private Expression BuildPropertyOrField(ExpressionTree node, Expression context)
		{
			if (node == null) throw new ArgumentNullException("node");

			var expressionObj = default(object);
			if (node.TryGetValue(ExpressionTree.EXPRESSION_ATTRIBUTE, out expressionObj) && expressionObj != null && expressionObj is ExpressionTree == false)
				throw new InvalidOperationException(string.Format(node.Position + " " + Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.EXPRESSION_ATTRIBUTE, "PropertyOrField"));

			var propertyOrFieldNameObj = default(object);
			if (node.TryGetValue(ExpressionTree.PROPERTY_OR_FIELD_NAME_ATTRIBUTE, out propertyOrFieldNameObj) == false || propertyOrFieldNameObj is string == false)
				throw new InvalidOperationException(string.Format(node.Position + " " + Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.PROPERTY_OR_FIELD_NAME_ATTRIBUTE, "PropertyOrField"));

			var propertyOrFieldName = (string)propertyOrFieldNameObj;
			var expression = default(Expression);
			var typeName = default(string);
			var type = default(Type);
			if (expressionObj != null && TryGetTypeName(expressionObj, out typeName) && TryResolveType(typeName, out type))
			{
				expression = null;
			}
			else if (expressionObj == null)
			{
				var paramExpression = default(Expression);
				if (propertyOrFieldName == "null")
					return Expression.Constant(null, typeof(object));
				else if (propertyOrFieldName == "true")
					return Expression.Constant(true, typeof(bool));
				else if (propertyOrFieldName == "false")
					return Expression.Constant(false, typeof(bool));
				else if ((paramExpression = parameters.FirstOrDefault(p => p.Name == propertyOrFieldName)) != null)
					return paramExpression;
				else if (context != null)
					expression = context;
			}
			else
			{
				expression = Build((ExpressionTree)expressionObj, context);
			}

			if (expression == null && type == null)
				throw new InvalidOperationException(string.Format(node.Position + " " + Properties.Resources.EXCEPTION_BUILD_UNABLETORESOLVENAME, propertyOrFieldName));

			if (expression != null)
				type = expression.Type;

			var property = type.GetProperty(propertyOrFieldName);
			var field = type.GetField(propertyOrFieldName);
			if (property != null)
				return Expression.Property(expression, property);
			else if (field != null)
				return Expression.Field(expression, field);
			else if (expression == null)
				throw new InvalidOperationException(string.Format(node.Position + " " + Properties.Resources.EXCEPTION_BUILD_UNABLETORESOLVENAME, propertyOrFieldName));
			else
				throw new InvalidOperationException(string.Format(node.Position + " " + Properties.Resources.EXCEPTION_BUILD_UNABLETORESOLVEMEMBERONTYPE, propertyOrFieldName, type));
		}
		private Expression BuildConstant(ExpressionTree node)
		{
			if (node == null) throw new ArgumentNullException("node");

			var typeObj = default(object);
			var valueObj = default(object);
			if (node.TryGetValue(ExpressionTree.TYPE_ATTRIBUTE, out typeObj) == false || typeObj is string == false)
				throw new InvalidOperationException(string.Format(node.Position + " " + Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.TYPE_ATTRIBUTE, "Constant"));
			if (node.TryGetValue(ExpressionTree.VALUE_ATTRIBUTE, out valueObj) == false)
				throw new InvalidOperationException(string.Format(node.Position + " " + Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.VALUE_ATTRIBUTE, "Constant"));

			var type = ResolveType(Convert.ToString(typeObj, Format));
			var value = ChangeType(valueObj, type);
			return Expression.Constant(value);
		}
		private Expression BuildIndex(ExpressionTree node, Expression context)
		{
			if (node == null) throw new ArgumentNullException("node");

			var expressionObj = default(object);
			if (node.TryGetValue(ExpressionTree.EXPRESSION_ATTRIBUTE, out expressionObj) == false || expressionObj is ExpressionTree == false)
				throw new InvalidOperationException(string.Format(node.Position + " " + Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.EXPRESSION_ATTRIBUTE, "Index"));

			var argumentsObj = default(object);
			if (node.TryGetValue(ExpressionTree.ARGUMENTS_ATTRIBUTE, out argumentsObj) && argumentsObj != null && argumentsObj is ExpressionTree == false)
				throw new InvalidOperationException(string.Format(node.Position + " " + Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.ARGUMENTS_ATTRIBUTE, "Index"));

			var arguments = (ExpressionTree)argumentsObj ?? EmptyArguments;
			var expression = Build((ExpressionTree)expressionObj, context);
			var properties = expression.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
			Array.Sort(properties, (x, y) => x.GetIndexParameters().Length.CompareTo(y.GetIndexParameters().Length));
			foreach (var property in properties)
			{
				var indexerParameters = property.GetIndexParameters();
				if (indexerParameters.Length == 0) continue;

				var getMethod = property.GetGetMethod(nonPublic: false);
				var argumentExpressions = default(Expression[]);
				if (getMethod == null || TryBindMethod(indexerParameters, arguments, out argumentExpressions, context) == false)
					continue;

				return Expression.Call(expression, getMethod, argumentExpressions);
			}
			throw new InvalidOperationException(string.Format(node.Position + " " + Properties.Resources.EXCEPTION_BUILD_UNABLETOBINDINDEXER, string.Join(", ", arguments.Keys.ToArray()), expression.Type));
		}
		private Expression BuildCall(ExpressionTree node, Expression context)
		{
			if (node == null) throw new ArgumentNullException("node");

			var expressionObj = default(object);
			if (node.TryGetValue(ExpressionTree.EXPRESSION_ATTRIBUTE, out expressionObj) && expressionObj != null && expressionObj is ExpressionTree == false)
				throw new InvalidOperationException(string.Format(node.Position + " " + Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.EXPRESSION_ATTRIBUTE, "Call"));

			var argumentsObj = default(object);
			if (node.TryGetValue(ExpressionTree.ARGUMENTS_ATTRIBUTE, out argumentsObj) && argumentsObj != null && argumentsObj is ExpressionTree == false)
				throw new InvalidOperationException(string.Format(node.Position + " " + Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.ARGUMENTS_ATTRIBUTE, "Call"));

			var methodObj = default(object);
			if (node.TryGetValue(ExpressionTree.METHOD_ATTRIBUTE, out methodObj) == false || methodObj is string == false)
				throw new InvalidOperationException(string.Format(node.Position + " " + Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.METHOD_ATTRIBUTE, "Call"));

			if (expressionObj == null && context == null)
				throw new InvalidOperationException(string.Format(node.Position + " " + Properties.Resources.EXCEPTION_BUILD_UNABLETORESOLVENAME, methodObj));

			var methodName = (string)methodObj;
			var expression = default(Expression);
			var arguments = (ExpressionTree)argumentsObj ?? EmptyArguments;

			var typeName = default(string);
			var type = default(Type);
			var isStatic = true;
			if (TryGetTypeName(expressionObj, out typeName) == false || TryResolveType(typeName, out type) == false)
			{
				expression = Build((ExpressionTree)expressionObj);
				type = expression.Type;
				isStatic = false;
			}

			var methods = type.GetMethods(BindingFlags.Public | (isStatic ? BindingFlags.Static : BindingFlags.Instance));
			Array.Sort(methods, (x, y) => x.GetParameters().Length.CompareTo(y.GetParameters().Length));

			foreach (var method in methods)
			{
				if (method.IsGenericMethod) continue;
				if (methodName != method.Name) continue;

				var methodParameters = method.GetParameters();
				var argumentExpressions = default(Expression[]);
				if (TryBindMethod(methodParameters, arguments, out argumentExpressions, context) == false)
					continue;

				return expression == null ?
					Expression.Call(method, argumentExpressions) : // static call
					Expression.Call(expression, method, argumentExpressions); // instance call
			}
			throw new InvalidOperationException(string.Format(node.Position + " " + Properties.Resources.EXCEPTION_BUILD_UNABLETOBINDCALL, methodObj, string.Join(", ", arguments.Keys.ToArray()), type));
		}
		private Expression BuildInvoke(ExpressionTree node, Expression context)
		{
			if (node == null) throw new ArgumentNullException("node");

			var expressionObj = default(object);
			if (node.TryGetValue(ExpressionTree.EXPRESSION_ATTRIBUTE, out expressionObj) == false || expressionObj is ExpressionTree == false)
				throw new InvalidOperationException(string.Format(node.Position + " " + Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.EXPRESSION_ATTRIBUTE, "Invoke"));

			var argumentsObj = default(object);
			if (node.TryGetValue(ExpressionTree.ARGUMENTS_ATTRIBUTE, out argumentsObj) && argumentsObj != null && argumentsObj is ExpressionTree == false)
				throw new InvalidOperationException(string.Format(node.Position + " " + Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.ARGUMENTS_ATTRIBUTE, "Invoke"));

			var expressionTree = (ExpressionTree)expressionObj;
			var expression = default(Expression);
			var arguments = (ExpressionTree)argumentsObj ?? EmptyArguments;

			if ((string)expressionTree[ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE] == "PropertyOrField")
			{
				var propertyOrFieldExpressionObj = default(object);
				if (expressionTree.TryGetValue(ExpressionTree.EXPRESSION_ATTRIBUTE, out propertyOrFieldExpressionObj) && propertyOrFieldExpressionObj != null && propertyOrFieldExpressionObj is ExpressionTree == false)
					throw new InvalidOperationException(string.Format(node.Position + " " + Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.EXPRESSION_ATTRIBUTE, "PropertyOrField"));
				var propertyOrFieldNameObj = default(object);
				if (expressionTree.TryGetValue(ExpressionTree.PROPERTY_OR_FIELD_NAME_ATTRIBUTE, out propertyOrFieldNameObj) == false || propertyOrFieldNameObj is string == false)
					throw new InvalidOperationException(string.Format(node.Position + " " + Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.PROPERTY_OR_FIELD_NAME_ATTRIBUTE, "PropertyOrField"));

				var typeName = default(string);
				var type = default(Type);
				var isStatic = true;
				if (TryGetTypeName(propertyOrFieldExpressionObj, out typeName) == false || TryResolveType(typeName, out type) == false)
				{
					var propertyOrFieldExpression = propertyOrFieldExpressionObj != null ? Build((ExpressionTree)propertyOrFieldExpressionObj, context) : context;
					type = propertyOrFieldExpression.Type;
					isStatic = false;
				}

				var methodBindingFlags = BindingFlags.Public | (isStatic ? BindingFlags.Static : BindingFlags.Instance);
				if (type != null && type.GetMethods(methodBindingFlags).Any(m => m.Name == (string)propertyOrFieldNameObj))
				{
					var callNode = new Dictionary<string, object>(node);
					callNode[ExpressionTree.METHOD_ATTRIBUTE] = (string)propertyOrFieldNameObj;
					callNode[ExpressionTree.EXPRESSION_ATTRIBUTE] = propertyOrFieldExpressionObj;
					return this.BuildCall(new ExpressionTree(callNode), context);
				}
			}

			expression = Build((ExpressionTree)expressionObj, context);

			if (typeof(Delegate).IsAssignableFrom(expression.Type) == false)
				throw new InvalidOperationException(string.Format(node.Position + " " + Properties.Resources.EXCEPTION_BUILD_UNABLETOINVOKENONDELEG, expression.Type, string.Join(", ", arguments.Keys.ToArray())));

			var method = expression.Type.GetMethod("Invoke");
			var methodParameters = method.GetParameters();
			var argumentExpressions = default(Expression[]);
			if (TryBindMethod(methodParameters, arguments, out argumentExpressions, context) == false)
				throw new InvalidOperationException(string.Format(node.Position + " " + Properties.Resources.EXCEPTION_BUILD_UNABLETOBINDDELEG, expression.Type, string.Join(", ", arguments.Keys.ToArray())));

			return Expression.Invoke(expression, argumentExpressions);
		}

		private bool TryBindMethod(ParameterInfo[] methodParameters, IDictionary<string, object> arguments, out Expression[] callArguments, Expression context)
		{
			callArguments = null;

			// check argument count
			if (arguments.Count > methodParameters.Length)
				return false; // not all arguments are bound to parameters

			var requiredParametersCount = methodParameters.Length - methodParameters.Count(p => p.IsOptional);
			if (arguments.Count < requiredParametersCount)
				return false; // not all required parameters has values

			// bind arguments
			var parametersByName = methodParameters.ToDictionary(p => p.Name);
			var parametersByPos = methodParameters.ToDictionary(p => p.Position);
			var argumentNames = arguments.Keys.ToArray();
			callArguments = new Expression[methodParameters.Length];
			foreach (var argName in argumentNames)
			{
				var parameter = default(ParameterInfo);
				var parameterIndex = 0;
				if (argName.All(Char.IsDigit))
				{
					parameterIndex = Int32.Parse(argName, Format);
					if (parametersByPos.TryGetValue(parameterIndex, out parameter) == false)
						return false; // position out of range

					if (arguments.ContainsKey(parameter.Name))
						return false; // positional intersects named
				}
				else
				{
					if (parametersByName.TryGetValue(argName, out parameter) == false)
						return false; // parameter is not found
					parameterIndex = parameter.Position;
				}

				var argValue = arguments[argName] as Expression;
				if (argValue == null)
				{
					argValue = this.Build((ExpressionTree)arguments[argName], context);
					// arguments[argName] = argValue // no arguments optimization
				}

				callArguments[parameterIndex] = argValue;

				var expectedType = parameter.ParameterType;
				var actualType = callArguments[parameterIndex].Type;

				// type casting
				if (expectedType == actualType ||
					expectedType.IsAssignableFrom(actualType))
				{
					continue;
				}

				// convert to/from enum, nullable
				var nullableUnderlyingType = Nullable.GetUnderlyingType(expectedType);
				if ((expectedType.IsEnum && Enum.GetUnderlyingType(expectedType) == actualType) ||
					(actualType.IsEnum && Enum.GetUnderlyingType(actualType) == expectedType) ||
					(nullableUnderlyingType != null && nullableUnderlyingType == actualType))
				{
					callArguments[parameterIndex] = Expression.Convert(callArguments[parameterIndex], expectedType);
					continue;
				}

				// implicit convertion
				var implicitConvertion = expectedType.GetMethod("op_Implicit", BindingFlags.Public | BindingFlags.Static, null, new Type[] { actualType }, null);
				if (implicitConvertion != null && implicitConvertion.ReturnType == expectedType)
				{
					callArguments[parameterIndex] = Expression.Convert(callArguments[parameterIndex], expectedType, implicitConvertion);
					continue;
				}


				return false; // parameters types doesn't match
			}

			for (var i = 0; i < callArguments.Length; i++)
			{
				if (callArguments[i] != null) continue;
				var parameter = parametersByPos[i];
				if (parameter.IsOptional == false)
					return false; // missing required parameter
				callArguments[i] = Expression.Constant(GetDefaultValue(parameter.ParameterType), parameter.ParameterType);
			}

			return true;
		}
		private static object ChangeType(object value, Type toType)
		{
			if (toType == null) throw new ArgumentNullException("toType");

			if (toType.IsEnum)
				return Enum.Parse(toType, Convert.ToString(value, Format));
			else
				return Convert.ChangeType(value, toType, Format);
		}
		private static object GetDefaultValue(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			return type.IsValueType ? Activator.CreateInstance(type) : null;
		}
		private static void PromoteNumeric(MethodInfo method, object[] methodArguments)
		{
			if (method == null) throw new ArgumentNullException("method");
			if (methodArguments == null) throw new ArgumentNullException("methodArguments");

			var left = default(Expression);
			var right = default(Expression);
			var leftIdx = -1;
			var rightIdx = -1;
			foreach (var parameter in method.GetParameters())
			{
				switch (parameter.Name)
				{
					case "left":
					case "ifTrue":
						left = (Expression)methodArguments[parameter.Position];
						leftIdx = parameter.Position;
						break;
					case "right":
					case "ifFalse":
						right = (Expression)methodArguments[parameter.Position];
						rightIdx = parameter.Position;
						break;
				}
			}

			if (left == null || right == null || leftIdx < 0 || rightIdx < 0)
				return;

			if (left.Type.IsEnum)
				left = Expression.Convert(left, Enum.GetUnderlyingType(left.Type));
			if (right.Type.IsEnum)
				right = Expression.Convert(right, Enum.GetUnderlyingType(right.Type));

			//if (Nullable.GetUnderlyingType(left.Type) != null)
			//	left = Expression.Property(left, "Value");
			//if (Nullable.GetUnderlyingType(right.Type) != null)
			//	right = Expression.Property(right, "Value");

			if (left.Type == right.Type)
				return;

			if (left.Type == typeof(object))
				methodArguments[rightIdx] = Expression.Convert(right, typeof(object));
			else if (right.Type == typeof(object))
				methodArguments[leftIdx] = Expression.Convert(left, typeof(object));

			var leftType = Type.GetTypeCode(left.Type);
			var rightType = Type.GetTypeCode(right.Type);
			if (Array.BinarySearch(Numeric, leftType) < 0 || Array.BinarySearch(Numeric, rightType) < 0)
				return;

			if (leftType == TypeCode.Decimal || rightType == TypeCode.Decimal)
			{
				if (leftType == TypeCode.Double || leftType == TypeCode.Single || rightType == TypeCode.Double || rightType == TypeCode.Single)
					return; // will throw exception
				if (leftType == TypeCode.Decimal)
					methodArguments[rightIdx] = Expression.Convert(right, typeof(Decimal));
				else
					methodArguments[leftIdx] = Expression.Convert(left, typeof(Decimal));
			}
			else if (leftType == TypeCode.Double || rightType == TypeCode.Double)
			{
				if (leftType == TypeCode.Double)
					methodArguments[rightIdx] = Expression.Convert(right, typeof(Double));
				else
					methodArguments[leftIdx] = Expression.Convert(left, typeof(Double));
			}
			else if (leftType == TypeCode.Single || rightType == TypeCode.Single)
			{
				if (leftType == TypeCode.Single)
					methodArguments[rightIdx] = Expression.Convert(right, typeof(Single));
				else
					methodArguments[leftIdx] = Expression.Convert(left, typeof(Single));
			}
			else if (leftType == TypeCode.UInt64 || rightType == TypeCode.UInt64)
			{
				if (Array.IndexOf(SignedIntegerTypes, leftType) > 0 || Array.IndexOf(SignedIntegerTypes, rightType) > 0)
					return; // will throw exception

				if (leftType == TypeCode.UInt64)
					methodArguments[rightIdx] = Expression.Convert(right, typeof(UInt64));
				else
					methodArguments[leftIdx] = Expression.Convert(left, typeof(UInt64));
			}
			else if (leftType == TypeCode.Int64 || rightType == TypeCode.Int64)
			{
				if (leftType == TypeCode.Int64)
					methodArguments[rightIdx] = Expression.Convert(right, typeof(Int64));
				else
					methodArguments[leftIdx] = Expression.Convert(left, typeof(Int64));
			}
			else if (leftType == TypeCode.UInt32 || Array.IndexOf(SignedIntegerTypes, rightType) > 0)
			{
				methodArguments[rightIdx] = Expression.Convert(right, typeof(Int64));
				methodArguments[leftIdx] = Expression.Convert(left, typeof(Int64));
			}
			else if (leftType == TypeCode.UInt32 || rightType == TypeCode.UInt32)
			{
				if (leftType == TypeCode.UInt32)
					methodArguments[rightIdx] = Expression.Convert(right, typeof(UInt32));
				else
					methodArguments[leftIdx] = Expression.Convert(left, typeof(UInt32));
			}
			else
			{
				methodArguments[rightIdx] = Expression.Convert(right, typeof(Int32));
				methodArguments[leftIdx] = Expression.Convert(left, typeof(Int32));
			}
		}

		private Type ResolveType(string typeName)
		{
			if (typeName == null) throw new ArgumentNullException("typeName");

			var type = default(Type);
			var types = default(Type[]);
			if (this.knownTypes.TryGetValue(typeName, out types) == false)
			{
				if (typeName.IndexOf('.') >= 0 || this.TryResolveType("System." + typeName, out type) == false)
					throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNABLETORESOLVETYPE, typeName, string.Join(", ", this.knownTypes.Keys.ToArray())));
			}
			else if (types.Length > 1)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNABLETORESOLVETYPEMULTIPLE, typeName, string.Join(", ", Array.ConvertAll(types, t => t.FullName))));
			else
				type = types[0];

			return type;
		}
		private bool TryResolveType(string typeName, out Type type)
		{
			if (typeName == null) throw new ArgumentNullException("typeName");

			type = default(Type);
			var types = default(Type[]);
			if (this.knownTypes.TryGetValue(typeName, out types) == false)
				return typeName.IndexOf('.') < 0 && this.TryResolveType("System." + typeName, out type);
			else if (types.Length > 1)
				return false;
			else
				type = types[0];
			return true;
		}
		private bool TryGetTypeName(object value, out string typeName)
		{
			typeName = default(string);
			if (value is ExpressionTree)
			{
				var typeNameParts = new List<string>();
				var current = (ExpressionTree)value;
				while (current != null)
				{
					var expressionTypeObj = default(object);
					if (current.TryGetValue(ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE, out expressionTypeObj) == false || expressionTypeObj is string == false)
						throw new InvalidOperationException(string.Format(current.Position + " " + Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE));

					var expressionType = (string)expressionTypeObj;
					if (expressionType != "PropertyOrField")
						return false;

					var expressionObj = default(object);
					if (current.TryGetValue(ExpressionTree.EXPRESSION_ATTRIBUTE, out expressionObj) && expressionObj != null && expressionObj is ExpressionTree == false)
						throw new InvalidOperationException(string.Format(current.Position + " " + Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.EXPRESSION_ATTRIBUTE, "PropertyOrField"));

					var typeNamePartObj = default(object);
					if (current.TryGetValue(ExpressionTree.PROPERTY_OR_FIELD_NAME_ATTRIBUTE, out typeNamePartObj) == false || typeNamePartObj is string == false)
						throw new InvalidOperationException(string.Format(current.Position + " " + Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.PROPERTY_OR_FIELD_NAME_ATTRIBUTE, "PropertyOrField"));

					var typeNamePart = (string)typeNamePartObj;
					typeNameParts.Add(typeNamePart);
					current = expressionObj as ExpressionTree;
				}

				typeNameParts.Reverse();
				typeName = string.Join(".", typeNameParts.ToArray());
				return true;
			}
			else
			{
				typeName = Convert.ToString(value, Format);
				return true;
			}
		}

		public static Expression DefaultValue(Type forType)
		{
			if (forType == null) throw new ArgumentNullException("forType");

			if (forType.IsValueType)
				return Expression.Constant(Activator.CreateInstance(forType), forType);
			else
				return Expression.Constant(null, forType);
		}
		private static HashSet<Type> GetKnownTypes(params Type[] types)
		{
			var knownTypes = new HashSet<Type>();
			knownTypes = new HashSet<Type>
			{
				typeof (Object),
				typeof (Boolean),
				typeof (Char),
				typeof (SByte),
				typeof (Byte),
				typeof (Int16),
				typeof (UInt16),
				typeof (Int32),
				typeof (UInt32),
				typeof (Int64),
				typeof (UInt64),
				typeof (Single),
				typeof (Double),
				typeof (Decimal),
				typeof (DateTime),
				typeof (TimeSpan),
				typeof (String),
				typeof (Math)
			};

			foreach (var type in types)
			{
				foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
					knownTypes.Add(property.PropertyType);

				foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
					knownTypes.Add(field.FieldType);

				foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public))
				{
					knownTypes.Add(method.ReturnType);

					foreach (var parameter in method.GetParameters())
						knownTypes.Add(parameter.ParameterType);
				}

				foreach (ExpressionKnownTypeAttribute knownTypeAttribute in type.GetCustomAttributes(typeof(ExpressionKnownTypeAttribute), true))
					knownTypes.Add(knownTypeAttribute.Type);
			}

			knownTypes.RemoveWhere(t => t.IsGenericType);

			return knownTypes;
		}
	}
}
