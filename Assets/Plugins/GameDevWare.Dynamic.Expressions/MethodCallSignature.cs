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
using GameDevWare.Dynamic.Expressions.Properties;

namespace GameDevWare.Dynamic.Expressions
{
	internal class MethodCallSignature
	{
		private readonly int hashCode;

		public readonly Type Parameter1Type;
		public readonly Type Parameter2Type;
		public readonly Type Parameter3Type;
		public readonly Type Parameter4Type;

		public readonly string Parameter1Name;
		public readonly string Parameter2Name;
		public readonly string Parameter3Name;
		public readonly string Parameter4Name;

		public readonly Type ReturnType;

		public readonly int Count;

		public MethodCallSignature(Type returnType)
		{
			if (returnType == null) throw new ArgumentNullException("returnType");

			this.ReturnType = returnType;
			this.hashCode = ComputeHashCode();
			this.Count = 0;
		}
		public MethodCallSignature(Type parameter1Type, string parameter1Name, Type returnType)
			: this(returnType)
		{
			if (parameter1Type == null) throw new ArgumentNullException("parameter1Type");
			if (parameter1Name == null) throw new ArgumentNullException("parameter1Name");
			if (returnType == null) throw new ArgumentNullException("returnType");

			this.Parameter1Type = parameter1Type;
			this.Parameter1Name = parameter1Name;

			this.Count = 1;
		}
		public MethodCallSignature(Type parameter1Type, string parameter1Name, Type parameter2Type, string parameter2Name, Type returnType)
			: this(returnType)
		{
			if (parameter1Type == null) throw new ArgumentNullException("parameter1Type");
			if (parameter2Type == null) throw new ArgumentNullException("parameter2Type");
			if (parameter1Name == null) throw new ArgumentNullException("parameter1Name");
			if (parameter2Name == null) throw new ArgumentNullException("parameter2Name");
			if (returnType == null) throw new ArgumentNullException("returnType");

			this.Parameter1Type = parameter1Type;
			this.Parameter2Type = parameter2Type;

			this.Parameter1Name = parameter1Name;
			this.Parameter2Name = parameter2Name;

			this.Count = 2;
		}
		public MethodCallSignature(Type parameter1Type, string parameter1Name, Type parameter2Type, string parameter2Name, Type parameter3Type, string parameter3Name, Type returnType)
			: this(returnType)
		{
			if (parameter1Type == null) throw new ArgumentNullException("parameter1Type");
			if (parameter2Type == null) throw new ArgumentNullException("parameter2Type");
			if (parameter3Type == null) throw new ArgumentNullException("parameter3Type");
			if (parameter1Name == null) throw new ArgumentNullException("parameter1Name");
			if (parameter2Name == null) throw new ArgumentNullException("parameter2Name");
			if (parameter3Name == null) throw new ArgumentNullException("parameter3Name");
			if (returnType == null) throw new ArgumentNullException("returnType");

			this.Parameter1Type = parameter1Type;
			this.Parameter2Type = parameter2Type;
			this.Parameter3Type = parameter3Type;

			this.Parameter1Name = parameter1Name;
			this.Parameter2Name = parameter2Name;
			this.Parameter3Name = parameter3Name;

			this.Count = 3;
		}
		public MethodCallSignature(Type parameter1Type, string parameter1Name, Type parameter2Type, string parameter2Name, Type parameter3Type, string parameter3Name, Type parameter4Type, string parameter4Name, Type returnType)
			: this(returnType)
		{
			if (parameter1Type == null) throw new ArgumentNullException("parameter1Type");
			if (parameter2Type == null) throw new ArgumentNullException("parameter2Type");
			if (parameter3Type == null) throw new ArgumentNullException("parameter3Type");
			if (parameter4Type == null) throw new ArgumentNullException("parameter4Type");
			if (parameter1Name == null) throw new ArgumentNullException("parameter1Name");
			if (parameter2Name == null) throw new ArgumentNullException("parameter2Name");
			if (parameter3Name == null) throw new ArgumentNullException("parameter3Name");
			if (parameter4Name == null) throw new ArgumentNullException("parameter4Name");
			if (returnType == null) throw new ArgumentNullException("returnType");

			this.Parameter1Type = parameter1Type;
			this.Parameter2Type = parameter2Type;
			this.Parameter3Type = parameter3Type;
			this.Parameter4Type = parameter4Type;

			this.Parameter1Name = parameter1Name;
			this.Parameter2Name = parameter2Name;
			this.Parameter3Name = parameter3Name;
			this.Parameter4Name = parameter4Name;

			this.Count = 4;
		}
		public MethodCallSignature(MethodInfo method, bool includeParameterNames = true)
		{
			if (method == null) throw new ArgumentNullException("method");

			var parameter = method.GetParameters();
			switch (parameter.Length)
			{
				case 4:

					Parameter4Name = includeParameterNames ?  parameter[3].Name : "";
					Parameter4Type = parameter[3].ParameterType;
					goto arg3;
				case 3:
					arg3:
					Parameter3Name = includeParameterNames ? parameter[2].Name : "";
					Parameter3Type = parameter[2].ParameterType;
					goto arg2;
				case 2:
					arg2:
					Parameter2Name = includeParameterNames ? parameter[1].Name : "";
					Parameter2Type = parameter[1].ParameterType;
					goto arg1;
				case 1:
					arg1:
					Parameter1Name = includeParameterNames ? parameter[0].Name : "";
					Parameter1Type = parameter[0].ParameterType;
					goto arg0;
				case 0:
					arg0:
					this.ReturnType = method.ReturnType;
					break;
				default:
					throw new ArgumentException(Resources.EXCEPTION_UNBOUNDEXPR_INVALIDPARAMCOUNT, "method");
			}
			this.Count = parameter.Length;
			this.hashCode = this.ComputeHashCode();
		}

