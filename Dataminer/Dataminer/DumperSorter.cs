using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
//using OModAPI;
using System.Text.RegularExpressions;

namespace OutwardExplorer
{
    public class DumperSorter : MonoBehaviour
    {
        public DumperScript script;

        public Dictionary<string, string> Folders = new Dictionary<string, string>
        {
            { "Lists", @"Dumps\Sorted\Lists" },
            { "Enemies", @"Dumps\Sorted\Enemies" },
            { "Merchants", @"Dumps\Sorted\Merchants" },
            { "Droptables", @"Dumps\Sorted\Droptables" },
            { "ItemSources", @"Dumps\Sorted\ItemSources" },
            { "LootContainers", @"Dumps\Sorted\LootContainers" },
            { "SceneSummaries", @"Dumps\Sorted\SceneSummaries" }
        };

        // ================================ scene sorting =================================

        public void SceneSummaries()
        {
            // setup the 4 summary folders (lazy)
            foreach (KeyValuePair<string, string> entry in Folders)
            {
                Directory.CreateDirectory(entry.Value);
            }

            // lists to compile and save at the end
            Dictionary<string, SortTemplates.DropTableSummary> dropTables = new Dictionary<string, SortTemplates.DropTableSummary>(); // filename : template
            Dictionary<int, SortTemplates.ItemSources> itemSources = new Dictionary<int, SortTemplates.ItemSources>(); // ID : sourcesTemplate
            Dictionary<string, SortTemplates.UniqueEnemySummary> allEnemies = new Dictionary<string, SortTemplates.UniqueEnemySummary>(); // gameobject name : template
            Dictionary<string, SortTemplates.ContainerSummary> containerSummaries = new Dictionary<string, SortTemplates.ContainerSummary>(); // Name : template
            //List<string> allMerchants = new List<string>();

            // saving as we go: merchants, scene summaries

            for (int i = 0; i < 5; i++)
            {
                string region = "";
                switch (i)
                {
                    case 0: region = "Chersonese"; break;
                    case 1: region = "Hallowed Marsh"; break;
                    case 2: region = "Abrassar"; break;
                    case 3: region = "Enmerkar Forest"; break;
                    case 4: region = "Other"; break;
                    default: break;
                }
                foreach (string scenePath in Directory.GetDirectories("Dumps/Scenes/" + region))
                {
                    string scene = GetFolderName(scenePath);

                    // Debug.Log("parsing scene " + scene);

                    SortTemplates.SceneSummary sceneSummary = new SortTemplates.SceneSummary
                    {
                        Scene_Name = scene,
                        Enemies = new List<string>(),
                        Enemy_Counts = new List<int>(),
                        Loot_Containers = new List<string>(),
                        Container_Counts = new List<int>(),
                        All_Containers = new List<string>(),
                        Merchants = new List<string>(),
                        ItemSpawns = new List<string>(),
                        ItemSpawnCounts = new List<int>(),
                    };

                    // check enemies in scene
                    foreach (string enemyPath in Directory.GetFiles(scenePath + "/Enemies"))
                    {
                        ParseEnemyCheck(enemyPath, ref allEnemies, ref itemSources, ref dropTables, ref sceneSummary, scene);
                    }

                    // check merchants  
                    foreach (string merchPath in Directory.GetFiles(scenePath + "/Merchants"))
                    {
                        ParseMerchantCheck(merchPath, ref dropTables, ref sceneSummary, scene);
                    }

                    // check item containers
                    foreach (string containerPath in Directory.GetFiles(scenePath + "/Loot"))
                    {
                        ParseContainerCheck(containerPath, ref containerSummaries, ref dropTables, ref sceneSummary, scene);
                    }

                    // add item spawns 
                    foreach (string spawnPath in Directory.GetFiles(scenePath + "/Loot/Spawns"))
                    {
                        Item item = new Item();
                        JsonUtility.FromJsonOverwrite(File.ReadAllText(spawnPath), item);

                        Debug.Log("item spawn: " + item.Name);

                        if (!sceneSummary.ItemSpawns.Contains(item.Name))
                        {
                            sceneSummary.ItemSpawns.Add(item.Name);
                            sceneSummary.ItemSpawnCounts.Add(1);
                        }
                        else
                        {
                            sceneSummary.ItemSpawnCounts[sceneSummary.ItemSpawns.IndexOf(item.Name)] += 1;
                        }

                        if (!itemSources.ContainsKey(item.ItemID))
                        {
                            NewItemSource(item.ItemID, ref itemSources);
                        }

                        if (!itemSources[item.ItemID].SpawnSources.Contains(scene))
                        {
                            itemSources[item.ItemID].SpawnSources.Add(scene);
                        }
                    }

                    // save scene summmary
                    script.SaveJsonOverwrite(sceneSummary, Folders["SceneSummaries"], region + " - " + scene);
                }
            }

            // ====== sorting and list building after parsing is done: =========

            SortDroptables(ref itemSources, ref dropTables);

            SaveEnemies(allEnemies);

            // File.WriteAllLines(Folders["Lists"] + "/Merchants.txt", allMerchants.ToArray());

            SaveContainers(containerSummaries);

            SaveItemSources(itemSources);

            Debug.LogWarning("Scene parsing done!");
        }

        private void ParseEnemyCheck(string enemyPath,
            ref Dictionary<string, SortTemplates.UniqueEnemySummary> uniqueEnemies,
            ref Dictionary<int, SortTemplates.ItemSources> itemSources,
            ref Dictionary<string, SortTemplates.DropTableSummary> dropTables,
            ref SortTemplates.SceneSummary sceneSummary,
            string scene)
        {
            Templates.EnemyTemplate origTemplate = new Templates.EnemyTemplate();
            JsonUtility.FromJsonOverwrite(File.ReadAllText(enemyPath), origTemplate);

            // regex and fix the gameobject name, remove the UID and the (count)
            string name_and_goname = Path.GetFileNameWithoutExtension(enemyPath);
            name_and_goname = name_and_goname.Substring(0, name_and_goname.Length - (origTemplate.UID.Length + 1));
            name_and_goname = Regex.Replace(name_and_goname, @"\s*[(][\d][)]$", "");

            name_and_goname = origTemplate.Name + " (" + name_and_goname + ")";

            Debug.Log("checking enemy " + name_and_goname);

            // fix status immunities lists
            if (origTemplate.Status_Immunities.Count > 1)
            {
                origTemplate.Status_Immunities.Sort();
            }

            // check for unique
            for (int j = 0; j < 20; j++)
            {
                string nameN = name_and_goname;
                if (j > 0) { nameN += "_" + (j + 1); }

                if (uniqueEnemies.ContainsKey(nameN))
                {
                    if (CompareEnemies(origTemplate, uniqueEnemies[nameN].enemyJson))
                    {
                        // a complete duplicate has been found, just add to our location tally for this enemy
                        if (!uniqueEnemies[nameN].Locations.Contains(scene)) { uniqueEnemies[name_and_goname].Locations.Add(scene); }
                        break;
                    }
                    else { continue; } // increase 1 to index, check again
                }
                else
                {
                    name_and_goname = nameN;
                    // a new unique has been found
                    SortTemplates.UniqueEnemySummary newEnemy = new SortTemplates.UniqueEnemySummary
                    {
                        Name = origTemplate.Name,
                        GameObject_Name = name_and_goname,
                        Locations = new List<string> { scene },
                        enemyJson = origTemplate
                    };
                    uniqueEnemies.Add(name_and_goname, newEnemy);

                    // add to item sources from guaranteed drops
                    foreach (int id in origTemplate.GuaranteedIDs)
                    {
                        if (itemSources.ContainsKey(id))
                        {
                            if (!itemSources[id].EnemySources.Contains(name_and_goname))
                            {
                                itemSources[id].EnemySources.Add(name_and_goname);
                            }
                        }
                        else
                        {
                            NewItemSource(id, ref itemSources);
                            itemSources[id].EnemySources.Add(name_and_goname);
                        }
                    }

                    // add to drop table sources, and to compile item sources later
                    foreach (string tableName in origTemplate.DropTables)
                    {
                        if (dropTables.ContainsKey(tableName))
                        {
                            if (!dropTables[tableName].EnemySources.Contains(name_and_goname))
                                dropTables[tableName].EnemySources.Add(name_and_goname);
                        }
                        else
                        {
                            NewDroptableSource(tableName, ref dropTables);
                            dropTables[tableName].EnemySources.Add(name_and_goname);
                        }
                    }
                    break;
                }
            }

            // check scene summary using new unique go_name
            if (sceneSummary.Enemies.Contains(name_and_goname))
            {
                sceneSummary.Enemy_Counts[sceneSummary.Enemies.IndexOf(name_and_goname)] += 1;
            }
            else
            {
                sceneSummary.Enemies.Add(name_and_goname);
                sceneSummary.Enemy_Counts.Add(1);
            }
        }

