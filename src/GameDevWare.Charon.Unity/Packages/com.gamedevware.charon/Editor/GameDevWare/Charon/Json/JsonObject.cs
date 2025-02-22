﻿/*
 * Author @ Atsushi Enomoto  <atsushi@ximian.com>
 * Copyright (c) 2001, 2002, 2003 Ximian, Inc and the individuals listed
on the ChangeLog entries.

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using JetBrains.Annotations;
using JsonPair = System.Collections.Generic.KeyValuePair<string, Editor.GameDevWare.Charon.Json.JsonValue>;
using JsonPairEnumerable = System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, Editor.GameDevWare.Charon.Json.JsonValue>>;

namespace Editor.GameDevWare.Charon.Json
{
	[PublicAPI, UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
#if JSON_NET_3_0_2_OR_NEWER
	internal
#else
	public
#endif
	class JsonObject : JsonValue, IDictionary<string, JsonValue>, ICollection<KeyValuePair<string, JsonValue>>
	{
		private static readonly Dictionary<Type, Dictionary<string, MemberInfo>> TypeMembers = new Dictionary<Type, Dictionary<string, MemberInfo>>();
		// Use SortedDictionary to make result of ToString() deterministic
		private readonly SortedDictionary<string, JsonValue> map;
		public JsonObject(params JsonPair[] items)
		{
			this.map = new SortedDictionary<string, JsonValue>(StringComparer.Ordinal);

			if (items != null)
				this.AddRange(items);
		}
		public JsonObject(JsonPairEnumerable items)
		{
			if (items == null)
				throw new ArgumentNullException(nameof(items));

			this.map = new SortedDictionary<string, JsonValue>(StringComparer.Ordinal);
			this.AddRange(items);
		}
		public override JsonType JsonType => JsonType.Object;
		public override int Count => this.map.Count;
		public IEnumerator<JsonPair> GetEnumerator()
		{
			return this.map.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.map.GetEnumerator();
		}
		public sealed override JsonValue this[string key]
		{
			get => this.map[key];
			set => this.map[key] = value;
		}
		public ICollection<string> Keys => this.map.Keys;
		public ICollection<JsonValue> Values => this.map.Values;
		public void Add(string key, JsonValue value)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));

			this.map.Add(key, value);
		}
		public void Add(JsonPair pair)
		{
			this.Add(pair.Key, pair.Value);
		}
		public void Clear()
		{
			this.map.Clear();
		}
		bool ICollection<JsonPair>.Contains(JsonPair item)
		{
			return (this.map as ICollection<JsonPair>).Contains(item);
		}
		bool ICollection<JsonPair>.Remove(JsonPair item)
		{
			return (this.map as ICollection<JsonPair>).Remove(item);
		}
		public override bool ContainsKey(string key)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));

			return this.map.ContainsKey(key);
		}
		public void CopyTo(JsonPair[] array, int arrayIndex)
		{
			(this.map as ICollection<JsonPair>).CopyTo(array, arrayIndex);
		}
		public bool Remove(string key)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));

			return this.map.Remove(key);
		}
		bool ICollection<JsonPair>.IsReadOnly => false;
		public bool TryGetValue(string key, out JsonValue value)
		{
			return this.map.TryGetValue(key, out value);
		}
		public void AddRange(JsonPairEnumerable items)
		{
			if (items == null)
				throw new ArgumentNullException(nameof(items));

			foreach (var pair in items)
				this.map.Add(pair.Key, pair.Value);
		}
		public void AddRange(params JsonPair[] items)
		{
			this.AddRange((JsonPairEnumerable)items);
		}
		public override void Save(Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			stream.WriteByte((byte)'{');
			var following = false;
			foreach (var pair in this.map)
			{
				if (following)
				{
					stream.WriteByte((byte)',');
					stream.WriteByte((byte)' ');
				}
				following = true;

				stream.WriteByte((byte)'"');
				var bytes = Encoding.UTF8.GetBytes(this.EscapeString(pair.Key));
				stream.Write(bytes, 0, bytes.Length);
				stream.WriteByte((byte)'"');
				stream.WriteByte((byte)':');
				stream.WriteByte((byte)' ');
				if (pair.Value == null)
				{
					stream.WriteByte((byte)'n');
					stream.WriteByte((byte)'u');
					stream.WriteByte((byte)'l');
					stream.WriteByte((byte)'l');
				}
				else
				{
					pair.Value.Save(stream);
				}
			}
			stream.WriteByte((byte)'}');
		}
		public override object ToObject(Type type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			var instance = Activator.CreateInstance(type);
			var members = GetTypeMembers(type);

			foreach (var pair in this.map)
			{
				var member = default(MemberInfo);
				if (members.TryGetValue(pair.Key, out member) == false)
					continue;

				var prop = member as PropertyInfo;
				var field = member as FieldInfo;
				var value = pair.Value;
				if (value != null)
				{
					if (prop != null && prop.CanWrite)
						prop.SetValue(instance, value.ToObject(prop.PropertyType), null);
					else if (field != null && field.IsInitOnly == false)
						field.SetValue(instance, value.ToObject(field.FieldType));
				}
				else
				{
					if (prop != null && prop.CanWrite)
						prop.SetValue(instance, null, null);
					else if (field != null && field.IsInitOnly == false)
						field.SetValue(instance, null);
				}
			}

			return instance;
		}
		public new static JsonValue From(object value)
		{
			if (value == null)
				return new JsonPrimitive(default(string));

			var members = GetTypeMembers(value.GetType());
			var pairs = new List<JsonPair>(members.Count);

			foreach (var memberKv in members)
			{
				var prop = memberKv.Value as PropertyInfo;
				var field = memberKv.Value as FieldInfo;
				var memberValue = default(object);
				if (prop != null && prop.CanWrite)
					memberValue = prop.GetValue(value, null);
				else if (field != null && field.IsInitOnly == false)
					memberValue = field.GetValue(value);
				pairs.Add(new JsonPair(memberKv.Key, JsonValue.From(memberValue)));
			}

			return new JsonObject(pairs);
		}
		private static Dictionary<string, MemberInfo> GetTypeMembers(Type type)
		{
			var members = default(Dictionary<string, MemberInfo>);
			lock (TypeMembers)
			{
				if (TypeMembers.TryGetValue(type, out members) == false)
				{
					var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
					var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);

					members = new Dictionary<string, MemberInfo>(StringComparer.OrdinalIgnoreCase);
					foreach (var prop in props)
					{
						if (prop.GetIndexParameters().Length != 0)
							continue;
						members[GetMemberName(prop)] = prop;
					}
					foreach (var field in fields)
					{
						members[GetMemberName(field)] = field;
					}

					TypeMembers.Add(type, members);
				}
			}
			return members;
		}
		private static string GetMemberName(MemberInfo member)
		{
			var jsonMember = member.GetCustomAttributes(typeof(DataMemberAttribute), true).OfType<DataMemberAttribute>().FirstOrDefault();
			if (jsonMember != null && string.IsNullOrEmpty(jsonMember.Name) == false)
				return jsonMember.Name;
			else
				return member.Name;
		}

	}
}
