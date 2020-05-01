using BepInEx;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using SideLoader;

namespace RandomStatsTest
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class StatRandomizer : BaseUnityPlugin
    {
        const string GUID = "com.sinai.randomstats";
        const string NAME = "Random Stats Test";
        const string VERSION = "1.0.0";

        public static BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        internal void Awake()
        {
            SL.OnPacksLoaded += SL_OnPacksLoaded;
        }

        private void SL_OnPacksLoaded()
        {
            var allItems = typeof(ResourcesPrefabManager).GetField("ITEM_PREFABS", flags).GetValue(null) as Dictionary<string, Item>;

            foreach (var item in allItems.Values)
            {
                if (item.NonSavable || !item.GetComponent<ItemStats>())
                {
                    continue;
                }

                var comp = RandomStats.AddToItem(item);
                DontDestroyOnLoad(comp);
            }
        }
    }

    public class RandomStats : ItemExtension
    {
        public bool StatsGenerated { get; private set; }

        private CustomItemStats m_stats;

        // add the base component to an Item
        public static RandomStats AddToItem(Item item)
        {
            if (item.GetExtension("RandomStats") is RandomStats existing)
            {
                Debug.Log($"{item.Name} already has a RandomStats component!");
                return existing;
            }

            var comp = item.gameObject.AddComponent<RandomStats>();
            comp.Savable = true;
            comp.m_item = item;

            return comp;
        }

        // One-time generate random stats
        public void RandomizeStats()
        {
            if (StatsGenerated)
            {
                return;
            }

            //Debug.Log($"Generating new random stats for {m_item.Name}_{m_item.UID}");

            m_stats = new CustomItemStats
            {
                m_baseValue = Random.Range(-100, 100)
            };

            StatsGenerated = true;
        }

        // Apply changes onto an Item
        public override void InitCachedInfo(Item _item)
        {
            if (m_stats == null)
            {
                RandomizeStats();
            }

            m_item = _item;

            var stats = _item.GetComponent<ItemStats>();

            var newValue = stats.BaseValue + this.m_stats.m_baseValue; 
            typeof(ItemStats).GetField("m_baseValue", StatRandomizer.flags).SetValue(stats, newValue);
        }

        // Serialize stats into a string, separated by ';'
        public override string ToNetworkInfo()
        {
            var _toSave = "";

            if (this.m_stats == null)
            {
                Debug.LogWarning("Trying to do ToNetworkInfo on RandomStats, but we have no stats to serialize!");
                return "";
            }

            // Iterating over the enum is a safe way to avoid forgetting to serialize something.
            for (int i = 0; i < (int)SyncOrder.COUNT; i++)
            {
                // add seperator
                if (_toSave != "") { _toSave += ";"; }

                // serialize value to string
                var synctype = (SyncOrder)i;
                switch (synctype)
                {
                    case SyncOrder.BaseValue:
                        _toSave += m_stats.m_baseValue; break;

                    default:
                        break;
                }
            }

            m_receivedInfo = _toSave.Split(new char[] { ';' });

            return _toSave;
        }

        // Deserialize the string[] data into actual fields
        public override void OnReceiveNetworkSync(string[] _networkInfo)
        {
            base.OnReceiveNetworkSync(_networkInfo);

            if (m_stats == null)
            {
                m_stats = new CustomItemStats();
            }

            for (int i = 0; i < _networkInfo.Length; i++)
            {
                var synctype = (SyncOrder)i;

                switch (synctype)
                {
                    case SyncOrder.BaseValue:
                        if (int.TryParse(_networkInfo[i], out int _int))
                        {
                            m_stats.m_baseValue = _int;
                        }
                        break;

                    default:
                        break;
                }
            }
            
            StatsGenerated = true;

            m_receivedInfo = _networkInfo;
        }

        public enum SyncOrder
        {
            BaseValue,
            COUNT
        }
    }

    public class CustomItemStats
    {
        public int m_baseValue = 0;
    }
}
