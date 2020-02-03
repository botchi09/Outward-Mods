using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
//using SinAPI;
using static CustomKeybindings;
using Partiality.Modloader;

namespace MixedGrip
{
    public class ModBase : PartialityMod
    {
        public GameObject _obj = null;
        public MixedGripGlobal script;
        public double version = 2.5;

        public ModBase()
        {
            this.ModID = "MixedGrip";
            this.Version = version.ToString("0.00");
            this.author = "Sinai";
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (_obj == null)
            {
                _obj = new GameObject("MixedGrip");
                GameObject.DontDestroyOnLoad(_obj);
            }

            script = _obj.AddComponent<MixedGripGlobal>();
            script._base = this;
            script.Init();
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }
    }    

    public class MixedGripGlobal : MonoBehaviour
    {
        public ModBase _base;
        public ModGUI gui;
        public Settings settings;

        public GripManager gripManager;

        public string MenuKey = "Mixed Grip Menu";
        public string ToggleKey = "Toggle Weapon Grip";

        public void Init()
        {
            // OLogger.CreateLog(new Rect(Screen.width - 465, Screen.height - 175, 465, 155), "Default", true, true);

            settings = new Settings();
            LoadSettings();

            // setup components
            gui = _base._obj.AddComponent(new ModGUI() { global = this });
            gripManager = _base._obj.AddComponent(new GripManager() { global = this });
            gripManager.Init();

            // custom keybindings
            AddAction(MenuKey, KeybindingsCategory.Menus, ControlType.Both, 5);
            AddAction(ToggleKey, KeybindingsCategory.Actions, ControlType.Both, 5);

            // Debug.Log("Initialised mixed grip");
        }

        private void LoadSettings()
        {
            settings = new Settings();
            try
            {
                Settings loadsettings = new Settings();
                loadsettings = JsonUtility.FromJson<Settings>(File.ReadAllText(@"Mods\MixedGrip.json"));
                if (loadsettings != null)
                {
                    settings = loadsettings;
                }
            }
            catch
            {
                settings = new Settings();
            }
        }

        internal void OnDisable()
        {
            SaveSettings();
        }

        private void SaveSettings()
        {
            if (!Directory.Exists(@"Mods"))
            {
                Directory.CreateDirectory(@"Mods");
            }

            string path = @"Mods\MixedGrip.json";

            if (File.Exists(path)) { File.Delete(path); }

            File.WriteAllText(path, JsonUtility.ToJson(settings, true));
        }
    }

    public class Settings
    {
        // mixed grip settings
        //public bool Remember_Lantern = false;
        public bool Swap_Animations = true;
        //public bool Unequip_Offhand_To_Pouch;
        //public bool Drop_Offhand = false;
        public bool Swap_On_Equip_And_Unequip = true;

        // balance settings
        public bool Balance_Weapons = true;
        //public float Weapon_Speed_Balance = 0.15f;
        //public float Weapon_Damage_Balance = 1.2f;
    }
}
