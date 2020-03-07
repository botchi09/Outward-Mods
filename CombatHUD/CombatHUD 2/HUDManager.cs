using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using SideLoader;
using SharedModConfig;

namespace CombatHUD
{
    public class HUDManager : MonoBehaviour
    {
        public static HUDManager Instance;
        public static ModConfig config;
        public GameObject HUDCanvas;

        internal void Awake()
        {
            Instance = this;
        }

        internal void Start()
        {
            config = SetupConfig();
            config.Register();

            StartCoroutine(SetupCoroutine());
        }
    
        internal void Update()
        {
            if (HUDCanvas == null || Global.Lobby.PlayersInLobbyCount < 1 || NetworkLevelLoader.Instance.IsGameplayPaused)
            {
                return;
            }

            bool disable = false;
            if (MenuManager.Instance.IsMapDisplayed)
            {
                disable = true;
            }
            else
            {
                foreach (SplitPlayer player in SplitScreenManager.Instance.LocalPlayers)
                {
                    if (player.AssignedCharacter == null)
                    {
                        continue;
                    }

                    if (player.AssignedCharacter.CharacterUI.GetCurrentMenu() is MenuPanel panel && panel.IsDisplayed)
                    {
                        disable = true;
                        break;
                    }
                }
            }

            if (disable && HUDCanvas.activeSelf)
            {
                HUDCanvas.SetActive(false);
            }
            else if (!disable && !HUDCanvas.activeSelf)
            {
                HUDCanvas.SetActive(true);
            }
        }

        private IEnumerator SetupCoroutine()
        {
            while (SL.Instance == null || !SL.Instance.IsInitDone())
            {
                yield return new WaitForSeconds(0.1f);
            }

            Debug.Log(ModBase.ModName + " started, version: " + ModBase.ModVersion);

            var bundle = SL.Instance.LoadedBundles["combathud"];

            if (bundle.LoadAsset("HUDCanvas") is GameObject canvasAsset)
            {
                HUDCanvas = Instantiate(canvasAsset);
                DontDestroyOnLoad(HUDCanvas);

                // setup draw order
                var canvas = HUDCanvas.GetComponent<Canvas>();
                canvas.sortingOrder = 999; // higher = shown above other layers.

                // setup the autonomous components

                // ====== target manager ======
                var targetMgrHolder = HUDCanvas.transform.Find("TargetManager_Holder");

                var mgr_P1 = targetMgrHolder.transform.Find("TargetManager_P1").GetOrAddComponent<TargetManager>();
                mgr_P1.Split_ID = 0;

                var mgr_P2 = targetMgrHolder.transform.Find("TargetManager_P2").GetOrAddComponent<TargetManager>();
                mgr_P2.Split_ID = 1;

                // ====== player manager ======
                var statusTimerHolder = HUDCanvas.transform.Find("PlayerStatusTimers");
                statusTimerHolder.gameObject.AddComponent<PlayersManager>();

                // ====== damage labels ======
                var damageLabels = HUDCanvas.transform.Find("DamageLabels");
                damageLabels.gameObject.AddComponent<DamageLabels>();
            }
            else
            {
                Debug.LogError("[CombatHUD] Fatal error loading the AssetBundle. Make sure SideLoader is enabled, and the asset exists at Mods/SideLoader/CombatHUD/");
                Destroy(this.gameObject);
            }
        }

