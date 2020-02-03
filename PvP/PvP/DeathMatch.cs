using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using SinAPI;

namespace PvP
{
    public class DeathMatch : MonoBehaviour
    {
        public PvPGlobal global;

        public void UpdateDM()
        {
            List<Character.Factions> teamsLeft = global.playerManager.GetRemainingTeams();
            if (teamsLeft.Count == 1)
            {
                // winner
                string winner = teamsLeft[0].ToString();
                if (winner.EndsWith("s")) { winner = winner.Substring(0, winner.Length - 1); }
                global.StopGameplay(winner + "s have won!");
            }
            else if (teamsLeft.Count <= 0)
            {
                // ???????
                global.StopGameplay("Game ended because there are no active teams!");
            }
        }
    }
}