        private void SaveEnemies(Dictionary<string, SortTemplates.UniqueEnemySummary> uniqueEnemies)
        {
            //List<string> simpleList = new List<string>();

            foreach (KeyValuePair<string, SortTemplates.UniqueEnemySummary> entry in uniqueEnemies)
            {
                //simpleList.Add(entry.Key);
                Debug.Log("saving enemy " + entry.Key);

                string savename = script.ReplaceInvalidChars(entry.Key);

                string path = Folders["Enemies"] + "/" + savename + ".json";

                var toAppend = new Dictionary<string, string> {
                    { "Enemy Stats", JsonUtility.ToJson(entry.Value.enemyJson, true) }
                };

                string fixedJson = script.AppendJsonList(JsonUtility.ToJson(entry.Value, true), toAppend);

                if (File.Exists(path))
                    File.Delete(path);
                File.WriteAllText(path, fixedJson);
            }

            //string path2 = Folders["Lists"] + "/Enemies.txt";
            //if (File.Exists(path2))
            //    File.Delete(path2);
            //File.WriteAllLines(path2, simpleList.ToArray());
        }

        public bool CompareEnemies(Templates.EnemyTemplate enemy1, Templates.EnemyTemplate enemy2)
        {
            bool flag = true;

            foreach (FieldInfo fi in enemy1.GetType().GetFields())
            {
                if (fi.Name == "UID"
                    || fi.Name == "Location"
                    || fi.Name == "Faction"
                    || fi.Name == "Targetable_Factions"
                    || fi.GetValue(enemy1).Equals(fi.GetValue(enemy2)))
                {
                    continue;
                }
                else
                {
                    if (fi.GetValue(enemy1) == null && fi.GetValue(enemy2) == null) { continue; }

                    if (fi.GetValue(enemy1) != null && fi.GetValue(enemy2) == null) { flag = false; break; }
                    if (fi.GetValue(enemy1) == null && fi.GetValue(enemy2) != null) { flag = false; break; }

                    if (fi.FieldType == typeof(float[]) && fi.GetValue(enemy1) is float[] arr1 && fi.GetValue(enemy2) is float[] arr2)
                    {
                        for (int i = 0; i < arr1.Count(); i++)
                        {
                            if (arr1[i].Equals(arr2[i]))
                                continue;

                            flag = false;
                            break;
                        }
                    }
                    else if (fi.FieldType == typeof(List<string>) && fi.GetValue(enemy1) is List<string> list1 && fi.GetValue(enemy2) is List<string> list2)
                    {
                        for (int i = 0; i < list1.Count; i++)
                        {
                            if (i >= list2.Count) { flag = false; break; }

                            if (!list1[i].Equals(list2[i])) { flag = false; break; }
                        }
                    }
                    else if (fi.FieldType == typeof(List<int>) && fi.GetValue(enemy1) is List<int> list3 && fi.GetValue(enemy2) is List<int> list4)
                    {
                        for (int i = 0; i < list3.Count; i++)
                        {
                            if (i >= list4.Count) { flag = false; break; }

                            if (!list3[i].Equals(list4[i])) { flag = false; break; }
                        }
                    }
                    else if (fi.Name == "Weapon_Damage")
                    {
                        if (fi.GetValue(enemy1) is DamageList dmg1 && fi.GetValue(enemy2) is DamageList dmg2)
                        {
                            for (int k = 0; k < dmg1.List.Count(); k++)
                            {
                                if (k >= dmg2.List.Count()) { flag = false; break; }

                                if (dmg1.List[k].Damage == dmg2.List[k].Damage) { continue; }

                                flag = false; break;
                            }
                        }
                    }
                    else if (!fi.GetValue(enemy1).Equals(fi.GetValue(enemy2)))
                    {
                        flag = false;
                    }

                    if (!flag)
                        break;
                }
            }

            //Debug.Log("Enemies equal: " + flag); 

            return flag;
        }

        // parse merchant

        private void ParseMerchantCheck(string merchantPath,
            ref Dictionary<string, SortTemplates.DropTableSummary> dropTables,
            ref SortTemplates.SceneSummary sceneSummary,
            string scene)
        {
            Templates.Merchant origTemplate = new Templates.Merchant();
            JsonUtility.FromJsonOverwrite(File.ReadAllText(merchantPath), origTemplate);

            Debug.Log("Checking merchant " + origTemplate.Name);

            //if (allMerchants.Contains(origTemplate.Name + " - " + scene)) { allMerchants.Add(origTemplate.Name + " (2) - " + scene); }
            //else { allMerchants.Add(origTemplate.Name + " - " + scene); }

            if (dropTables.ContainsKey(scene + " - " + origTemplate.DropTables[0]))
            {
                if (!dropTables[scene + " - " + origTemplate.DropTables[0]].MerchantSources.Contains(origTemplate.Name + " - " + scene))
                    dropTables[scene + " - " + origTemplate.DropTables[0]].MerchantSources.Add(origTemplate.Name + " - " + scene);
            }
            else
            {
                NewDroptableSource(scene + " - " + origTemplate.DropTables[0], ref dropTables);
                dropTables[scene + " - " + origTemplate.DropTables[0]].MerchantSources.Add(origTemplate.Name + " - " + scene);
            }

            string path = Folders["Merchants"] + "/";

            if (sceneSummary.Merchants.Contains(origTemplate.Name))
            {
                sceneSummary.Merchants.Add(origTemplate.Name + " (2)");
                path += origTemplate.Name + " (2)";
            }
            else
            {
                sceneSummary.Merchants.Add(origTemplate.Name);
                path += origTemplate.Name;
            }
            path += " - " + scene + ".json";

            if (File.Exists(path)) { File.Delete(path); }
            File.WriteAllText(path, JsonUtility.ToJson(origTemplate, true));
        }

