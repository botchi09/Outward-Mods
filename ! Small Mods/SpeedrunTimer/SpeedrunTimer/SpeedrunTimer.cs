using System;
using System.Collections;
using System.Collections.Generic;
using Partiality.Modloader;
using UnityEngine;
using System.IO;
using System.Linq;

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
        public string ConditionKey = "F10";
    }

    public class SpeedrunTimer : MonoBehaviour
    {
        public static SpeedrunTimer Instance;

        public Settings settings = new Settings();

        public KeyCode StartKey;
        public KeyCode StopKey;
        public KeyCode ConditionKey;

        private static readonly string defaultTimeString = "0:00.000";
        public float m_Time = 0.0f;
        public string timeString = defaultTimeString;
        public bool timerRunning = false;
        private bool runCompleted = false;

        public Dictionary<string, List<int>> StopConditions = new Dictionary<string, List<int>>
        {
            { "Well-Earned Rest", new List<int> { 7011104, 7011204, 7011304 } }, // checks for all 3 "Peacemaker" quests
            { "Blood Price", new List<int> { 7011001} }, // checks for Call to Adventure (does not check success)
        };
        private int m_currentStopCondition = 0;

        public static string configPath = "Mods/SpeedrunTimer.json";

        internal void Awake()
        {
            bool flag = true;
            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);
                var tempSettings = JsonUtility.FromJson<Settings>(json);
                if (tempSettings != null)
                {
                    if (Enum.IsDefined(typeof(KeyCode), tempSettings.StartKey) 
                        && Enum.IsDefined(typeof(KeyCode), tempSettings.StopKey)
                        && Enum.IsDefined(typeof(KeyCode), tempSettings.ConditionKey)) 
                    {
                        settings = tempSettings;
                        flag = false;
                    }
                    else
                    {
                        Debug.LogError("[SpeedrunTimer] Could not parse KeyCodes! Please make sure they are valid KeyCodes");
                    }
                }
            }
            if (flag)
            {
                File.WriteAllText(configPath, JsonUtility.ToJson(settings, true));
            }

            m_currentStopCondition = 0;

            StartKey = (KeyCode)Enum.Parse(typeof(KeyCode), settings.StartKey);
            StopKey = (KeyCode)Enum.Parse(typeof(KeyCode), settings.StopKey);
            ConditionKey = (KeyCode)Enum.Parse(typeof(KeyCode), settings.ConditionKey);
        }

        internal void Update()
        {
            if (Input.GetKeyDown(StartKey))
            {
                timerRunning = true;
                m_Time = 0;
                timeString = defaultTimeString;
                runCompleted = false;
            }

            if (Input.GetKeyDown(StopKey))
            {
                timerRunning = false;
                runCompleted = false;
            }

            if (Input.GetKeyDown(ConditionKey))
            {
                if (StopConditions.Count() - 1 > m_currentStopCondition)
                {
                    m_currentStopCondition++;
                }
                else
                {
                    m_currentStopCondition = 0;
                }
            }

            if (IsGameplayRunning() && timerRunning)
            {
                m_Time += Time.deltaTime;

                TimeSpan time = TimeSpan.FromSeconds(m_Time);
                timeString = (time.Hours > 0 ? (time.Hours + ":") : "") + time.Minutes + ":" + time.Seconds.ToString("00") + "." + time.Milliseconds.ToString("000");

                // todo check stop condition
                var c = CharacterManager.Instance.GetFirstLocalCharacter();
                foreach (int id in StopConditions.ElementAt(m_currentStopCondition).Value)
                {
                    var quest = c.Inventory.QuestKnowledge.GetItemFromItemID(id);
                    if (quest && (quest as Quest).IsCompleted)
                    {
                        timerRunning = false;
                        runCompleted = true;
                    }
                }

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
                if (runCompleted)
                    GUI.color = Color.green;
                else 
                    GUI.color = Color.yellow;
            else
                GUI.color = Color.white;
            GUILayout.Label(timeString, GUILayout.Height(35));

            GUI.skin.label.fontSize = 13;
            if (!timerRunning)
            {
                GUILayout.Label(StartKey.ToString() + " to start...");
            }
            
            GUILayout.Label("Stop condition: (" + settings.ConditionKey + ") " + StopConditions.ElementAt(m_currentStopCondition).Key);

            GUILayout.EndVertical();
            GUILayout.EndArea();
            GUI.skin.label.fontSize = origFontsize;
            GUI.color = Color.white;
        }
    }
}
