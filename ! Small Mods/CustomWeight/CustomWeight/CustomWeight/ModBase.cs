using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Partiality.Modloader;
using UnityEngine;
using SharedModConfig;

namespace CustomWeight
{
    public class ModBase : PartialityMod
    {
        public ModBase()
        {
            this.author = "Sinai";
            this.ModID = "CustomWeight";
            this.Version = "1.00";
        }

        public override void OnEnable()
        {
            base.OnEnable();

            var obj = new GameObject("CustomWeight");
            GameObject.DontDestroyOnLoad(obj);
            obj.AddComponent<WeightManager>();
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }
    }

    public class WeightManager : MonoBehaviour
    {
        public static WeightManager Instance;
        public ModConfig config;

        public bool PatchedRPM = false;
        public int PatchedCharacters = 0;
        public Dictionary<int, float> OrigCapacities = new Dictionary<int, float>(); // dictionary containing original weight limits on bags (ID : Weight)

        internal void Start()
        {
            Instance = this;
            // set up and load settings
            config = SetupConfig();
            StartCoroutine(SetupCoroutine());

            // hooks
            On.PlayerCharacterStats.UpdateWeight += CharacterWeightHook;
        }

        private IEnumerator SetupCoroutine()
        {
            while (ConfigManager.Instance == null || !ConfigManager.Instance.IsInitDone())
            {
                Debug.Log("waiting for isinitdone");
                yield return new WaitForSeconds(0.1f);
            }

            config.Register();

            Debug.Log("Registered betterCustomWeight");
        }

        internal void Update()
        {
            if (!PatchedRPM && ResourcesPrefabManager.Instance.Loaded && ConfigManager.Instance.IsInitDone())
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
        }

        private void PatchPlayer(Character c)
        {
            float newValue = 10.0f + (float)config.GetValue(Settings.PouchBonus);

            if ((bool)config.GetValue(Settings.NoContainerLimit)) { newValue = -1; }

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

                        //string s = ", GetValue: " + At.GetValue(typeof(ItemContainer), container, "m_baseContainerCapacity").ToString();
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
            cap *= (float)config.GetValue(Settings.BagBonusMulti);
            cap += (float)config.GetValue(Settings.BagBonusFlat);

            if ((bool)config.GetValue(Settings.NoContainerLimit)) { cap = -1; }
            return cap;
        }

        private void CharacterWeightHook(On.PlayerCharacterStats.orig_UpdateWeight orig, PlayerCharacterStats self)
        {
            if ((bool)config.GetValue(Settings.DisableAllBurdens))
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

        private ModConfig SetupConfig()
        {
            var newConfig = new ModConfig
            {
                ModName = "Better Custom Weight",
                SettingsVersion = 1.0,
                Settings = new List<BBSetting>
                {
                    new BoolSetting
                    {
                        Name = Settings.NoContainerLimit,
                        Description = "Disable limits on all containers",
                        DefaultValue = false,
                    },
                    new BoolSetting
                    {
                        Name = Settings.DisableAllBurdens,
                        Description = "Disable all burdens from weight",
                        DefaultValue = false,
                    },
                    new FloatSetting
                    {
                        Name = Settings.PouchBonus,
                        Description = "Extra Pouch capacity",
                        MinValue = 0f,
                        MaxValue = 1000f,
                        DefaultValue = 0f,
                        RoundTo = 0,
                        ShowPercent = false,
                    },
                    new FloatSetting
                    {
                        Name = Settings.BagBonusFlat,
                        Description = "Extra Bag capacity (flat bonus)",
                        MinValue = 0f,
                        MaxValue = 1000f,
                        DefaultValue = 0f,
                        RoundTo = 0,
                        ShowPercent = false,
                    },
                    new FloatSetting
                    {
                        Name = Settings.BagBonusMulti,
                        Description = "Extra Bag capacity (multiplier)",
                        MinValue = 0f,
                        MaxValue = 10f,
                        DefaultValue = 1.0f,
                        RoundTo = 2,
                        ShowPercent = false,
                    },
                }
            };

            return newConfig;
        }
    }

    public static class Settings
    {
        public static string NoContainerLimit = "NoContainerLimit";
        public static string DisableAllBurdens = "DisableAllBurdens";
        public static string PouchBonus = "PouchBonus";
        public static string BagBonusFlat = "BagBonusFlat";
        public static string BagBonusMulti = "BagBonusMulti";
    }
}