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
        public static ModLoader Instance;

        public GameObject obj;
        public string ID = "PvP";
        public double version = 1.3;

        public ModLoader()
        {
            this.author = "Sinai";
            this.ModID = ID;
            this.Version = version.ToString("0.00");
        }

        public override void OnEnable()
        {
            Instance = this;

            base.OnEnable();

            obj = new GameObject(ID);
            GameObject.DontDestroyOnLoad(obj);

            obj.AddComponent<PvPGlobal>();
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }
    }

    //SETTINGS
    public class Settings
    {
        public bool Enable_Menu_Scaling = false;
        public bool Show_Menu_On_Startup = true;
    }

    // PVP GLOBAL SCRIPT
    public class PvPGlobal : Photon.MonoBehaviour
    {
        public static PvPGlobal Instance;

        public Settings settings = new Settings();
        private static readonly string savePath = @"Mods\PvP.json";

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

        internal void Awake()
        {
            Instance = this;
        }

        internal void Start()
        {
            this.gameObject.AddComponent(new PhotonView() { viewID = 904 });
            Debug.Log("Registered PvP with ViewID " + this.photonView.viewID);

            LoadSettings();

            gameObject.AddComponent<PvPGUI>();
            gameObject.AddComponent<PlayerManager>();
            gameObject.AddComponent<BattleRoyale>();
            gameObject.AddComponent<DeathMatch>();

            AddAction(MenuKey, KeybindingsCategory.Menus, ControlType.Both, 5, InputActionType.Button);

            // Custom Gameplay hooks
            On.InteractionRevive.OnActivate += DisableReviveInteractionHook;
            On.InteractionRevive.ProcessText += ReviveTextHook;
        }

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
            if (Global.Lobby.PlayersInLobbyCount < 1 || NetworkLevelLoader.Instance.IsGameplayPaused) 
            { 
                return; 
            }

            // handle player input 
            foreach (PlayerSystem ps in Global.Lobby.PlayersInLobby.Where(x => x.ControlledCharacter.IsLocalPlayer))
            {
                if (m_playerInputManager[ps.PlayerID].GetButtonDown(MenuKey))
                {
                    PvPGUI.Instance.showGui = !PvPGUI.Instance.showGui;
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
            if (PvPGUI.Instance.showGui) { PvPGUI.Instance.showGui = false; }

            if (_mode == (int)GameModes.BattleRoyale)
            {
                // actual moment that gameplay starts for the players
                BattleRoyale.Instance.IsGameplayStarting = false;
                BattleRoyale.Instance.LastSupplyDropTime = -1;
                BattleRoyale.Instance.LastEnemySpawnTime = Time.time;
                BattleRoyale.Instance.SupplyDropCounter = 0;
                BattleRoyale.Instance.ActiveItemContainers.Clear();
                BattleRoyale.Instance.ActiveBeamObjects.Clear();
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
                if (!BattleRoyale.Instance.IsGameplayStarting)
                {
                    BattleRoyale.Instance.UpdateBR();
                }
            }
            else if (CurrentGame == GameModes.Deathmatch)
            {
                DeathMatch.Instance.UpdateDM();
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
                BattleRoyale.Instance.EndBattleRoyale();
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

            BattleRoyale.Instance.IsGameplayStarting = true;
            BattleRoyale.Instance.ForceNoSaves = true;
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
                BattleRoyale.Instance.WasCheatsEnabled = true;
                Global.CheatsEnabled = false;

                foreach (PlayerSystem ps in Global.Lobby.PlayersInLobby.Where(x => x.ControlledCharacter.IsLocalPlayer))
                {
                    Character c = ps.ControlledCharacter;
                    (c.CharacterControl as LocalCharacterControl).MovementMultiplier = 1f;
                }
            }

            StartCoroutine(BattleRoyale.Instance.SetupAfterSceneLoad(skipLoad));
        }

        [PunRPC]
        public void SendSpawnEnemyRPC(string uid, float x, float y, float z)
        {
            if (BattleRoyale.Instance.EnemyCharacters.Find(w => w.UID == uid) is Character c)
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

                //BattleRoyale.Instance..FixEnemyStats(c.Stats);

                //foreach (PlayerSystem ps in Global.Lobby.PlayersInLobby.Where(w => w.IsLocalPlayer))
                //{
                //    SendUIMessageLocal(ps.ControlledCharacter, c.Name + " has spawned!");
                //}

                //BattleRoyale.Instance..EnemyCharacters.Remove(c);
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
            StartCoroutine(BattleRoyale.Instance.SupplyDropLocalCoroutine(itemUID, new Vector3(x, y, z)));
        }

        [PunRPC]
        public void RPCSendCleanup()
        {
            BattleRoyale.Instance.CleanupSupplyObjects();
        }

        [PunRPC]
        public void RPCGenerateStash(int itemID, string UID, float x, float y, float z)
        {
            TreasureChest chest = ItemManager.Instance.GenerateItemNetwork(itemID).GetComponent<TreasureChest>();
            chest.UID = UID;
            chest.SaveType = Item.SaveTypes.Savable;
            chest.transform.position = new Vector3(x, y, z);
            BattleRoyale.Instance.ActiveItemContainers.Add(chest.gameObject);
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

                var list = PlayerManager.Instance.AllFactions.Where(x => (int)x != (int)faction).ToList();
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
            bool flag = false;
            if (File.Exists(savePath))
            {
                if (JsonUtility.FromJson<Settings>(File.ReadAllText(savePath)) is Settings s2)
                {
                    settings = s2;
                    flag = true;
                }
            }
            if (!flag)
            {
                settings = new Settings();
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