using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using System.Xml.Serialization;

namespace Dataminer
{
    public class ListManager : MonoBehaviour
    {
        public static ListManager Instance;

        // Scene Summary dictionary
        public static Dictionary<string, SceneSummary> SceneSummaries = new Dictionary<string, SceneSummary>();

        // Tag Sources
        public static Dictionary<string, List<string>> TagSources = new Dictionary<string, List<string>>();

        // Item Loot Sources (spawns and loot containers)
        public static Dictionary<string, ItemSource> ItemLootSources = new Dictionary<string, ItemSource>();

        // Container Summaries
        public static Dictionary<string, ContainerSummary> ContainerSummaries = new Dictionary<string, ContainerSummary>();

        // Lists
        public static Dictionary<string, List<EnemyHolder>> EnemyManifest = new Dictionary<string, List<EnemyHolder>>();
        public static Dictionary<string, MerchantHolder> Merchants = new Dictionary<string, MerchantHolder>();
        public static Dictionary<string, ItemHolder> Items = new Dictionary<string, ItemHolder>();
        public static Dictionary<string, StatusEffectHolder> Effects = new Dictionary<string, StatusEffectHolder>();
        public static Dictionary<string, RecipeHolder> Recipes = new Dictionary<string, RecipeHolder>();
        public static Dictionary<string, DroptableHolder> DropTables = new Dictionary<string, DroptableHolder>();

        internal void Awake()
        {
            Instance = this;
        }

        //internal void Update()
        //{
        //    if (Input.GetKeyDown(KeyCode.ScrollLock))
        //    {
        //        Debug.Log("Force saving lists");
        //        SaveLists();
        //    }
        //}

        public static string GetSceneSummaryKey(Vector3 position)
        {
            return SceneManager.Instance.GetCurrentRegion() + ":" + SceneManager.Instance.GetCurrentLocation(position);
        }

        public static void AddTagSource(Tag tag, string source)
        {
            string key = tag.ToString();

            if (TagSources.ContainsKey(key))
            {
                if (source != null && !TagSources[key].Contains(source))
                {
                    TagSources[key].Add(source);
                }
            }
            else
            {
                TagSources.Add(key, new List<string> { });
                if (source != null)
                {
                    TagSources[key].Add(source);
                }
            }
        }

        public static void AddContainerSummary(string name, string location, List<string> dropTables)
        {
            if (ContainerSummaries.ContainsKey(name))
            {
                var summary = ContainerSummaries[name];

                bool addLocation = true;
                foreach (var loc in summary.Locations_Found)
                {
                    if (loc.Name == location)
                    {
                        loc.Quantity++;
                        addLocation = false;
                        break;
                    }
                }
                if (addLocation)
                {
                    summary.Locations_Found.Add(new SceneSummary.QuantityHolder
                    {
                        Name = location,
                        Quantity = 1
                    });
                }

                foreach (string table in dropTables)
                {
                    if (!summary.All_DropTables.Contains(table))
                    {
                        summary.All_DropTables.Add(table);
                    }
                }
            }
            else
            {
                ContainerSummaries.Add(name, new ContainerSummary
                {
                    Name = name,
                    Locations_Found = new List<SceneSummary.QuantityHolder>
                    {
                        new SceneSummary.QuantityHolder
                        {
                            Name = location,
                            Quantity = 1
                        }
                    },
                    All_DropTables = dropTables ?? new List<string>()
                });
            }
        }

