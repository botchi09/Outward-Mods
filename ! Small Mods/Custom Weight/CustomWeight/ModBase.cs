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

        public float m_timeOfLastUpdate;

        // original capacities on bags (ID : Capacity)
        public Dictionary<int, float> OrigCapacities = new Dictionary<int, float>();

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
                yield return new WaitForSeconds(0.1f);
            }

            config.Register();
        }

        internal void Update()
        {
            if (Time.time - m_timeOfLastUpdate > 2f)
            {
                m_timeOfLastUpdate = Time.time;

                if (Global.Lobby.PlayersInLobbyCount > 0 && !NetworkLevelLoader.Instance.IsGameplayPaused)
                {
                    foreach (PlayerSystem player in Global.Lobby.PlayersInLobby)
                    {
                        if (player.ControlledCharacter)
                        {
                            UpdatePlayer(player.ControlledCharacter);
                        }
                    }
                }
            }
        }

        private void UpdatePlayer(Character player)
        {
            float newValue = (bool)config.GetValue(Settings.NoContainerLimit) ? -1 : 10.0f + (float)config.GetValue(Settings.PouchBonus);

            if ((float)At.GetValue(typeof(ItemContainer), player.Inventory.Pouch, "m_baseContainerCapacity") != newValue)
            {
                At.SetValue(newValue, typeof(ItemContainer), player.Inventory.Pouch, "m_baseContainerCapacity");
            }

            if (player.Inventory.EquippedBag)
            {
                UpdateBag(player.Inventory.EquippedBag);
            }
        }

        private void UpdateBag(Bag bag)
        {
            float cap;

            if (At.GetValue(typeof(Bag), bag, "m_container") is ItemContainerStatic container)
            {
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

                At.SetValue(cap, typeof(ItemContainer), container, "m_baseContainerCapacity");
            }
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