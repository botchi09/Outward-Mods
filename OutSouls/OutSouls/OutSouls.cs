using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Partiality.Modloader;
using SharedModConfig;

namespace OutSoulsMod
{
    public class ModBase : PartialityMod
    {
        public ModBase()
        {
            this.ModID = "OutSouls";
            this.Version = OutSouls.version.ToString("0.00");
            this.author = "Sinai";
        }

        public override void OnEnable()
        {
            base.OnEnable();

            var _obj = new GameObject(this.ModID);
            GameObject.DontDestroyOnLoad(_obj);

            _obj.AddComponent<OutSouls>();
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }
    }    

    public class OutSouls : MonoBehaviour
    {
        public static OutSouls Instance;
        public static double version = 2.0;

        public static ModConfig config;

        internal void Awake()
        {
            Instance = this;

            this.gameObject.AddComponent<BonfireManager>();
            this.gameObject.AddComponent<BonfireGUI>();
            this.gameObject.AddComponent<RPCManager>();
        }

        internal void Start()
        {
            config = SetupConfig();
            config.Register();
        }

        private ModConfig SetupConfig()
        {
            var newConfig = new ModConfig
            {
                ModName = "OutSouls",
                SettingsVersion = 1.0,
                Settings = new List<BBSetting>
                {
                    new BoolSetting
                    {
                        Name = Settings.Enable_Bonfire_System,
                        DefaultValue = true,
                        Description = "Enable Bonfires (requires scene reload)",
                    },
                    new BoolSetting
                    {
                        Name = Settings.Bonfires_Heal_Enemies,
                        DefaultValue = false,
                        Description = "Bonfires heal and resurrect enemies",
                    },
                    new BoolSetting
                    {
                        Name = Settings.Disable_Bonfire_Costs,
                        DefaultValue = false,
                        Description = "Disable all Bonfire costs",
                    },
                    new BoolSetting
                    {
                        Name = Settings.Cant_Use_Bonfires_In_Combat,
                        DefaultValue = true,
                        Description = "Can't use Bonfires in combat",
                    },
                }
            };

            return newConfig;
        }

        public bool CanInteract(Character c)
        {
            return (bool)At.Call(c, "CanInteract", null);
        }
        
    }

    public class Settings
    {
        public static string Enable_Bonfire_System = "Enable_Bonfire_System";
        public static string Bonfires_Heal_Enemies = "Bonfires_Heal_Enemies";
        public static string Disable_Bonfire_Costs = "Disable_Bonfire_Costs";
        public static string Cant_Use_Bonfires_In_Combat = "Cant_Use_Bonfires_In_Combat";
        //public static string Disable_Scaling = "Disable_Scaling";
    }
}