        // parse item container

        private void ParseContainerCheck(string containerPath,
            ref Dictionary<string, SortTemplates.ContainerSummary> containerSummaries,
            ref Dictionary<string, SortTemplates.DropTableSummary> dropTables,
            ref SortTemplates.SceneSummary sceneSummary,
            string scene)
        {
            Templates.ItemContainerTemplate origTemplate = new Templates.ItemContainerTemplate();
            JsonUtility.FromJsonOverwrite(File.ReadAllText(containerPath), origTemplate);

            Debug.Log("Checking loot " + origTemplate.Name);

            string savename = origTemplate.Name;

            if (origTemplate.Type == "Gatherable") { savename += " (" + origTemplate.DropTableNames[0] + ")"; }

            if (containerSummaries.ContainsKey(savename))
            {
                if (!containerSummaries[savename].Locations.Contains(scene))
                {
                    containerSummaries[savename].Locations.Add(scene);
                }
            }
            else
            {
                NewContainerSummary(savename, ref containerSummaries);
                containerSummaries[savename].Name = origTemplate.Name;
                containerSummaries[savename].Type = origTemplate.Type;
                containerSummaries[savename].Locations.Add(scene);
            }

            foreach (string s in origTemplate.DropTableNames)
            {
                if (!containerSummaries[savename].AllDropTables.Contains(s))
                {
                    containerSummaries[savename].AllDropTables.Add(s);
                }

                string listName = origTemplate.Name;
                if (!(origTemplate.Type == "Gatherable"))
                {
                    listName += " - " + scene;
                }

                if (dropTables.ContainsKey(s))
                {
                    if (!dropTables[s].ContainerSources.Contains(listName))
                    {
                        dropTables[s].ContainerSources.Add(listName);
                    }
                }
                else
                {
                    NewDroptableSource(s, ref dropTables);
                    dropTables[s].ContainerSources.Add(listName);
                }
            }

            if (!sceneSummary.Loot_Containers.Contains(savename))
            {
                sceneSummary.Loot_Containers.Add(savename);
                sceneSummary.Container_Counts.Add(1);
            }
            else
            {
                sceneSummary.Container_Counts[sceneSummary.Loot_Containers.IndexOf(savename)] += 1;
            }
            
            if (origTemplate.ContainerType != "Gatherable")
            {
                string realname = Path.GetFileNameWithoutExtension(containerPath);
                sceneSummary.All_Containers.Add(realname);
            }
        }


        // save container summaries

        private void SaveContainers(Dictionary<string, SortTemplates.ContainerSummary> containerSummaries)
        {
            //List<string> simpleList = new List<string>();

            foreach (KeyValuePair<string, SortTemplates.ContainerSummary> entry in containerSummaries)
            {
                string listName = entry.Key;

                //simpleList.Add(listName);

                string path = Folders["LootContainers"] + "/" + listName + ".json";

                if (File.Exists(path)) { File.Delete(path); }
                File.WriteAllText(path, JsonUtility.ToJson(entry.Value, true));
            }

            //string path2 = Folders["Lists"] + "/LootContainers.txt";
            //if (File.Exists(path2))
            //    File.Delete(path2);
            //File.WriteAllLines(path2, simpleList.ToArray());
        }

        // sort drop tables (add to item sources, save summaries)

        private void SortDroptables(ref Dictionary<int, SortTemplates.ItemSources> itemSources,
            ref Dictionary<string, SortTemplates.DropTableSummary> dropTables)
        {
            foreach (KeyValuePair<string, SortTemplates.DropTableSummary> entry in dropTables)
            {
                foreach (int id in entry.Value.ContainerBase.GuaranteedIDs)
                {
                    AddDroptableEntryToItemSources(id, ref itemSources, entry, entry.Key);
                }

                Dictionary<string, string> tableJsons = new Dictionary<string, string>();
                int i = 1;
                foreach (Templates.DropTableTemplate table in entry.Value.DropTables)
                {
                    tableJsons.Add("Droptable_" + i, JsonUtility.ToJson(table, true));
                    i++;

                    foreach (int id in table.ItemChanceIDs)
                    {
                        AddDroptableEntryToItemSources(id, ref itemSources, entry, entry.Key);
                    }
                }

                string origJson = JsonUtility.ToJson(entry.Value, true);

                string tableContents = JsonUtility.ToJson(entry.Value.ContainerBase, true);

                tableContents = script.AppendJsonList(tableContents, tableJsons);

                origJson = script.AppendJsonList(origJson, new Dictionary<string, string> { { "DropTable_1", tableContents } });

                string path = Folders["Droptables"] + "/" + entry.Key + ".json";
                if (File.Exists(path)) { File.Delete(path); }
                File.WriteAllText(path, origJson);
            }
        }

        private void AddDroptableEntryToItemSources(int id, ref Dictionary<int, SortTemplates.ItemSources> itemSources, KeyValuePair<string, SortTemplates.DropTableSummary> entry, string tableName)
        {
            if (!itemSources.ContainsKey(id))
            {
                NewItemSource(id, ref itemSources);
            }

            if (!itemSources[id].DropTableSources.Contains(tableName) && tableName.ToLower().Contains("droptable"))
            {
                itemSources[id].DropTableSources.Add(tableName);
            }

            foreach (string merchant in entry.Value.MerchantSources)
            {
                if (!itemSources[id].MerchantSources.Contains(merchant))
                {
                    itemSources[id].MerchantSources.Add(merchant);
                }
            }

            foreach (string enemy in entry.Value.EnemySources)
            {
                if (!itemSources[id].EnemySources.Contains(enemy))
                {
                    itemSources[id].EnemySources.Add(enemy);
                }
            }

            foreach (string container in entry.Value.ContainerSources)
            {
                if (!itemSources[id].ContainerSources.Contains(container))
                {
                    itemSources[id].ContainerSources.Add(container);
                }
            }
        }

        // sort item sources

        private void SaveItemSources(Dictionary<int, SortTemplates.ItemSources> itemSources)
        {
            foreach (KeyValuePair<int, SortTemplates.ItemSources> entry in itemSources)
            {
                Item prefab = ResourcesPrefabManager.Instance.GetItemPrefab(entry.Key);
                string savename = script.ReplaceInvalidChars(prefab.name);

                string path = Folders["ItemSources"] + "/" + savename + ".json";

                if (File.Exists(path)) { File.Delete(path); }

                File.WriteAllText(path, JsonUtility.ToJson(entry.Value, true));
            }
        }

        // ====== new unique dictionary entry functions ========

        private void NewItemSource(int id, ref Dictionary<int, SortTemplates.ItemSources> itemSources)
        {
            itemSources.Add(id, new SortTemplates.ItemSources
            {
                Name = ResourcesPrefabManager.Instance.GetItemPrefab(id).Name,
                ID = id,
                DropTableSources = new List<string>(),
                ContainerSources = new List<string>(),
                EnemySources = new List<string>(),
                MerchantSources = new List<string>(),
                SpawnSources = new List<string>()
            });
        }

