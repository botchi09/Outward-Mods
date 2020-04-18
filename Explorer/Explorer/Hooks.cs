using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;


namespace Explorer
{
    public class Hooks
    {
        public static void InitHooks()
        {
            //// mouse control
            //On.Global.Update += Hooks.Global_Update;

            // debug quest events
            On.SendQuestEventInteraction.OnActivate += Hooks.SendQuestInteractionHook;
            On.NodeCanvas.Tasks.Actions.SendQuestEvent.OnExecute += Hooks.SendQuestEventHook;

            // fix area names on teleport menu
            On.DT_CharacterCheats.InitAreaSwitches += Hooks.DT_CharacterCheats_InitAreaSwitches;

            // Skip Logos hook
            On.StartupVideo.Start += Hooks.StartupVideo_Start;
        }

        // ** MOUSE CONTROL ** //

        public static void Global_Update(On.Global.orig_Update orig, Global self)
        {
            if (WindowManager.ShowWindows && Input.GetKey(KeyCode.LeftAlt))
            {
                if (Cursor.lockState != CursorLockMode.None)
                {
                    Cursor.lockState = CursorLockMode.None;
                }
                if (!Cursor.visible)
                {
                    Cursor.visible = true;
                }
            }

            bool flag = false;
            for (int i = 0; i < SplitScreenManager.Instance.LocalPlayers.Count; i++)
            {
                if (SplitScreenManager.Instance.LocalPlayers[i].CharUI && SplitScreenManager.Instance.LocalPlayers[i].CharUI.IsInputFieldFocused)
                {
                    flag = false;
                    break;
                }
            }
            if (Global.CheatsEnabled)
            {
                bool flag2 = !flag && Input.GetKeyDown(KeyCode.Keypad1);
                flag2 |= Input.GetMouseButtonDown(4);
                if (flag2)
                {
                    Time.timeScale = ((Time.timeScale != 1f) ? 1f : 0.2f);
                    Time.fixedDeltaTime = 0.022f * Time.timeScale;
                }
            }
            if (Global.CheatsEnabled && !flag && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.H))
            {
                bool flag3 = true;
                for (int j = 0; j < SplitScreenManager.Instance.LocalPlayers.Count; j++)
                {
                    if (SplitScreenManager.Instance.LocalPlayers[j].CharUI && SplitScreenManager.Instance.LocalPlayers[j].CharUI.IsInputFieldFocused)
                    {
                        flag3 = false;
                        break;
                    }
                }
                if (flag3)
                {
                    bool hidden = (bool)At.GetValue(typeof(Global), Global.Instance, "m_hideUI");
                    At.SetValue(!hidden, typeof(Global), Global.Instance, "m_hideUI");
                }
            }
        }

        // *************** FIX DEBUG AREA SWITCH NAMES *********** //

        public static void DT_CharacterCheats_InitAreaSwitches(On.DT_CharacterCheats.orig_InitAreaSwitches orig, DT_CharacterCheats self)
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
                _event.callback.AddListener(delegate (BaseEventData data) { OnAreaSelected(dd); });

                trigger.triggers.Add(_event);
            }
        }

        private static void OnAreaSelected(Dropdown dd)
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
        public static void StartupVideo_Start(On.StartupVideo.orig_Start orig, StartupVideo self)
        {
            //StoreManager.Experimental = false;
            StartupVideo.HasPlayedOnce = true;
            orig(self);
        }

        // ********************* QUEST HOOKS ********************* //

        public static void SendQuestInteractionHook(On.SendQuestEventInteraction.orig_OnActivate orig, SendQuestEventInteraction self)
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

        public static void SendQuestEventHook(On.NodeCanvas.Tasks.Actions.SendQuestEvent.orig_OnExecute orig, NodeCanvas.Tasks.Actions.SendQuestEvent self)
        {
            var _event = self.QuestEventRef.Event;
            //var s = self.QuestEventRef.EventUID;

            if (_event != null)
            {
                LogQuestEvent(_event, self.StackAmount);
            }

            orig(self);
        }

        public static void LogQuestEvent(QuestEventSignature _event, int stack = -1)
        {
            if (Explorer.QuestDebugging)
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
