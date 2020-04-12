using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Explorer
{
    public class Explorer : MonoBehaviour
    {
        public static Explorer Instance;

        // quest debugging
        public bool QuestDebugging { get; set; } = true;
        //public Dictionary<string, QuestEventSignature> QuestEvents = new Dictionary<string, QuestEventSignature>();

        internal void Awake()
        {
            Instance = this;

            // log to game window
            Application.logMessageReceived += Application_logMessageReceived;

            // debug quest events
            On.SendQuestEventInteraction.OnActivate += SendQuestInteractionHook;
            On.NodeCanvas.Tasks.Actions.SendQuestEvent.OnExecute += SendQuestEventHook;

            // fix area names on teleport menu
            On.DT_CharacterCheats.InitAreaSwitches += DT_CharacterCheats_InitAreaSwitches;

            // Skip Logos hook
            On.StartupVideo.Start += new On.StartupVideo.hook_Start(StartupVideo_Start);
        }

        internal void Start()
        {
            // create logger
            var m_logger = new Vector2(600, 260);
            OLogger.CreateLog(new Rect(Screen.width - m_logger.x - 5, Screen.height - m_logger.y - 5, m_logger.x, m_logger.y));            

            // done init
            MenuManager.ShowMenu = true;
            OLogger.Log("Initialised Explorer. Unity version: " + Application.unityVersion.ToString());
        }

        internal void Update()
        {
            if (Input.GetKeyDown(KeyCode.F7))
            {
                MenuManager.ShowMenu = !MenuManager.ShowMenu;
            }
        }

        // ************** public helpers **************

        public static Type GetType(string _type)
        {
            Type type = null;
            if (TryGetType(_type, "Assembly-CSharp") is Type gameType)
            {
                type = gameType;
            }
            else if (TryGetType(_type, "UnityEngine") is Type unityType)
            {
                type = unityType;
            }
            return type;
        }

        private static Type TryGetType(string _type, string _assembly)
        {
            try
            {
                var type = Type.GetType(_type + ", " + _assembly + ", Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
                if (type == null)
                {
                    throw new Exception();
                }
                else
                {
                    return type;
                }
            }
            catch
            {
                return null;
            }
        }

        // ***************** LOG DEBUGGING ******************** //

        // Log Debug messages to OLogger window
        private void Application_logMessageReceived(string message, string stackTrace, LogType type)
        {
            // useless spam errors (unity warnings)
            string[] blacklist = new string[]
            {
                "Internal: JobTempAlloc",
                "GUI Error:",
                "BoxColliders does not support negative scale or size",
                "is registered with more than one LODGroup",
                "only 0 controls when doing Repaint",
                "it is not close enough to the NavMesh"
            };

            foreach (string s in blacklist)
            {
                if (message.ToLower().Contains(s.ToLower()))
                {
                    return;
                }
            }

            if (type == LogType.Exception)
            {
                OLogger.Error(message + "\r\nStack Trace: " + stackTrace);
            }
            else if (type == LogType.Warning)
            {
                OLogger.Warning(message);
            }
            else
            {
                OLogger.Log(message);
            }
        }

        // *************** FIX DEBUG AREA SWITCH NAMES *********** //

        private void DT_CharacterCheats_InitAreaSwitches(On.DT_CharacterCheats.orig_InitAreaSwitches orig, DT_CharacterCheats self)
        {
            orig(self);

            var families = At.GetValue(typeof(DT_CharacterCheats), self, "m_ddFamilies") as Dropdown[];

            foreach (var dd in families)
            {
                if (dd == null) { continue; }

                var trigger = dd.GetComponent<EventTrigger>();
                if (trigger == null) { continue; }

                var _event = new EventTrigger.Entry()
                {
                    eventID = EventTriggerType.PointerUp
                };
                _event.callback.AddListener(delegate (BaseEventData data) { this.OnAreaSelected(dd); });

                trigger.triggers.Add(_event);
            }
        }

        private void OnAreaSelected(Dropdown dd)
        {
            foreach (var option in dd.options)
            {
                if (SceneBuildNames.ContainsKey(option.text))
                {
                    option.text = SceneBuildNames[option.text];
                }
            }
        }

        public static Dictionary<string, string> SceneBuildNames = new Dictionary<string, string>
        {
            { "CierzoTutorial", "Shipwreck Beach" },
            { "CierzoNewTerrain", "Cierzo" },
            { "CierzoDestroyed", "Cierzo (Destroyed)" },
            { "ChersoneseNewTerrain", "Chersonese" },
            { "Chersonese_Dungeon1", "Vendavel Fortress" },
            { "Chersonese_Dungeon2", "Blister Burrow" },
            { "Chersonese_Dungeon3", "Ghost Pass" },
            { "Chersonese_Dungeon4_BlueChamber", "Blue Chamber’s Conflux Path" },
            { "Chersonese_Dungeon4_HolyMission", "Holy Mission’s Conflux Path" },
            { "Chersonese_Dungeon4_Levant", "Heroic Kingdom’s Conflux Path" },
            { "Chersonese_Dungeon5", "Voltaic Hatchery" },
            { "Chersonese_Dungeon4_CommonPath", "Conflux Chambers" },
            { "Chersonese_Dungeon6", "Corrupted Tombs" },
            { "Chersonese_Dungeon8", "Cierzo Storage" },
            { "Chersonese_Dungeon9", "Montcalm Clan Fort" },
            { "ChersoneseDungeonsSmall", "Chersonese Misc. Dungeons" },
            { "ChersoneseDungeonsBosses", "Unknown Arena" },
            { "Monsoon", "Monsoon" },
            { "HallowedMarshNewTerrain", "Hallowed Marsh" },
            { "Hallowed_Dungeon1", "Jade Quarry" },
            { "Hallowed_Dungeon2", "Giants’ Village" },
            { "Hallowed_Dungeon3", "Reptilian Lair" },
            { "Hallowed_Dungeon4_Interior", "Dark Ziggurat Interior" },
            { "Hallowed_Dungeon5", "Spire of Light" },
            { "Hallowed_Dungeon6", "Ziggurat Passage" },
            { "Hallowed_Dungeon7", "Dead Roots" },
            { "HallowedDungeonsSmall", "Marsh Misc. Dungeons" },
            { "HallowedDungeonsBosses", "Unknown Arena" },
            { "Levant", "Levant" },
            { "Abrassar", "Abrassar" },
            { "Abrassar_Dungeon1", "Undercity Passage" },
            { "Abrassar_Dungeon2", "Electric Lab" },
            { "Abrassar_Dungeon3", "The Slide" },
            { "Abrassar_Dungeon4", "Stone Titan Caves" },
            { "Abrassar_Dungeon5", "Ancient Hive" },
            { "Abrassar_Dungeon6", "Sand Rose Cave" },
            { "AbrassarDungeonsSmall", "Abrassar Misc. Dungeons" },
            { "AbrassarDungeonsBosses", "Unknown Arena" },
            { "Berg", "Berg" },
            { "Emercar", "Enmerkar Forest" },
            { "Emercar_Dungeon1", "Royal Manticore’s Lair" },
            { "Emercar_Dungeon2", "Forest Hives" },
            { "Emercar_Dungeon3", "Cabal of Wind Temple" },
            { "Emercar_Dungeon4", "Face of the Ancients" },
            { "Emercar_Dungeon5", "Ancestor’s Resting Place" },
            { "Emercar_Dungeon6", "Necropolis" },
            { "EmercarDungeonsSmall", "Enmerkar Misc. Dungeons" },
            { "EmercarDungeonsBosses", "Unknown Arena" },
            { "DreamWorld", "In Between" },
        };

        // ****************** SKIP LOGOS HOOK ********************** //

        // Skip Logos hook
        public void StartupVideo_Start(On.StartupVideo.orig_Start orig, StartupVideo self)
        {
            //StoreManager.Experimental = false;
            StartupVideo.HasPlayedOnce = true;
            orig(self);
        }

        // ********************* QUEST HOOKS ********************* //

        private void SendQuestInteractionHook(On.SendQuestEventInteraction.orig_OnActivate orig, SendQuestEventInteraction self)
        {
            var _ref = At.GetValue(typeof(SendQuestEventInteraction), self, "m_questReference") as QuestEventReference;
            var _event = _ref.Event;
            var s = _ref.EventUID;

            if (_event != null && s != null)
            {
                LogQuestEvent(_event, -1);
            }

            orig(self);
        }

        private void SendQuestEventHook(On.NodeCanvas.Tasks.Actions.SendQuestEvent.orig_OnExecute orig, NodeCanvas.Tasks.Actions.SendQuestEvent self)
        {
            var _event = self.QuestEventRef.Event;
            var s = self.QuestEventRef.EventUID;

            if (_event != null && s != null)
            {
                LogQuestEvent(_event, self.StackAmount);
            }

            orig(self);
        }

        private void LogQuestEvent(QuestEventSignature _event, int stack = -1)
        {
            if (QuestDebugging)
            {
                Debug.LogWarning(
                "------ ADDING QUEST EVENT -------" +
                "\r\nName: " + _event.EventName +
                "\r\nDescription: " + _event.Description +
                (stack == -1 ? "" : "\r\nStack: " + stack) +
                "\r\n---------------------------");
            }
        }



        //private void QuestLoad(On.QuestEventDictionary.orig_Load orig)
        //{
        //    orig();

        //    Type t = typeof(QuestEventDictionary);
        //    FieldInfo fi = t.GetField("m_questEvents", BindingFlags.Static | BindingFlags.NonPublic);
        //    if (fi.GetValue(null) is Dictionary<string, QuestEventSignature> m_questEvents)
        //    {
        //        foreach (QuestEventSignature sig in m_questEvents.Values)
        //        {
        //            if (QuestEvents.ContainsKey(sig.EventName)) { continue; }
        //            QuestEvents.Add(sig.EventName, sig);
        //        }
        //    }
        //}
    }
}
