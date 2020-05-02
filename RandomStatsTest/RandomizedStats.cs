using BepInEx;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace StatRandomizer
{
    public class RandomizedStats : ItemExtension
    {
        private Dictionary<SyncOrder, RandomStat> m_stats;

        // add the base component to an Item
        public static RandomizedStats AddToItem(Item item)
        {
            if (item.GetExtension("RandomStats") is RandomizedStats existing)
            {
                Debug.Log($"{item.Name} already has a RandomStats component!");
                return existing;
            }

            var comp = item.gameObject.AddComponent<RandomizedStats>();
            comp.Savable = true;
            comp.m_item = item;

            return comp;
        }

        // One-time generate random stats
        public void RandomizeStats()
        {
            if (m_stats != null)
            {
                RemoveStats();
            }

            m_stats = NewStats();

            foreach (var stat in m_stats.Values)
            {
                stat.Randomize();
            }
        }

        public void RemoveStats()
        {
            foreach (var stat in m_stats.Values)
            {
                stat.RemoveValue(m_item.Stats);
            }
        }

        // Deserialize the string[] data into actual fields
        public override void OnReceiveNetworkSync(string[] _networkInfo)
        {
            if (m_stats != null)
            {
                RemoveStats();
            }

            m_stats = NewStats();

            for (int i = 0; i < _networkInfo.Length; i++)
            {
                var stat = m_stats[(SyncOrder)i];
                stat.Deserialize(_networkInfo[i]);
            }

            m_receivedInfo = _networkInfo;

            ApplyValues();
        }

        private void ApplyValues()
        {
            var stats = m_item.GetComponent<ItemStats>();

            foreach (var stat in m_stats.Values)
            {
                stat.SetValue(stats);
            }

            if (m_item is Weapon m_weapon)
            {
                typeof(Weapon).GetMethod("RefreshEnchantmentModifiers", StatRandomizer.flags).Invoke(m_weapon, new object[0]);
            }
            else if (m_item is Equipment m_equipment)
            {
                m_equipment.Stats.RefreshEnchantmentStatModifications();
            }
        }

        // Serialize stats into a string, separated by ';'
        public override string ToNetworkInfo()
        {
            var _toSave = "";

            if (m_stats == null)
            {
                Debug.LogWarning("Trying to do ToNetworkInfo on RandomStats, but we have no stats to serialize!");
                return "";
            }

            // Iterating over the enum is a safe way to avoid forgetting to serialize something.
            for (int i = 0; i < (int)SyncOrder.COUNT; i++)
            {
                var key = (SyncOrder)i;

                if (!m_stats.ContainsKey(key))
                {
                    // not implemented yet
                    continue;
                }

                // add seperator
                if (_toSave != "") { _toSave += ";"; }

                var stat = m_stats[key];
                _toSave += stat.ToString();
            }

            // for debug
            m_receivedInfo = _toSave.Split(new char[] { ';' });

            return _toSave;
        }

        // generate a new empty stats dictionary
        public Dictionary<SyncOrder, RandomStat> NewStats()
        {
            return new Dictionary<SyncOrder, RandomStat>()
            {
                {
                    SyncOrder.DamageResistance,
                    new RandomFloatArray()
                    {
                        FieldInfo = typeof(EquipmentStats).GetField("m_damageResistance", StatRandomizer.flags),
                        Range = new Vector2(0, 10)
                    }
                },
                {
                    SyncOrder.ImpactResistance,
                    new RandomFloat()
                    {
                        FieldInfo = typeof(EquipmentStats).GetField("m_impactResistance", StatRandomizer.flags),
                        Range = new Vector2(0, 10)
                    }
                },
                {
                    SyncOrder.DamageBonus,
                    new RandomFloatArray()
                    {
                        FieldInfo = typeof(EquipmentStats).GetField("m_damageAttack", StatRandomizer.flags),
                        Range = new Vector2(0, 10)
                    }
                },
                // etc...
            };
        }

        public enum SyncOrder
        {
            DamageResistance,
            ImpactResistance,
            DamageBonus,
            ImpactBonus,
            StamCost,
            ManaCost,
            MoveSpeed,
            HeatProtect,
            ColdProtect,
            CorruptProtect,
            AttackSpeed,
            Impact,
            Damage,
            COUNT
        }
    }
}
