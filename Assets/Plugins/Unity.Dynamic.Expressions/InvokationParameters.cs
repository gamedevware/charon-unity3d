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

namespace Unity.Dynamic.Expressions
{
	class InvokationParameters
	{
		private readonly int hashCode;

		public readonly Type Argument1Type;
		public readonly Type Argument2Type;
		public readonly Type Argument3Type;
		public readonly Type Argument4Type;

		public readonly string Argument1Name;
		public readonly string Argument2Name;
		public readonly string Argument3Name;
		public readonly string Argument4Name;

		public readonly Type ReturnType;

		public InvokationParameters(Type returnType)
		{
			if (returnType == null) throw new ArgumentNullException("returnType");

			this.ReturnType = returnType;
		}
		public InvokationParameters(Type argument1Type, string argument1Name, Type returnType)
		{
			if (argument1Type == null) throw new ArgumentNullException("argument1Type");
			if (argument1Name == null) throw new ArgumentNullException("argument1Name");
			if (returnType == null) throw new ArgumentNullException("returnType");

			this.Argument1Type = argument1Type;
			this.Argument1Name = argument1Name;

			this.ReturnType = returnType;
			this.hashCode = ComputeHashCode();
		}

		public InvokationParameters(Type argument1Type, string argument1Name, Type argument2Type, string argument2Name, Type returnType)
		{
			if (argument1Type == null) throw new ArgumentNullException("argument1Type");
			if (argument2Type == null) throw new ArgumentNullException("argument2Type");
			if (argument1Name == null) throw new ArgumentNullException("argument1Name");
			if (argument2Name == null) throw new ArgumentNullException("argument2Name");
			if (returnType == null) throw new ArgumentNullException("returnType");

			this.Argument1Type = argument1Type;
			this.Argument2Type = argument2Type;

			this.Argument1Name = argument1Name;
			this.Argument2Name = argument2Name;

			this.ReturnType = returnType;
		}
		public InvokationParameters(Type argument1Type, string argument1Name, Type argument2Type, string argument2Name, Type argument3Type, string argument3Name, Type returnType)
		{
			if (argument1Type == null) throw new ArgumentNullException("argument1Type");
			if (argument2Type == null) throw new ArgumentNullException("argument2Type");
			if (argument3Type == null) throw new ArgumentNullException("argument3Type");
			if (argument1Name == null) throw new ArgumentNullException("argument1Name");
			if (argument2Name == null) throw new ArgumentNullException("argument2Name");
			if (argument3Name == null) throw new ArgumentNullException("argument3Name");
			if (returnType == null) throw new ArgumentNullException("returnType");

			this.Argument1Type = argument1Type;
			this.Argument2Type = argument2Type;
			this.Argument3Type = argument3Type;

			this.Argument1Name = argument1Name;
			this.Argument2Name = argument2Name;
			this.Argument3Name = argument3Name;

			this.ReturnType = returnType;
		}
		public InvokationParameters(Type argument1Type, string argument1Name, Type argument2Type, string argument2Name, Type argument3Type, string argument3Name, Type argument4Type, string argument4Name, Type returnType)
		{
			if (argument1Type == null) throw new ArgumentNullException("argument1Type");
			if (argument2Type == null) throw new ArgumentNullException("argument2Type");
			if (argument3Type == null) throw new ArgumentNullException("argument3Type");
			if (argument4Type == null) throw new ArgumentNullException("argument4Type");
			if (argument1Name == null) throw new ArgumentNullException("argument1Name");
			if (argument2Name == null) throw new ArgumentNullException("argument2Name");
			if (argument3Name == null) throw new ArgumentNullException("argument3Name");
			if (argument4Name == null) throw new ArgumentNullException("argument4Name");
			if (returnType == null) throw new ArgumentNullException("returnType");

			this.Argument1Type = argument1Type;
			this.Argument2Type = argument2Type;
			this.Argument3Type = argument3Type;
			this.Argument4Type = argument4Type;

			this.Argument1Name = argument1Name;
			this.Argument2Name = argument2Name;
			this.Argument3Name = argument3Name;
			this.Argument4Name = argument4Name;

			this.ReturnType = returnType;
		}

		public override bool Equals(object obj)
		{
			var parameters = obj as InvokationParameters;
			if (parameters == null) return false;
			if (ReferenceEquals(parameters, this)) return true;

			return this.Argument1Type == parameters.Argument1Type &&
				this.Argument2Type == parameters.Argument2Type &&
				this.Argument3Type == parameters.Argument3Type &&
				this.Argument4Type == parameters.Argument4Type &&
				this.Argument1Name == parameters.Argument1Name &&
				this.Argument2Name == parameters.Argument2Name &&
				this.Argument3Name == parameters.Argument3Name &&
				this.Argument4Name == parameters.Argument4Name &&
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
				(this.Argument1Type != null ? this.Argument1Type.GetHashCode() : 0) +
				(this.Argument2Type != null ? this.Argument2Type.GetHashCode() : 0) +
				(this.Argument3Type != null ? this.Argument3Type.GetHashCode() : 0) +
				(this.Argument4Type != null ? this.Argument4Type.GetHashCode() : 0) +
				(this.Argument1Name != null ? this.Argument1Name.GetHashCode() : 0) +
				(this.Argument2Name != null ? this.Argument2Name.GetHashCode() : 0) +
				(this.Argument3Name != null ? this.Argument3Name.GetHashCode() : 0) +
				(this.Argument4Name != null ? this.Argument4Name.GetHashCode() : 0) +
				this.ReturnType.GetHashCode()
				);
		}

		public override string ToString()
		{
			return this.Argument1Type + ", " + this.Argument2Type + ", " + this.Argument3Type + ", " + this.Argument4Type + ", " + this.ReturnType;
		}
	}
}
