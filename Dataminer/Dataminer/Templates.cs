using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;

namespace OutwardExplorer
{
    public class Templates
    {
        public class ItemTemplate
        {
            public string Type; // class type
            public int ItemID;
            public int LegacyItemID;
            public string Name;
            public string Description;
            public int BaseValue;
            public float Weight;
            public int Durability;

            public List<string> Tags;
        }

        public class EquipmentTemplate : ItemTemplate
        {
            public string EquipmentSlot;
            public float Protection;
            public float[] DamageAttack;
            public float[] DamageResistance;
            public float ImpactResistance;
            public float StaminaUsePenalty;
            public float ManaUseModifier;
            public float MovementPenalty;
            public float PouchBonus;
            public float HeatProtect;
            public float ColdProtect;
        }

        public class BagTemplate : EquipmentTemplate
        {
            public bool RestrictDodge;
            public float InventoryProtection;

            public List<string> PreservedTypes;
            public List<float> PreservationAmounts;
        }

        public class WeaponTemplate : EquipmentTemplate
        {
            public string WeaponType;
            // WeaponStats
            public DamageList BaseDamage;
            public float Impact;
            public float AttackSpeed;
            public int AttackCount;
            public WeaponStats.AttackData[] Attacks;
            public List<string> HitEffects;
            public List<float> HitEffects_Buildups;
        }

        public class ConsumableTemplate : ItemTemplate
        {
            public float Hunger;
            public float Thirst;
            public List<string> Effects;
        }

        public class PassiveSkillTemplate : ItemTemplate
        {
            public List<string> AffectedStats;
            public List<float> Values;
        }

        public class ActiveSkillTemplate : ItemTemplate
        {
            public float StaminaCost;
            public float ManaCost;
            public float DurabilityCost;
            public float Cooldown;
            public float Lifespan;

            public List<string> RequiredItems;
            public List<bool> ItemsConsumed;
            public List<string> Required_Mainhand_Types;
            public List<string> Required_Offhand_Types;
        }

        public class SkillDamage
        {
            public DamageType[] Damages;
            public bool AddWeaponDamage = false;
            public float DamageMultiplier = 1.0f;
            public int DamageOverride = -1;
            public float Impact;
            public float ImpactMultiplier = 1.0f;

            public List<string> HitEfects;
        }

        // EffectPresets
        public class StatusEffectTemplate
        {
            public string Name;
            public int EffectID;
            public string Type; // (hex / boon / imbue / simple)
            public float Lifespan;
            public bool Purgeable;

            //public StatusData.EffectData[] EffectData;
            public List<string> AffectedStats;
            public List<string> Values;
            public List<string> Values_AI;

            public DamageType[] Imbue_Damage;
            public float Imbue_Multiplier;
            public string Imbue_HitEffect;
        }

        // recipes
        public class RecipeTemplate
        {
            public string RecipeType;
            public string Result;
            public int ResultCount;
            public List<string> Ingredients;
        }

        // Scene Dumper

        public class EnemyTemplate
        {
            //gameObject
            public string Name;
            public string UID;
            public string Location;

            public float Radius; // collider radius
            public float HoursToReset;

            //CharacterStats
            public float MaxHealth;
            public float HealthRegen; // per second
            public float ImpactResistance;
            public float Protection;
            public float[] DamageResistances;
            public float[] DamageMultipliers;
            public List<string> Status_Immunities;

            //Equipment
            public List<string> Equipment;
            public DamageList Weapon_Damage;
            public float Weapon_Impact;
            public List<string> Inflicts;
            public List<string> Skills;

            public string Faction;
            public List<string> Targetable_Factions;

            // drops
            public List<string> GuaranteedDrops;
            public List<int> GuaranteedQtys;
            public List<int> GuaranteedIDs;
            public List<string> DropTables;
        }

        public class Merchant
        {
            public string Name;
            public string Location;

            public List<string> DropTables;
        }

        public class ItemContainerTemplate : ItemTemplate
        {
            public string Location; // map loaded from
            public string ContainerType;
            public List<string> DropTableNames;
        }

        public class DropTableContainer
        {
            public string Name;

            public List<string> GuaranteedDrops;
            public List<int> GuaranteedIDs;
            public List<int> GuaranteedMinQtys;
            public List<int> GuaranteedMaxQtys;

            public int MinRandomDrops;
            public int MaxRandomDrops;
        }

        public class DropTableTemplate
        {
            public int MinNumberOfDrops;
            public int MaxNumberOfDrops;
            public int MaxDiceValue;
            public float EmptyDropChance;

            public List<string> ItemChances;
            public List<int> ItemChanceIDs;
            public List<int> ChanceMinQtys;
            public List<int> ChanceMaxQtys;
            public List<float> ChanceDropChances;
        }
    }
}
