using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace GameDevWare.Charon.Json
{
	internal class JsonArray : JsonValue, IList<JsonValue>
	{
		private readonly List<JsonValue> list;
		public JsonArray(params JsonValue[] items)
		{
			this.list = new List<JsonValue>();
			this.AddRange(items);
		}
		public JsonArray(IEnumerable<JsonValue> items)
		{
			if (items == null)
				throw new ArgumentNullException("items");

			this.list = new List<JsonValue>(items);
		}
		public override JsonType JsonType
		{
			get { return JsonType.Array; }
		}
		public override int Count
		{
			get { return this.list.Count; }
		}
		public bool IsReadOnly
		{
			get { return false; }
		}
		public sealed override JsonValue this[int index]
		{
			get { return this.list[index]; }
			set { this.list[index] = value; }
		}
		public void Add(JsonValue item)
		{
			if (item == null)
				throw new ArgumentNullException("item");

			this.list.Add(item);
		}
		public void Clear()
		{
			this.list.Clear();
		}
		public bool Contains(JsonValue item)
		{
			return this.list.Contains(item);
		}
		public void CopyTo(JsonValue[] array, int arrayIndex)
		{
			this.list.CopyTo(array, arrayIndex);
		}
		public int IndexOf(JsonValue item)
		{
			return this.list.IndexOf(item);
		}
		public void Insert(int index, JsonValue item)
		{
			this.list.Insert(index, item);
		}
		public bool Remove(JsonValue item)
		{
			return this.list.Remove(item);
		}
		public void RemoveAt(int index)
		{
			this.list.RemoveAt(index);
		}
		IEnumerator<JsonValue> IEnumerable<JsonValue>.GetEnumerator()
		{
			return this.list.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.list.GetEnumerator();
		}
		public void AddRange(IEnumerable<JsonValue> items)
		{
			if (items == null)
				throw new ArgumentNullException("items");

			this.list.AddRange(items);
		}
		public void AddRange(params JsonValue[] items)
		{
			if (items == null)
				return;

			this.list.AddRange(items);
		}
		public override void Save(Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");
			stream.WriteByte((byte) '[');
			for (var i = 0; i < this.list.Count; i++)
			{
				var v = this.list[i];
				if (v != null)
					v.Save(stream);
				else
				{
					stream.WriteByte((byte) 'n');
					stream.WriteByte((byte) 'u');
					stream.WriteByte((byte) 'l');
					stream.WriteByte((byte) 'l');
				}

				if (i < this.Count - 1)
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
