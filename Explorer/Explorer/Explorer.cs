using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;

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
        private void Application_logMessageReceived(string condition, string stackTrace, LogType type)
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
                if (condition.ToLower().Contains(s.ToLower()))
                {
                    return;
                }
            }

            if (type == LogType.Exception)
            {
                OLogger.Error(condition + "\r\nStack Trace: " + stackTrace);
            }
            else if (type == LogType.Warning)
            {
                OLogger.Warning(condition);
            }
            else
            {
                OLogger.Log(condition);
            }
        }

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
