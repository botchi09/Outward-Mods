using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Partiality.Modloader;
using UnityEngine;
using SinAPI;
using System.IO;
using static CustomKeybindings;

namespace BetterCustomWeight
{
    // PARTIALITY LOADER
    public class BetterCustomWeight : PartialityMod
    {
        public GameObject obj;
        public string ID = "BetterCustomWeight";
        public double version = 1.0;

        public static BetterWeightScript Instance;

        public BetterCustomWeight()
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

            Instance = obj.AddComponent<BetterWeightScript>();
            Instance._base = this;
            Instance.Init();
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }
    }

    // ACTUAL MOD
    public class BetterWeightScript : MonoBehaviour
    {
        public BetterCustomWeight _base;

        public BetterWeightGUI gui;
        public Settings settings;

        // keep track of what we've done, and what we need to do
        public bool PatchedRPM = false;
        public int PatchedCharacters = 0;
        public Dictionary<int, float> OrigCapacities = new Dictionary<int, float>(); // dictionary containing original weight limits on bags (ID : Weight)

        // custom keybinding
        public string MenuKey = "Custom Weight Menu";

        public void Init()
        {
            // set up and load settings
            LoadSettings();

            // add GUI component
            gui = _base.obj.AddComponent(new BetterWeightGUI() { script = this });

            if (!settings.ShowMenuOnStartup) { gui.ShowMenu = false; }

            // hooks
            On.PlayerCharacterStats.UpdateWeight += CharacterWeightHook;

            // custom keybinding
            AddAction(MenuKey, KeybindingsCategory.Menus, ControlType.Both, 5, InputActionType.Button);
        }        

        internal void Update()
        {
            if (!PatchedRPM && ResourcesPrefabManager.Instance.Loaded)
            {
                PatchRPM();

                // check if gameplay is running, patch active bags
                if (Global.Lobby.PlayersInLobbyCount > 0)
                {
                    PatchActiveBags();
                }

                PatchedRPM = true;
            }

            // patch active characters
            if (Global.Lobby.PlayersInLobbyCount != PatchedCharacters)
            {
                foreach (PlayerSystem ps in Global.Lobby.PlayersInLobby.Where(x => x.ControlledCharacter.IsLocalPlayer))
                {
                    PatchPlayer(ps.ControlledCharacter);
                }
                PatchedCharacters = Global.Lobby.PlayersInLobbyCount;
            }

            // check for menu input
            foreach (PlayerSystem ps in Global.Lobby.PlayersInLobby.Where(x => x.ControlledCharacter.IsLocalPlayer))
            {
                int id = ps.PlayerID;
                if (m_playerInputManager[id].GetButtonDown(MenuKey))
                {
                    gui.ShowMenu = !gui.ShowMenu;
                }
            }
        }

        private void PatchPlayer(Character c)
        {
            float newValue = 10.0f + settings.PouchBonus;

            if (settings.NoContainerLimit) { newValue = -1; }

            At.SetValue(newValue, typeof(ItemContainer), c.Inventory.Pouch, "m_baseContainerCapacity");
        }

        private void PatchRPM()
        {
            foreach (UnityEngine.Object obj in ResourcesPrefabManager.AllPrefabs)
            {
                if (!(obj is GameObject go) || !(go.GetComponent<Bag>() is Bag bag)) { continue; }

                foreach (Transform child in bag.transform)
                {
                    if (child.GetComponent<ItemContainer>() is ItemContainer container)
                    {
                        // get current (or original) limit
                        float cap = GetCap(bag, container);

                        //typeof(ItemContainer).GetField("m_baseContainerCapacity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(container, cap);

                        At.SetValue(cap, typeof(ItemContainer), container, "m_baseContainerCapacity");

                        string s = ", GetValue: " + At.GetValue(typeof(ItemContainer), container, "m_baseContainerCapacity").ToString();
                        break;
                    }
                }
            }
        }

        private void PatchActiveBags()
        {
            foreach (Bag bag in Resources.FindObjectsOfTypeAll<Bag>())
            {
                if (At.GetValue(typeof(Bag), bag, "m_container") is ItemContainerStatic container)
                {
                    // get current (or original) limit
                    float cap = GetCap(bag, container as ItemContainer);

                    At.SetValue(cap, typeof(ItemContainer), container, "m_baseContainerCapacity");
                }
            }
        }

        private float GetCap(Bag bag, ItemContainer container)
        {
            float cap;

            if (OrigCapacities.ContainsKey(bag.ItemID))
            {
                cap = OrigCapacities[bag.ItemID];
            }
            else
            {
                cap = (float)At.GetValue(typeof(ItemContainer), container, "m_baseContainerCapacity");
                OrigCapacities.Add(bag.ItemID, cap);
            }

            // set new limit based on settings
            cap *= settings.BagBonusMulti;
            cap += settings.BagBonusFlat;

            if (settings.NoContainerLimit) { cap = -1; }
            return cap;
        }

        private void CharacterWeightHook(On.PlayerCharacterStats.orig_UpdateWeight orig, PlayerCharacterStats self)
        {
            if (settings.DisableAllBurdens)
            {
                CharacterStats cStats = self as CharacterStats;

                bool flag = (bool)At.GetValue(typeof(PlayerCharacterStats), self, "m_generalBurdenPenaltyActive");

                if (flag) // one-time disable all burdens
                {
                    At.SetValue(false, typeof(PlayerCharacterStats), self, "m_generalBurdenPenaltyActive");
                    At.SetValue(0, typeof(PlayerCharacterStats), self, "m_generalBurdenRatio");

                    if (At.GetValue(typeof(CharacterStats), cStats, "m_movementSpeed") is Stat m_movementSpeed
                        && At.GetValue(typeof(CharacterStats), cStats, "m_staminaRegen") is Stat m_staminaRegen
                        && At.GetValue(typeof(CharacterStats), cStats, "m_staminaUseModifiers") is Stat m_staminaUseModifiers)
                    {
                        m_movementSpeed.RemoveMultiplierStack("Burden");
                        m_movementSpeed.RemoveMultiplierStack("PouchBurden");
                        m_movementSpeed.RemoveMultiplierStack("BagBurden");

                        m_staminaRegen.RemoveMultiplierStack("Burden");

                        m_staminaUseModifiers.RemoveMultiplierStack("Burden_Dodge");
                        m_staminaUseModifiers.RemoveMultiplierStack("Burden_Sprint");
                    }
                }
            }
            else // otherwise normal update
            {
                orig(self);
            }
        }

        private Settings NewSettings()
        {
            Settings newSets = new Settings()
            {
                ShowMenuOnStartup = true,
                DisableAllBurdens = false,
                NoContainerLimit = false,
                PouchBonus = 10,
                BagBonusFlat = 20,
                BagBonusMulti = 1.0f,
            };

            return newSets;
        }

        private void LoadSettings()
        {
            settings = NewSettings();

            string path = @"Mods\BetterCustomWeight.json";
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                if (JsonUtility.FromJson<Settings>(json) is Settings newSettings)
                {
                    settings = newSettings;
                }
            }
        }

        private void SaveSettings()
        {
            string dir = "Mods";
            string path = dir + @"\BetterCustomWeight.json";

            if (!Directory.Exists(dir)) { Directory.CreateDirectory(dir); }

            Jt.SaveJsonOverwrite(path, settings);
        }

        internal void OnDisable()
        {
            SaveSettings();
        }
    }

    public class Settings
    {
        public bool ShowMenuOnStartup;

        public bool NoContainerLimit;
        public bool DisableAllBurdens;

        public int PouchBonus;
        public int BagBonusFlat;
        public float BagBonusMulti;
    }
}
