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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Assets.Editor.GameDevWare.Charon.Json
{
	internal class JsonReader
	{
		private readonly TextReader r;
		private readonly StringBuilder vb = new StringBuilder();
		private bool hasPeek;
		private int line = 1, column;
		private int peek;
		private bool prevLf;

		public JsonReader(TextReader reader)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");
			this.r = reader;
		}
		public JsonValue Read()
		{
			var v = ReadCore();
			SkipSpaces();
			if (ReadChar() >= 0)
				throw JsonError(String.Format("extra characters in JSON input"));
			return v;
		}
		private JsonValue ReadCore()
		{
			SkipSpaces();
			var c = PeekChar();
			if (c < 0)
				throw JsonError("Incomplete JSON input");
			switch (c)
			{
				case '[':
					ReadChar();
					var list = new List<JsonValue>();
					SkipSpaces();
					if (PeekChar() == ']')
					{
						ReadChar();
						return new JsonArray(list);
					}
					while (true)
					{
						list.Add(ReadCore());
						SkipSpaces();
						c = PeekChar();
						if (c != ',')
							break;
						ReadChar();
					}
					if (ReadChar() != ']')
						throw JsonError("JSON array must end with ']'");
					return new JsonArray(list);
				case '{':
					ReadChar();
					var obj = new List<KeyValuePair<string, JsonValue>>();
					SkipSpaces();
					if (PeekChar() == '}')
					{
						ReadChar();
						return new JsonObject(obj);
					}
					while (true)
					{
						SkipSpaces();
						if (PeekChar() == '}')
						{
							ReadChar();
							break;
						}
						var name = ReadStringLiteral();
						SkipSpaces();
						Expect(':');
						SkipSpaces();
						obj.Add(new KeyValuePair<string, JsonValue>(name, ReadCore())); // it does not reject duplicate names.
						SkipSpaces();
						c = ReadChar();
						if (c == ',')
							continue;
						if (c == '}')
							break;
					}
					return new JsonObject(obj);

				case 't':
					Expect("true");
					return new JsonPrimitive(true);
				case 'f':
					Expect("false");
					return new JsonPrimitive(false);
				case 'n':
					Expect("null");
					return null;
				case '"':
					return new JsonPrimitive(ReadStringLiteral());
				default:
					if ('0' <= c && c <= '9' || c == '-')
						return ReadNumericLiteral();
					throw JsonError(String.Format("Unexpected character '{0}'", (char) c));
			}
		}
		private int PeekChar()
		{
			if (!hasPeek)
			{
				peek = r.Read();
				hasPeek = true;
			}
			return peek;
		}
		private int ReadChar()
		{
			var v = hasPeek ? peek : r.Read();

			hasPeek = false;

			if (prevLf)
			{
				line++;
				column = 0;
				prevLf = false;
			}

			if (v == '\n')
				prevLf = true;
			column++;

			return v;
		}
		private void SkipSpaces()
		{
			while (true)
			{
				switch (PeekChar())
				{
					case ' ':
					case '\t':
					case '\r':
					case '\n':
						ReadChar();
						continue;
					default:
						return;
				}
			}
		}
		// It could return either int, long or decimal, depending on the parsed value.
		private JsonValue ReadNumericLiteral()
		{
			var sb = new StringBuilder();

			if (PeekChar() == '-')
				sb.Append((char) ReadChar());

			int c;
			var x = 0;
			var zeroStart = PeekChar() == '0';
			for (;; x++)
			{
				c = PeekChar();
				if (c < '0' || '9' < c)
					break;
				sb.Append((char) ReadChar());
				if (zeroStart && x == 1)
					throw JsonError("leading zeros are not allowed");
			}
			if (x == 0) // Reached e.g. for "- "
				throw JsonError("Invalid JSON numeric literal; no digit found");

			// fraction
			var hasFrac = false;
			var fdigits = 0;
			if (PeekChar() == '.')
			{
				hasFrac = true;
				sb.Append((char) ReadChar());
				if (PeekChar() < 0)
					throw JsonError("Invalid JSON numeric literal; extra dot");
				while (true)
				{
					c = PeekChar();
					if (c < '0' || '9' < c)
						break;
					sb.Append((char) ReadChar());
					fdigits++;
				}
				if (fdigits == 0)
					throw JsonError("Invalid JSON numeric literal; extra dot");
			}

			c = PeekChar();
			if (c != 'e' && c != 'E')
			{
				if (!hasFrac)
				{
					int valueInt;
					if (int.TryParse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out valueInt))
						return valueInt;

					long valueLong;
					if (long.TryParse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out valueLong))
						return valueLong;

					ulong valueUlong;
					if (ulong.TryParse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out valueUlong))
						return valueUlong;
				}
				decimal valueDecimal;
				if (decimal.TryParse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out valueDecimal) && valueDecimal != 0)
					return valueDecimal;
			}
			else
			{
				// exponent
				sb.Append((char) ReadChar());
				if (PeekChar() < 0)
					throw new ArgumentException("Invalid JSON numeric literal; incomplete exponent");

				c = PeekChar();
				if (c == '-')
					sb.Append((char) ReadChar());
				else if (c == '+')
					sb.Append((char) ReadChar());

				if (PeekChar() < 0)
					throw JsonError("Invalid JSON numeric literal; incomplete exponent");
				while (true)
				{
					c = PeekChar();
					if (c < '0' || '9' < c)
						break;
					sb.Append((char) ReadChar());
				}
			}

			return double.Parse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture);
		}
		private string ReadStringLiteral()
		{
			if (PeekChar() != '"')
				throw JsonError("Invalid JSON string literal format");

			ReadChar();
			vb.Length = 0;
			while (true)
			{
				var c = ReadChar();
				if (c < 0)
					throw JsonError("JSON string is not closed");
				if (c == '"')
					return vb.ToString();
				if (c != '\\')
				{
					vb.Append((char) c);
					continue;
				}

				// escaped expression
				c = ReadChar();
				if (c < 0)
					throw JsonError("Invalid JSON string literal; incomplete escape sequence");
				switch (c)
				{
					case '"':
					case '\\':
					case '/':
						vb.Append((char) c);
						break;
					case 'b':
						vb.Append('\x8');
						break;
					case 'f':
						vb.Append('\f');
						break;
					case 'n':
						vb.Append('\n');
						break;
					case 'r':
						vb.Append('\r');
						break;
					case 't':
						vb.Append('\t');
						break;
					case 'u':
						ushort cp = 0;
						for (var i = 0; i < 4; i++)
						{
							cp <<= 4;
							if ((c = ReadChar()) < 0)
								throw JsonError("Incomplete unicode character escape literal");
							if ('0' <= c && c <= '9')
								cp += (ushort) (c - '0');
							if ('A' <= c && c <= 'F')
								cp += (ushort) (c - 'A' + 10);
							if ('a' <= c && c <= 'f')
								cp += (ushort) (c - 'a' + 10);
						}
						vb.Append((char) cp);
						break;
					default:
						throw JsonError("Invalid JSON string literal; unexpected escape character");
				}
			}
		}
		private void Expect(char expected)
		{
			int c;
			if ((c = ReadChar()) != expected)
				throw JsonError(String.Format("Expected '{0}', got '{1}'", expected, (char) c));
		}
		private void Expect(string expected)
		{
			for (var i = 0; i < expected.Length; i++)
			{
				if (ReadChar() != expected[i])
					throw JsonError(String.Format("Expected '{0}', differed at {1}", expected, i));
			}
		}
		private Exception JsonError(string msg)
		{
			return new ArgumentException(String.Format("{0}. At line {1}, column {2}", msg, line, column));
		}
	}
}
