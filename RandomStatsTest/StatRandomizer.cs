using BepInEx;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using SideLoader;

namespace StatRandomizer
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

                var comp = RandomizedStats.AddToItem(item);
                DontDestroyOnLoad(comp);
            }
        }
    }
}
