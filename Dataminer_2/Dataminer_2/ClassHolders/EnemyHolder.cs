using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Text.RegularExpressions;
using System.IO;

namespace Dataminer_2
{
    public class EnemyHolder : IEquatable<EnemyHolder>
    {
        public string Name;
        public string GameObject_Name; // with UID removed
        public int Unique_ID;

        public List<string> Locations_Found = new List<string>();

        public float Max_Health;
        public float Health_Regen_Per_Second;
        public float Impact_Resistance;
        public float[] Protection;
        public float[] Damage_Resistances;
        public float[] Damage_Bonuses;
        public float Collider_Radius;

        public List<Damages> True_WeaponDamage = new List<Damages>();
        public float Weapon_Impact;

        public List<string> Status_Immunities = new List<string>();

        public List<string> Starting_Equipment = new List<string>();
        public List<string> Skill_Knowledge = new List<string>();

        public string Faction;
        public bool Allied_To_Same_Faction;

        public List<DropTableEntry> Guaranteed_Drops = new List<DropTableEntry>();
        public List<string> DropTable_Names = new List<string>();

        public static EnemyHolder ParseEnemy(Character character, Vector3 origPos)
        {
            Debug.Log("parsing enemy " + character.Name + " (" + character.gameObject.name + ")");

            var enemyHolder = new EnemyHolder
            {
                Name = character.Name,
                Unique_ID = 1,
                Max_Health = character.Stats.MaxHealth,
                Health_Regen_Per_Second = character.Stats.HealthRegen,
                Impact_Resistance = character.Stats.GetImpactResistance(),
                Damage_Resistances = character.Stats.DamageResistance,
                Damage_Bonuses = At.GetValue(typeof(CharacterStats), character.Stats, "m_totalDamageAttack") as float[],
                Protection = character.Stats.DamageProtection,
                Faction = character.Faction.ToString(),
                Allied_To_Same_Faction = character.TargetingSystem.AlliedToSameFaction,
                Collider_Radius = character.CharacterController.radius
            };

            // *----------------- TEMPORARY DEBUG -----------------* //
            int z = 0;
            foreach (float f in enemyHolder.Protection)
            {
                if (z > 0 && f > 0.01f)
                {
                    Debug.LogError("!!!!!!! ENEMY HAS " + (DamageType.Types)z + " PROTECTION !!!!!!!!");
                }
                z++;
            }
            // *---------------------------------------------------* //


            // adjust some stats to expected value format
            for (int i = 0; i < 9; i++)
            {
                enemyHolder.Damage_Resistances[i] = (float)Math.Round(enemyHolder.Damage_Resistances[i] * 100f, 2);
                enemyHolder.Damage_Bonuses[i] = (float)Math.Round((enemyHolder.Damage_Bonuses[i] - 1) * 100f, 2);
            }

            // fix a few enemy names
            switch (enemyHolder.Name)
            {
                case "???":
                    enemyHolder.Name = "Concealed Knight"; break;
                default: break;
            }
            if (enemyHolder.Name.ToLower().Contains("bandit")
                && SceneManager.Instance.GetCurrentLocation(character.CenterPosition).ToLower().Contains("vendavel"))
            {
                enemyHolder.Name = "Vendavel " + enemyHolder.Name;
            }

            // regex and fix the gameobject name, remove the UID and the (count)
            string goName = character.gameObject.name;
            goName = goName.Substring(0, goName.Length - (character.UID.ToString().Length + 1));
            goName = Regex.Replace(goName, @"\s*[(][\d][)]$", "");
            enemyHolder.GameObject_Name = goName;

            // add to Locations (first location)
            string location = ListManager.GetSceneSummaryKey(origPos);
            enemyHolder.Locations_Found.Add(location);

            // Immunities
            foreach (TagSourceSelector tagSelector in At.GetValue(typeof(CharacterStats), character.Stats, "m_statusEffectsNaturalImmunity") as TagSourceSelector[])
            {
                enemyHolder.Status_Immunities.Add(tagSelector.Tag.TagName);
            }
            foreach (KeyValuePair<Tag, List<string>> entry in At.GetValue(typeof(CharacterStats), character.Stats, "m_statusEffectsImmunity") as Dictionary<Tag, List<string>>)
            {
                if (entry.Value.Count > 0)
                {
                    enemyHolder.Status_Immunities.Add(entry.Key.TagName);
                }
            }

            // Drops
            bool dropWeapon = false;
            bool dropPouch = false;
            if (character.GetComponent<LootableOnDeath>() is LootableOnDeath lootableOnDeath)
            {
                dropWeapon = lootableOnDeath.DropWeapons;
                dropPouch = lootableOnDeath.EnabledPouch;

                if (At.GetValue(typeof(LootableOnDeath), lootableOnDeath, "m_lootDroppers") is Dropable[] m_lootDroppers)
                {
                    foreach (Dropable dropper in m_lootDroppers)
                    {
                        var dropTableHolder = DroptableHolder.ParseDropTable(dropper);
                        enemyHolder.DropTable_Names.Add(dropTableHolder.Name); 
                    }
                }
            }

            // Starting Equipment
            if (character.GetComponent<StartingEquipment>() is StartingEquipment startingEquipment)
            {
                Equipment[] equipments = null;

                if (startingEquipment.OverrideStartingEquipments != null && startingEquipment.OverrideStartingEquipments.Length > 0)
                {
                    equipments = startingEquipment.OverrideStartingEquipments;
                }
                else if (startingEquipment.Equipments != null && startingEquipment.Equipments.Length > 0)
                {
                    equipments = startingEquipment.Equipments;
                }

                if (equipments != null)
                {
                    foreach (Equipment equipment in equipments.Where(x => x != null))
                    {
                        enemyHolder.Starting_Equipment.Add(equipment.Name + " (" + equipment.ItemID + ")");

                        if (equipment is Weapon weapon)
                        {
                            bool addDrop = false;

                            if (weapon.Type != Weapon.WeaponType.Shield)
                            {
                                if (character.CurrentWeapon != null)
                                {
                                    SetWeaponDamage(enemyHolder, character.CurrentWeapon);
                                    if (dropWeapon && equipment.IsPickable)
                                    {
                                        enemyHolder.Guaranteed_Drops.Add(new DropTableEntry
                                        {
                                            Item_ID = character.CurrentWeapon.ItemID,
                                            Item_Name = character.CurrentWeapon.Name,
                                            Min_Quantity = 1,
                                            Max_Quantity = 1
                                        });
                                    }

                                    addDrop = dropPouch;
                                }
                                else
                                {
                                    SetWeaponDamage(enemyHolder, weapon);
                                    addDrop = dropWeapon;
                                }
                            }
                           
                            if ((addDrop || weapon.Type == Weapon.WeaponType.Shield) && equipment.IsPickable)
                            {
                                bool flag = false;
                                if (weapon.TwoHanded)
                                {
                                    foreach (DropTableEntry entry in enemyHolder.Guaranteed_Drops)
                                    {
                                        if (entry.Item_ID == weapon.ItemID)
                                        {
                                            flag = true;
                                            break;
                                        }
                                    }
                                }
                                if (!flag)
                                {
                                    enemyHolder.Guaranteed_Drops.Add(new DropTableEntry
                                    {
                                        Item_ID = equipment.ItemID,
                                        Item_Name = equipment.Name,
                                        Min_Quantity = 1,
                                        Max_Quantity = 1
                                    });
                                }
                            } 
                        }
                    }
                }

                if (dropPouch)
                {
                    if (startingEquipment.StartingPouchItems != null)
                    {
                        foreach (ItemQuantity itemQuantity in startingEquipment.StartingPouchItems)
                        {
                            enemyHolder.Guaranteed_Drops.Add(new DropTableEntry
                            {
                                Item_ID = itemQuantity.Item.ItemID,
                                Item_Name = itemQuantity.Item.Name,
                                Min_Quantity = itemQuantity.Quantity,
                                Max_Quantity = itemQuantity.Quantity
                            });
                        }
                    }
                }
            }

            // Skills
            if (character.Inventory != null 
                && character.Inventory.SkillKnowledge != null
                && At.GetValue(typeof(CharacterKnowledge), character.Inventory.SkillKnowledge as CharacterKnowledge, "m_learnedItems") is List<Item> learned
                && learned.Count() > 0)
            {
                foreach (Item skill in learned)
                {
                    enemyHolder.Skill_Knowledge.Add(skill.Name ?? skill.name + " (" + skill.ItemID + ")");
                }
            }

            // compare to existing enemies, see if we should save as new unique
            bool newSave = true;
            if (ListManager.EnemyManifest.ContainsKey(enemyHolder.Name))
            {
                bool newVariant = true;
                int count = 1;
                foreach (EnemyHolder existingHolder in ListManager.EnemyManifest[enemyHolder.Name])
                {
                    if (enemyHolder.Equals(existingHolder))
                    {
                        Debug.Log("Enemy was a copy of ID " + count);
                        enemyHolder.Unique_ID = existingHolder.Unique_ID;

                        newVariant = false;
                        newSave = false;
                        if (!existingHolder.Locations_Found.Contains(location))
                        {
                            existingHolder.Locations_Found.Add(location);
                            SaveEnemy(existingHolder);
                        }
                        break;
                    }
                    count++;
                }
                if (newVariant)
                {
                    enemyHolder.Unique_ID = count;
                    ListManager.EnemyManifest[enemyHolder.Name].Add(enemyHolder);
                }
            }
            else // new Unique
            {
                ListManager.EnemyManifest.Add(enemyHolder.Name, new List<EnemyHolder> { enemyHolder });
            }
            
            if (newSave)
            {
                SaveEnemy(enemyHolder);
            }

            return enemyHolder;
        }

