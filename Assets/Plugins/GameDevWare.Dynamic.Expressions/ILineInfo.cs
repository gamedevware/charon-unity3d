namespace GameDevWare.Dynamic.Expressions
{
	internal interface ILineInfo
	{
		int LineNumber { get; }
		int ColumnNumber { get; }
		int TokenLength { get; }
	}
}
