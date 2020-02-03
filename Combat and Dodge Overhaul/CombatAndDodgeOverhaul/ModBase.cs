using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;
using System.IO;
using Partiality.Modloader;
using static CustomKeybindings;

namespace CombatAndDodgeOverhaul
{
    #region ModLoader
    public class ModBase : PartialityMod
    {
        public GameObject _obj = null;
        public double version = 1.0;

        public ModBase()
        {
            this.ModID = "CombatAndDodgeOverhaul";
            this.Version = version.ToString("0.00");
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

            _obj.AddComponent<OverhaulGlobal>();
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }
    }
    #endregion

    #region Settings Class
    public class Settings
    {
        // global disables
        public bool Enable_StabilityMods = true;
        public bool Enable_Enemy_Mods = true;

        // player settings
        public bool Dodge_Cancelling = true;
        public bool Custom_Bag_Burden = true;
        public float min_burden_weight = 0.4f;
        public float min_slow_effect = 0.2f;
        public float max_slow_effect = 1.0f;

        public bool Attack_Cancels_Blocking = true;
        public float Stamina_Regen_Delay = 2;
        public float Extra_Stamina_Regen = 5f;

        // enemy mods
        public bool All_Enemies_Allied = false;
        public bool Enemy_Balancing = true;
        public float Enemy_Health = 1.0f; // multiplier
        public float Enemy_Damages = 0f; // flat to damage bonus
        public float Enemy_ImpactDmg = 0f;
        public float Enemy_Resistances = 0f; // flat (scaled)
        public float Enemy_ImpactRes = 0f;

        // animation collision slowdown
        public float SlowDown_Modifier = 1.0f;

        // stability mods
        public bool Blocking_Staggers_Attacker = false;
        public bool No_Stability_Regen_When_Blocking = false;
        public float Stability_Regen_Speed = 1.0f;
        public float Stability_Regen_Delay = 1.0f;
        public float Stagger_Threshold = 50.0f;
        public float Stagger_Immunity_Period = 0f;
        public float Knockdown_Threshold = 0;
        public float Enemy_AutoKD_Count = 3f;
        public float Enemy_AutoKD_Reset_Time = 8f;

        public bool Disable_Scaling = false;
    }
    #endregion

    public class OverhaulGlobal : MonoBehaviour
    {
        public static OverhaulGlobal Instance;

        public static Settings settings = new Settings();
        private static readonly string savePath = @"Mods/CombatAndDodgeOverhaul.json";

        public static string MenuKey = "Combat and Dodge Menu";

        internal void Awake()
        {
            Instance = this;

            LoadSettings();

            // custom keybindings
            AddAction(MenuKey, KeybindingsCategory.Menus, ControlType.Both, 5);

            var _obj = this.gameObject;
            _obj.AddComponent<EnemyManager>();
            _obj.AddComponent<PlayerManager>();
            _obj.AddComponent<StabilityManager>();
            _obj.AddComponent<ModGUI>();
            _obj.AddComponent<RPCManager>();
        }

        internal void Update()
        {
            foreach (PlayerSystem ps in Global.Lobby.PlayersInLobby.Where(x => x.IsLocalPlayer))
            {
                if (m_playerInputManager[ps.PlayerID].GetButtonDown(MenuKey))
                {
                    ModGUI.Instance.showGui = !ModGUI.Instance.showGui;
                }
            }
        }

        // ============ settings ============== //

        internal void OnDisable()
        {
            SaveSettings();
        }

        internal void OnApplicationQuit()
        {
            SaveSettings();
        }

        private void LoadSettings()
        {
            if (!Directory.Exists("Mods"))
            {
                Directory.CreateDirectory("Mods");
            }
            if (File.Exists(savePath))
            {
                string json = File.ReadAllText(savePath);
                try
                {
                    var s2 = JsonUtility.FromJson<Settings>(json);
                    if (s2 is Settings)
                    {
                        settings = s2;
                    }
                }
                catch
                {
                    Debug.LogError("Error reading DodgeOverhaul settings from " + savePath);
                }
            }
        }

        private void SaveSettings()
        {
            if (File.Exists(savePath)) { File.Delete(savePath); }

            File.WriteAllText(savePath, JsonUtility.ToJson(settings, true));
        }
    }
}

