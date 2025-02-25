/*
	Copyright (c) 2025 Denis Zykov

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

using System;

namespace GameDevWare.Charon.Editor.Cli
{
	public class ListFilter
	{
		public string PropertyName;
		public ListFilterOperation Operation;
		public string Value;

		public ListFilter(string propertyName, ListFilterOperation operation, string value)
		{
			if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));
			if (value == null) throw new ArgumentNullException(nameof(value));

			this.PropertyName = propertyName;
			this.Operation = operation;
			this.Value = value;
		}

		public string GetOperationName()
		{
			switch (this.Operation)
			{
				case ListFilterOperation.GreaterThan: return "GreaterThan";
				case ListFilterOperation.GreaterThanOrEqual: return "GreaterThanOrEqual";
				case ListFilterOperation.LessThan: return "LessThan";
				case ListFilterOperation.LessThanOrEqual: return "LessThanOrEqual";
				case ListFilterOperation.Equal: return "Equal";
				case ListFilterOperation.NotEqual: return "NotEqual";
				case ListFilterOperation.Like: return "Like";
				case ListFilterOperation.In: return "In";
				default: return "INVALID_OPERATION";
			}
		}

		public string GetValueQuoted()
		{
			bool isQuoted = this.Value.StartsWith("\"") && this.Value.EndsWith("\"");
			bool bHasInvalidChars = !isQuoted && (this.Value.IndexOf(" ", StringComparison.Ordinal) >= 0 || this.Value.IndexOf("\"", StringComparison.Ordinal) > 0);

			if (string.IsNullOrEmpty(this.Value))
			{
				return "\"\"";
			}
			else if (bHasInvalidChars)
			{
				return string.Concat("\"", this.Value.Replace("\"", "\"\""), "\"");
			}
			else
			{
				return this.Value;
			}
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.PropertyName + " " + this.GetOperationName() + " " + this.GetValueQuoted();
		}
	}
}
