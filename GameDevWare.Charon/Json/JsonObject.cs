using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using JsonPair = System.Collections.Generic.KeyValuePair<string, Assets.Editor.GameDevWare.Charon.Json.JsonValue>;
using JsonPairEnumerable = System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, Assets.Editor.GameDevWare.Charon.Json.JsonValue>>;

namespace Assets.Editor.GameDevWare.Charon.Json
{
	public class JsonObject : JsonValue, IDictionary<string, JsonValue>, ICollection<KeyValuePair<string, JsonValue>>
	{
		private static readonly Dictionary<Type, Dictionary<string, MemberInfo>> TypeMembers = new Dictionary<Type, Dictionary<string, MemberInfo>>();
		// Use SortedDictionary to make result of ToString() deterministic
		private readonly SortedDictionary<string, JsonValue> map;
		public JsonObject(params JsonPair[] items)
		{
			map = new SortedDictionary<string, JsonValue>(StringComparer.Ordinal);

			if (items != null)
				AddRange(items);
		}
		public JsonObject(JsonPairEnumerable items)
		{
			if (items == null)
				throw new ArgumentNullException("items");

			map = new SortedDictionary<string, JsonValue>(StringComparer.Ordinal);
			AddRange(items);
		}
		public override JsonType JsonType
		{
			get { return JsonType.Object; }
		}
		public override int Count
		{
			get { return map.Count; }
		}
		public IEnumerator<JsonPair> GetEnumerator()
		{
			return map.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return map.GetEnumerator();
		}
		public override sealed JsonValue this[string key]
		{
			get { return map[key]; }
			set { map[key] = value; }
		}
		public ICollection<string> Keys
		{
			get { return map.Keys; }
		}
		public ICollection<JsonValue> Values
		{
			get { return map.Values; }
		}
		public void Add(string key, JsonValue value)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			map.Add(key, value);
		}
		public void Add(JsonPair pair)
		{
			Add(pair.Key, pair.Value);
		}
		public void Clear()
		{
			map.Clear();
		}
		bool ICollection<JsonPair>.Contains(JsonPair item)
		{
			return (map as ICollection<JsonPair>).Contains(item);
		}
		bool ICollection<JsonPair>.Remove(JsonPair item)
		{
			return (map as ICollection<JsonPair>).Remove(item);
		}
		public override bool ContainsKey(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			return map.ContainsKey(key);
		}
		public void CopyTo(JsonPair[] array, int arrayIndex)
		{
			(map as ICollection<JsonPair>).CopyTo(array, arrayIndex);
		}
		public bool Remove(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			return map.Remove(key);
		}
		bool ICollection<JsonPair>.IsReadOnly
		{
			get { return false; }
		}
		public bool TryGetValue(string key, out JsonValue value)
		{
			return map.TryGetValue(key, out value);
		}
		public void AddRange(JsonPairEnumerable items)
		{
			if (items == null)
				throw new ArgumentNullException("items");

			foreach (var pair in items)
				map.Add(pair.Key, pair.Value);
		}
		public void AddRange(params JsonPair[] items)
		{
			AddRange((JsonPairEnumerable)items);
		}
		public override void Save(Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");
			stream.WriteByte((byte)'{');
			foreach (var pair in map)
			{
				stream.WriteByte((byte)'"');
				var bytes = Encoding.UTF8.GetBytes(EscapeString(pair.Key));
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
			var instance = Activator.CreateInstance(type);
			var members = (Dictionary<string, MemberInfo>)GetTypeMembers(type);

			foreach (var pair in this.map)
			{
				var member = default(MemberInfo);
				if (members.TryGetValue(pair.Key, out member) == false)
					continue;

				var prop = member as PropertyInfo;
				var field = member as FieldInfo;
				if (prop != null && prop.CanWrite)
					prop.SetValue(instance, pair.Value.As(prop.PropertyType), null);
				else if (field != null && field.IsInitOnly == false)
					field.SetValue(instance, pair.Value.As(field.FieldType));
			}

			return instance;
		}
		new public static JsonValue From(object value)
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

			return new JsonObject(pairs); ;
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
						members[prop.Name] = prop;
					}
					foreach (var field in fields)
						members[field.Name] = field;

					TypeMembers.Add(type, members);
				}
			}
			return members;
		}

	}
}
