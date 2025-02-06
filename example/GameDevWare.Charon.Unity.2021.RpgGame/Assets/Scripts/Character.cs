using System;
using System.Linq;

namespace Assets.Scripts
{
	public class Character
	{
		private readonly Hero characterParams;

		public string Name { get { return this.characterParams.Name; } }
		public string Bio { get { return this.characterParams.Bio; } }

		public int HitPoints { get; private set; }
		public int MaxHitPoints { get; private set; }

		public int Stress { get; private set; }

		public Character(Hero characterParams)
		{
			if (characterParams == null) throw new ArgumentNullException("characterParams");

			this.characterParams = characterParams;

			this.HitPoints = characterParams.Armors.AsList.First().HitPoints;
			this.MaxHitPoints = this.HitPoints;
		}

		public bool Is(MonsterType type)
		{
			return type == MonsterType.Human;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.Name;
		}
	}
}