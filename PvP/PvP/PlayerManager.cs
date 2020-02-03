using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
//using SinAPI;

namespace PvP
{
    public class PlayerManager : MonoBehaviour
    {
        public PvPGlobal global;

        //public List<Character> PlayerCharacters = new List<Character>();

        public List<Character.Factions> AllFactions;

        public void Init()
        {
            AllFactions = new List<Character.Factions>();
            for (int i = 0; i < (int)Character.Factions.COUNT; i++)
            {
                AllFactions.Add((Character.Factions)i);
            }

            // FIX TRAPS
            On.TrapTrigger.OnTriggerEnter += TrapTriggerHook;
            On.DeployableTrap.ProcessEffect += TrapProcessHook;
        }

        private void TrapProcessHook(On.DeployableTrap.orig_ProcessEffect orig, DeployableTrap self, Effect _effect)
        {
            if (_effect is Shooter shooter && global.CurrentGame == PvPGlobal.GameModes.BattleRoyale)
            {
                shooter.Setup(AllFactions.ToArray(), (!(self.FX_HolderTrans != null)) ? self.transform : self.FX_HolderTrans);
            }
            else
            {
                orig(self, _effect);
            }
        }

        private void TrapTriggerHook(On.TrapTrigger.orig_OnTriggerEnter orig, TrapTrigger self, Collider _other)
        {
            if (_other == null)
            {
                return;
            }

            if (global.CurrentGame != PvPGlobal.GameModes.NONE)
            {
                if (self.GetComponentInParent<DeployableTrap>() is DeployableTrap trap)
                {
                    //Debug.Log("setting trap factions");
                    At.SetValue(AllFactions.ToArray(), typeof(DeployableTrap), trap, "m_targetFactions");
                }

                var m_charactersInTrigger = At.GetValue(typeof(TrapTrigger), self, "m_charactersInTrigger") as List<Character>;

                Character component = _other.GetComponent<Character>();

                if (component != null && !m_charactersInTrigger.Contains(component))
                {
                    m_charactersInTrigger.Add(component);
                    At.SetValue(m_charactersInTrigger, typeof(TrapTrigger), self, "m_charactersInTrigger");
                    if (!(bool)At.GetValue(typeof(TrapTrigger), self, "m_alreadyTriggered"))
                    {
                        (self as TriggerColliderFlag).Trigger.ActivateBasicAction(component, self.OnEnterState - TrapTrigger.ToggleState.Off);
                        //OLogger.Warning("Triggered custom trap!");
                    }
                    At.SetValue(true, typeof(TrapTrigger), self, "m_alreadyTriggered");
                }
            }
            else
            {
                orig(self, _other);
            }
        }

        //internal void Update()
        //{
        //    if (Global.Lobby.PlayersInLobbyCount != PlayerCharacters.Count() && !NetworkLevelLoader.Instance.IsGameplayPaused)
        //    {
        //        PlayerCharacters.Clear();

        //        foreach (PlayerSystem ps in Global.Lobby.PlayersInLobby)
        //        {
        //            PlayerCharacters.Add(ps.ControlledCharacter);
        //        }
        //    }
        //}

        public void ChangeFactions(Character c, Character.Factions faction)
        {
            if (!PhotonNetwork.offlineMode)
            { // int factionInt, string UID, bool alliedToSame = true
                global.photonView.RPC("SendChangeFactionsRPC", PhotonTargets.All, new object[] { (int)faction, c.UID.ToString(), true });
            }
            else
            {
                global.SendChangeFactionsRPC((int)faction, c.UID.ToString());
            }
        }

        public List<Character.Factions> GetRemainingTeams()
        {
            List<Character.Factions> remainingTeams = new List<Character.Factions>();
            foreach (KeyValuePair<Character.Factions, List<PlayerSystem>> entry in global.CurrentPlayers)
            {
                bool anyAlive = false;
                foreach (PlayerSystem ps in entry.Value)
                {
                    if (ps.ControlledCharacter != null && !ps.ControlledCharacter.IsDead)
                    {
                        anyAlive = true;
                        break;
                    }
                }
                if (anyAlive)
                    remainingTeams.Add(entry.Key);
            }

            return remainingTeams;
        }

        public List<Character> GetRemainingPlayers()
        {
            List<Character> remainingPlayers = new List<Character>();

            foreach (KeyValuePair<Character.Factions, List<PlayerSystem>> entry in global.CurrentPlayers)
            {
                foreach (PlayerSystem ps in global.CurrentPlayers[entry.Key])
                {
                    if (ps.ControlledCharacter != null && !ps.ControlledCharacter.IsDead)
                        remainingPlayers.Add(ps.ControlledCharacter);
                }
            }

            return remainingPlayers;
        }
    }
}