        private void NewDroptableSource(string tableName, ref Dictionary<string, SortTemplates.DropTableSummary> dropTables)
        {
            dropTables.Add(tableName, new SortTemplates.DropTableSummary
            {
                Name = tableName,
                ContainerBase = new Templates.DropTableContainer { },
                EnemySources = new List<string>(),
                MerchantSources = new List<string>(),
                ContainerSources = new List<string>(),
                DropTables = new List<Templates.DropTableTemplate>(),
            });

            // load up the full saved json for this dropable
            string origJson = File.ReadAllText(script.Folders["Droptables"] + "/" + tableName + ".json");

            // fix the json and add to our droptables list for this dropable (that cannot be serialized)
            dropTables[tableName].DropTables = DroptableJsonFix(ref origJson);

            // fix the container base template with the remaining fixed orig json
            JsonUtility.FromJsonOverwrite(origJson, dropTables[tableName].ContainerBase);
        }

        private void NewContainerSummary(string containerName, ref Dictionary<string, SortTemplates.ContainerSummary> containerSummaries)
        {
            containerSummaries.Add(containerName, new SortTemplates.ContainerSummary
            {
                Name = containerName,
                AllDropTables = new List<string>(),
                Locations = new List<string>(),
            });
        }



        // ================================ list building =================================

        public void BuildLists()
        {
            Directory.CreateDirectory(Folders["Lists"]);

            AllPrefabList();

            EnemyTable();

            RecipeTable();

            EffectsTable();

            MerchantsTable();

            LootContainerTable();

            DroptablesTable();

            SceneSummaryTable();

            ItemSourcesTable();

            Debug.LogWarning("list building done!");
        }

        public void SaveTable(List<string> Table, string path)
        {
            if (File.Exists(path)) { File.Delete(path); }
            File.WriteAllLines(path, Table.ToArray());
        }

        public void AllPrefabList()
        {
            List<string> filePaths = Directory.GetFiles(script.Folders["Items"], "*.json").ToList();

            List<string> AllItems = new List<string>();
            Dictionary<string, string> ItemTagSources = new Dictionary<string, string>();
            List<string> TagSourceFix = new List<string>();
            List<string> WeaponTable = new List<string>();
            List<string> EquipTable = new List<string>();

            foreach (string s in filePaths)
            {
                Templates.ItemTemplate item = new Templates.ItemTemplate();
                JsonUtility.FromJsonOverwrite(File.ReadAllText(s), item);
                AllItems.Add(item.ItemID + "	" + item.Name + "	" + Path.GetFileNameWithoutExtension(s));

                if (item.Tags.Count > 0)
                {
                    foreach (string tag in item.Tags)
                    {
                        if (ItemTagSources.ContainsKey(tag))
                        {
                            ItemTagSources[tag] += ", " + item.Name;
                            int index = ItemTagSources.Keys.ToArray().IndexOf(tag);
                            TagSourceFix[index] += ", " + Path.GetFileNameWithoutExtension(s);
                        }
                        else
                        {
                            ItemTagSources.Add(tag, item.Name);
                            TagSourceFix.Add(Path.GetFileNameWithoutExtension(s));
                        }
                    }
                }

                if (item.Type.Contains("Weapon"))
                {
                    WeaponTableEntry(ref WeaponTable, item, s);
                }

                if (item.Type == "Armor" || item.Type == "Equipment" || item.Type == "Bag")
                {
                    EquipmentTableEntry(ref EquipTable, item, s);
                }
            }

            List<string> TagTable = new List<string>();
            int z = 0;
            foreach (KeyValuePair<string,string> entry in ItemTagSources)
            {
                TagTable.Add(entry.Key + "	" + entry.Value + "	" + TagSourceFix[z]);
                z++;
            }

            SaveTable(AllItems, Folders["Lists"] + "/" + "All Prefabs.txt");
            SaveTable(TagTable, Folders["Lists"] + "/" + "Item Tag Sources.txt");

            SaveTable(WeaponTable, Folders["Lists"] + "/" + "SHEETS WeaponTable.txt");
            SaveTable(EquipTable, Folders["Lists"] + "/" + "SHEETS EquipTable.txt");
        }
        
        public void WeaponTableEntry(ref List<string>WeaponTable, Templates.ItemTemplate item, string s)
        {
            // blacklist
            if (item.Name == "-"
                || item.Name == "Blacksmith's Hammer"
                || item.Name == "CalixaMaceGun"
                || item.Name == "Elite Queen Trog Mana Staff"
                || item.Name == "EliteBeastGolemBeak"
                || item.Name == "EliteCrescentShark Jaw"
                || item.Name == "Etheral Axe"
                || item.Name == "Giant Arch Priest Blade Guandao"
                || item.Name == "ImmaculateBlade"
                || item.Name == "Lich Gold TwoHanded Spear"
                || item.Name == "Mantis Claws"
                || (item.Name == "Marble Greataxe" && item.ItemID != 2110020) // theres 3 duplicated "Marble Greataxe" items for NPCs
                || item.Name.ToLower().Contains("newghost")
                || item.Name == "ShellHorrorClawWeak"
                || (item.Name == "Palladium Sword" && item.ItemID != 2000140) // cyrene's special palladium sword
            )
            {
                return;
            }

            Templates.WeaponTemplate weapon = new Templates.WeaponTemplate();
            JsonUtility.FromJsonOverwrite(File.ReadAllText(s), weapon);

            List<string> toAdd = new List<string> { weapon.Name };

            try { toAdd.Add(weapon.BaseDamage[DamageType.Types.Physical].Damage.ToString()); } catch { toAdd.Add("0"); }
            try { toAdd.Add(weapon.BaseDamage[DamageType.Types.Ethereal].Damage.ToString()); } catch { toAdd.Add("0"); }
            try { toAdd.Add(weapon.BaseDamage[DamageType.Types.Decay].Damage.ToString()); } catch { toAdd.Add("0"); }
            try { toAdd.Add(weapon.BaseDamage[DamageType.Types.Electric].Damage.ToString()); } catch { toAdd.Add("0"); }
            try { toAdd.Add(weapon.BaseDamage[DamageType.Types.Frost].Damage.ToString()); } catch { toAdd.Add("0"); }
            try { toAdd.Add(weapon.BaseDamage[DamageType.Types.Fire].Damage.ToString()); } catch { toAdd.Add("0"); }

            toAdd.Add(weapon.Impact.ToString());
            toAdd.Add(weapon.AttackSpeed.ToString());

            int type = (int)(ResourcesPrefabManager.Instance.GetItemPrefab(weapon.ItemID) as Weapon).Type;
            toAdd.Add(type.ToString());

            for (int j = 0; j < 6; j++) { toAdd.Add(weapon.DamageAttack[j].ToString()); }

            toAdd.Add(weapon.ManaUseModifier.ToString());
            toAdd.Add(weapon.Durability.ToString());
            toAdd.Add(weapon.Weight.ToString());

            string HitEffects = "";
            int i = 1;
            foreach (string fx in weapon.HitEffects)
            {
                HitEffects += fx + " (" + weapon.HitEffects_Buildups[i - 1] + ")";
                if (i < weapon.HitEffects.Count()) { HitEffects += ", "; }
                i++;
            }
            toAdd.Add(HitEffects);

            toAdd.Add(weapon.BaseValue.ToString());
            toAdd.Add((weapon.BaseValue * 0.3f).ToString());

            string newDesc = Regex.Replace(weapon.Description, @"\n\n", " ");
            toAdd.Add(newDesc);

            // POST-FIX: legacy item IDs
            toAdd.Add(weapon.LegacyItemID.ToString());

            // add to actual weapon table
            string entry = "";
            int k = 0;
            foreach (string field in toAdd)
            {
                entry += field;
                if (k < toAdd.Count() - 1) { entry += "	"; } // the gap is a TAB character (	)
                k++;
            }
            WeaponTable.Add(entry);
        }

