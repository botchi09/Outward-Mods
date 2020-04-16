using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Partiality.Modloader;
using UnityEngine;

namespace ShowInvisibleWalls
{
    public class ShowInvisibleWalls : PartialityMod
    {
        public GameObject obj;
        public string ID = "ShowInvisWalls";
        public double version = 1.0;

        public static WallScript Instance;

        public ShowInvisibleWalls()
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

            Instance = obj.AddComponent<WallScript>();
            Instance._base = this;
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }
    }

    public class WallScript : MonoBehaviour
    {
        public ShowInvisibleWalls _base;

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
                RevealWalls();

                //foreach (PlayerSystem ps in Global.Lobby.PlayersInLobby.Where(x => x.IsLocalPlayer))
                //{

                //    ps.ControlledCharacter.CharacterCamera.OverrideTransform = ps.ControlledCharacter.Visuals.HeadTrans;
                //    DisableVisuals(ps.ControlledCharacter);
                //}
            }
        }

        private void RevealWalls()
        {
            foreach (GameObject obj in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                string s = obj.name.ToLower();
                if (s.Contains("cube") || s.Contains("collision") || s.Contains("collider") || s.Contains("bounds"))
                {
                    if (obj.GetComponent<MeshRenderer>())
                        DestroyImmediate(obj.GetComponent<MeshRenderer>());

                    obj.AddComponent<MeshRenderer>();
                }
            }
        }


        //private void DisableVisuals(Character c)
        //{
        //    c.Visuals.Head.gameObject.SetActive(false);
        //    c.Visuals.ActiveVisualsBody.gameObject.SetActive(false);
        //    c.Visuals.ActiveVisualsFoot.gameObject.SetActive(false);
        //    c.Visuals.ActiveVisualsHelmOrHead.gameObject.SetActive(false);
        //    if (c.Inventory.EquippedBag && c.Inventory.EquippedBag.LoadedVisual)
        //    {
        //        c.Inventory.EquippedBag.LoadedVisual.gameObject.SetActive(false);
        //    }
        //}

    }
}
