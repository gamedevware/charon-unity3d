//------------------------------------------------------------------------------
// <auto-generated>
//	 This code was generated by a tool.
//	 Changes to this file may cause incorrect behavior and will be lost if
//	 the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// ReSharper disable All

using System;
using GameDevWare.Charon;
using JetBrains.Annotations;
using UnityEngine;

namespace Assets.Scripts
{
	[PublicAPI, Serializable]
	public class ProjectSettingDocumentReference: GameDataDocumentReference
	{
		public string predefinedSchemaNameOrId = Assets.Scripts.ProjectSetting.SchemaName;

		[CanBeNull]
		public Assets.Scripts.ProjectSetting GetReferencedDocument()
		{
			if (this.IsEmpty)
			{
				return null;
			}

			return (Assets.Scripts.ProjectSetting)this.gameData?.FindGameDataDocumentById(this.schemaNameOrId, this.id);
		}
	}
	[PublicAPI, Serializable]
	public class ParameterDocumentReference: GameDataDocumentReference
	{
		public string predefinedSchemaNameOrId = Assets.Scripts.Parameter.SchemaName;

		[CanBeNull]
		public Assets.Scripts.Parameter GetReferencedDocument()
		{
			if (this.IsEmpty)
			{
				return null;
			}

			return (Assets.Scripts.Parameter)this.gameData?.FindGameDataDocumentById(this.schemaNameOrId, this.id);
		}
	}
	[PublicAPI, Serializable]
	public class ParameterValueDocumentReference: GameDataDocumentReference
	{
		public string predefinedSchemaNameOrId = Assets.Scripts.ParameterValue.SchemaName;

		[CanBeNull]
		public Assets.Scripts.ParameterValue GetReferencedDocument()
		{
			if (this.IsEmpty)
			{
				return null;
			}

			return (Assets.Scripts.ParameterValue)this.gameData?.FindGameDataDocumentById(this.schemaNameOrId, this.id);
		}
	}
	[PublicAPI, Serializable]
	public class ProvisionDocumentReference: GameDataDocumentReference
	{
		public string predefinedSchemaNameOrId = Assets.Scripts.Provision.SchemaName;

		[CanBeNull]
		public Assets.Scripts.Provision GetReferencedDocument()
		{
			if (this.IsEmpty)
			{
				return null;
			}

			return (Assets.Scripts.Provision)this.gameData?.FindGameDataDocumentById(this.schemaNameOrId, this.id);
		}
	}
	[PublicAPI, Serializable]
	public class HeroDocumentReference: GameDataDocumentReference
	{
		public string predefinedSchemaNameOrId = Assets.Scripts.Hero.SchemaName;

		[CanBeNull]
		public Assets.Scripts.Hero GetReferencedDocument()
		{
			if (this.IsEmpty)
			{
				return null;
			}

			return (Assets.Scripts.Hero)this.gameData?.FindGameDataDocumentById(this.schemaNameOrId, this.id);
		}
	}
	[PublicAPI, Serializable]
	public class ItemDocumentReference: GameDataDocumentReference
	{
		public string predefinedSchemaNameOrId = Assets.Scripts.Item.SchemaName;

		[CanBeNull]
		public Assets.Scripts.Item GetReferencedDocument()
		{
			if (this.IsEmpty)
			{
				return null;
			}

			return (Assets.Scripts.Item)this.gameData?.FindGameDataDocumentById(this.schemaNameOrId, this.id);
		}
	}
	[PublicAPI, Serializable]
	public class LocationDocumentReference: GameDataDocumentReference
	{
		public string predefinedSchemaNameOrId = Assets.Scripts.Location.SchemaName;

		[CanBeNull]
		public Assets.Scripts.Location GetReferencedDocument()
		{
			if (this.IsEmpty)
			{
				return null;
			}

			return (Assets.Scripts.Location)this.gameData?.FindGameDataDocumentById(this.schemaNameOrId, this.id);
		}
	}
	[PublicAPI, Serializable]
	public class TrinketDocumentReference: GameDataDocumentReference
	{
		public string predefinedSchemaNameOrId = Assets.Scripts.Trinket.SchemaName;

		[CanBeNull]
		public Assets.Scripts.Trinket GetReferencedDocument()
		{
			if (this.IsEmpty)
			{
				return null;
			}

			return (Assets.Scripts.Trinket)this.gameData?.FindGameDataDocumentById(this.schemaNameOrId, this.id);
		}
	}
	[PublicAPI, Serializable]
	public class MonsterDocumentReference: GameDataDocumentReference
	{
		public string predefinedSchemaNameOrId = Assets.Scripts.Monster.SchemaName;

		[CanBeNull]
		public Assets.Scripts.Monster GetReferencedDocument()
		{
			if (this.IsEmpty)
			{
				return null;
			}

			return (Assets.Scripts.Monster)this.gameData?.FindGameDataDocumentById(this.schemaNameOrId, this.id);
		}
	}
	[PublicAPI, Serializable]
	public class LootDocumentReference: GameDataDocumentReference
	{
		public string predefinedSchemaNameOrId = Assets.Scripts.Loot.SchemaName;

