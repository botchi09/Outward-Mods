using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
//using SinAPI;

namespace CombatHUD
{
    public class TargetInfo
    {
        public PlayerInfo playerInfo;
        public Character lockedCharacter;
        public Vector3 UIBarPos;
    }

    public class TargetManager : MonoBehaviour
    {
        public CombatHudGlobal global;

        public List<TargetInfo> PlayerLockInfos = new List<TargetInfo>();

        public void UpdateTarget()
        {
            if (global.sceneChangeFlag || global.LocalPlayers.Count != PlayerLockInfos.Count)
            {
                // Debug.Log("resetting target info");

                PlayerLockInfos.Clear();

                foreach (PlayerInfo c in global.LocalPlayers)
                {
                    //Debug.Log("adding target info for " + c.character.Name);
                    PlayerLockInfos.Add(new TargetInfo { playerInfo = c });
                }
            }

            foreach (TargetInfo player in PlayerLockInfos)
            {
                try
                {
                    Character c = player.playerInfo.character;
                    if (c.TargetingSystem.Locked)
                    {
                        player.lockedCharacter = c.TargetingSystem.LockedCharacter;
                        Vector3 pos = c.TargetingSystem.LockedCharacter.UIBarPosition;
                        player.UIBarPos = c.CharacterCamera.CameraScript.WorldToScreenPoint(pos);
                    }
                    else
                    {
                        player.lockedCharacter = null;
                        player.UIBarPos = Vector3.zero;
                    }
                }
                catch // (Exception ex)
                { 
                    //OLogger.Error("Combat HUD TargetManager Update: " + ex.Message + " | " + ex.StackTrace); 
                }
            }
        }
    }
}
