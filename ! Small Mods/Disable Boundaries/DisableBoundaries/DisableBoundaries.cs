using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Partiality.Modloader;
using UnityEngine;

namespace DisableBoundaries
{
    public class DisableBoundaries : PartialityMod
    {
        public GameObject obj;
        public string ID = "DisableBoundaries";
        public double version = 1.0;

        public static BoundaryScript Instance;

        public DisableBoundaries()
        {
            this.author = "Sinai";
            this.ModID = ID;
            this.Version = version.ToString("0.00");
        }

        public override void OnEnable()
        {
            base.OnEnable();

            obj = new GameObject(ID);
            GameObject.DontDestroyOnLoad(obj);

            Instance = obj.AddComponent<BoundaryScript>();
            Instance._base = this;
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }
    }

    public class BoundaryScript : MonoBehaviour
    {
        public DisableBoundaries _base;

        public string CurrentScene = "";
        public bool SceneChangeFlag = false;

        internal void Update()
        {
            if (CurrentScene != SceneManagerHelper.ActiveSceneName) { SceneChangeFlag = true; }

            if (Global.Lobby.PlayersInLobbyCount < 1 || NetworkLevelLoader.Instance.IsGameplayPaused)
            {
                return;
            }

            if (SceneChangeFlag)
            {
                SceneChangeFlag = false;
                CurrentScene = SceneManagerHelper.ActiveSceneName;

                SetBoundaryInactive();
            }
        }

        private void SetBoundaryInactive()
        {
            if (GameObject.Find("MapBounds") is GameObject Bounds)
            {
                Bounds.SetActive(false);
            }
        }
    }
}
