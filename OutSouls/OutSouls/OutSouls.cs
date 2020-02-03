using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using SinAPI;
using static CustomKeybindings;
using Partiality.Modloader;

namespace OutSoulsMod
{
    public class ModBase : PartialityMod
    {
        public GameObject _obj = null;

        public ModBase()
        {
            this.ModID = "OutSouls";
            this.Version = OutSouls.version.ToString("0.00");
            this.author = "Sinai";
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (_obj == null)
            {
                _obj = new GameObject(this.ModID);
                GameObject.DontDestroyOnLoad(_obj);
            }

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

        public static Settings settings;
        public static readonly string settingsPath = @"Mods\OutSouls\OutSouls.json";

        public string MenuKey = "OutSouls Menu";

        internal void Awake()
        {
            Instance = this;

            settings = new Settings();
            LoadSettings();

            this.gameObject.AddComponent<OutSoulsGUI>();
            this.gameObject.AddComponent<BonfireManager>();
            this.gameObject.AddComponent<BonfireGUI>();
            this.gameObject.AddComponent<RPCManager>();

            // custom keybindings
            AddAction(MenuKey, KeybindingsCategory.Menus, ControlType.Both, 5);
        }

        internal void Update()
        {
            //if (Global.Lobby.PlayersInLobbyCount < 1 || NetworkLevelLoader.Instance.IsGameplayPaused)
            //{
            //    return;
            //}

            foreach (PlayerSystem ps in Global.Lobby.PlayersInLobby.Where(x => x.ControlledCharacter.IsLocalPlayer))
            {
                if (m_playerInputManager[ps.PlayerID].GetButtonDown(MenuKey))
                {
                    OutSoulsGUI.Instance.showGui = !OutSoulsGUI.Instance.showGui;
                }
            }
        }

        public bool CanInteract(Character c)
        {
            return (bool)At.Call(c, "CanInteract", null);
        }

        // =============== settings =====================


        private void LoadSettings()
        {
            settings = new Settings();

            try
            {
                Settings loadsettings = new Settings();
                loadsettings = JsonUtility.FromJson<Settings>(File.ReadAllText(settingsPath));
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
            if (!Directory.Exists(@"Mods\OutSouls"))
            {
                Directory.CreateDirectory(@"Mods\OutSouls");
            }

            if (File.Exists(settingsPath))
            {
                File.Delete(settingsPath);
            }
            File.WriteAllText(settingsPath, JsonUtility.ToJson(settings, true));
        }
    }

    public class Settings
    {
        // global enable/disable
        public bool Disable_Scaling = false;

        // bonfire settings
        public bool Enable_Bonfire_System = true;
        public bool Bonfires_Heal_Enemies = false;
        public bool Disable_Bonfire_Costs = false;
        public bool Cant_Use_Bonfires_In_Combat = true;
    }
}