        public void EquipmentTableEntry(ref List<string> EquipTable, Templates.ItemTemplate item, string s)
        {
            // blacklist
            if (item.Name == "-"
                || item.Name.ToLower().Contains("booster")
                || item.Name.Contains("Merton")
                || item.Name.Contains("Skeleton")
                || item.Name.Contains("Rissa")
                || item.Name.Contains("Oliele")
                || item.Name.Contains("Calixa")
                || item.Name.Contains("Yzan")
            )
            {
                return;
            }

            Templates.EquipmentTemplate equipment = new Templates.EquipmentTemplate();
            JsonUtility.FromJsonOverwrite(File.ReadAllText(s), equipment);

            if (equipment.DamageResistance == null) { return; } // will only be NULL if item has no item stats at all (eg cosmetic NPC armor)

            List<string> toAdd = new List<string> { equipment.Name };

            toAdd.Add(equipment.EquipmentSlot);

            for (int j = 0; j < 6; j++) { toAdd.Add(equipment.DamageResistance[j].ToString()); }

            toAdd.Add(equipment.ImpactResistance.ToString());
            toAdd.Add(equipment.Protection.ToString());

            for (int j = 0; j < 6; j++) { toAdd.Add(equipment.DamageAttack[j].ToString()); }

            toAdd.Add(equipment.StaminaUsePenalty.ToString());
            toAdd.Add(equipment.MovementPenalty.ToString());
            toAdd.Add(equipment.ManaUseModifier.ToString());

            toAdd.Add(equipment.PouchBonus.ToString());
            toAdd.Add(equipment.HeatProtect.ToString());
            toAdd.Add(equipment.ColdProtect.ToString());

            toAdd.Add(equipment.Durability.ToString());
            toAdd.Add(equipment.Weight.ToString());

            toAdd.Add(equipment.BaseValue.ToString());
            toAdd.Add((0.3f * equipment.BaseValue).ToString());
            string newDesc = Regex.Replace(equipment.Description, @"\n\n", " ");
            toAdd.Add(newDesc);

            // POST-FIX: legacy item IDs
            toAdd.Add(equipment.LegacyItemID.ToString());

            // add to actual table
            string entry = "";
            int k = 0;
            foreach (string field in toAdd)
            {
                entry += field;
                if (k < toAdd.Count() - 1) { entry += "	"; } // the gap is a TAB character (	)
                k++;
            }
            EquipTable.Add(entry);
        }

        public void EnemyTable()
        {
            List<string> EnemyTable = new List<string>();

            List<string> filePaths = Directory.GetFiles(Folders["Enemies"], "*.json").ToList();
            foreach (string s in filePaths)
            {
                Templates.EnemyTemplate enemy = new Templates.EnemyTemplate();

                string json = File.ReadAllText(s);
                enemy = EnemyJsonFix(ref json);
                SortTemplates.UniqueEnemySummary uniqueEnemy = new SortTemplates.UniqueEnemySummary();
                JsonUtility.FromJsonOverwrite(json, uniqueEnemy);

                List<string> toAdd = new List<string> { uniqueEnemy.GameObject_Name };

                toAdd.Add(enemy.MaxHealth.ToString());

                for (int i = 0; i < 6; i++)
                {
                    toAdd.Add(enemy.DamageResistances[i].ToString());
                }

                toAdd.Add(enemy.ImpactResistance.ToString());
                toAdd.Add(enemy.Protection.ToString());
                toAdd.Add(enemy.Radius.ToString());

                if (enemy.Status_Immunities != null)
                {
                    string immune = "";
                    int j = 0;
                    foreach (string fx in enemy.Status_Immunities)
                    {
                        immune += fx;
                        if (j < enemy.Status_Immunities.Count() - 1)
                        {
                            immune += ", ";
                        }
                        j++;
                    }
                    toAdd.Add(immune);
                }
                else
                {
                    toAdd.Add("");
                }

                if (enemy.Weapon_Damage != null)
                {
                    try { toAdd.Add(enemy.Weapon_Damage[DamageType.Types.Physical].Damage.ToString()); } catch { toAdd.Add("0"); }
                    try { toAdd.Add(enemy.Weapon_Damage[DamageType.Types.Ethereal].Damage.ToString()); } catch { toAdd.Add("0"); }
                    try { toAdd.Add(enemy.Weapon_Damage[DamageType.Types.Decay].Damage.ToString()); } catch { toAdd.Add("0"); }
                    try { toAdd.Add(enemy.Weapon_Damage[DamageType.Types.Electric].Damage.ToString()); } catch { toAdd.Add("0"); }
                    try { toAdd.Add(enemy.Weapon_Damage[DamageType.Types.Frost].Damage.ToString()); } catch { toAdd.Add("0"); }
                    try { toAdd.Add(enemy.Weapon_Damage[DamageType.Types.Fire].Damage.ToString()); } catch { toAdd.Add("0"); }
                }
                else
                {
                    for (int z = 0; z < 6; z++) { toAdd.Add("0"); }
                }

                toAdd.Add(enemy.Weapon_Impact.ToString());

                if (enemy.Inflicts != null)
                {
                    string hitEffects = "";
                    int j = 0;
                    foreach (string fx in enemy.Inflicts)
                    {
                        hitEffects += fx;
                        if (j < enemy.Inflicts.Count() - 1)
                        {
                            hitEffects += ", ";
                        }
                        j++;
                    }
                    toAdd.Add(hitEffects);
                }
                else
                {
                    toAdd.Add("");
                }

                if (enemy.Skills != null)
                {
                    string skills = "";
                    int j = 0;
                    foreach (string skill in enemy.Skills)
                    {
                        skills += skill;
                        if (j < enemy.Skills.Count() - 1)
                        {
                            skills += ", ";
                        }
                        j++;
                    }
                    toAdd.Add(skills);
                }
                else
                {
                    toAdd.Add("");
                }

                if (enemy.GuaranteedDrops != null)
                {
                    string drops = "";
                    int j = 0;
                    foreach (string drop in enemy.GuaranteedDrops)
                    {
                        drops += drop + " (" + enemy.GuaranteedQtys[j] + ")";
                        if (j < enemy.GuaranteedDrops.Count() - 1)
                        {
                            drops += ", ";
                        }
                        j++;
                    }
                    toAdd.Add(drops);
                }
                else
                {
                    toAdd.Add("");
                }

                if (enemy.DropTables != null)
                {
                    string tables = "";
                    int j = 0;
                    foreach (string table in enemy.DropTables)
                    {
                        tables += table;
                        if (j < enemy.DropTables.Count() - 1)
                        {
                            tables += ", ";
                        }
                        j++;
                    }
                    toAdd.Add(tables);
                }
                else
                {
                    toAdd.Add("");
                }

                if (uniqueEnemy.Locations.Count > 1)
                {
                    string locations = "";
                    for (int j = 0; j < uniqueEnemy.Locations.Count; j++)
                    {
                        locations += uniqueEnemy.Locations[j];
                        if (j < uniqueEnemy.Locations.Count - 1) { locations += ", "; }
                    }
                    toAdd.Add(locations);
                }
                else
                {
                    toAdd.Add(uniqueEnemy.Locations[0]);
                }

                if (enemy.Equipment.Count > 1)
                {
                    string equipment = "";
                    for (int j = 0; j < enemy.Equipment.Count; j++)
                    {
                        equipment += enemy.Equipment[j];
                        if (j < enemy.Equipment.Count - 1) { equipment += ", "; }
                    }
                    toAdd.Add(equipment);
                }

                // late addition: enemy health regen
                toAdd.Add(enemy.HealthRegen.ToString());

                // add to actual table
                string entry = "";
                int k = 0;
                foreach (string field in toAdd)
                {
                    entry += field;
                    if (k < toAdd.Count() - 1) { entry += "	"; } // the gap is a TAB character (	)
                    k++;
                }
                EnemyTable.Add(entry);
            }

            SaveTable(EnemyTable, Folders["Lists"] + "/" + "SHEETS EnemyTable.txt");
        }

