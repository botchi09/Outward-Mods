using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Xml.Serialization;
using System.IO;

namespace Dataminer
{
    public class DroptableHolder
    {
        public string Name;

        public List<DropTableEntry> Guaranteed_Drops = new List<DropTableEntry>();
        public List<DropGeneratorHolder> Random_Tables = new List<DropGeneratorHolder>();

        public static DroptableHolder ParseDropTable(Dropable dropper, Merchant merchant = null, string containerName = "")
        {
            var dropTableHolder = new DroptableHolder
            {
                Name = dropper.name
            };

            if (dropTableHolder.Name == "InventoryTable" && merchant != null)
            {
                dropTableHolder.Name = SceneManager.Instance.GetCurrentLocation(merchant.transform.position) + " - " + merchant.ShopName;
            }

            if (At.GetValue(typeof(Dropable), dropper, "m_allGuaranteedDrops") is List<GuaranteedDrop> guaranteedDrops)
            {
                foreach (GuaranteedDrop gDropper in guaranteedDrops)
                {
                    if (At.GetValue(typeof(GuaranteedDrop), gDropper, "m_itemDrops") is List<BasicItemDrop> gItemDrops && gItemDrops.Count > 0)
                    {
                        foreach (BasicItemDrop gItemDrop in gItemDrops)
                        {
                            var pos = dropper.transform.position;
                            AddGuaranteedDrop(dropTableHolder,
                                gItemDrop.DroppedItem.ItemID,
                                gItemDrop.DroppedItem.Name,
                                gItemDrop.MinDropCount,
                                gItemDrop.MaxDropCount,
                                containerName,
                                pos);
                        }
                    }
                }
            }

            if (At.GetValue(typeof(Dropable), dropper, "m_mainDropTables") is List<DropTable> dropTables)
            {
                foreach (DropTable table in dropTables)
                {
                    var generatorHolder = new DropGeneratorHolder
                    {
                        MinNumberOfDrops = table.MinNumberOfDrops,
                        MaxNumberOfDrops = table.MaxNumberOfDrops,
                        MaxDiceValue = (int)At.GetValue(typeof(DropTable), table, "m_maxDiceValue"),
                    };

                    if (At.GetValue(typeof(DropTable), table, "m_dropAmount") is SimpleRandomChance dropAmount)
                    {
                        generatorHolder.ChanceReduction = dropAmount.ChanceReduction;
                        generatorHolder.ChanceRegenQty = dropAmount.ChanceRegenQty;
                        if (dropAmount.CanRegen)
                        {
                            generatorHolder.RegenTime = dropAmount.ChanceRegenDelay;
                        }
                        else
                        {
                            generatorHolder.RegenTime = -1;
                        }
                    }
                    else
                    {
                        generatorHolder.RegenTime = -1;
                    }

                    if (At.GetValue(typeof(DropTable), table, "m_emptyDropChance") is int i)
                    {
                        decimal emptyChance = (decimal)i / generatorHolder.MaxDiceValue;
                        generatorHolder.EmptyDrop = (float)emptyChance * 100;
                    }

                    if (At.GetValue(typeof(DropTable), table, "m_itemDrops") is List<ItemDropChance> itemDrops)
                    {
                        foreach (ItemDropChance dropChance in itemDrops)
                        {
                            float percentage = (float)((decimal)dropChance.DropChance / generatorHolder.MaxDiceValue) * 100f;

                            percentage = (float)Math.Round(percentage, 2);

                            generatorHolder.Item_Drops.Add(new DropTableChanceEntry
                            {
                                Item_ID = dropChance.DroppedItem.ItemID,
                                Item_Name = dropChance.DroppedItem.Name,
                                Min_Quantity = dropChance.MinDropCount,
                                Max_Quantity = dropChance.MaxDropCount,
                                Drop_Chance = percentage,
                                Dice_Range = dropChance.MaxDiceRollValue - dropChance.MinDiceRollValue,
                                ChanceReduction = dropChance.ChanceReduction,
                                ChanceRegenDelay = dropChance.ChanceRegenDelay,
                                ChanceRegenQty = dropChance.ChanceRegenQty
                            });

                            //if (percentage == 100)
                            //{
                            //    var pos = dropper.transform.position;
                            //    AddGuaranteedDrop(dropTableHolder, 
                            //        dropChance.DroppedItem.ItemID, 
                            //        dropChance.DroppedItem.Name, 
                            //        dropChance.MinDropCount, 
                            //        dropChance.MaxDropCount,
                            //        containerName,
                            //        pos);
                            //}
                            //else if (percentage > 0)
                            //{
                            //    generatorHolder.Item_Drops.Add(new DropTableChanceEntry
                            //    {
                            //        Item_ID = dropChance.DroppedItem.ItemID,
                            //        Item_Name = dropChance.DroppedItem.Name,
                            //        Min_Quantity = dropChance.MinDropCount,
                            //        Max_Quantity = dropChance.MaxDropCount,
                            //        Drop_Chance = percentage,
                            //        Dice_Range = dropChance.MaxDiceRollValue - dropChance.MinDiceRollValue,
                            //        ChanceReduction = dropChance.ChanceReduction,
                            //        ChanceRegenDelay = dropChance.ChanceRegenDelay,
                            //        ChanceRegenQty = dropChance.ChanceRegenQty
                            //    });

                            //    //if (!string.IsNullOrEmpty(containerName))
                            //    //{
                            //    //    int id = dropChance.DroppedItem.ItemID;
                            //    //    var pos = dropper.transform.position;
                            //    //    AddItemSource(id, dropChance.DroppedItem.Name, containerName, pos);
                            //    //}
                            //}
                        }
                    }

                    dropTableHolder.Random_Tables.Add(generatorHolder);
                }
            }

            if (merchant == null)
            {
                string dir = Folders.Prefabs + "/DropTables";
                if (!File.Exists(dir + "/" + dropTableHolder.Name + ".xml"))
                {
                    ListManager.DropTables.Add(dropTableHolder.Name, dropTableHolder);
                    Dataminer.SerializeXML(dir, dropTableHolder.Name, dropTableHolder, typeof(DroptableHolder));
                }
            }

            return dropTableHolder;
        }

