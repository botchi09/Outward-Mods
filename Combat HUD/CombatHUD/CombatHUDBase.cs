using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Partiality.Modloader;
//using SinAPI;
using static CustomKeybindings;

namespace CombatHUD
{
    // Partiality Loader
    public class CombatHUDBase : PartialityMod
    {
        public double version = 3.4;

        public GameObject _obj = null;
        public CombatHudGlobal script;

        public CombatHUDBase()
        {
            this.ModID = "Combat HUD";
            this.Version = version.ToString();
            this.author = "Sinai";
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (_obj == null)
            {
                _obj = new GameObject("Combat_HUD");
                GameObject.DontDestroyOnLoad(_obj);
            }

            script = _obj.AddComponent<CombatHudGlobal>();
            script._base = this;
            script.Init();
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }
    }

    public class PlayerInfo
    {
        public Character character;
        public Camera camera;
        public int ID;
    }

    // Global Manager
    public class CombatHudGlobal : MonoBehaviour
    {
        public CombatHUDBase _base;

        public Settings settings;
        public string MenuKey = "Combat HUD Menu";

        public GUIManager GUIManager;
        public DamageManager DamageMgr;
        public StatusManager StatusMgr;
        public TargetManager TargetMgr;
        // public DPSCounter DPSCounter;

        public string currentScene;
        public List<PlayerInfo> LocalPlayers = new List<PlayerInfo>();

        public void Init()
        {
            //OLogger.CreateLog(new Rect(525, 5, 450, 150), "Default", true, true);

            NewSettings();
            LoadSettings();

            try
            {
                AddGlobalComponent(typeof(GUIManager), "GUIManager");
                AddGlobalComponent(typeof(DamageManager), "DamageMgr");
                AddGlobalComponent(typeof(StatusManager), "StatusMgr");
                AddGlobalComponent(typeof(TargetManager), "TargetMgr");
                // AddGlobalComponent(typeof(DPSCounter), "DPSCounter");
            }
            catch
            {
                // Debug.Log("Couldn't add components");
            }

            if (!settings.Show_On_Load)
            {
                GUIManager.showMenu = false;
            }

            AddAction(MenuKey, KeybindingsCategory.Menus, ControlType.Both, 5);

            //Debug.Log("Combat HUD " + _base.version + " enabled!");
        }

        private void AddGlobalComponent(Type t, string FieldName)
        {
            object obj;
            if (_base._obj.GetComponent(t) is object obj2) { obj = obj2; }
            else { obj = _base._obj.AddComponent(t); }

            try
            {
                GetType().GetField(FieldName).SetValue(this, obj);
                obj.GetType().GetField("global").SetValue(obj, this);
            }
            catch //(Exception ex)
            {
            }
        }

        public bool sceneChangeFlag = false;

        internal void Update()
        {
            if (currentScene != SceneManagerHelper.ActiveSceneName) { sceneChangeFlag = true; }

            if (NetworkLevelLoader.Instance.IsGameplayPaused)
            {
                return;
            }

            bool flag2 = false;

            if (CharacterManager.Instance.PlayerCharacters.Count > 0)
            {
                bool flag = false;
                if (sceneChangeFlag || LocalPlayers.Count != CharacterManager.Instance.Characters.Values.Where(x => x.IsLocalPlayer).Count())
                {
                    LocalPlayers.Clear();
                    flag = true;
                }

                int id = 0;
                foreach (Character c in CharacterManager.Instance.Characters.Values.Where(x => x.IsLocalPlayer))
                {
                    if (flag)
                    {                        
                        if (c.CharacterCamera != null && c.CharacterCamera.CameraScript != null)
                        {
                            LocalPlayers.Add(new PlayerInfo { character = c, ID = id, camera = c.CharacterCamera.CameraScript });
                        }
                        else
                        {
                            flag2 = true;
                            break;
                        }
                    }

                    if (m_playerInputManager[id].GetButtonDown(MenuKey))
                    {
                        GUIManager.showMenu = !GUIManager.showMenu;
                    }

                    id++;
                }
            }

            if (!flag2)
            {
                DamageMgr.UpdateDamage();
                StatusMgr.UpdateStatus();
                TargetMgr.UpdateTarget();

                currentScene = SceneManagerHelper.ActiveSceneName;
                sceneChangeFlag = false;
            }

            // menu mouse fix

            if (lastMenuToggle != GUIManager.showMenu)
            {
                lastMenuToggle = GUIManager.showMenu;

                Character c = CharacterManager.Instance.GetFirstLocalCharacter();

                if (c.CharacterUI.PendingDemoCharSelectionScreen is Panel panel)
                {
                    if (lastMenuToggle)
                        panel.Show();
                    else
                        panel.Hide();
                }
                else if (lastMenuToggle)
                {
                    GameObject obj = new GameObject();
                    obj.transform.parent = c.transform;
                    obj.SetActive(true);

                    Panel newPanel = obj.AddComponent<Panel>();
                    At.SetValue(newPanel, typeof(CharacterUI), c.CharacterUI, "PendingDemoCharSelectionScreen");
                    newPanel.Show();
                }
            }
        }

