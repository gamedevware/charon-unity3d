using System;
using System.Collections.Generic;

namespace Assets.Scripts
{
	public class GameWorld
	{
		public Dungeon Dungeon { get; private set; }

		public List<InventoryItem> Inventory { get; private set; }
		public List<Character> Characters { get; private set; }

		public GameWorld(RpgGameData gameData)
		{
			if (gameData == null) throw new ArgumentNullException("gameData");

			this.Inventory = new List<InventoryItem>();
			foreach (var itemWithCount in gameData.StartingSet.Items)
			{
				var item = new InventoryItem(itemWithCount.Item, itemWithCount.Count);
				this.Inventory.Add(item);
			}

			this.Characters = new List<Character>();
			foreach (var hero in gameData.StartingSet.Heroes)
			{
				var character = new Character(hero);
				this.Characters.Add(character);
			}

			this.Dungeon = new Dungeon(gameData.StartingSet.Location);
		}
	}
}