using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using SideLoader;
using SinAPI;
using Partiality.Modloader;

namespace BlacksmithsToolbox
{
    public class ModBase : PartialityMod
    {
        public static ToolboxGlobal Instance;

        public GameObject obj;
        public string ID = "ArmorToolbox";
        public double version = 1.2;

        public ModBase()
        {
            this.author = "Sinai";
            this.ModID = ID;
            this.Version = version.ToString("0.00");
        }

        public override void OnEnable()
        {
            base.OnEnable();

            obj = new GameObject(ID);
            GameObject.DontDestroyOnLoad(obj);

            Instance = obj.AddComponent<ToolboxGlobal>();
            Instance._base = this;
            Instance.Init();
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }
    }

    public class Settings
    {
        public int Iron_Scrap_Cost = 5;
        public int Toolbox_Cost = 300;
        public float Durability_Cost_Per_Use = 5.0f;
    }

    public class ToolboxGlobal : MonoBehaviour
    {
        public ModBase _base;

        private bool SetupDone;
        public static Settings settings = new Settings();
        private static readonly string savePath = @"Mods\BlacksmithsToolbox.json";

        public static readonly int Toolbox_ID = 5850750;
        public Item ToolboxPrefab;

        private string CurrentSceneName = "";
        private bool SceneChangeFlag = false;

        public void Init()
        {
            LoadSettings();

            //On.Item.Use += OnItemUse;
        }

        private void LoadSettings()
        {
            if (File.Exists(savePath))
            {
                JsonUtility.FromJsonOverwrite(File.ReadAllText(savePath), settings);
            }
            else
            {
                File.WriteAllText(savePath, JsonUtility.ToJson(settings, true));
            }
        }

        internal void Update()
        {
            // patch once
            if (!SetupDone && SL.Instance.InitDone > 0)
            {
                SetupToolboxItem();
                SetupDone = true;
            }

            // check for scene change flag
            if (CurrentSceneName != SceneManagerHelper.ActiveSceneName)
            {
                CurrentSceneName = SceneManagerHelper.ActiveSceneName;
                SceneChangeFlag = true;
            }

            // patch on scene change, after gameplay resumes
            if (SceneChangeFlag && !NetworkLevelLoader.Instance.IsGameplayPaused && Global.Lobby.PlayersInLobbyCount > 0)
            {
                SetupBlacksmith();

                SceneChangeFlag = false;
            }
        }

        // Set up the Toolbox item prefab

        private void SetupToolboxItem()
        {
            CustomItem ArmorToolboxTemplate = new CustomItem
            {
                CloneTarget_ItemID = 6600227, // vendavel hospitality ingot
                New_ItemID = Toolbox_ID,
                Name = "Blacksmith's Toolbox",
                Description = string.Format("Requires: {0} Iron Scrap\n\nA box containing a whetstone and other useful tools for mending one's equipment.", settings.Iron_Scrap_Cost),
                BaseValue = settings.Toolbox_Cost,
                Durability = 100,
                Weight = 5.0f,
                ItemIconName = "ToolboxIcon",
            };

            CustomItems.Instance.ApplyCustomItem(ArmorToolboxTemplate);

            if (ResourcesPrefabManager.Instance.GetItemPrefab(Toolbox_ID) is Item item)
            {
                ToolboxPrefab = item;

                item.IsUsable = true; // need to make it Usable, as thats how this item works
                item.QtyRemovedOnUse = -1; // dont consume itself on use
                item.GroupItemInDisplay = false; // dont group toolboxes in inventory
                item.BehaviorOnNoDurability = Item.BehaviorOnNoDurabilityType.Destroy;

                // just some stuff to remove the Tags from the clone item I used.
                TagSource tagSource = item.GetComponent<TagSource>();
                At.SetValue(false, typeof(TagSource), tagSource, "m_isIngredient");
                List<Tag> emptyTag = new List<Tag>();
                At.SetValue(emptyTag, typeof(TagListSelectorComponent), tagSource as TagListSelectorComponent, "m_tags");
                List<TagSourceSelector> emptyTag2 = new List<TagSourceSelector>();
                At.SetValue(emptyTag2, typeof(TagListSelectorComponent), tagSource as TagListSelectorComponent, "m_tagSelectors");

                // add our custom effect
                var effects = new GameObject("Effects");
                effects.transform.parent = item.transform;
                effects.AddComponent<ToolboxEffect>();
            }
        }

        // Function for setting up Blacksmith NPCs whenever a scene is loaded. Just adds our item to their pouch, if they dont already have it.

        private void SetupBlacksmith()
        {
            List<GameObject> list = Resources.FindObjectsOfTypeAll<GameObject>().Where(x => x.name == "HumanSNPC_Blacksmith").ToList();

            foreach (GameObject obj in list)
            {
                if (obj.GetComponentInChildren<MerchantPouch>(true) is MerchantPouch pouch
                    && !pouch.ContainsOfSameID(Toolbox_ID))
                {
                    Item item = ItemManager.Instance.GenerateItemNetwork(Toolbox_ID);
                    item.transform.parent = pouch.transform;
                }
            }
        }
    }
}
