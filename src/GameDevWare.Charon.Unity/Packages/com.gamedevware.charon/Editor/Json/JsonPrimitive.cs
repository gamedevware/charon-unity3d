/*
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
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using GameDevWare.Charon.Unity.Utils;
using JetBrains.Annotations;

namespace GameDevWare.Charon.Unity.Json
{
	[PublicAPI, UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal class JsonPrimitive : JsonValue
	{
		private static readonly byte[] TrueBytes = Encoding.UTF8.GetBytes("true");
		private static readonly byte[] FalseBytes = Encoding.UTF8.GetBytes("false");
		public JsonPrimitive(bool value)
		{
			this.Value = value;
		}
		public JsonPrimitive(byte value)
		{
			this.Value = value;
		}
		public JsonPrimitive(char value)
		{
			this.Value = value;
		}
		public JsonPrimitive(decimal value)
		{
			this.Value = value;
		}
		public JsonPrimitive(double value)
		{
			this.Value = value;
		}
		public JsonPrimitive(float value)
		{
			this.Value = value;
		}
		public JsonPrimitive(int value)
		{
			this.Value = value;
		}
		public JsonPrimitive(long value)
		{
			this.Value = value;
		}
		public JsonPrimitive(sbyte value)
		{
			this.Value = value;
		}
		public JsonPrimitive(short value)
		{
			this.Value = value;
		}
		public JsonPrimitive(string value)
		{
			this.Value = value;
		}
		public JsonPrimitive(DateTime value)
		{
			this.Value = value;
		}
		public JsonPrimitive(uint value)
		{
			this.Value = value;
		}
		public JsonPrimitive(ulong value)
		{
			this.Value = value;
		}
		public JsonPrimitive(ushort value)
		{
			this.Value = value;
		}
		public JsonPrimitive(DateTimeOffset value)
		{
			this.Value = value;
		}
		public JsonPrimitive(Guid value)
		{
			this.Value = value;
		}
		public JsonPrimitive(TimeSpan value)
		{
			this.Value = value;
		}
		public JsonPrimitive(Uri value)
		{
			this.Value = value;
		}
		internal object Value { get; private set; }
		public override JsonType JsonType
		{
			get
			{
				// FIXME: what should we do for null? Handle it as null so far.
				if (this.Value == null)
					return JsonType.String;

				switch (Type.GetTypeCode(this.Value.GetType()))
				{
					case TypeCode.Boolean:
						return JsonType.Boolean;
					case TypeCode.Char:
					case TypeCode.String:
					case TypeCode.DateTime:
					case TypeCode.Object: // DateTimeOffset || Guid || TimeSpan || Uri
						return JsonType.String;
					case TypeCode.Empty:
					case TypeCode.DBNull:
					case TypeCode.SByte:
					case TypeCode.Byte:
					case TypeCode.Int16:
					case TypeCode.UInt16:
					case TypeCode.Int32:
					case TypeCode.UInt32:
					case TypeCode.Int64:
					case TypeCode.UInt64:
					case TypeCode.Single:
					case TypeCode.Double:
					case TypeCode.Decimal:
					default:
						return JsonType.Number;
				}
			}
		}
		public override void Save(Stream stream)
		{
			switch (this.JsonType)
			{
				case JsonType.Boolean:
					if ((bool)this.Value)
						stream.Write(TrueBytes, 0, 4);
					else
						stream.Write(FalseBytes, 0, 5);
					break;
				case JsonType.String:
					stream.WriteByte((byte)'\"');
					var bytes = Encoding.UTF8.GetBytes(this.EscapeString(this.Value.ToString()));
					stream.Write(bytes, 0, bytes.Length);
					stream.WriteByte((byte)'\"');
					break;
				case JsonType.Number:
				case JsonType.Object:
				case JsonType.Array:
				default:
					bytes = Encoding.UTF8.GetBytes(this.GetFormattedString());
					stream.Write(bytes, 0, bytes.Length);
					break;
			}
		}
		internal string GetFormattedString()
		{
			switch (this.JsonType)
			{
				case JsonType.String:
					if (this.Value is string || this.Value == null)
						return (string)this.Value;
					if (this.Value is char)
						return this.Value.ToString();
					if (this.Value is DateTime)
						return ((DateTime)this.Value).ToString("o");
					throw new NotImplementedException("GetFormattedString from value type " + this.Value.GetType());
				case JsonType.Number:
					string s;
					if (this.Value is float || this.Value is double)
						// Use "round-trip" format
						s = ((IFormattable)this.Value).ToString("R", NumberFormatInfo.InvariantInfo);
					else
						s = ((IFormattable)this.Value).ToString("G", NumberFormatInfo.InvariantInfo);
					if (s == "NaN" || s == "Infinity" || s == "-Infinity")
						return "\"" + s + "\"";
					return s;
				case JsonType.Object:
				case JsonType.Array:
				case JsonType.Boolean:
				default:
					throw new InvalidOperationException();
			}
		}
		public override object As(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			var value = this.Value;
			if (value == null)
			{
				return type.IsValueType ? Activator.CreateInstance(type) : null;
			}
			else if (type == typeof(object))
			{
				return value;
			}

			try
			{
				if (type.IsEnum)
				{
					return Enum.Parse(type, Convert.ToString(value, CultureInfo.InvariantCulture) ?? "", true);
				}

				var converter = TypeDescriptor.GetConverter(type);
				if (converter.CanConvertFrom(typeof(string)))
				{
					return converter.ConvertFromInvariantString((string)Convert.ChangeType(value, typeof(string), CultureInfo.InvariantCulture));
				}
				else if (converter.CanConvertFrom(value.GetType()))
				{
					return converter.ConvertFrom(value);
				}
				else if (type == typeof(Version))
				{
					return new Version((string)Convert.ChangeType(value, typeof(string), CultureInfo.InvariantCulture));
				}
				else if (type == typeof(SemanticVersion))
				{
					return new SemanticVersion((string)Convert.ChangeType(value, typeof(string), CultureInfo.InvariantCulture));
				}
				else if (type == typeof(DateTime))
				{
					return DateTime.Parse((string)Convert.ChangeType(value, typeof(string), CultureInfo.InvariantCulture), CultureInfo.InvariantCulture,
						DateTimeStyles.AssumeUniversal);
				}
				else if (type == typeof(DateTimeOffset))
				{
					return DateTimeOffset.Parse((string)Convert.ChangeType(value, typeof(string), CultureInfo.InvariantCulture), CultureInfo.InvariantCulture,
						DateTimeStyles.AssumeUniversal);
				}
				else
				{
					return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
				}
			}
			catch (FormatException formatException)
			{
				throw new FormatException(string.Format("Failed to convert '{0}' to {1} type.", value, type.Name), formatException);
			}
		}
	}
}
