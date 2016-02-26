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