        public bool lastMenuToggle;


        internal void OnDisable()
        {
            SaveSettings();
        }

        public void NewSettings()
        {
            settings = new Settings
            {
                Show_Player_Vitals = true,
                Show_Player_DamageLabels = true,
                Show_Player_StatusTimers = true,

                Show_Enemy_DamageLabels = true,
                Show_TargetEnemy_Health = true,
                Show_Enemy_Status = true,
                Show_Enemy_Status_Lifespan = true,
                Show_BuildUps = true,

                Show_Target_Detailed = true,
                Player1_Detailed_Pos = new Vector2(0,0),
                Player2_Detailed_Pos = new Vector2(0,0),

                labelsStayAtHitPos = false,
                disableColors = false,
                maxDistance = 40,
                minDamage = 0,
                damageStrength = 50.0f,
                labelMaxSpeed = 0.3f,
                labelMinSpeed = 0.07f,
                labelMinTime = 1.0f,
                labelMaxTime = 2.25f,
                labelMinSize = 14,
                labelMaxSize = 20,
                labelRandomX = 15,
                labelRandomY = 5,
                MinTransparency = 0.3f,

                Show_On_Load = true,
                disableScaling = false,
                StatusTimerScale = 1.0f,
                StatusTimerX = 0,
                StatusTimerY = 0,
                EnemyHealthScale = 1.0f,
                EnemyHealthX = 0,
                EnemyHealthY = 0,
                EnemyStatusIconScale = 1.0f,
                EnemyStatusTextScale = 1.0f,
                EnemyStatusX = 0,
                EnemyStatusY = 0
            };
        }

        private void LoadSettings()
        {
            if (settings == null) { NewSettings(); }

            if (File.Exists(@"Mods\CombatHUD\CombatHUD.json"))
            {
                try
                {
                    Settings settings2 = new Settings();
                    settings2 = JsonUtility.FromJson<Settings>(File.ReadAllText(@"Mods\CombatHUD\CombatHUD.json"));
                    if (settings2 != null) { settings = settings2; }
                }
                catch
                {
                    Debug.LogError(@"[Combat HUD] The settings file at Outward\Mods\CombatHUD\CombatHUD.json is not compatible with this version, or was corrupted.");
                }
            }

            // temp fix
            if (settings.StatusTimerScale <= 0 || settings.EnemyHealthScale <= 0 || settings.EnemyStatusTextScale <= 0 || settings.EnemyStatusIconScale <= 0)
            {
                settings.StatusTimerScale = 1.0f;
                settings.EnemyHealthScale = 1.0f;
                settings.EnemyStatusTextScale = 1.0f;
                settings.EnemyStatusIconScale = 1.0f;
            }
        }

        private void SaveSettings()
        {
            Directory.CreateDirectory(@"Mods");
            Directory.CreateDirectory(@"Mods\CombatHUD");

            Jt.SaveJsonOverwrite(@"Mods\CombatHUD\CombatHUD.json", settings);
        }
    }

    public class Settings
    {
        public bool disableScaling;

        public float StatusTimerScale;
        public float StatusTimerX;
        public float StatusTimerY;

        public float EnemyHealthScale;
        public float EnemyHealthX;
        public float EnemyHealthY;

        public float EnemyStatusIconScale;
        public float EnemyStatusTextScale;
        public float EnemyStatusX;
        public float EnemyStatusY;

        public bool Show_Player_DamageLabels;
        public bool Show_Player_StatusTimers;
        public bool Show_Player_Vitals;

        public bool Show_Enemy_DamageLabels;
        public bool Show_Enemy_Status;
        public bool Show_Enemy_Status_Lifespan;
        public bool Show_TargetEnemy_Health;
        public bool Show_BuildUps;

        public bool Show_Target_Detailed;
        public Vector2 Player1_Detailed_Pos;
        public Vector2 Player2_Detailed_Pos;

        public bool labelsStayAtHitPos;
        public float maxDistance;
        public bool disableColors;
        public float minDamage;
        public float damageStrength;
        public float MinTransparency;
        public float labelMinTime;
        public float labelMaxTime;
        public float labelMinSize;
        public float labelMaxSize;
        public float labelMinSpeed;
        public float labelMaxSpeed;
        public float labelRandomX;
        public float labelRandomY;

        public bool Show_On_Load;
    }
}
