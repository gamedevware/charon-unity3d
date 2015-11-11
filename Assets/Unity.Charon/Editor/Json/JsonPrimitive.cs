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
using System.Globalization;
using System.IO;
using System.Text;

namespace Assets.Unity.Charon.Editor.Json
{
	public class JsonPrimitive : JsonValue
	{
		private static readonly byte[] true_bytes = Encoding.UTF8.GetBytes("true");
		private static readonly byte[] false_bytes = Encoding.UTF8.GetBytes("false");
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
				if (Value == null)
					return JsonType.String;

				switch (Type.GetTypeCode(Value.GetType()))
				{
					case TypeCode.Boolean:
						return JsonType.Boolean;
					case TypeCode.Char:
					case TypeCode.String:
					case TypeCode.DateTime:
					case TypeCode.Object: // DateTimeOffset || Guid || TimeSpan || Uri
						return JsonType.String;
					default:
						return JsonType.Number;
				}
			}
		}
		public override void Save(Stream stream)
		{
			switch (JsonType)
			{
				case JsonType.Boolean:
					if ((bool) Value)
						stream.Write(true_bytes, 0, 4);
					else
						stream.Write(false_bytes, 0, 5);
					break;
				case JsonType.String:
					stream.WriteByte((byte) '\"');
					var bytes = Encoding.UTF8.GetBytes(EscapeString(Value.ToString()));
					stream.Write(bytes, 0, bytes.Length);
					stream.WriteByte((byte) '\"');
					break;
				default:
					bytes = Encoding.UTF8.GetBytes(GetFormattedString());
					stream.Write(bytes, 0, bytes.Length);
					break;
			}
		}
		internal string GetFormattedString()
		{
			switch (JsonType)
			{
				case JsonType.String:
					if (Value is string || Value == null)
						return (string) Value;
					if (Value is char)
						return Value.ToString();
					throw new NotImplementedException("GetFormattedString from value type " + Value.GetType());
				case JsonType.Number:
					string s;
					if (Value is float || Value is double)
						// Use "round-trip" format
						s = ((IFormattable) Value).ToString("R", NumberFormatInfo.InvariantInfo);
					else
						s = ((IFormattable) Value).ToString("G", NumberFormatInfo.InvariantInfo);
					if (s == "NaN" || s == "Infinity" || s == "-Infinity")
						return "\"" + s + "\"";
					return s;
				default:
					throw new InvalidOperationException();
			}
		}
		public override object As(Type type)
		{
			if (type.IsEnum)
				return Enum.Parse(type, Convert.ToString(this.Value, CultureInfo.InvariantCulture), true);
			return Convert.ChangeType(this.Value, type, CultureInfo.InvariantCulture);
		}
	}
}
