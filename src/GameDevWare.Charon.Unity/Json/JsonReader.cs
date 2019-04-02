using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace GameDevWare.Charon.Unity.Json
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
			var v = this.ReadCore();
			this.SkipSpaces();
			if (this.ReadChar() >= 0)
				throw this.JsonError("extra characters in JSON input");
			return v;
		}
		private JsonValue ReadCore()
		{
			this.SkipSpaces();
			var c = this.PeekChar();
			if (c < 0)
				throw this.JsonError("Incomplete JSON input");
			switch (c)
			{
				case '[':
					this.ReadChar();
					var list = new List<JsonValue>();
					this.SkipSpaces();
					if (this.PeekChar() == ']')
					{
						this.ReadChar();
						return new JsonArray(list);
					}
					while (true)
					{
						list.Add(this.ReadCore());
						this.SkipSpaces();
						c = this.PeekChar();
						if (c != ',')
							break;

						this.ReadChar();
					}
					if (this.ReadChar() != ']')
						throw this.JsonError("JSON array must end with ']'");
					return new JsonArray(list);
				case '{':
					this.ReadChar();
					var obj = new List<KeyValuePair<string, JsonValue>>();
					this.SkipSpaces();
					if (this.PeekChar() == '}')
					{
						this.ReadChar();
						return new JsonObject(obj);
					}
					while (true)
					{
						this.SkipSpaces();
						if (this.PeekChar() == '}')
						{
							this.ReadChar();
							break;
						}
						var name = this.ReadStringLiteral();
						this.SkipSpaces();
						this.Expect(':');
						this.SkipSpaces();
						obj.Add(new KeyValuePair<string, JsonValue>(name, this.ReadCore())); // it does not reject duplicate names.
						this.SkipSpaces();
						c = this.ReadChar();
						if (c == ',')
							continue;
						if (c == '}')
							break;
					}
					return new JsonObject(obj);

				case 't':
					this.Expect("true");
					return new JsonPrimitive(true);
				case 'f':
					this.Expect("false");
					return new JsonPrimitive(false);
				case 'n':
					this.Expect("null");
					return null;
				case '"':
					return new JsonPrimitive(this.ReadStringLiteral());
				default:
					if ('0' <= c && c <= '9' || c == '-')
						return this.ReadNumericLiteral();
					throw this.JsonError(string.Format("Unexpected character '{0}'", (char) c));
			}
		}
		private int PeekChar()
		{
			if (!this.hasPeek)
			{
				this.peek = this.r.Read();
				this.hasPeek = true;
			}
			return this.peek;
		}
		private int ReadChar()
		{
			var v = this.hasPeek ? this.peek : this.r.Read();

			this.hasPeek = false;

			if (this.prevLf)
			{
				this.line++;
				this.column = 0;
				this.prevLf = false;
			}

			if (v == '\n')
				this.prevLf = true;
			this.column++;

			return v;
		}
		private void SkipSpaces()
		{
			while (true)
			{
				switch (this.PeekChar())
				{
					case ' ':
					case '\t':
					case '\r':
					case '\n':
						this.ReadChar();
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

			if (this.PeekChar() == '-')
				sb.Append((char)this.ReadChar());

			int c;
			var x = 0;
			var zeroStart = this.PeekChar() == '0';
			for (;; x++)
			{
				c = this.PeekChar();
				if (c < '0' || '9' < c)
					break;
				sb.Append((char)this.ReadChar());
				if (zeroStart && x == 1)
					throw this.JsonError("leading zeros are not allowed");
			}
			if (x == 0) // Reached e.g. for "- "
				throw this.JsonError("Invalid JSON numeric literal; no digit found");

			// fraction
			var hasFrac = false;
			var fdigits = 0;
			if (this.PeekChar() == '.')
			{
				hasFrac = true;
				sb.Append((char)this.ReadChar());
				if (this.PeekChar() < 0)
					throw this.JsonError("Invalid JSON numeric literal; extra dot");
				while (true)
				{
					c = this.PeekChar();
					if (c < '0' || '9' < c)
						break;
					sb.Append((char)this.ReadChar());
					fdigits++;
				}
				if (fdigits == 0)
					throw this.JsonError("Invalid JSON numeric literal; extra dot");
			}

			c = this.PeekChar();
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
				sb.Append((char)this.ReadChar());
				if (this.PeekChar() < 0)
					throw new ArgumentException("Invalid JSON numeric literal; incomplete exponent");

				c = this.PeekChar();
				if (c == '-')
					sb.Append((char)this.ReadChar());
				else if (c == '+')
					sb.Append((char)this.ReadChar());

				if (this.PeekChar() < 0)
					throw this.JsonError("Invalid JSON numeric literal; incomplete exponent");
				while (true)
				{
					c = this.PeekChar();
					if (c < '0' || '9' < c)
						break;
					sb.Append((char)this.ReadChar());
				}
			}

			return double.Parse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture);
		}
		private string ReadStringLiteral()
		{
			if (this.PeekChar() != '"')
				throw this.JsonError("Invalid JSON string literal format");

			this.ReadChar();
			this.vb.Length = 0;
			while (true)
			{
				var c = this.ReadChar();
				if (c < 0)
					throw this.JsonError("JSON string is not closed");
				if (c == '"')
					return this.vb.ToString();
				if (c != '\\')
				{
					this.vb.Append((char) c);
					continue;
				}

				// escaped expression
				c = this.ReadChar();
				if (c < 0)
					throw this.JsonError("Invalid JSON string literal; incomplete escape sequence");
				switch (c)
				{
					case '"':
					case '\\':
					case '/':
						this.vb.Append((char) c);
						break;
					case 'b':
						this.vb.Append('\x8');
						break;
					case 'f':
						this.vb.Append('\f');
						break;
					case 'n':
						this.vb.Append('\n');
						break;
					case 'r':
						this.vb.Append('\r');
						break;
					case 't':
						this.vb.Append('\t');
						break;
					case 'u':
						ushort cp = 0;
						for (var i = 0; i < 4; i++)
						{
							cp <<= 4;
							if ((c = this.ReadChar()) < 0)
								throw this.JsonError("Incomplete unicode character escape literal");
							if ('0' <= c && c <= '9')
								cp += (ushort) (c - '0');
							if ('A' <= c && c <= 'F')
								cp += (ushort) (c - 'A' + 10);
							if ('a' <= c && c <= 'f')
								cp += (ushort) (c - 'a' + 10);
						}

						this.vb.Append((char) cp);
						break;
					default:
						throw this.JsonError("Invalid JSON string literal; unexpected escape character");
				}
			}
		}
		private void Expect(char expected)
		{
			int c;
			if ((c = this.ReadChar()) != expected)
				throw this.JsonError(string.Format("Expected '{0}', got '{1}'", expected, (char) c));
		}
		private void Expect(string expected)
		{
			for (var i = 0; i < expected.Length; i++)
			{
				if (this.ReadChar() != expected[i])
					throw this.JsonError(string.Format("Expected '{0}', differed at {1}", expected, i));
			}
		}
		private Exception JsonError(string msg)
		{
			return new ArgumentException(string.Format("{0}. At line {1}, column {2}", msg, this.line, this.column));
		}
	}
}
