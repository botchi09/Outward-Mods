using Partiality.Modloader;
using System;
using System.IO;
using UnityEngine;
using System.Reflection;
using UnityEngine.UI;
using static CustomKeybindings;
using System.Collections.Generic;
using System.Linq;

// ************************************************************ //

    // ALL CREDIT TO FAEDAR (Skully250) AND ASHNAL FOR THE ORIGINAL MOD

    // FIXED BY SINAI FOR OCTOBER 2019 PATCH

// ************************************************************ //

namespace MPLimitRemover
{
    public class Settings
    {
        public int PlayerLimit = 4;

        public bool Show_Menu_On_Startup = true;
        public bool Enable_Menu_Scaling = false;
    }

    public class ModBase : PartialityMod
    {
        public string ID = "MP Limit Remover";
        public double version = 2.0;

        public ModBase()
        {
            this.ModID = ID;
            this.Version = version.ToString("0.00");
            this.author = "Sinai";
        }

        public static PlayerLimitRemover limitRemover;

        public override void OnEnable()
        {
            base.OnEnable();

            GameObject obj = new GameObject();
            GameObject.DontDestroyOnLoad(obj);
            limitRemover = obj.AddComponent(new PlayerLimitRemover() { _base = this });
            limitRemover.Initialise();
        }
    }

    public class PlayerLimitRemover : MonoBehaviour
    {
        public ModBase _base;
        public PlayerLimitGUI gui;
        public Settings settings = new Settings();
        public static string savePath = @"Mods\MPLimitRemover.json";

        public string MenuKey = "MP Limit Menu";

        public void Initialise()
        {
            LoadSettings();

            AddAction(MenuKey, KeybindingsCategory.Menus, ControlType.Both, 5, InputActionType.Button);

            gui = gameObject.AddComponent(new PlayerLimitGUI() { global = this, showGui = settings.Show_Menu_On_Startup });

            // fix pause menu
            On.PauseMenu.Show += new On.PauseMenu.hook_Show(ShowPatch);
            On.PauseMenu.Update += new On.PauseMenu.hook_Update(UpdatePatch);
        }

        internal void Update()
        {
            if (Global.Lobby.PlayersInLobbyCount < 1 || NetworkLevelLoader.Instance.IsGameplayPaused)
            {
                return;
            }

            // handle player input 
            foreach (PlayerSystem ps in Global.Lobby.PlayersInLobby.Where(x => x.ControlledCharacter.IsLocalPlayer))
            {
                if (m_playerInputManager[ps.PlayerID].GetButtonDown(MenuKey))
                {
                    gui.showGui = !gui.showGui;
                }
            }

            // handle custom multiplayer limit 
            if (!PhotonNetwork.offlineMode && PhotonNetwork.isMasterClient)
            {
                // if the room limit is not set to our custom value, do that.
                if (PhotonNetwork.room.maxPlayers != settings.PlayerLimit)
                {
                    //OLogger.Warning("Room is set to " + PhotonNetwork.room.maxPlayers + "! Setting to " + settings.Multiplayer_Limit);
                    PhotonNetwork.room.maxPlayers = settings.PlayerLimit;
                }

                // handle logic for opening / closing room based on custom limit.
                if (!PhotonNetwork.room.open && PhotonNetwork.room.playerCount < settings.PlayerLimit)
                {
                    //OLogger.Warning("Room is closed, but it should be open! Opening room.");
                    PhotonNetwork.room.open = true;
                }
                else if (PhotonNetwork.room.open && PhotonNetwork.room.playerCount >= settings.PlayerLimit)
                {
                    //OLogger.Warning("Room is open, but it should be closed! Closing room.");
                    PhotonNetwork.room.open = false;
                }
            }
        }

        // fix pause menu 1
        public static void ShowPatch(On.PauseMenu.orig_Show orig, PauseMenu self)
        {
            orig(self);
            Button onlineButton = typeof(PauseMenu).GetField("m_btnToggleNetwork", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(self) as Button;

            //Due to spawning bugs, only allow disconnect if you are the master, or if you are a client with no splitscreen, force splitscreen to quit before disconnect
            if (PhotonNetwork.isMasterClient || SplitScreenManager.Instance.LocalPlayerCount == 1)
            {
                onlineButton.interactable = true;
            }

            SetSplitButtonInteractable(self);

            //If this is used with a second splitscreen player both players load in missing inventory. Very BAD. Disabled for now.
            //Button findMatchButton = typeof(PauseMenu).GetField("m_btnFindMatch", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance) as Button;
            //findMatchButton.interactable = PhotonNetwork.offlineMode;
        }

        // fix pause menu 2
        //for some reason the update function also forces the split button interactable, so we have to override it here too
        public static void UpdatePatch(On.PauseMenu.orig_Update orignal, PauseMenu instance)
        {
            orignal(instance);
            SetSplitButtonInteractable(instance);
        }

        public static void SetSplitButtonInteractable(PauseMenu instance)
        {
            //Debug.Log("isMasterClient: " + PhotonNetwork.isMasterClient);
            Button splitButton = typeof(PauseMenu).GetField("m_btnSplit", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance) as Button;
            if (!PhotonNetwork.isMasterClient || !PhotonNetwork.isNonMasterClientInRoom)
            {
                splitButton.interactable = true;
            }
        }

        // =========== settings =============

        private void LoadSettings()
        {
            if (!Directory.Exists(@"Mods")) { Directory.CreateDirectory("Mods"); }

            bool newSettings = true;
            if (File.Exists(savePath))
            {
                string json = File.ReadAllText(savePath);
                if (JsonUtility.FromJson<Settings>(json) is Settings s2)
                {
                    settings = s2;
                    newSettings = false;
                }
            }
            if (newSettings)
            {
                SaveSettings();
            }
        }

        private void SaveSettings()
        {
            if (File.Exists(savePath)) { File.Delete(savePath); }
            File.WriteAllText(savePath, JsonUtility.ToJson(settings, true));
        }

        internal void OnDisable()
        {
            SaveSettings();
        }
    }
}
