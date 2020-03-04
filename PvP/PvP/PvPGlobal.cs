using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Partiality.Modloader;
using UnityEngine;
using System.Reflection;
using UnityEngine.UI;
//using SinAPI;
using System.IO;
using static CustomKeybindings;

namespace PvP
{
    // PARTIALITY LOADER
    public class ModLoader : PartialityMod
    {
        public GameObject obj;
        public string ID = "PvP";
        public double version = 1.3;

        public static PvPGlobal Instance;

        public ModLoader()
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

            Instance = obj.AddComponent<PvPGlobal>();
            Instance._base = this;
            Instance.Init();
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }
    }

    //SETTINGS
    public class Settings
    {
        public int Multiplayer_Limit = 2;

        public bool Enable_Menu_Scaling = false;
        public bool Show_Menu_On_Startup = true;
    }

    // PVP GLOBAL SCRIPT
    public class PvPGlobal : Photon.MonoBehaviour
    {
        public ModLoader _base;
        public Settings settings = new Settings();
        private static readonly string savePath = @"Mods\PvP.json";

        public PvPGUI gui;
        public PlayerManager playerManager;
        public BattleRoyale BRManager;
        public DeathMatch DMManager;

        public string MenuKey = "PvP Menu";

        public GameModes CurrentGame = GameModes.NONE;
        public float GameStartTime = 0f;
        public Dictionary<Character.Factions, List<PlayerSystem>> CurrentPlayers = new Dictionary<Character.Factions, List<PlayerSystem>>();

        public enum GameModes
        {
            NONE,
            Deathmatch,
            BattleRoyale
        }

        public void Init()
        {
            LoadSettings();

            gui = gameObject.AddComponent(new PvPGUI() { global = this, showGui = settings.Show_Menu_On_Startup });
            playerManager = gameObject.AddComponent(new PlayerManager() { global = this });
            playerManager.Init();
            BRManager = gameObject.AddComponent(new BattleRoyale { global = this });
            DMManager = gameObject.AddComponent(new DeathMatch { global = this });

            AddAction(MenuKey, KeybindingsCategory.Menus, ControlType.Both, 5, InputActionType.Button);

            //// Multiplayer hooks
            //On.PauseMenu.Show += ShowPatch;
            //On.PauseMenu.Update += UpdatePatch;

            // Custom Gameplay hooks
            On.InteractionRevive.OnActivate += DisableReviveInteractionHook;
            On.InteractionRevive.ProcessText += ReviveTextHook;
        }

        // =================== MASTER UPDATE ========================

        internal void Update()
        {
            if ((MenuManager.Instance.IsReturningToMainMenu || Global.IsApplicationClosing) && CurrentGame != GameModes.NONE)
            {
                CurrentGame = GameModes.NONE;
                if (!PhotonNetwork.isNonMasterClientInRoom)
                {
                    StopGameplay("The host has left the game!");
                }
            }

            // make sure game is running
            if (Global.Lobby.PlayersInLobbyCount < 1 || NetworkLevelLoader.Instance.IsGameplayPaused) { return; }

            // setup PhotonView (add or remove when needed)
            if (PhotonNetwork.inRoom && photonView == null)
            {
                gameObject.AddComponent(new PhotonView() { viewID = 994 });
            }

            // handle custom multiplayer limit 
            if (!PhotonNetwork.offlineMode && PhotonNetwork.isMasterClient)
            {
                // if the room limit is not set to our custom value, do that.
                if (PhotonNetwork.room.maxPlayers != settings.Multiplayer_Limit)
                {
                    PhotonNetwork.room.maxPlayers = settings.Multiplayer_Limit;
                }

                // handle logic for opening / closing room based on custom limit.
                if (!PhotonNetwork.room.open && PhotonNetwork.room.playerCount < settings.Multiplayer_Limit)
                {
                    PhotonNetwork.room.open = true;
                }
                else if (PhotonNetwork.room.open && PhotonNetwork.room.playerCount >= settings.Multiplayer_Limit)
                {
                    PhotonNetwork.room.open = false;
                }
            }

            // handle player input 
            foreach (PlayerSystem ps in Global.Lobby.PlayersInLobby.Where(x => x.ControlledCharacter.IsLocalPlayer))
            {
                if (m_playerInputManager[ps.PlayerID].GetButtonDown(MenuKey))
                {
                    gui.showGui = !gui.showGui;
                }
            }

            // update custom gameplay
            if (CurrentGame != GameModes.NONE)
            {
                UpdateGameplay();
            }
        }

        // ========================== GENERAL GAMEPLAY CONTROL =========================

        public void StartGameplay(int _mode, string messageToPlayers = "")
        {
            CurrentGame = (GameModes)_mode;

            if (!PhotonNetwork.offlineMode)
            {
                photonView.RPC("StartGameplayRPC", PhotonTargets.All, new object[] { _mode, messageToPlayers });
            }
            else
            {
                StartGameplayRPC(_mode, messageToPlayers);
            }
        }

        [PunRPC]
        public void StartGameplayRPC(int _mode, string messageToPlayers = "")
        {
            if (gui.showGui) { gui.showGui = false; }

            if (_mode == (int)GameModes.BattleRoyale)
            {
                // actual moment that gameplay starts for the players
                BRManager.IsGameplayStarting = false;
                BRManager.LastSupplyDropTime = -1;
                BRManager.LastEnemySpawnTime = Time.time;
                BRManager.SupplyDropCounter = 0;
                BRManager.ActiveItemContainers.Clear();
                BRManager.ActiveBeamObjects.Clear();
            }

            // get the current teams to a list. send message to local players.
            CurrentPlayers.Clear();
            foreach (PlayerSystem ps in Global.Lobby.PlayersInLobby)
            {
                if (CurrentPlayers.ContainsKey(ps.ControlledCharacter.Faction))
                {
                    CurrentPlayers[ps.ControlledCharacter.Faction].Add(ps);
                }
                else
                {
                    CurrentPlayers.Add(ps.ControlledCharacter.Faction, new List<PlayerSystem> { ps });
                }

                if (ps.ControlledCharacter.IsLocalPlayer)
                {
                    SendUIMessageLocal(ps.ControlledCharacter, messageToPlayers);
                }
            }

            CurrentGame = (GameModes)_mode;
            GameStartTime = Time.time;
        }

        // the game-mode specific Update functions, called directly instead of MonoBehaviour.Update()
        private void UpdateGameplay()
        {
            if (CurrentGame == GameModes.BattleRoyale)
            {
                if (!BRManager.IsGameplayStarting)
                {
                    BRManager.UpdateBR();
                }
            }
            else if (CurrentGame == GameModes.Deathmatch)
            {
                DMManager.UpdateDM();
            }
        }

        // only the host can call this whenever they want, other players may trigger it when they are the winning player.
        public void StopGameplay(string messageToPlayers = "")
        {
            if (!PhotonNetwork.offlineMode)
            {
                photonView.RPC("StopGameplayRPC", PhotonTargets.All, new object[] { messageToPlayers });
            }
            else
            {
                StopGameplayRPC(messageToPlayers);
            }
        }

        [PunRPC]
        private void StopGameplayRPC(string messageToPlayers = "")
        {
            // custom fix for Battle Royale when game ends
            if (CurrentGame == GameModes.BattleRoyale)
            {
                //OLogger.Warning("Todo restore things for BR on game end?");
                BRManager.EndBattleRoyale();
            }

            CurrentGame = GameModes.NONE;

            var list = Global.Lobby.PlayersInLobby;
            if (PhotonNetwork.isNonMasterClientInRoom)
            {
                list = list.Where(x => x.ControlledCharacter != null && x.ControlledCharacter.IsLocalPlayer).ToList();
            }

            foreach (PlayerSystem ps in list)
            {
                if (messageToPlayers != "" && ps.ControlledCharacter != null && ps.ControlledCharacter.IsLocalPlayer)
                {
                    SendUIMessageLocal(ps.ControlledCharacter, messageToPlayers);
                }

                if (!PhotonNetwork.isNonMasterClientInRoom && ps.ControlledCharacter != null && ps.ControlledCharacter.IsDead)
                {
                    SendResurrect(ps.ControlledCharacter);
                }
            }
        }

        // ================= BATTLE ROYALE RPC =======================

        // RPC calls for the Battle Royale mode. Putting it in here so there is only 1 photonView class for the mod.

        [PunRPC]
        public void RPCStartBattleRoyale(bool skipLoad = false)
        {
            if (SceneManagerHelper.ActiveSceneName == "Monsoon") { skipLoad = true; }

            BRManager.IsGameplayStarting = true;
            BRManager.ForceNoSaves = true;
            CurrentGame = GameModes.BattleRoyale;

            if (!skipLoad)
            {
                foreach (PlayerSystem ps in Global.Lobby.PlayersInLobby.Where(x => x.ControlledCharacter.IsLocalPlayer))
                {
                    Character c = ps.ControlledCharacter;
                    CharacterManager.Instance.RequestAreaSwitch(c, AreaManager.Instance.GetAreaFromSceneName("Monsoon"), 0, 0, 1.5f, "Battle Royale!");
                }
            }

            if (Global.CheatsEnabled)
            {
                //OLogger.Warning("Disabling cheats!");
                BRManager.WasCheatsEnabled = true;
                Global.CheatsEnabled = false;

                foreach (PlayerSystem ps in Global.Lobby.PlayersInLobby.Where(x => x.ControlledCharacter.IsLocalPlayer))
                {
                    Character c = ps.ControlledCharacter;
                    (c.CharacterControl as LocalCharacterControl).MovementMultiplier = 1f;
                }
            }

            StartCoroutine(BRManager.SetupAfterSceneLoad(skipLoad));
        }

        [PunRPC]
        public void SendSpawnEnemyRPC(string uid, float x, float y, float z)
        {
            if (BRManager.EnemyCharacters.Find(w => w.UID == uid) is Character c)
            {
                c.gameObject.SetActive(true);

                c.Teleport(new Vector3(x, y, z), c.transform.rotation);                

                // HIGHLY SPECIFIC TO MONSOON
                int value = 50;
                if (c.Name.ToLower().Contains("butcher"))
                {
                    value = 200;
                }
                else if (c.name.ToLower().Contains("illuminator"))
                {
                    value = 20;
                }
                At.SetValue(new Stat(value), typeof(CharacterStats), c.Stats, "m_maxHealthStat");

                //BRManager.FixEnemyStats(c.Stats);

                //foreach (PlayerSystem ps in Global.Lobby.PlayersInLobby.Where(w => w.IsLocalPlayer))
                //{
                //    SendUIMessageLocal(ps.ControlledCharacter, c.Name + " has spawned!");
                //}

                //BRManager.EnemyCharacters.Remove(c);
            }
        }

        [PunRPC]
        public void EndBattleRoyaleRPC()
        {
            MenuManager.Instance.BackToMainMenu();
            //foreach (PlayerSystem ps in Global.Lobby.PlayersInLobby.Where(x => x.ControlledCharacter.IsLocalPlayer))
            //{
            //    Character c = ps.ControlledCharacter;
            //    CharacterManager.Instance.RequestAreaSwitch(c, AreaManager.Instance.GetAreaFromSceneName(previousScene), 0, 0, 1.5f, "");
            //}
        }

        [PunRPC]
        public void RPCSendSupplyDrop(string itemUID, float x, float y, float z)
        {
            StartCoroutine(BRManager.SupplyDropLocalCoroutine(itemUID, new Vector3(x, y, z)));
        }

        [PunRPC]
        public void RPCSendCleanup()
        {
            BRManager.CleanupSupplyObjects();
        }

        [PunRPC]
        public void RPCGenerateStash(int itemID, string UID, float x, float y, float z)
        {
            TreasureChest chest = ItemManager.Instance.GenerateItemNetwork(itemID).GetComponent<TreasureChest>();
            chest.UID = UID;
            chest.SaveType = Item.SaveTypes.Savable;
            chest.transform.position = new Vector3(x, y, z);
            BRManager.ActiveItemContainers.Add(chest.gameObject);
        }

        // ======================= SMALL GAMEPLAY FUNCTIONS ============================== //

        
        public void SendMessageToAll(string message)
        {
            if (PhotonNetwork.offlineMode)
            {
                SendUIMessageLocal(CharacterManager.Instance.GetFirstLocalCharacter(), message);
            }
            else
            {
                photonView.RPC("SendMessageToAllRPC", PhotonTargets.All, new object[] { message });
            }
        }

        [PunRPC]
        public void SendMessageToAllRPC(string message)
        {
            foreach (PlayerSystem ps in Global.Lobby.PlayersInLobby.Where(x => x.ControlledCharacter.IsLocalPlayer))
            {
                SendUIMessageLocal(ps.ControlledCharacter, message);
            }
        }

        // Send UI message - should only be sent to local players.

        public void SendUIMessageLocal(Character c, string message)
        {
            c.CharacterUI.NotificationPanel.ShowNotification(message, 5);
        }

        // SendChangeFactions, and will also fix targeting system (to NOT target own faction)

        [PunRPC]
        public void SendChangeFactionsRPC(int factionInt, string UID, bool alliedToSame = true)
        {
            if (CharacterManager.Instance.GetCharacter(UID) is Character c)
            {
                var faction = (Character.Factions)factionInt;
                c.Faction = faction;
                c.DetectabilityEmitter.Faction = faction;
                //c.BroadcastMessage("ReprocessEffects", SendMessageOptions.DontRequireReceiver);

                var list = playerManager.AllFactions.Where(x => (int)x != (int)faction).ToList();
                c.TargetingSystem.TargetableFactions = list.ToArray();

                if (!alliedToSame)
                {
                    c.TargetingSystem.AlliedToSameFaction = false;
                }
                else
                {
                    c.TargetingSystem.AlliedToSameFaction = true;
                }
            }
        }

        // resurrect 

        public void SendResurrect(Character _character)
        {
            if (!PhotonNetwork.offlineMode)
            {
                _character.photonView.RPC("SendResurrect", PhotonTargets.All, new object[]
                {
                    true,
                    string.Empty,
                    true
                });
                //photonView.RPC("SendResurrectRPC", PhotonTargets.All, new object[] { _character.UID.ToString() });
            }
            else
            {
                _character.Resurrect();
            }
        }

        // ================================== HOOKS ===================================

        private void DisableReviveInteractionHook(On.InteractionRevive.orig_OnActivate orig, InteractionRevive self)
        {
            if (CurrentGame == GameModes.NONE)
            {
                orig(self);
            }
            else
            {
                if (At.GetValue(typeof(InteractionBase), self as InteractionBase, "m_lastCharacter") is Character m_lastCharacter && m_lastCharacter.IsLocalPlayer)
                {
                    SendUIMessageLocal(m_lastCharacter, "You cannot revive players during a game!");
                    StartCoroutine(FixReviveInteraction(self, self.OnActivationEvent));
                    self.OnActivationEvent = null;
                }
            }
        }

        private IEnumerator FixReviveInteraction(InteractionBase _base, UnityEngine.Events.UnityAction action)
        {
            yield return new WaitForSeconds(0.1f);

            _base.OnActivationEvent = action;
        }

        private string ReviveTextHook(On.InteractionRevive.orig_ProcessText orig, InteractionRevive self, string _text)
        {
            if (CurrentGame != GameModes.NONE)
            {
                return "";
            }
            else
            {
                return orig(self, _text);
            }
        }


        // ============= settings ==============

        private void LoadSettings()
        {
            if (File.Exists(savePath))
            {
                if (JsonUtility.FromJson<Settings>(File.ReadAllText(savePath)) is Settings s2)
                {
                    settings = s2;
                }
            }
            else
            {
                SaveSettings();
            }
        }

        internal void OnDisable()
        {
            SaveSettings();
        }

        private void SaveSettings()
        {
            if (!Directory.Exists("Mods")) { Directory.CreateDirectory("Mods"); }

            if (File.Exists(savePath)) { File.Delete(savePath); }

            File.WriteAllText(savePath, JsonUtility.ToJson(settings, true));
        }
    }
}