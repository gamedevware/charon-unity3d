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
using System.Runtime.Serialization;

namespace GameDevWare.Dynamic.Expressions
{
	public sealed class ExpressionParserException : Exception, ILineInfo
	{
		public int LineNumber { get; set; }
		public int ColumnNumber { get; set; }
		public int TokenLength { get; set; }

		public ExpressionParserException()
		{

		}
		public ExpressionParserException(string message, int lineNumber = 0, int columnNumber = 0, int tokenLength = 0)
			: base(message)
		{
			this.LineNumber = lineNumber;
			this.ColumnNumber = columnNumber;
			this.TokenLength = tokenLength;
		}
		public ExpressionParserException(string message, Exception innerException, int lineNumber = 0, int columnNumber = 0, int tokenLength = 0)
			: base(message, innerException)
		{
			this.LineNumber = lineNumber;
			this.ColumnNumber = columnNumber;
			this.TokenLength = tokenLength;
		}
		internal ExpressionParserException(string message, ILineInfo lineInfo)
			: base(message)
		{
			if (lineInfo == null)
				return;

			this.LineNumber = lineInfo.LineNumber;
			this.ColumnNumber = lineInfo.ColumnNumber;
			this.TokenLength = lineInfo.TokenLength;
		}
		internal ExpressionParserException(string message, Exception innerException, ILineInfo lineInfo)
			: base(message, innerException)
		{
			if (lineInfo == null)
				return;

			this.LineNumber = lineInfo.LineNumber;
			this.ColumnNumber = lineInfo.ColumnNumber;
			this.TokenLength = lineInfo.TokenLength;
		}
		// ReSharper disable once UnusedMember.Local
		private ExpressionParserException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			this.LineNumber = info.GetInt32("LineNumber");
			this.ColumnNumber = info.GetInt32("ColumnNumber");
			this.TokenLength = info.GetInt32("TokenLength");
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("LineNumber", (int)this.LineNumber);
			info.AddValue("ColumnNumber", (int)this.ColumnNumber);
			info.AddValue("TokenLength", (int)this.TokenLength);

			base.GetObjectData(info, context);
		}

		public override string ToString()
		{
			if (this.TokenLength != 0)
				return string.Format("[{0},{1}+{2}]{3}", this.LineNumber, this.ColumnNumber, this.TokenLength, base.ToString());
			else
				return base.ToString();
		}
	}
}
