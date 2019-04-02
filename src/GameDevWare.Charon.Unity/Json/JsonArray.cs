using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GameDevWare.Charon.Unity.Json
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
			stream.WriteByte((byte)'[');
			for (var i = 0; i < this.list.Count; i++)
			{
				var v = this.list[i];
				if (v != null)
					v.Save(stream);
				else
				{
					stream.WriteByte((byte)'n');
					stream.WriteByte((byte)'u');
					stream.WriteByte((byte)'l');
					stream.WriteByte((byte)'l');
				}

				if (i < this.Count - 1)
				{
					stream.WriteByte((byte)',');
					stream.WriteByte((byte)' ');
				}
			}
			stream.WriteByte((byte)']');
		}
		public override object As(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			var itemType = default(Type);
			var genericEnumerableInterface = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
			if (type.IsArray)
			{
				itemType = type.GetElementType();
			}
			else if (genericEnumerableInterface != null)
			{
				itemType = genericEnumerableInterface.GetGenericArguments()[0];
			}
			else
			{
				itemType = typeof(object);
			}
			var items = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType));

			foreach (var item in this.list)
			{
				if (item == null)
					items.Add(null);
				else
					items.Add(item.As(itemType));
			}

			if (items.GetType().IsAssignableFrom(type))
			{
				return items;
			}
			else if (type.IsArray)
			{
				return items.GetType().InvokeMember("ToArray", BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, items, null);
			}
			else
			{
				throw new InvalidOperationException(string.Format("Can't convert JsonArray to non-list type '{0}'", type));
			}
		}
	}
}
