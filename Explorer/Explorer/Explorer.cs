using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using BepInEx;
using System.IO;
using System.Reflection;

namespace Explorer
{
    [BepInPlugin(ID, NAME, VERSION)]
    //[BepInDependency("com.sinai.PartialityWrapper", BepInDependency.DependencyFlags.HardDependency)]
    public class Explorer : BaseUnityPlugin
    {
        public static Explorer Instance;

        const string ID = "com.sinai.explorer";
        const string NAME = "Explorer";
        const string VERSION = "2.0";

        public static bool QuestDebugging { get; set; } = true;

        internal void Awake()
        {
            Instance = this;

            LoadMCS();

            this.gameObject.AddComponent<WindowManager>();
            this.gameObject.AddComponent<MainMenu>();

            // log to game window
            Application.logMessageReceived += Application_logMessageReceived;

            // init debugging hooks
            Hooks.InitHooks();
        }

        private void LoadMCS()
        {
            var mcsPath = Path.GetDirectoryName(Info.Location) + @"\mcs.dll";
            if (File.Exists(mcsPath))
            {
                Assembly.LoadFile(mcsPath);
                Logger.LogMessage("Loaded mcs.dll");
            }
            else
            {
                Logger.LogError("Could not find mcs.dll!");
            }
        }

        internal void Start()
        {
            // create logger
            var m_logger = new Vector2(600, 260);
            OLogger.CreateLog(new Rect(Screen.width - m_logger.x - 5, Screen.height - m_logger.y - 5, m_logger.x, m_logger.y));            

            // done init
            WindowManager.ShowWindows = true;
            OLogger.Log("Initialised Explorer. Unity version: " + Application.unityVersion.ToString());
        }

        internal void Update()
        {
            if (Input.GetKeyDown(KeyCode.F7))
            {
                WindowManager.ShowWindows = !WindowManager.ShowWindows;
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

        // logs i want to ignore
        private static readonly string[] blacklist = new string[]
        {
            "Internal: JobTempAlloc",
            "GUI Error:",
            "BoxColliders does not support negative scale or size",
            "is registered with more than one LODGroup",
            "only 0 controls when doing Repaint",
            "it is not close enough to the NavMesh",
            "Start Node:",
        };

        // Log Debug messages to OLogger window
        private void Application_logMessageReceived(string message, string stackTrace, LogType type)
        {
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
    }
}