        public void RecipeTable()
        {
            List<string> RecipeTable = new List<string>();
            List<string> UniqueRecipes = new List<string>();

            List<string> filePaths = Directory.GetFiles(script.Folders["Recipes"], "*.json").ToList();

            foreach (string s in filePaths)
            {
                List<string> toAdd = new List<string>();

                Templates.RecipeTemplate recipe = new Templates.RecipeTemplate();
                JsonUtility.FromJsonOverwrite(File.ReadAllText(s), recipe);

                string uName = "";
                if (UniqueRecipes.Contains(recipe.Result))
                {
                    for (int i = 2; i < 99; i++)
                    {
                        uName = recipe.Result + "_" + i;
                        if (UniqueRecipes.Contains(uName)) { continue; }
                        UniqueRecipes.Add(uName);
                        break;
                    }
                }
                else
                {
                    uName = recipe.Result;
                    UniqueRecipes.Add(recipe.Result);
                }

                toAdd.Add(uName);
                toAdd.Add(recipe.ResultCount.ToString());
                toAdd.Add(recipe.RecipeType);
                for (int j = 0; j < 4; j++)
                {
                    if (recipe.Ingredients.Count <= j) { toAdd.Add(""); }
                    else { toAdd.Add(recipe.Ingredients[j]); }
                }

                // add to actual table
                string entry = "";
                int k = 0;
                foreach (string field in toAdd)
                {
                    entry += field;
                    if (k < toAdd.Count() - 1) { entry += "	"; } // the gap is a TAB character (	)
                    k++;
                }
                RecipeTable.Add(entry);
            }

            SaveTable(RecipeTable, Folders["Lists"] + "/" + "SHEETS RecipeTable.txt");
        }

        public void EffectsTable()
        {
            List<string> EffectsTable = new List<string>();

            List<string> filePaths = Directory.GetFiles(script.Folders["Effects"], "*.json").ToList();

            foreach (string s in filePaths)
            {
                List<string> toAdd = new List<string>();

                Templates.StatusEffectTemplate effect = new Templates.StatusEffectTemplate();
                JsonUtility.FromJsonOverwrite(File.ReadAllText(s), effect);

                toAdd.Add(effect.EffectID.ToString());
                toAdd.Add(effect.Name);
                toAdd.Add(effect.Type);
                toAdd.Add(effect.Lifespan.ToString());
                toAdd.Add(effect.Purgeable.ToString());

                string affectedStats = "";
                string affectedValues = "";
                string valuesAI = "";
                for (int i = 0; i < effect.AffectedStats.Count; i++)
                {
                    affectedStats += effect.AffectedStats[i];
                    try { affectedValues += effect.Values[i]; } catch { affectedValues += "?"; }
                    try { valuesAI += effect.Values_AI[i]; } catch { valuesAI += "-"; }

                    if (i < effect.AffectedStats.Count - 1) { affectedStats += ", "; affectedValues += ", "; valuesAI += ","; }
                }
                toAdd.Add(affectedStats);
                toAdd.Add(affectedValues);
                toAdd.Add(valuesAI);

                if (effect.Type == "Imbue")
                {
                    if (effect.Imbue_Damage.Count() > 0)
                    {
                        toAdd.Add(effect.Imbue_Damage[0].Damage + " " + effect.Imbue_Damage[0].Type.ToString());
                        toAdd.Add(effect.Imbue_Multiplier.ToString());
                    }
                    else
                    {
                        toAdd.Add("");
                        toAdd.Add("1.0");
                    }
                    toAdd.Add(effect.Imbue_HitEffect);
                }

                // add to actual table
                string entry = "";
                int k = 0;
                foreach (string field in toAdd)
                {
                    entry += field;
                    if (k < toAdd.Count() - 1) { entry += "	"; } // the gap is a TAB character (	)
                    k++;
                }
                EffectsTable.Add(entry);
            }

            SaveTable(EffectsTable, Folders["Lists"] + "/" + "SHEETS EffectsTable.txt");
        }

        public void MerchantsTable()
        {
            List<string> MerchantTable = new List<string>();

            List<string> filePaths = Directory.GetFiles(Folders["Merchants"], "*.json").ToList();

            foreach (string s in filePaths)
            {
                List<string> toAdd = new List<string>();

                Templates.Merchant merchant = new Templates.Merchant();
                JsonUtility.FromJsonOverwrite(File.ReadAllText(s), merchant);

                toAdd.Add(merchant.Name);
                toAdd.Add(merchant.Location);

                string droptablePath = script.Folders["Droptables"] + "/" + merchant.Location + " - " + merchant.DropTables[0] + ".json";
                if (File.Exists(droptablePath))
                {
                    string json = File.ReadAllText(droptablePath);

                    List<Templates.DropTableTemplate> droptables = new List<Templates.DropTableTemplate>();
                    droptables = DroptableJsonFix(ref json);

                    Templates.DropTableContainer summary = new Templates.DropTableContainer();
                    JsonUtility.FromJsonOverwrite(json, summary);

                    toAdd.Add(DropTableAsString(summary, droptables));
                }
                else
                {
                    Debug.LogError("orig table not found for " + merchant.Name);
                }

                // add to actual table
                string entry = "";
                int k = 0;
                foreach (string field in toAdd)
                {
                    entry += field;
                    if (k < toAdd.Count() - 1) { entry += "	"; } // the gap is a TAB character (	)
                    k++;
                }
                MerchantTable.Add(entry);
            }

            SaveTable(MerchantTable, Folders["Lists"] + "/" + "SHEETS MerchantTable.txt");
        }

