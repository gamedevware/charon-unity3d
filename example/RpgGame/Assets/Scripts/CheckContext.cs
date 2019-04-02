namespace Assets.Scripts
{
	// Class for demonstration of Formula data type
	public sealed class CheckContext
	{
		public Dungeon Dungeon { get; private set; }
		public Character Source { get; private set; }
		public Character Target { get; private set; }
		public Combat Combat { get; private set; }
		public Attack Attack { get; private set; }
	}

	public sealed class Combat
	{
		public int Round { get; private set; }
	}

	public class Attack
	{
		public AttackType Type { get; private set; }
	}

	public enum AttackType
	{
		Ranged,
		Melee
	}
}