        private static void AddGuaranteedDrop(DroptableHolder dropTableHolder, int item_ID, string item_Name, int min_Qty, int max_Qty, string containerName, Vector3 pos)
        {
            // check if we already have this guaranteed drop, if so add to quantity
            bool newDrop = true;
            foreach (DropTableEntry gDropHolder in dropTableHolder.Guaranteed_Drops)
            {
                if (item_ID == gDropHolder.Item_ID)
                {
                    newDrop = false;
                    gDropHolder.Min_Quantity += min_Qty;
                    gDropHolder.Max_Quantity += max_Qty;
                }
            }
            if (newDrop)
            {
                dropTableHolder.Guaranteed_Drops.Add(new DropTableEntry
                {
                    Item_Name = item_Name,
                    Item_ID = item_ID,
                    Min_Quantity = min_Qty,
                    Max_Quantity = max_Qty
                });

                //if (!string.IsNullOrEmpty(containerName))
                //{
                //    AddItemSource(item_ID, item_Name, containerName, pos);
                //}
            }
        }

        //private static void AddItemSource(int item_ID, string item_Name, string containerName, Vector3 pos)
        //{
        //    if (ListManager.ItemLootSources.ContainsKey(item_ID.ToString()))
        //    {
        //        ListManager.ItemLootSources[item_ID.ToString()].Container_Sources.Add(containerName + " (" + ListManager.GetSceneSummaryKey(pos) + ")");
        //    }
        //    else
        //    {
        //        ListManager.ItemLootSources.Add(item_ID.ToString(), new ItemSource
        //        {
        //            ItemID = item_ID,
        //            ItemName = item_Name,
        //            Container_Sources = new List<string>
        //                {
        //                    containerName + " (" + ListManager.GetSceneSummaryKey(pos) + ")"
        //                },
        //            Spawn_Sources = new List<string>()
        //        });
        //    }
        //}

        public class DropGeneratorHolder
        {
            public int MinNumberOfDrops;
            public int MaxNumberOfDrops;
            public int MaxDiceValue;
            public float EmptyDrop;
            
            public float RegenTime;
            public int ChanceReduction;
            public int ChanceRegenQty;

            public List<DropTableChanceEntry> Item_Drops = new List<DropTableChanceEntry>();
        }
    }
}
