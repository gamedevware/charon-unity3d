using System;

namespace GameDevWare.Charon.Unity.Utils;

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