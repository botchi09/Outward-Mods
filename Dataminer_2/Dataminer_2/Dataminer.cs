using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Xml.Serialization;
using System.IO;
using System.Reflection;

namespace Dataminer_2
{
    public class Dataminer : MonoBehaviour
    {
        public static Dataminer Instance;

        // Key: "Item.Name (item.gameObject.name)", Value: "Path to .xml file"
        public Dictionary<string, string> ItemManifest = new Dictionary<string, string>(); 

        // ------------------ Functions ------------------ //

        internal void Awake()
        {
            Instance = this;
        }

        internal void Start()
        {
            bool newDump = Folders.MakeFolders();

            if (newDump)
            {
                Debug.Log("[Dataminer] No " + Folders.SaveFolder + " folder detected. Dumping prefabs...");
                StartCoroutine(PrefabCoroutine());
            }
        }

        private IEnumerator PrefabCoroutine()
        {
            while (!ResourcesPrefabManager.Instance.Loaded)
            {
                yield return new WaitForSeconds(1f);
            }

            Debug.Log("-------- Beginning Prefab Datamine --------");

            if (At.GetValue(typeof(ResourcesPrefabManager), null, "ITEM_PREFABS") is Dictionary<string, Item> ItemPrefabs)
            {
                foreach (Item item in ItemPrefabs.Values)
                {
                    Debug.Log("Parsing " + item.Name + ", typeof: " + item.GetType());

                    // Parse the item. This will recursively dive.
                    var itemHolder = ItemHolder.ParseItem(item);

                    // Folder and Save Name
                    string dir = GetItemFolder(item, itemHolder);
                    string saveName = ReplaceInvalidChars(item.Name + " (" + item.gameObject.name + ")");

                    // Serialize and add to manifest
                    ItemManifest.Add(saveName, dir);
                    SerializeXML(dir + "/" + saveName + ".xml", itemHolder, typeof(ItemHolder), TypesToSerialize.Types.ToArray());
                }

                List<string> ManifestToTable = new List<string>();
                foreach (KeyValuePair<string,string> entry in ItemManifest)
                {
                    ManifestToTable.Add(entry.Key + "  " + entry.Value); // space is a tab, so can copy+paste into a spreadsheet 
                }
                Debug.Log("Parsed items: " + ItemManifest.Count + ". Saving Manifest table of count: " + ManifestToTable.Count);
                File.WriteAllLines(Folders.SaveFolder + "/ItemManifest.txt", ManifestToTable.ToArray());
            }
            else
            {
                Debug.LogError("Could not find Item Prefabs!");
            }

            Debug.Log("-------- Finished Prefab Datamine --------");
        }

        private string GetItemFolder(Item item, ItemHolder itemHolder)
        {
            string dir = Folders.Prefabs + "/Items";

            if (item.GetType().ToString() != "Item")
            {
                dir += GetRelativeTypeDirectory(item, "", item.GetType());
            }
            else
            {
                if (itemHolder.Tags.Contains("Consummable"))
                {
                    dir += "/Consumable";
                }
                else
                {
                    dir += "/_Unsorted";
                }
            }
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir); // will automatically create necessary parents
            }

            return dir;
        }

        public static void SerializeXML(string path, object obj, Type type, Type[] extraTypes)
        {
            if (File.Exists(path))
            {
                Debug.LogWarning("[Dataminer] SerializeXML: A file already exists at " + path + ", skipping...");
                //File.Delete(path);
            }
            else
            {
                XmlSerializer xml = new XmlSerializer(type, extraTypes);
                FileStream file = File.Create(path);
                xml.Serialize(file, obj);
                file.Close();
            }
        }

        public static string GetRelativeTypeDirectory(Item item, string dir, Type type)
        {
            dir = "/" + type + dir;
            var baseType = type.BaseType;
            if (baseType != typeof(Item))
            {
                dir = GetRelativeTypeDirectory(item, dir, baseType);
            }
            return dir;
        }

        public static string ReplaceInvalidChars(string filename)
        {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        }
    }
    
    public class TypesToSerialize
    {
        public static List<Type> Types { get; } = new List<Type>
        {
            typeof(AddStatusEffectBuildupHolder),
            typeof(AddStatusEffectHolder),
            typeof(AffectBurntHealthHolder),
            typeof(AffectBurntManaHolder),
            typeof(AffectBurntStaminaHolder),
            typeof(AffectHealthHolder),
            typeof(AffectHealthParentOwnerHolder),
            typeof(AffectManaHolder),
            typeof(AffectNeedHolder),
            typeof(AffectStabilityHolder),
            typeof(AffectStaminaHolder),
            typeof(AffectStatHolder),
            typeof(BagHolder),
            typeof(Damages),
            typeof(EffectConditionHolder),
            typeof(EffectConditionHolder.ConditionHolder),
            typeof(EffectHolder),
            typeof(EffectTransformHolder),
            typeof(EnemyHolder),
            typeof(EquipmentHolder),
            typeof(EquipmentStatsHolder),
            typeof(ImbueWeaponHolder),
            typeof(ItemHolder),
            typeof(ItemStatsHolder),
            typeof(PunctualDamageHolder),
            typeof(ReduceDurabilityHolder),
            typeof(RemoveStatusEffectHolder),
            typeof(ShootBlastHolder),
            typeof(ShootProjectileHolder),
            typeof(TrapEffectHOlder),
            typeof(TrapHolder),
            typeof(WeaponHolder),
            typeof(WeaponStatsHolder),
            typeof(WeaponStats.AttackData),
            typeof(WeaponDamageHolder),
        };
    }

    public class Folders
    {
        public static bool MakeFolders()
        {
            bool madeFolder = false;
            foreach (FieldInfo fi in typeof(Folders).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
            {
                string path = (string)fi.GetValue(null);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    madeFolder = true;
                }
            }
            return madeFolder;
        }

        public static readonly string SaveFolder = @"Dumps_2";        
        public static readonly string Prefabs = SaveFolder + "/Prefabs"; // ResourcesPrefabManager Prefabs (Items, Effects, etc)
    }
}
