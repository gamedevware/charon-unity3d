/*
	Copyright (c) 2016 Denis Zykov

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

using System.Reflection;
using UnityEngine;

namespace Assets.Editor.GameDevWare.Charon
{
	static class ScriptableObjectExtentions
	{
		public static object Invoke(this ScriptableObject target, string methodName, params object[] args)
		{
			return target.GetType().InvokeMember(
				methodName,
				BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
				null,
				target,
				args
			);
		}
		public static object GetField(this ScriptableObject target, string fieldName)
		{
			return target.GetType().InvokeMember(
				fieldName,
				BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
				null,
				target,
				null
			);
		}
		public static void SetField(this ScriptableObject target, string fieldName, object value)
		{
			target.GetType().InvokeMember(
				fieldName,
				BindingFlags.SetField | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
				null,
				target,
				new object[] { value }
			);
		}
		public static object GetProperty(this ScriptableObject target, string propertyName)
		{
			return target.GetType().InvokeMember(
				propertyName,
				BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
				null,
				target,
				null
			);
		}
		public static void SetProperty(this ScriptableObject target, string propertyName, object value)
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
