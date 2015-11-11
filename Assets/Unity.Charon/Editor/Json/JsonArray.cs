/*
	 Copyright (C) 2001-2002  Ximian, Inc.
	 
	Permission is hereby granted, free of charge, to any person obtaining a
	copy of this software and associated documentation files (the "Software"),
	to deal in the Software without restriction, including without limitation
	the rights to use, copy, modify, merge, publish, distribute, sublicense,
	and/or sell copies of the Software, and to permit persons to whom the
	Software is furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included
	in all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
	THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR
	OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
	ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
	OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Assets.Unity.Charon.Editor.Json
{
	public class JsonArray : JsonValue, IList<JsonValue>
	{
		private readonly List<JsonValue> list;
		public JsonArray(params JsonValue[] items)
		{
			list = new List<JsonValue>();
			AddRange(items);
		}
		public JsonArray(IEnumerable<JsonValue> items)
		{
			if (items == null)
				throw new ArgumentNullException("items");

			list = new List<JsonValue>(items);
		}
		public override JsonType JsonType
		{
			get { return JsonType.Array; }
		}
		public override int Count
		{
			get { return list.Count; }
		}
		public bool IsReadOnly
		{
			get { return false; }
		}
		public override sealed JsonValue this[int index]
		{
			get { return list[index]; }
			set { list[index] = value; }
		}
		public void Add(JsonValue item)
		{
			if (item == null)
				throw new ArgumentNullException("item");

			list.Add(item);
		}
		public void Clear()
		{
			list.Clear();
		}
		public bool Contains(JsonValue item)
		{
			return list.Contains(item);
		}
		public void CopyTo(JsonValue[] array, int arrayIndex)
		{
			list.CopyTo(array, arrayIndex);
		}
		public int IndexOf(JsonValue item)
		{
			return list.IndexOf(item);
		}
		public void Insert(int index, JsonValue item)
		{
			list.Insert(index, item);
		}
		public bool Remove(JsonValue item)
		{
			return list.Remove(item);
		}
		public void RemoveAt(int index)
		{
			list.RemoveAt(index);
		}
		IEnumerator<JsonValue> IEnumerable<JsonValue>.GetEnumerator()
		{
			return list.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return list.GetEnumerator();
		}
		public void AddRange(IEnumerable<JsonValue> items)
		{
			if (items == null)
				throw new ArgumentNullException("items");

			list.AddRange(items);
		}
		public void AddRange(params JsonValue[] items)
		{
			if (items == null)
				return;

			list.AddRange(items);
		}
		public override void Save(Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");
			stream.WriteByte((byte) '[');
			for (var i = 0; i < list.Count; i++)
			{
				var v = list[i];
				if (v != null)
					v.Save(stream);
				else
				{
					stream.WriteByte((byte) 'n');
					stream.WriteByte((byte) 'u');
					stream.WriteByte((byte) 'l');
					stream.WriteByte((byte) 'l');
				}

				if (i < Count - 1)
				{
					stream.WriteByte((byte) ',');
					stream.WriteByte((byte) ' ');
				}
			}
			stream.WriteByte((byte) ']');
		}
		public override object As(Type type)
		{
			if (type.IsArray == false)
				throw new InvalidOperationException(string.Format("Can't convert JsonArray to non-array type '{0}'", type));

			var elementType = type.GetElementType();
			Debug.Assert(elementType != null, "elementType != null");
			var newArray = Array.CreateInstance(elementType, this.Count);
			for (var i = 0; i < this.list.Count; i++)
			{
				if (this.list[i] == null)
					continue;

				newArray.SetValue(this.list[i].As(elementType), i);
			}
			return newArray;
		}
	}
}
