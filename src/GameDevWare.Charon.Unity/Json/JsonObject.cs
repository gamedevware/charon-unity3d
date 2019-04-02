using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using JsonPair = System.Collections.Generic.KeyValuePair<string, GameDevWare.Charon.Unity.Json.JsonValue>;
using JsonPairEnumerable = System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, GameDevWare.Charon.Unity.Json.JsonValue>>;

namespace GameDevWare.Charon.Unity.Json
{
	internal class JsonObject : JsonValue, IDictionary<string, JsonValue>, ICollection<KeyValuePair<string, JsonValue>>
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
				throw new ArgumentNullException("items");

			this.map = new SortedDictionary<string, JsonValue>(StringComparer.Ordinal);
			this.AddRange(items);
		}
		public override JsonType JsonType
		{
			get { return JsonType.Object; }
		}
		public override int Count
		{
			get { return this.map.Count; }
		}
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
			get { return this.map[key]; }
			set { this.map[key] = value; }
		}
		public ICollection<string> Keys
		{
			get { return this.map.Keys; }
		}
		public ICollection<JsonValue> Values
		{
			get { return this.map.Values; }
		}
		public void Add(string key, JsonValue value)
		{
			if (key == null)
				throw new ArgumentNullException("key");

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
				throw new ArgumentNullException("key");

			return this.map.ContainsKey(key);
		}
		public void CopyTo(JsonPair[] array, int arrayIndex)
		{
			(this.map as ICollection<JsonPair>).CopyTo(array, arrayIndex);
		}
		public bool Remove(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			return this.map.Remove(key);
		}
		bool ICollection<JsonPair>.IsReadOnly
		{
			get { return false; }
		}
		public bool TryGetValue(string key, out JsonValue value)
		{
			return this.map.TryGetValue(key, out value);
		}
		public void AddRange(JsonPairEnumerable items)
		{
			if (items == null)
				throw new ArgumentNullException("items");

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
				throw new ArgumentNullException("stream");
			stream.WriteByte((byte)'{');
			foreach (var pair in this.map)
			{
				stream.WriteByte((byte)'"');
				var bytes = Encoding.UTF8.GetBytes(this.EscapeString(pair.Key));
				stream.Write(bytes, 0, bytes.Length);
				stream.WriteByte((byte)'"');
				stream.WriteByte((byte)',');
				stream.WriteByte((byte)' ');
				if (pair.Value == null)
				{
					stream.WriteByte((byte)'n');
					stream.WriteByte((byte)'u');
					stream.WriteByte((byte)'l');
					stream.WriteByte((byte)'l');
				}
				else
					pair.Value.Save(stream);
			}
			stream.WriteByte((byte)'}');
		}
		public override object As(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

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
						prop.SetValue(instance, value.As(prop.PropertyType), null);
					else if (field != null && field.IsInitOnly == false)
						field.SetValue(instance, value.As(field.FieldType));
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
			var jsonMember = member.GetCustomAttributes(typeof(JsonMemberAttribute), true).FirstOrDefault() as JsonMemberAttribute;
			if (jsonMember != null && string.IsNullOrEmpty(jsonMember.Name) == false)
				return jsonMember.Name;
			else
				return member.Name;
		}

	}
}
