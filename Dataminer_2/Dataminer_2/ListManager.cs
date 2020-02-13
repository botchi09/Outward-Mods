using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using System.Xml.Serialization;

namespace Dataminer_2
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
        public static Dictionary<string, ItemHolder> Items = new Dictionary<string, ItemHolder>();
        public static Dictionary<string, StatusEffectHolder> Effects = new Dictionary<string, StatusEffectHolder>();
        public static Dictionary<string, RecipeHolder> Recipes = new Dictionary<string, RecipeHolder>();
        public static Dictionary<string, DroptableHolder> DropTables = new Dictionary<string, DroptableHolder>();

        internal void Awake()
        {
            Instance = this;
        }

        internal void Update()
        {
            if (Input.GetKeyDown(KeyCode.ScrollLock))
            {
                Debug.Log("Force saving lists");
                SaveLists();
            }
        }

        public static string GetSceneSummaryKey(Vector3 position)
        {
            return SceneManager.Instance.GetCurrentRegion() + ":" + SceneManager.Instance.GetCurrentLocation(position);
        }

        public static void AddTagSource(string tag, string source)
        {
            if (TagSources.ContainsKey(tag))
            {
                if (!TagSources[tag].Contains(source))
                {
                    TagSources[tag].Add(source);
                }
            }
            else
            {
                TagSources.Add(tag, new List<string> { source });
            }
        }

        public static void AddContainerSummary(string name, string location, List<string> dropTables)
        {
            if (ContainerSummaries.ContainsKey(name))
            {
                ContainerSummaries[name].Locations_Found.Add(location);
                ContainerSummaries[name].Locations_Found.Sort();

                foreach (string table in dropTables)
                {
                    if (!ContainerSummaries[name].All_DropTables.Contains(table))
                    {
                        ContainerSummaries[name].All_DropTables.Add(table);
                    }
                }
            }
            else
            {
                ContainerSummaries.Add(name, new ContainerSummary
                {
                    Name = name,
                    Locations_Found = new List<string>
                    {
                        location
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

            // ========== Items ==========
            List<string> ItemTable = new List<string>();
            foreach (var entry in Items)
            {
                // todo save spreadsheet list.
                // "ItemID | Item.Name | item.gameObject.name | dir"
                ItemTable.Add(entry.Key + "	" + entry.Value.Name + "	" + entry.Value.gameObjectName + "	" + entry.Value.saveDir);

            }
            File.WriteAllLines(Folders.Lists + "/Items.txt", ItemTable.ToArray());

            // ========== Effects ==========
            List<string> EffectsTable = new List<string>();
            foreach (var entry in Effects)
            {
                EffectsTable.Add(entry.Value + "	" + entry.Value.Name);
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

                RecipesTable.Add(entry.Key + "	" + entry.Value.Name + "	" + results + " " + ingredients);
            }
            File.WriteAllLines(Folders.Lists + "/Recipes.txt", RecipesTable.ToArray());

            // ========== DropTables ==========
            File.WriteAllLines(Folders.Lists + "/DropTables.txt", DropTables.Keys.ToArray());

            Debug.Log("[Dataminer] List building complete!");
        }

        //List<string> ManifestToTable = new List<string>();
        //foreach (KeyValuePair<string, string> entry in ItemManifest.Manifest)
        //{
        //    ManifestToTable.Add(entry.Key + "  " + entry.Value); // space is a tab, so can copy+paste into a spreadsheet 
        //}
        //Debug.Log("Parsed items: " + ItemManifest.Manifest.Count + ". Saving Manifest table of count: " + ManifestToTable.Count);
        //File.WriteAllLines(Folders.SaveFolder + "/ItemManifest.txt", ManifestToTable.ToArray());

        //List<string> ManifestToTable = new List<string>();
        //foreach (KeyValuePair<string, string> entry in RecipeManifest.Manifest)
        //{
        //    ManifestToTable.Add(entry.Key + "  " + entry.Value); // space is a tab, so can copy+paste into a spreadsheet 
        //}
        //Debug.Log("Parsed recipes: " + RecipeManifest.Manifest.Count + ". Saving Manifest table of count: " + ManifestToTable.Count);
        //File.WriteAllLines(Folders.SaveFolder + "/RecipeManifest.txt", ManifestToTable.ToArray());
    }
}
