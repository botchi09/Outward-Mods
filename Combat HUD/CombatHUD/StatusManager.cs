using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using SinAPI;

namespace CombatHUD
{
    public class StatusInfo
    {
        public Character character;
        public int localID;

        public StatusEffectManager fxManager;
        public StatusEffectPanel fxPanel;
        public CharacterBarListener barManager;
    }

    public class StatusManager : MonoBehaviour
    {
        public CombatHudGlobal global;

        public List<StatusInfo> PlayerInfos = new List<StatusInfo>();

        public void UpdateStatus()
        {
            if (global.sceneChangeFlag || global.LocalPlayers.Count != PlayerInfos.Count)
            {
                PlayerInfos.Clear();

                bool flag = false;

                foreach (PlayerInfo c in global.LocalPlayers)
                {
                    PlayerInfos.Add(new StatusInfo { character = c.character, fxManager = c.character.StatusEffectMngr, localID = c.ID });
                }

                foreach (StatusEffectPanel panel in FindObjectsOfType<StatusEffectPanel>())
                {
                    if (PlayerInfos.Find(x => x.character.UID == (panel as UIElement).LocalCharacter.UID) is StatusInfo info)
                    {
                        info.fxPanel = panel;
                    }
                    else { flag = true; break; }
                }

                foreach (CharacterBarListener panel in FindObjectsOfType<CharacterBarListener>())
                {
                    if (PlayerInfos.Find(x => x.character.UID == (panel as UIElement).LocalCharacter.UID) is StatusInfo info)
                    {
                        info.barManager = panel;
                    }
                    else { flag = true; break; }
                }

                if (flag) { PlayerInfos.Clear(); }
            }
        }
    }

    public class StatusBuildup
    {
        public StatusEffect status;
        public float Time;
        public float Buildup;
    }
}
