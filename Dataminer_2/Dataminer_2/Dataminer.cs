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

        internal void Awake()
        {
            Instance = this;
        }

        internal void Start()
        {
            bool newDump = Folders.MakeFolders();

            if (newDump)
            {
                StartCoroutine(PrefabCoroutine());
            }
        }

        private IEnumerator PrefabCoroutine()
        {
            while (!ResourcesPrefabManager.Instance.Loaded)
            {
                yield return new WaitForSeconds(1f);
            }

            // setup tags
            for (int i = 1; i < 500; i++)
            {
                if (TagSourceManager.Instance.GetTag(i.ToString()) is Tag tag)
                {
                    if (tag == Tag.None)
                    {
                        break;
                    }

                    ListManager.TagSources.Add(tag.TagName, new List<string>());
                }
                else
                {
                    break;
                }
            }

            ItemHolder.ParseAllItems();

            StatusEffectHolder.ParseAllEffects();

            RecipeHolder.ParseAllRecipes();
        }

        public static void SerializeXML(string dir, string saveName, object obj, Type type, Type[] extraTypes = null)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            saveName = ReplaceInvalidChars(saveName);

            string path = dir + "/" + saveName + ".xml";
            if (File.Exists(path))
            {
                Debug.LogWarning("[Dataminer] SerializeXML: A file already exists at " + path + ", skipping...");
                //File.Delete(path);
            }
            else
            {
                var typesToSerialize = extraTypes ?? TypesToSerialize.Types;

                XmlSerializer xml = new XmlSerializer(type, typesToSerialize);
                FileStream file = File.Create(path);
                xml.Serialize(file, obj);
                file.Close();
            }
        }

        public static string ReplaceInvalidChars(string filename)
        {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        }
    }
    
    public class TypesToSerialize
    {
        public static Type[] Types { get; } = new Type[]
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
            typeof(ContainerSummary),
            typeof(Damages),
            typeof(DropTableChanceEntry),
            typeof(DropTableEntry),
            typeof(DroptableHolder),
            typeof(DroptableHolder.DropGeneratorHolder),
            typeof(EffectConditionHolder),
            typeof(EffectConditionHolder.ConditionHolder),
            typeof(EffectHolder),
            typeof(EffectTransformHolder),
            typeof(EnemyHolder),
            typeof(EquipmentHolder),
            typeof(EquipmentStatsHolder),
            typeof(ImbueWeaponHolder),
            typeof(ItemHolder),
            typeof(ItemSource),
            typeof(GatherableHolder),
            typeof(ItemStatsHolder),
            typeof(ItemSpawnHolder),
            typeof(LootContainerHolder),
            typeof(MerchantHolder),
            typeof(QuestHolder),
            typeof(PunctualDamageHolder),
            typeof(RecipeHolder),
            typeof(RecipeHolder.ItemQuantityHolder),
            typeof(ReduceDurabilityHolder),
            typeof(RemoveStatusEffectHolder),
            typeof(SceneSummary),
            typeof(SceneSummary.QuantityHolder),
            typeof(SkillHolder),
            typeof(SkillHolder.SkillItemReq),
            typeof(ShootBlastHolder),
            typeof(ShootProjectileHolder),
            typeof(StatusEffectHolder),
            typeof(TrapEffectHolder),
            typeof(TrapHolder),
            typeof(Vector3),
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
        public static readonly string Prefabs = SaveFolder + "/Prefabs";
        public static readonly string Lists = SaveFolder + "/Lists";
        public static readonly string Scenes = SaveFolder + "/Scenes";
        public static readonly string Enemies = SaveFolder + "/Enemies";
        public static readonly string Merchants = SaveFolder + "/Merchants";
    }
}
