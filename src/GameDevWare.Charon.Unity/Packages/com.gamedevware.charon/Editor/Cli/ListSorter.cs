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
	public class ListSorter
	{
		public string PropertyName;
		public ListSorterDirection Direction;

		public ListSorter(string propertyName, ListSorterDirection direction)
		{
			if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));

			this.PropertyName = propertyName;
			this.Direction = direction;
		}

		public string GetDirectionName()
		{
			switch (this.Direction)
			{
				case ListSorterDirection.Ascending: return "ASC";
				case ListSorterDirection.Descending: return "DESC";
				default: return "UNKNOWN_DIRECTION";
			}
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.PropertyName + " " + this.GetDirectionName();
		}
	}
}