        private static void SetWeaponDamage(EnemyHolder holder, Weapon weapon)
        {
            WeaponStats stats = weapon.Stats ?? ResourcesPrefabManager.Instance.GetItemPrefab(weapon.ItemID).Stats as WeaponStats;

            if (stats != null)
            {
                holder.Weapon_Impact = stats.Impact;

                var list = stats.BaseDamage.Clone();
                for (int i = 0; i < 6; i++)
                {
                    float multi = holder.Damage_Bonuses[i];
                    if (list[(DamageType.Types)i] != null)
                    {
                        list[(DamageType.Types)i].Damage *= 1 + (0.01f * multi);
                    }
                }
                holder.True_WeaponDamage = Damages.ParseDamageList(list);
            }
            else
            {
                Debug.LogWarning("Null stats for " + weapon.Name);
            }
        }

        private static void SaveEnemy(EnemyHolder holder)
        {
            Debug.LogWarning(string.Format("Saving enemy '{0}' (unique ID: {1})", holder.Name, holder.Unique_ID));

            var dir = Folders.Enemies;
            string saveName = holder.Name + " (" + holder.Unique_ID + ")";

            // overwrite all enemies, to update Locations_Found list automatically.
            string path = dir + "/" + saveName + ".xml";
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            Dataminer.SerializeXML(dir, saveName, holder, typeof(EnemyHolder));
        }

