using System;
using Partiality.Modloader;
using UnityEngine;
using System.IO;

namespace SpeedrunTimer
{
    public class ModBase : PartialityMod
    {
        public ModBase()
        {
            this.ModID = "Speedrun Timer";
            this.Version = "1.3";
            this.author = "Sinai";
        }

        public override void OnEnable()
        {
            base.OnEnable();

            var _obj = new GameObject("Speedrun_Timer");
            GameObject.DontDestroyOnLoad(_obj);

            _obj.AddComponent<SpeedrunTimer>();
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }
    }

    public class Settings
    {
        public string StartKey = "F8";
        public string StopKey = "F9";
    }

    public class SpeedrunTimer : MonoBehaviour
    {
        public static SpeedrunTimer Instance;

        public Settings settings = new Settings();

        public KeyCode StartKey;
        public KeyCode StopKey;

        private static readonly string defaultTimeString = "0:00.000";
        public float m_Time = 0.0f;
        public string timeString = defaultTimeString;
        public bool timerRunning = false;

        public static string configPath = "Mods/SpeedrunTimer.json";

        internal void Awake()
        {
            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);
                var tempSettings = JsonUtility.FromJson<Settings>(json);
                if (tempSettings != null)
                {
                    if (Enum.IsDefined(typeof(KeyCode), tempSettings.StartKey) && Enum.IsDefined(typeof(KeyCode), tempSettings.StopKey)) 
                    {
                        settings = tempSettings;
                    }
                    else
                    {
                        Debug.LogError("[SpeedrunTimer] Could not parse KeyCodes! Please make sure they are a valid KeyCode");
                    }
                }
            }
            else
            {
                File.WriteAllText(configPath, JsonUtility.ToJson(settings, true));
            }

            StartKey = (KeyCode)Enum.Parse(typeof(KeyCode), settings.StartKey);
            StopKey = (KeyCode)Enum.Parse(typeof(KeyCode), settings.StopKey);
        }

        internal void Update()
        {
            if (Input.GetKeyDown(StartKey))
            {
                timerRunning = true;
                m_Time = 0;
                timeString = defaultTimeString;
            }

            if (Input.GetKeyDown(StopKey))
            {
                timerRunning = false;
            }

            if (IsGameplayRunning() && timerRunning)
            {
                m_Time += Time.deltaTime;

                TimeSpan time = TimeSpan.FromSeconds(m_Time);
                timeString = time.Minutes + ":" + time.Seconds.ToString("00") + "." + time.Milliseconds.ToString("000");
            }
        }

        private bool IsGameplayRunning()
        {
            return Global.Lobby.PlayersInLobbyCount > 0 && !NetworkLevelLoader.Instance.IsGameplayPaused;
        }

        internal void OnGUI()
        {
            GUILayout.BeginArea(new Rect(8, 5, 250, 200));
            GUILayout.BeginVertical();
            int origFontsize = GUI.skin.label.fontSize;

            GUI.skin.label.padding = new RectOffset(0, 0, 0, 0);
            GUI.skin.label.margin = new RectOffset(3, 3, 3, 3);
            GUI.skin.label.fontSize = 14;
            GUILayout.Label("In-game time:", GUILayout.Height(24));

            GUI.skin.label.fontSize = 25;
            if (!timerRunning || !IsGameplayRunning())
                GUI.color = Color.yellow;
            else
                GUI.color = Color.white;
            GUILayout.Label(timeString, GUILayout.Height(35));

            GUI.skin.label.fontSize = 13;
            if (!timerRunning)
            {
                GUILayout.Label("Press " + StartKey.ToString() + " to start...");
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
            GUI.skin.label.fontSize = origFontsize;
            GUI.color = Color.white;
        }
    }
}