        public void DroptablesTable()
        {
            List<string> DTTable = new List<string>();

            List<string> filePaths = Directory.GetFiles(Folders["Droptables"], "*.json").ToList();

            foreach (string s in filePaths)
            {
                string tablepathname = Path.GetFileNameWithoutExtension(s);
                if (!tablepathname.ToLower().Contains("droptable"))
                {
                    continue;
                }

                List<string> toAdd = new List<string>();

                SortTemplates.DropTableSummary summary = new SortTemplates.DropTableSummary();
                JsonUtility.FromJsonOverwrite(File.ReadAllText(s), summary);

                toAdd.Add(summary.Name.Substring(10));

                string enemys = "";
                string merchants = "";
                string containers = "";
                if (summary.EnemySources.Count > 0)
                {
                    for (int i = 0; i < summary.EnemySources.Count; i++)
                    {
                        enemys += summary.EnemySources[i];
                        if (i < summary.EnemySources.Count - 1) { enemys += ", "; }
                    }
                }
                if (summary.MerchantSources.Count > 0)
                {
                    for (int i = 0; i < summary.MerchantSources.Count; i++)
                    {
                        merchants += summary.MerchantSources[i];
                        if (i < summary.MerchantSources.Count - 1) { merchants += ", "; }
                    }
                }
                if (summary.ContainerSources.Count > 0)
                {
                    for (int i = 0; i < summary.ContainerSources.Count; i++)
                    {
                        containers += summary.ContainerSources[i];
                        if (i < summary.ContainerSources.Count - 1) { containers += ", "; }
                    }
                }
                toAdd.Add(enemys);
                toAdd.Add(merchants);
                toAdd.Add(containers);

                string droptablePath = script.Folders["Droptables"] + "/" + Path.GetFileName(s);
                if (File.Exists(droptablePath))
                {
                    string json = File.ReadAllText(droptablePath);

                    List<Templates.DropTableTemplate> droptables = new List<Templates.DropTableTemplate>();
                    droptables = DroptableJsonFix(ref json);

                    Templates.DropTableContainer container = new Templates.DropTableContainer();
                    JsonUtility.FromJsonOverwrite(json, container);

                    toAdd.Add(DropTableAsString(container, droptables));
                }

                // add to actual table
                string entry = "";
                int k = 0;
                foreach (string field in toAdd)
                {
                    entry += field;
                    if (k < toAdd.Count() - 1) { entry += "	"; } // the gap is a TAB character (	)
                    k++;
                }
                DTTable.Add(entry);
            }

            SaveTable(DTTable, Folders["Lists"] + "/" + "SHEETS DropTableTable.txt");
        }

        public string DropTableAsString(Templates.DropTableContainer container, List<Templates.DropTableTemplate> tables)
        {
            string entry = "";
            List<string> toAdd = new List<string>();

            string guaDrops = "";
            string guaMins = "";
            string guaMaxs = "";
            for (int i = 0; i < container.GuaranteedDrops.Count; i++)
            {
                guaDrops += container.GuaranteedDrops[i];
                guaMins += container.GuaranteedMinQtys[i];
                guaMaxs += container.GuaranteedMaxQtys[i];

                if (i < container.GuaranteedDrops.Count - 1) { guaDrops += ", "; guaMins += ", "; guaMaxs += ", "; }
            }
            toAdd.Add(guaDrops);
            toAdd.Add(guaMins);
            toAdd.Add(guaMaxs);

            foreach (Templates.DropTableTemplate table in tables)
            {
                toAdd.Add(table.MinNumberOfDrops.ToString());
                toAdd.Add(table.MaxNumberOfDrops.ToString());
                toAdd.Add(table.MaxDiceValue.ToString());
                toAdd.Add(table.EmptyDropChance.ToString());

                string items = "";
                string itemMins = "";
                string itemMaxs = "";
                string itemChances = "";
                for (int j = 0; j < table.ItemChances.Count; j++)
                {
                    items += table.ItemChances[j];
                    itemMins += table.ChanceMinQtys[j];
                    itemMaxs += table.ChanceMaxQtys[j];
                    itemChances += table.ChanceDropChances[j];

                    if (j < table.ItemChances.Count - 1) { items += ", "; itemMins += ", "; itemMaxs += ", "; itemChances += ", "; }
                }
                toAdd.Add(items);
                toAdd.Add(itemMins);
                toAdd.Add(itemMaxs);
                toAdd.Add(itemChances);
            }

            int k = 0;
            foreach (string field in toAdd)
            {
                entry += field;
                if (k < toAdd.Count() - 1) { entry += "	"; } // the gap is a TAB character (	)
                k++;
            }

            return entry;
        }

        public void LootContainerTable()
        {
            List<string> ContainerTable = new List<string>();

            List<string> filePaths = Directory.GetFiles(Folders["LootContainers"], "*.json").ToList();

            foreach (string s in filePaths)
            {
                List<string> toAdd = new List<string>();

                SortTemplates.ContainerSummary summary = new SortTemplates.ContainerSummary();
                JsonUtility.FromJsonOverwrite(File.ReadAllText(s), summary);

                toAdd.Add(summary.Name);
                toAdd.Add(summary.Type);

                string locs = "";
                for (int i = 0; i < summary.Locations.Count; i++)
                {
                    locs += summary.Locations[i];
                    if (i < summary.Locations.Count - 1) { locs += ", "; }
                }
                string tables = "";
                for (int i = 0; i < summary.AllDropTables.Count; i++)
                {
                    tables += summary.AllDropTables[i];
                    if (i < summary.AllDropTables.Count - 1) { tables += ", "; }
                }
                toAdd.Add(locs);
                toAdd.Add(tables);

                // add to actual table
                string entry = "";
                int k = 0;
                foreach (string field in toAdd)
                {
                    entry += field;
                    if (k < toAdd.Count() - 1) { entry += "	"; } // the gap is a TAB character (	)
                    k++;
                }
                ContainerTable.Add(entry);
            }

            SaveTable(ContainerTable, Folders["Lists"] + "/" + "SHEETS ContainerTable.txt");
        }

        public void SceneSummaryTable()
        {
            List<string> ScenesTable = new List<string>();

            List<string> filePaths = Directory.GetFiles(Folders["SceneSummaries"], "*.json").ToList();

            foreach (string s in filePaths)
            {
                List<string> toAdd = new List<string>();

                SortTemplates.SceneSummary summary = new SortTemplates.SceneSummary();
                JsonUtility.FromJsonOverwrite(File.ReadAllText(s), summary);

                toAdd.Add(summary.Scene_Name);

                string enemies = "";
                string enemyCounts = "";
                string merchants = "";
                string containers = "";
                string containerCounts = "";
                string allContainers = "";
                string spawns = "";
                string spawnCounts = "";
                if (summary.Enemies.Count > 0)
                {
                    for (int i = 0; i < summary.Enemies.Count; i++)
                    {
                        enemies += summary.Enemies[i];
                        enemyCounts += summary.Enemy_Counts[i];

                        if (i < summary.Enemies.Count - 1) { enemies += ", "; enemyCounts += ", "; }
                    }
                }
                if (summary.Merchants.Count > 0)
                {
                    for (int i = 0; i < summary.Merchants.Count; i++)
                    {
                        merchants += summary.Merchants[i];

                        if (i < summary.Merchants.Count - 1) { merchants += ", "; }
                    }
                }
                if (summary.Loot_Containers.Count > 0)
                {
                    for (int i = 0; i < summary.Loot_Containers.Count; i++)
                    {
                        containers += summary.Loot_Containers[i];
                        containerCounts += summary.Container_Counts[i];

                        if (i < summary.Loot_Containers.Count - 1) { containers += ", "; containerCounts += ", "; }
                    }
                }
                if (summary.All_Containers.Count > 0)
                {
                    for (int i = 0; i < summary.All_Containers.Count; i++)
                    {
                        allContainers += summary.All_Containers[i];

                        if (i < summary.All_Containers.Count - 1) { allContainers += ", "; }
                    }
                }
                if (summary.ItemSpawns.Count > 0)
                {
                    for (int i = 0; i < summary.ItemSpawns.Count; i++)
                    {
                        spawns += summary.ItemSpawns[i];
                        spawnCounts += summary.ItemSpawnCounts[i];

                        if (i < summary.ItemSpawnCounts.Count - 1) { spawns += ", "; spawnCounts += ", "; }
                    }
                }
                toAdd.Add(enemies);
                toAdd.Add(enemyCounts);
                toAdd.Add(merchants);
                toAdd.Add(containers);
                toAdd.Add(containerCounts);
                toAdd.Add(allContainers);
                toAdd.Add(spawns);
                toAdd.Add(spawnCounts);

                // add to actual table
                string entry = "";
                int k = 0;
                foreach (string field in toAdd)
                {
                    entry += field;
                    if (k < toAdd.Count() - 1) { entry += "	"; } // the gap is a TAB character (	)
                    k++;
                }
                ScenesTable.Add(entry);
            }

            SaveTable(ScenesTable, Folders["Lists"] + "/" + "SHEETS SceneSummariesTable.txt");
        }