        private ModConfig SetupConfig()
        {
            config = new ModConfig
            {
                ModName = "CombatHUD",
                SettingsVersion = 1.1,
                Settings = new List<BBSetting>()
                {
                    new BoolSetting
                    {
                        Name = Settings.PlayerVitals,
                        SectionTitle = "Player Settings",
                        Description = "Show player vitals as numerical values",
                        DefaultValue = true
                    },
                    new BoolSetting
                    {
                        Name = Settings.PlayerDamageLabels,
                        Description = "Show player's damage dealt",
                        DefaultValue = true
                    },
                    new BoolSetting
                    {
                        Name = Settings.PlayerStatusTimers,
                        Description = "Show remaining lifespan on player's status effects",
                        DefaultValue = true
                    },
                    new BoolSetting
                    {
                        Name = Settings.EnemyHealth,
                        SectionTitle = "Enemy Settings",
                        Description = "Show targeted enemy's health as numerical value",
                        DefaultValue = true
                    },
                    new BoolSetting
                    {
                        Name = Settings.EnemyStatus,
                        Description = "Show inflicted status effects on targeted enemy",
                        DefaultValue = true
                    },
                    new BoolSetting
                    {
                        Name = Settings.EnemyStatusTimers,
                        Description = "Show remaining lifespans on enemy status effects",
                        DefaultValue = true
                    },
                    new BoolSetting
                    {
                        Name = Settings.EnemyBuildup,
                        Description = "Show the 'build-up' value for status effects",
                        DefaultValue = true
                    },
                    new BoolSetting
                    {
                        Name = Settings.EnemyDamageLabels,
                        Description = "Show damage dealt by enemies",
                        DefaultValue = true
                    },
                    new BoolSetting
                    {
                        Name = Settings.EnemyInfobox,
                        Description = "Show a detailed info-box for the targeted enemy",
                        DefaultValue = true
                    },
                    new BoolSetting
                    {
                        Name = Settings.LabelsStayAtHitPos,
                        SectionTitle = "Damage Labels",
                        Description = "Damage labels stay at the position of the hit (otherwise track to the Character)",
                        DefaultValue = false
                    },
                    new BoolSetting
                    {
                        Name = Settings.DisableColors,
                        Description = "White damage label text (otherwise color of highest damage)",
                        DefaultValue = false
                    },
                    new FloatSetting
                    {
                        Name = Settings.MinFontSize,
                        Description = "Minimum damage label font size (smallest end of scale)",
                        DefaultValue = 15f,
                        MaxValue = 40,
                        MinValue = 8,
                        RoundTo = 0,
                        ShowPercent = false
                    },
                    new FloatSetting
                    {
                        Name = Settings.MaxFontSize,
                        Description = "Maximum damage label font size (highest end of scale)",
                        DefaultValue = 30f,
                        MaxValue = 40f,
                        MinValue = 8f,
                        RoundTo = 0,
                        ShowPercent = false
                    },
                    new FloatSetting
                    {
                        Name = Settings.MaxDistance,
                        Description = "Maximum distance from player to show damage labels",
                        DefaultValue = 40f,
                        MaxValue = 250f,
                        MinValue = 0f,
                        RoundTo = 0,
                        ShowPercent = false
                    },
                    new FloatSetting
                    {
                        Name = Settings.DamageCeiling,
                        Description = "Damage Ceiling: highest damage number you want to scale to (for label size and speed)",
                        DefaultValue = 50f,
                        MaxValue = 1000f,
                        MinValue = 10f,
                        RoundTo = 0,
                        ShowPercent = false
                    },
                    new FloatSetting
                    {
                        Name = Settings.MinimumDamage,
                        Description = "Minimum Damage: any damage below this number will not be shown",
                        DefaultValue = 0f,
                        MaxValue = 50f,
                        MinValue = 0f,
                        RoundTo = 1,
                        ShowPercent = false
                    },
                    new FloatSetting
                    {
                        Name = Settings.LabelLifespan,
                        Description = "Label Lifespan: how long damage labels are shown for",
                        DefaultValue = 2.5f,
                        MaxValue = 5f,
                        MinValue = 0.5f,
                        RoundTo = 1,
                        ShowPercent = false
                    },
                    new FloatSetting
                    {
                        Name = Settings.Infobox_P1_X,
                        SectionTitle = "Target Infobox Settings",
                        Description = "Player 1 Infobox: Horizontal offset",
                        DefaultValue = 0f,
                        MaxValue = 4000,
                        MinValue = 0f,
                        RoundTo = 0,
                        ShowPercent = false
                    },
                    new FloatSetting
                    {
                        Name = Settings.Infobox_P1_Y,
                        Description = "Player 1 Infobox: Vertical offset",
                        DefaultValue = 0f,
                        MaxValue = 2000,
                        MinValue = 0f,
                        RoundTo = 0,
                        ShowPercent = false
                    },
                    new FloatSetting
                    {
                        Name = Settings.Infobox_P2_X,
                        Description = "Player 2 Infobox: Horizontal offset",
                        DefaultValue = 0f,
                        MaxValue = 4000,
                        MinValue = 0f,
                        RoundTo = 0,
                        ShowPercent = false
                    },
                    new FloatSetting
                    {
                        Name = Settings.Infobox_P2_Y,
                        Description = "Player 2 Infobox: Vertical offset",
                        DefaultValue = 0f,
                        MaxValue = 2000,
                        MinValue = 0f,
                        RoundTo = 0,
                        ShowPercent = false
                    }
                }
            };

            return config;
        }

        public static float Rel(float offset, bool height = false) // false for width, true for height
        {
            return offset * (height ? Screen.height : Screen.width) * 100f / (height ? 1080f : 1920f) * 0.01f;
        }
    }
}
