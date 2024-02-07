using System;

namespace Assets.Scripts
{
	public sealed class Dungeon
	{
		private readonly Location location;

		public string Id { get { return this.location.Id; } }
		public string Name { get { return this.location.Name; } }
		public int LightLevel { get; private set; }

		public Dungeon(Location location)
		{
			if (location == null) throw new ArgumentNullException("location");

			this.location = location;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.location.Name;
		}
	}
}