		[CanBeNull]
		public Assets.Scripts.Loot GetReferencedDocument()
		{
			if (this.IsEmpty)
			{
				return null;
			}

			return (Assets.Scripts.Loot)this.gameData?.FindGameDataDocumentById(this.schemaNameOrId, this.id);
		}
	}
	[PublicAPI, Serializable]
	public class CombatEffectDocumentReference: GameDataDocumentReference
	{
		public string predefinedSchemaNameOrId = Assets.Scripts.CombatEffect.SchemaName;

		[CanBeNull]
		public Assets.Scripts.CombatEffect GetReferencedDocument()
		{
			if (this.IsEmpty)
			{
				return null;
			}

			return (Assets.Scripts.CombatEffect)this.gameData?.FindGameDataDocumentById(this.schemaNameOrId, this.id);
		}
	}
	[PublicAPI, Serializable]
	public class CurioCleansingOptionDocumentReference: GameDataDocumentReference
	{
		public string predefinedSchemaNameOrId = Assets.Scripts.CurioCleansingOption.SchemaName;

		[CanBeNull]
		public Assets.Scripts.CurioCleansingOption GetReferencedDocument()
		{
			if (this.IsEmpty)
			{
				return null;
			}

			return (Assets.Scripts.CurioCleansingOption)this.gameData?.FindGameDataDocumentById(this.schemaNameOrId, this.id);
		}
	}
	[PublicAPI, Serializable]
	public class CurioDocumentReference: GameDataDocumentReference
	{
		public string predefinedSchemaNameOrId = Assets.Scripts.Curio.SchemaName;

		[CanBeNull]
		public Assets.Scripts.Curio GetReferencedDocument()
		{
			if (this.IsEmpty)
			{
				return null;
			}

			return (Assets.Scripts.Curio)this.gameData?.FindGameDataDocumentById(this.schemaNameOrId, this.id);
		}
	}
	[PublicAPI, Serializable]
	public class DiseaseDocumentReference: GameDataDocumentReference
	{
		public string predefinedSchemaNameOrId = Assets.Scripts.Disease.SchemaName;

		[CanBeNull]
		public Assets.Scripts.Disease GetReferencedDocument()
		{
			if (this.IsEmpty)
			{
				return null;
			}

			return (Assets.Scripts.Disease)this.gameData?.FindGameDataDocumentById(this.schemaNameOrId, this.id);
		}
	}
	[PublicAPI, Serializable]
	public class QuirkDocumentReference: GameDataDocumentReference
	{
		public string predefinedSchemaNameOrId = Assets.Scripts.Quirk.SchemaName;

		[CanBeNull]
		public Assets.Scripts.Quirk GetReferencedDocument()
		{
			if (this.IsEmpty)
			{
				return null;
			}

			return (Assets.Scripts.Quirk)this.gameData?.FindGameDataDocumentById(this.schemaNameOrId, this.id);
		}
	}
	[PublicAPI, Serializable]
	public class ConditionDocumentReference: GameDataDocumentReference
	{
		public string predefinedSchemaNameOrId = Assets.Scripts.Condition.SchemaName;

		[CanBeNull]
		public Assets.Scripts.Condition GetReferencedDocument()
		{
			if (this.IsEmpty)
			{
				return null;
			}

			return (Assets.Scripts.Condition)this.gameData?.FindGameDataDocumentById(this.schemaNameOrId, this.id);
		}
	}
	[PublicAPI, Serializable]
	public class WeaponDocumentReference: GameDataDocumentReference
	{
		public string predefinedSchemaNameOrId = Assets.Scripts.Weapon.SchemaName;

		[CanBeNull]
		public Assets.Scripts.Weapon GetReferencedDocument()
		{
			if (this.IsEmpty)
			{
				return null;
			}

			return (Assets.Scripts.Weapon)this.gameData?.FindGameDataDocumentById(this.schemaNameOrId, this.id);
		}
	}
	[PublicAPI, Serializable]
	public class ArmorDocumentReference: GameDataDocumentReference
	{
		public string predefinedSchemaNameOrId = Assets.Scripts.Armor.SchemaName;

		[CanBeNull]
		public Assets.Scripts.Armor GetReferencedDocument()
		{
			if (this.IsEmpty)
			{
				return null;
			}

			return (Assets.Scripts.Armor)this.gameData?.FindGameDataDocumentById(this.schemaNameOrId, this.id);
		}
	}
	[PublicAPI, Serializable]
	public class ItemWithCountDocumentReference: GameDataDocumentReference
	{
		public string predefinedSchemaNameOrId = Assets.Scripts.ItemWithCount.SchemaName;

		[CanBeNull]
		public Assets.Scripts.ItemWithCount GetReferencedDocument()
		{
			if (this.IsEmpty)
			{
				return null;
			}

			return (Assets.Scripts.ItemWithCount)this.gameData?.FindGameDataDocumentById(this.schemaNameOrId, this.id);
		}
	}
	[PublicAPI, Serializable]
	public class StartingSetDocumentReference: GameDataDocumentReference
	{
		public string predefinedSchemaNameOrId = Assets.Scripts.StartingSet.SchemaName;

		[CanBeNull]
		public Assets.Scripts.StartingSet GetReferencedDocument()
		{
			if (this.IsEmpty)
			{
				return null;
			}

			return (Assets.Scripts.StartingSet)this.gameData?.FindGameDataDocumentById(this.schemaNameOrId, this.id);
		}
	}

}