        public static void SaveLists()
        {
            // ========== Scene Summaries ==========
            foreach (var entry in SceneSummaries)
            {
                string[] array = entry.Key.Split(new char[] { ':' });
                string region = array[0];
                string location = array[1];

                string dir = Folders.Scenes + "/" + region + "/" + location;
                string saveName = "Summary";
                Dataminer.SerializeXML(dir, saveName, entry.Value, typeof(SceneSummary));
            }

            // ========== Tag Sources ==========
            List<string> TagTable = new List<string>();
            foreach (var entry in TagSources)
            {
                string s = "";
                foreach (var source in entry.Value)
                {
                    if (s != "") { s += ","; }
                    s += source;
                }
                TagTable.Add(entry.Key + "	" + s);
            }
            File.WriteAllLines(Folders.Lists + "/TagSources.txt", TagTable.ToArray());

            // ========== Item Sources ==========
            List<string> ItemSourcesTable = new List<string>();
            foreach (var entry in ItemLootSources)
            {
                string dir = Folders.Lists + "/ItemSources";
                Dataminer.SerializeXML(dir, entry.Key, entry.Value, typeof(ItemSource));

                ItemSourcesTable.Add(entry.Key + "	" + entry.Value.ItemName);
            }
            File.WriteAllLines(Folders.Lists + "/ItemSources.txt", ItemSourcesTable.ToArray());

            // ========== Container Sources ==========
            foreach (var entry in ContainerSummaries)
            {
                string dir = Folders.Lists + "/ContainerSummaries";
                Dataminer.SerializeXML(dir, entry.Key, entry.Value, typeof(ContainerSummary));
            }
            File.WriteAllLines(Folders.Lists + "/ContainerSummaries.txt", ContainerSummaries.Keys.ToArray());

            // ========== Enemies ==========
            List<string> EnemyTable = new List<string>();
            foreach (var entry in EnemyManifest)
            {
                foreach (var enemyHolder in entry.Value)
                {
                    EnemyTable.Add(enemyHolder.Name + " (" + enemyHolder.Unique_ID + ")");
                }
            }
            File.WriteAllLines(Folders.Lists + "/Enemies.txt", EnemyTable.ToArray());

            // ========== Merchants ===========
            List<string> MerchantTable = new List<string>();
            foreach (var entry in Merchants)
            {
                MerchantTable.Add(entry.Key);
            }
            File.WriteAllLines(Folders.Lists + "/Merchants.txt", MerchantTable.ToArray());

            // ========== Items ==========
            List<string> ItemTable = new List<string>();
            foreach (var entry in Items)
            {
                string saveDir = entry.Value.saveDir;
                if (string.IsNullOrEmpty(saveDir))
                {
                    if (entry.Value.Tags.Contains("Consummable"))
                    {
                        saveDir = "/Consumable";
                    }
                    else
                    {
                        saveDir = "/_Unsorted";
                    }
                }

                ItemTable.Add(entry.Key + "	" + entry.Value.Name + "	" + entry.Value.gameObjectName + "	" + saveDir);

            }
            File.WriteAllLines(Folders.Lists + "/Items.txt", ItemTable.ToArray());

            // ========== Effects ==========
            List<string> EffectsTable = new List<string>();
            foreach (var entry in Effects)
            {
                EffectsTable.Add(entry.Key + "	" + entry.Value.Name);
            }
            File.WriteAllLines(Folders.Lists + "/Effects.txt", EffectsTable.ToArray());

            // ========== Recipes ==========
            List<string> RecipesTable = new List<string>();
            foreach (var entry in Recipes)
            {
                string results = "";
                foreach (var result in entry.Value.Results)
                {
                    if (results != "") { results += ","; }
                    results += result.ItemName + " (" + result.Quantity + ")";
                }

                string ingredients = "";
                foreach (var ingredient in entry.Value.Ingredients)
                {
                    if (ingredients != "") { ingredients += ","; }
                    ingredients += ingredient;
                }

                RecipesTable.Add(entry.Key + "	" + entry.Value.StationType + "	" + results + "	" + ingredients);
            }
            File.WriteAllLines(Folders.Lists + "/Recipes.txt", RecipesTable.ToArray());

            // ========== DropTables ==========
            File.WriteAllLines(Folders.Lists + "/DropTables.txt", DropTables.Keys.ToArray());

            Debug.Log("[Dataminer] List building complete!");
        }
    }
}
