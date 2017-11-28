using System;

namespace Assets.Scripts
{
	public sealed class InventoryItem
	{
		private readonly Item itemParams;

		public string Name { get { return this.itemParams.Name; } }
		public string Description { get { return this.itemParams.Description; } }
		public int Count;

		public InventoryItem(Item itemParams, int count)
		{
			if (itemParams == null) throw new ArgumentNullException("itemParams");

			this.itemParams = itemParams;
			this.Count = count;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.Name + " x" + this.Count;
		}
	}
}