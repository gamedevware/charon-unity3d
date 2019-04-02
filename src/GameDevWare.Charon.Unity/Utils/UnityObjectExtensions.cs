/*
	Copyright (c) 2017 Denis Zykov

	This is part of "Charon: Game Data Editor" Unity Plugin.

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

namespace GameDevWare.Charon.Unity.Utils
{
	internal static class UnityObjectExtensions
	{
		public static object Invoke(this object target, string methodName, params object[] args)
		{
			var type = target as Type;
			if (type != null)
			{
				return type.InvokeMember(
					  methodName,
					  BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
					  null,
					  null,
					  args
				  );
			}
			else
			{
				return target.GetType().InvokeMember(
					  methodName,
					  BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
					  null,
					  target,
					  args
				  );
			}
		}

		public static bool HasField(this object target, string fieldName)
		{
			var type = target as Type;
			if (type != null)
			{
				return type.GetField(
					  fieldName,
					  BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public
				  ) != null;
			}
			else
			{
				return target.GetType().GetField(
					  fieldName,
					  BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
				  ) != null;
			}
		}

		public static object GetFieldValue(this object target, string fieldName)
		{
			var type = target as Type;
			if (type != null)
			{
				return type.InvokeMember(
					  fieldName,
					  BindingFlags.GetField | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
					  null,
					  null,
					  null
				  );
			}
			else
			{
				return target.GetType().InvokeMember(
					  fieldName,
					  BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
					  null,
					  target,
					  null
				  );
			}
		}

		public static void SetFieldValue(this object target, string fieldName, object value)
		{
			var type = target as Type;
			if (type != null)
			{
				type.InvokeMember(
					  fieldName,
					  BindingFlags.SetField | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
					  null,
					  null,
					  new object[] { value }
				  );
			}
			else
			{
				target.GetType().InvokeMember(
					  fieldName,
					  BindingFlags.SetField | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
					  null,
					  target,
					  new object[] { value }
				  );
			}
		}

		public static bool HasProperty(this object target, string fieldName)
		{
			var type = target as Type;
			if (type != null)
			{
				return type.GetProperty (
					fieldName,
					BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public
				) != null;
			}
			else
			{
				return target.GetType().GetProperty (
					fieldName,
					BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
				) != null;
			}
		}

		public static object GetPropertyValue(this object target, string propertyName)
		{
			var type = target as Type;
			if (type != null)
			{
				return type.InvokeMember(
					  propertyName,
					  BindingFlags.GetProperty | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
					  null,
					  null,
					  null
				  );
			}
			else
			{
				return target.GetType().InvokeMember(
					  propertyName,
					  BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
					  null,
					  target,
					  null
				  );
			}
		}

		public static void SetPropertyValue(this object target, string propertyName, object value)
		{
			var type = target as Type;
			if (type != null)
			{
				type.InvokeMember(
					  propertyName,
					  BindingFlags.SetProperty | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
					  null,
					  null,
					  new object[] { value }
				  );
			}
			else
			{
				target.GetType().InvokeMember(
					  propertyName,
					  BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
					  null,
					  target,
					  new object[] { value }
				  );
			}
		}
	}
}
