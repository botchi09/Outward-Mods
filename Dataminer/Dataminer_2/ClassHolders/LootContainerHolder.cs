using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Dataminer
{
    public class LootContainerHolder
    {
        public string Name;
        public int ItemID;
        public string UID;

        public List<string> DropTables = new List<string>();

        public static LootContainerHolder ParseLootContainer(TreasureChest loot)
        {
            var lootHolder = new LootContainerHolder
            {
                Name = loot.Name,
                ItemID = loot.ItemID,
                UID = loot.UID
            };

            if (lootHolder.Name == "Pocket")
            {
                lootHolder.Name = "Corpse";
            }

            if (At.GetValue(typeof(SelfFilledItemContainer), loot as SelfFilledItemContainer, "m_drops") is List<Dropable> droppers)
            {
                foreach (Dropable dropper in droppers)
                {
                    var dropableHolder = DroptableHolder.ParseDropTable(dropper, null, lootHolder.Name);
                    lootHolder.DropTables.Add(dropableHolder.Name);
                }
            }

            string dir = Folders.Scenes + "/" + SceneManager.Instance.GetCurrentRegion() + "/" + SceneManager.Instance.GetCurrentLocation(loot.transform.position);
            string saveName = lootHolder.Name + "_" + lootHolder.UID;
            Dataminer.SerializeXML(dir + "/LootContainers", saveName, lootHolder, typeof(LootContainerHolder));

            ListManager.AddContainerSummary(lootHolder.Name, ListManager.GetSceneSummaryKey(loot.transform.position), lootHolder.DropTables);

            return lootHolder;
        }
    }
}
