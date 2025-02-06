using System;

namespace GameDevWare.Charon.Unity.Utils;

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