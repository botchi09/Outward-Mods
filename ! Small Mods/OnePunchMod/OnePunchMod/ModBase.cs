using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Partiality.Modloader;

namespace OnePunchMod
{
    public class ModBase : PartialityMod
    {
        public GameObject _obj = null;
        public OnePunchMod script;
        public double version = 1.00;

        public ModBase()
        {
            this.ModID = "One Punch Mod";
            this.Version = version.ToString("0.00");
            this.author = "Sinai";
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (_obj == null)
            {
                _obj = new GameObject("OnePunchMod");
                GameObject.DontDestroyOnLoad(_obj);
            }

            script = _obj.AddComponent<OnePunchMod>();
            script._base = this;
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }
    }

    public class OnePunchMod : MonoBehaviour
    {
        public ModBase _base;

        private int LastPlayerCount;

        internal void Update()
        {
            if (Global.Lobby.PlayersInLobbyCount <= 0)
            {
                return;
            }

            if (LastPlayerCount != Global.Lobby.PlayersInLobbyCount)
            {
                LastPlayerCount = Global.Lobby.PlayersInLobbyCount;

                foreach (PlayerSystem ps in Global.Lobby.PlayersInLobby)
                {
                    if (ps.ControlledCharacter.Visuals.UnarmedHitDetector != null)
                    {
                        ps.ControlledCharacter.Visuals.UnarmedHitDetector.Damage = 99999;
                        ps.ControlledCharacter.Visuals.UnarmedHitDetector.Impact = 99999;
                    }
                    else
                    {
                        LastPlayerCount = -1;
                    }
                }
            }
        }
    }
}
