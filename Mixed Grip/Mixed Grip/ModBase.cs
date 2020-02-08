using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
//using SinAPI;
using SharedModConfig;
using Partiality.Modloader;
using static CustomKeybindings;

namespace MixedGrip
{
    public class ModBase : PartialityMod
    {
        public double version = 2.6;

        public ModBase()
        {
            this.ModID = "MixedGrip";
            this.Version = version.ToString("0.00");
            this.author = "Sinai";
        }

        public override void OnEnable()
        {
            base.OnEnable();

            var _obj = new GameObject("MixedGrip");
            GameObject.DontDestroyOnLoad(_obj);
            _obj.AddComponent<MixedGrip>();
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }
    }    

    public class MixedGrip : MonoBehaviour
    {
        public static MixedGrip Instance;

        public static ModConfig config;

        public string ToggleKey = "Toggle Weapon Grip";

        internal void Awake()
        {
            Instance = this;

            config = SetupConfig();

            StartCoroutine(SetupCoroutine());

            this.gameObject.AddComponent<GripManager>();

            // custom keybindings
            AddAction(ToggleKey, KeybindingsCategory.Actions, ControlType.Both, 5);
        }

        private IEnumerator SetupCoroutine()
        {
            while (ConfigManager.Instance == null || !ConfigManager.Instance.IsInitDone())
            {
                yield return new WaitForSeconds(0.1f);
            }

            config.Register();
        }

        private ModConfig SetupConfig()
        {
            var newConfig = new ModConfig
            {
                ModName = "Mixed Grip",
                SettingsVersion = 1.0,
                Settings = new List<BBSetting>
                {
                    new BoolSetting
                    {
                        Name = Settings.Swap_Animations,
                        Description = "Swap animations when swapping grip (for Swords, Axes and Maces)",
                        DefaultValue = true
                    },
                    new BoolSetting
                    {
                        Name = Settings.Swap_On_Equip_And_Unequip,
                        Description = "Automatically adjust grip when equipping or un-equipping gear",
                        DefaultValue = true
                    },
                    new BoolSetting
                    {
                        Name = Settings.Balance_Weapons,
                        Description = "Balance weapons when swapping grip (nerf if becoming 1H, buff if becoming 2H)",
                        DefaultValue = true
                    },
                }
            };

            return newConfig;
        }
    }

    public class Settings
    {
        public static string Swap_Animations = "Swap_Animations";
        public static string Swap_On_Equip_And_Unequip = "Swap_On_Equip_And_Unequip";
        public static string Balance_Weapons = "Balance_Weapons";
    }
}