        public bool Equals(EnemyHolder other)
        {
            bool equal = this.GameObject_Name == other.GameObject_Name
                && this.Max_Health == other.Max_Health
                && this.Health_Regen_Per_Second == other.Health_Regen_Per_Second
                && this.Impact_Resistance == other.Impact_Resistance
                && this.Guaranteed_Drops.Count == other.Guaranteed_Drops.Count
                && this.Starting_Equipment.Count == other.Starting_Equipment.Count
                && this.DropTable_Names.Count == other.DropTable_Names.Count
                && this.Status_Immunities.Count == other.Status_Immunities.Count
                && this.Skill_Knowledge.Count == other.Skill_Knowledge.Count;

            if (equal)
            {
                // check guaranteed drops
                for (int i = 0; i < this.Guaranteed_Drops.Count; i++)
                {
                    if (this.Guaranteed_Drops[i].Item_ID != other.Guaranteed_Drops[i].Item_ID
                        || this.Guaranteed_Drops[i].Min_Quantity != other.Guaranteed_Drops[i].Min_Quantity
                        || this.Guaranteed_Drops[i].Max_Quantity != other.Guaranteed_Drops[i].Max_Quantity)
                    {
                        return false;
                    }
                }

                // check drop table names
                for (int i = 0; i < this.DropTable_Names.Count; i++)
                {
                    if (this.DropTable_Names[i] != other.DropTable_Names[i])
                    {
                        return false;
                    }
                }

                // check status immunities
                for (int i = 0; i < this.Status_Immunities.Count; i++)
                {
                    if (this.Status_Immunities[i] != other.Status_Immunities[i])
                    {
                        return false;
                    }
                }

                // check skills
                for (int i = 0; i < this.Skill_Knowledge.Count; i++)
                {
                    if (this.Skill_Knowledge[i] != other.Skill_Knowledge[i])
                    {
                        return false;
                    }
                }

                // check damage bonus and resistances
                for (int i = 0; i < 6; i++)
                {
                    // fix for the comparison not using the corrected values sometimes.
                    // need to figure out why this is even happening.
                    float otherRes = (float)Math.Round(other.Damage_Resistances[i] * 100f, 2);
                    float otherBonus = (float)Math.Round((other.Damage_Bonuses[i] - 1) * 100f, 2);

                    if ((this.Damage_Resistances[i] != other.Damage_Resistances[i] && this.Damage_Resistances[i] != otherRes)
                        || (this.Damage_Bonuses[i] != other.Damage_Bonuses[i] && this.Damage_Bonuses[i] != otherBonus)
                        || this.Protection[i] != other.Protection[i])
                    {
                        return false;
                    }
                }

                // check equipment
                for (int i = 0; i < this.Starting_Equipment.Count; i++)
                {
                    if (this.Starting_Equipment[i] != other.Starting_Equipment[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