        public void ItemSourcesTable()
        {
            List<string> ItemSourcesTable = new List<string>();

            List<string> filePaths = Directory.GetFiles(Folders["ItemSources"], "*.json").ToList();

            foreach (string s in filePaths)
            {
                List<string> toAdd = new List<string>();

                SortTemplates.ItemSources sources = new SortTemplates.ItemSources();
                JsonUtility.FromJsonOverwrite(File.ReadAllText(s), sources);

                toAdd.Add(sources.Name);

                string tableSources = "";
                string enemies = "";
                string merchants = "";
                string containers = "";
                string spawns = "";

                if (sources.EnemySources.Count > 0)
                {
                    for (int i = 0; i < sources.EnemySources.Count; i++)
                    {
                        enemies += sources.EnemySources[i];

                        if (i < sources.EnemySources.Count - 1) { enemies += ", "; }
                    }
                }
                if (sources.MerchantSources.Count > 0)
                {
                    for (int i = 0; i < sources.MerchantSources.Count; i++)
                    {
                        merchants += sources.MerchantSources[i];

                        if (i < sources.MerchantSources.Count - 1) { merchants += "; "; }
                    }
                }
                if (sources.ContainerSources.Count > 0)
                {
                    for (int i = 0; i < sources.ContainerSources.Count; i++)
                    {
                        containers += sources.ContainerSources[i];

                        if (i < sources.ContainerSources.Count - 1) { containers += ", "; }
                    }
                }
                if (sources.SpawnSources.Count > 0)
                {
                    for (int i = 0; i < sources.SpawnSources.Count; i++)
                    {
                        spawns += sources.SpawnSources[i];

                        if (i < sources.SpawnSources.Count - 1) { spawns += ", "; }
                    }
                }
                if (sources.DropTableSources.Count > 0)
                {
                    for (int i = 0; i < sources.DropTableSources.Count; i++)
                    {
                        tableSources += sources.DropTableSources[i];

                        if (i < sources.DropTableSources.Count - 1) { tableSources += ", ";}
                    }
                }
                toAdd.Add(tableSources);
                toAdd.Add(enemies);
                toAdd.Add(merchants);
                toAdd.Add(containers);
                toAdd.Add(spawns);

                // add to actual table
                string entry = "";
                int k = 0;
                foreach (string field in toAdd)
                {
                    entry += field;
                    if (k < toAdd.Count() - 1) { entry += "	"; } // the gap is a TAB character (	)
                    k++;
                }
                ItemSourcesTable.Add(entry);
            }

            SaveTable(ItemSourcesTable, Folders["Lists"] + "/" + "SHEETS ItemSourcesTable.txt");
        }

        // =============================== special functions =================================
        public Templates.EnemyTemplate EnemyJsonFix(ref string orig)
        {
            Templates.EnemyTemplate returnEnemy = new Templates.EnemyTemplate();

            string search = "Enemy Stats\" : ";
            Regex rx = new Regex(search);
            Match match1 = rx.Match(orig);

            if (match1.Success)
            {
                int trimStart = search.Length;
                int startPos = match1.Index + trimStart;
                int length = orig.Length - 2 - startPos;

                string fix = orig.Substring(startPos, length);

                JsonUtility.FromJsonOverwrite(fix, returnEnemy);

                orig = orig.Substring(0, match1.Index - 5) + "}";
            }

            return returnEnemy;
        }


        public List<Templates.DropTableTemplate> DroptableJsonFix(ref string orig)
        {
            List<Templates.DropTableTemplate> returnList = new List<Templates.DropTableTemplate>();

            int firstmatch = -1;

            for (int i = 1; i < 99; i++)
            {
                Regex rx = new Regex("Droptable_" + i);
                Match match1 = rx.Match(orig);
                if (match1.Success)
                {
                    if (i == 1) { firstmatch = match1.Index; }

                    int trimStart = i.ToString().Length + 14;
                    int startpos = match1.Index + trimStart;
                    int length = orig.Length - 2 - startpos;

                    // check if theres more droptables
                    string s2 = "Droptable_" + (i + 1);
                    Regex rx2 = new Regex(s2);
                    if (rx2.Match(orig) is Match match2 && match2.Success)
                    {
                        length = match2.Index - 5 - startpos;
                    }

                    string fix = orig.Substring(startpos, length);

                    //Debug.Log("fixed json: \r\n" + fix);

                    returnList.Add(JsonUtility.FromJson(fix, typeof(Templates.DropTableTemplate)) as Templates.DropTableTemplate);
                    // return fix;
                }
                else
                {
                    break;
                }
            }

            // fix the orig string to not contain droptables
            if (firstmatch != -1) { orig = orig.Substring(0, firstmatch - 5) + "}"; }

            // Debug.Log("orig string: \r\n" + orig);

            return returnList;
        }


        public string GetFolderName(string path)
        {
            DirectoryInfo dir_info = new DirectoryInfo(path);
            string directory = dir_info.Name;
            return directory;
        }
    }

    public class SortTemplates
    {

        public class SceneSummary
        {
            public string Scene_Name;

            public List<string> Enemies;
            public List<int> Enemy_Counts;

            public List<string> Merchants;

            public List<string> Loot_Containers;
            public List<int> Container_Counts;
            public List<string> All_Containers;

            public List<string> ItemSpawns;
            public List<int> ItemSpawnCounts;
        }

        public class ItemSources
        {
            public string Name;
            public int ID;

            public List<string> DropTableSources;
            public List<string> MerchantSources;
            public List<string> SpawnSources; // scene location
            public List<string> EnemySources; // unique go_name_N enemy
            public List<string> ContainerSources; // (with location in string)
        }

        public class DropTableSummary
        {
            public string Name;
            public Templates.DropTableContainer ContainerBase;
            public List<Templates.DropTableTemplate> DropTables;

            public List<string> EnemySources;
            public List<string> MerchantSources;
            public List<string> ContainerSources; // with - scene
        }

        public class UniqueEnemySummary
        {
            public string Name;
            public string GameObject_Name;

            public List<string> Locations;

            public Templates.EnemyTemplate enemyJson;
        }

        public class ContainerSummary
        {
            public string Name;
            public string Type;

            public List<string> Locations;

            public List<string> AllDropTables;
        }
    }
}
