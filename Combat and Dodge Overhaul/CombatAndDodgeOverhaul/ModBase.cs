using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;
using System.IO;
using Partiality.Modloader;
using SharedModConfig;

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
        // player settings
        public static string Dodge_Cancelling = "Dodge_Cancelling";
        public static string Stamina_Cost_Stat = "Stamina_Cost_Stat";
        public static string Custom_Dodge_Cost = "Custom_Dodge_Cost";
        public static string Custom_Bag_Burden = "Custom_Bag_Burden";
        public static string min_burden_weight = "min_burden_weight";
        public static string min_slow_effect = "min_slow_effect";
        public static string max_slow_effect = "max_slow_effect";
      
        public static string Attack_Cancels_Blocking = "Attack_Cancels_Blocking";
        public static string Stamina_Regen_Delay = "Stamina_Regen_Delay";
        public static string Extra_Stamina_Regen = "Extra_Stamina_Regen";
               
        // enemy mods
        public static string All_Enemies_Allied = "All_Enemies_Allied";
        public static string Enemy_Balancing = "Enemy_Balancing";
        public static string Enemy_Health = "Enemy_Health"; // multiplier
        public static string Enemy_Damages = "Enemy_Damages"; // flat to damage bonus
        public static string Enemy_ImpactDmg = "Enemy_ImpactDmg";
        public static string Enemy_Resistances = "Enemy_Resistances"; // flat (scaled)
        public static string Enemy_ImpactRes = "Enemy_ImpactRes";

        // animation collision slowdown
        public static string SlowDown_Modifier = "SlowDown_Modifier";

        // stability mods
        public static string Blocking_Staggers_Attacker = "Blocking_Staggers_Attacker";
        public static string No_Stability_Regen_When_Blocking = "No_Stability_Regen_When_Blocking";
        public static string Stability_Regen_Speed = "Stability_Regen_Speed";
        public static string Stability_Regen_Delay = "Stability_Regen_Delay";
        public static string Stagger_Threshold = "Stagger_Threshold";
        public static string Stagger_Immunity_Period = "Stagger_Immunity_Period";
        public static string Knockdown_Threshold = "Knockdown_Threshold";
        public static string Enemy_AutoKD_Count = "Enemy_AutoKD_Count";
        public static string Enemy_AutoKD_Reset_Time = "Enemy_AutoKD_Reset_Time";
    }
    #endregion

    public class OverhaulGlobal : MonoBehaviour
    {
        public static OverhaulGlobal Instance;

        public static ModConfig config;

        public static string MenuKey = "Combat and Dodge Menu";

        internal void Awake()
        {
            Instance = this;

            var _obj = this.gameObject;
            _obj.AddComponent<EnemyManager>();
            _obj.AddComponent<PlayerManager>();
            _obj.AddComponent<StabilityManager>();
            _obj.AddComponent<RPCManager>();
        }

        internal void Start()
        {
            // LoadSettings();
            config = SetupConfig();

            StartCoroutine(SetupCoroutine());
        }

        private IEnumerator SetupCoroutine()
        {
            while (ConfigManager.Instance == null || !ConfigManager.Instance.IsInitDone())
            {
                yield return new WaitForSeconds(0.1f);
            }

            config.Register();
        }

        // ============ settings ============== //

        private ModConfig SetupConfig()
        {
            var newConfig = new ModConfig
            {
                ModName = "Combat and Dodge Overhaul",
                SettingsVersion = 1.2,
                Settings = new List<BBSetting>
                {
                    new BoolSetting
                    {
                        Name = Settings.Dodge_Cancelling,
                        SectionTitle = "Player Animations",
                        Description = "Allow animation cancelling with dodge",
                        DefaultValue = true
                    },
                    new BoolSetting
                    {
                        Name = Settings.Attack_Cancels_Blocking,
                        Description = "Allow attack input to interrupt blocking",
                        DefaultValue = true
                    },
                    new FloatSetting
                    {
                        Name = Settings.SlowDown_Modifier,
                        Description = "Stagger 'Slow-Down' animation speed multiplier (lower value = less slow-down)",
                        DefaultValue = 0.75f,
                        MinValue = 0.0f,
                        MaxValue = 2.0f,
                        ShowPercent = false,
                        RoundTo = 2
                    },
                    new FloatSetting
                    {
                        Name = Settings.Extra_Stamina_Regen,
                        SectionTitle = "Player Stamina Settings",
                        Description = "Extra stamina regen per second",
                        DefaultValue = 0f,
                        MinValue = 0.0f,
                        MaxValue = 100.0f,
                        ShowPercent = false,
                        RoundTo = 1
                    },
                    new FloatSetting
                    {
                        Name = Settings.Stamina_Regen_Delay,
                        Description = "Delay (in seconds) after last stamina use before extra regen takes effect",
                        DefaultValue = 2f,
                        MinValue = 0.0f,
                        MaxValue = 20.0f,
                        ShowPercent = false,
                        RoundTo = 1
                    },
                    new FloatSetting
                    {
                        Name = Settings.Stamina_Cost_Stat,
                        Description = "Custom Stamina Cost Modifier (added to your Stamina Cost stat)",
                        DefaultValue = 0f,
                        MinValue = -500.0f,
                        MaxValue = 500.0f,
                        ShowPercent = true,
                        RoundTo = 1
                    },
                    new FloatSetting
                    {
                        Name = Settings.Custom_Dodge_Cost,
                        Description = "Custom Dodge stamina cost (default value is 6)",
                        DefaultValue = 6f,
                        MinValue = 0f,
                        MaxValue = 100f,
                        ShowPercent = false,
                        RoundTo = 1
                    },
                    new BoolSetting
                    {
                        Name = Settings.Custom_Bag_Burden,
                        SectionTitle = "Backpack Dodge Burdens",
                        Description = "Enable custom bag dodge burdens",
                        DefaultValue = true
                    },
                    new FloatSetting
                    {
                        Name = Settings.min_burden_weight,
                        Description = "Minimum bag weight for dodge burdens to take effect",
                        DefaultValue = 40f,
                        MinValue = 0f,
                        MaxValue = 100f,
                        ShowPercent = true,
                        RoundTo = 0
                    },
                    new FloatSetting
                    {
                        Name = Settings.min_slow_effect,
                        Description = "Minimum dodge burden effect",
                        DefaultValue = 20f,
                        MinValue = 0f,
                        MaxValue = 100f,
                        ShowPercent = true,
                        RoundTo = 0
                    },
                    new FloatSetting
                    {
                        Name = Settings.max_slow_effect,
                        Description = "Maximum dodge burden effect",
                        DefaultValue = 100f,
                        MinValue = 0f,
                        MaxValue = 100f,
                        ShowPercent = true,
                        RoundTo = 0
                    },
                    new BoolSetting
                    {
                        Name = Settings.Blocking_Staggers_Attacker,
                        SectionTitle = "Stability and Stagger",
                        Description = "A successful block will stagger the attacker (using melee weapons)",
                        DefaultValue = true
                    },
                    new BoolSetting
                    {
                        Name = Settings.No_Stability_Regen_When_Blocking,
                        Description = "Chracters do not regain stability while blocking",
                        DefaultValue = true
                    },
                    new FloatSetting
                    {
                        Name = Settings.Stability_Regen_Speed,
                        Description = "Modifier for Stability regen speed (higher value = faster regen)",
                        DefaultValue = 1.0f,
                        MinValue = 0.01f,
                        MaxValue = 3.0f,
                        ShowPercent = false,
                        RoundTo = 2
                    },
                    new FloatSetting
                    {
                        Name = Settings.Stability_Regen_Delay,
                        Description = "Delay (in seconds) after last hit before stability will regenerate",
                        DefaultValue = 1.0f,
                        MinValue = 0f,
                        MaxValue = 5.0f,
                        ShowPercent = false,
                        RoundTo = 1
                    },
                    new FloatSetting
                    {
                        Name = Settings.Stagger_Threshold,
                        Description = "Stability threshold at which characters will be staggered",
                        DefaultValue = 50.0f,
                        MinValue = 0f,
                        MaxValue = 100.0f,
                        ShowPercent = true,
                        RoundTo = 1
                    },
                    new FloatSetting
                    {
                        Name = Settings.Stagger_Immunity_Period,
                        Description = "Immunity period (in seconds) after last stagger, to prevent stun locking",
                        DefaultValue = 0.0f,
                        MinValue = 0f,
                        MaxValue = 10.0f,
                        ShowPercent = false,
                        RoundTo = 1
                    },
                    new FloatSetting
                    {
                        Name = Settings.Knockdown_Threshold,
                        Description = "Stability threshold at which characters will be knocked down",
                        DefaultValue = 0.0f,
                        MinValue = 0f,
                        MaxValue = 100.0f,
                        ShowPercent = true,
                        RoundTo = 1
                    },
                    new FloatSetting
                    {
                        Name = Settings.Enemy_AutoKD_Count,
                        Description = "[Enemies] Amount of staggers in a row before enemy is auto-knocked down",
                        DefaultValue = 3f,
                        MinValue = 0f,
                        MaxValue = 10.0f,
                        ShowPercent = false,
                        RoundTo = 0
                    },
                    new FloatSetting
                    {
                        Name = Settings.Enemy_AutoKD_Reset_Time,
                        Description = "[Enemies] Timeout (in seconds) after last stagger to reset auto-knock count",
                        DefaultValue = 8.0f,
                        MinValue = 0f,
                        MaxValue = 15.0f,
                        ShowPercent = false,
                        RoundTo = 1
                    },
                    new BoolSetting
                    {
                        Name = Settings.All_Enemies_Allied,
                        SectionTitle = "Enemy Settings",
                        Description = "All enemies are allied (requires scene reload to take effect)",
                        DefaultValue = false
                    },
                    new BoolSetting
                    {
                        Name = Settings.Enemy_Balancing,
                        Description = "Enable custom enemy balancing (overrides co-op stat modifiers)",
                        DefaultValue = false
                    },
                    new FloatSetting
                    {
                        Name = Settings.Enemy_Health,
                        Description = "Enemy health multiplier (1.0x = no changes)",
                        DefaultValue = 1.0f,
                        MinValue = 0.1f,
                        MaxValue = 15,
                        ShowPercent = false,
                        RoundTo = 2
                    },
                    new FloatSetting
                    {
                        Name = Settings.Enemy_Damages,
                        Description = "Extra enemy damage bonus (added to enemy damage bonuses)",
                        DefaultValue = 0f,
                        MinValue = 0f,
                        MaxValue = 500f,
                        ShowPercent = true,
                        RoundTo = 0
                    },
                    new FloatSetting
                    {
                        Name = Settings.Enemy_ImpactDmg,
                        Description = "Extra enemy Impact damage bonus (added to enemy impact bonus stat)",
                        DefaultValue = 0f,
                        MinValue = 0f,
                        MaxValue = 500f,
                        ShowPercent = true,
                        RoundTo = 0
                    },
                    new FloatSetting
                    {
                        Name = Settings.Enemy_Resistances,
                        Description = "Extra enemy resistance stats (added to enemy resistance stat). Has diminished returns.",
                        DefaultValue = 0f,
                        MinValue = 0f,
                        MaxValue = 500f,
                        ShowPercent = true,
                        RoundTo = 0
                    },
                    new FloatSetting
                    {
                        Name = Settings.Enemy_ImpactRes,
                        Description = "Extra enemy Impact resistance (added to enemy impact resist stat)",
                        DefaultValue = 0f,
                        MinValue = 0f,
                        MaxValue = 500f,
                        ShowPercent = true,
                        RoundTo = 0
                    },
                }
            };

            return newConfig;
        }
    }
}