		public override bool Equals(object obj)
		{
			var parameters = obj as MethodCallSignature;
			if (parameters == null) return false;
			if (ReferenceEquals(parameters, this)) return true;

			return this.Parameter1Type == parameters.Parameter1Type &&
				this.Parameter2Type == parameters.Parameter2Type &&
				this.Parameter3Type == parameters.Parameter3Type &&
				this.Parameter4Type == parameters.Parameter4Type &&
				this.Parameter1Name == parameters.Parameter1Name &&
				this.Parameter2Name == parameters.Parameter2Name &&
				this.Parameter3Name == parameters.Parameter3Name &&
				this.Parameter4Name == parameters.Parameter4Name &&
				this.ReturnType == parameters.ReturnType;
		}
		public override int GetHashCode()
		{
			return this.hashCode;
		}
		private int ComputeHashCode()
		{
			return unchecked
				(
				(this.Parameter1Type != null ? this.Parameter1Type.GetHashCode() : 0) +
				(this.Parameter2Type != null ? this.Parameter2Type.GetHashCode() : 0) +
				(this.Parameter3Type != null ? this.Parameter3Type.GetHashCode() : 0) +
				(this.Parameter4Type != null ? this.Parameter4Type.GetHashCode() : 0) +
				(this.Parameter1Name != null ? this.Parameter1Name.GetHashCode() : 0) +
				(this.Parameter2Name != null ? this.Parameter2Name.GetHashCode() : 0) +
				(this.Parameter3Name != null ? this.Parameter3Name.GetHashCode() : 0) +
				(this.Parameter4Name != null ? this.Parameter4Name.GetHashCode() : 0) +
				this.ReturnType.GetHashCode()
				);
		}

		public override string ToString()
		{
			return this.Parameter1Type + ", " + this.Parameter2Type + ", " + this.Parameter3Type + ", " + this.Parameter4Type + ", " + this.ReturnType;
		}
	}
}
