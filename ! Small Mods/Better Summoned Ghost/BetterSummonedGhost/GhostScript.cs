using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
//using SinAPI;
using static CustomKeybindings;

namespace BetterSummonedGhost
{
    public class GhostScript : MonoBehaviour
    {
        public ScriptGUI gui;

        public Settings settings = new Settings
        {
            CustomLifespan = 280,
            CustomHealth = 110,
            KeepGhostClose = true,
            KeepCloseDistance = 20,
            GiveGhostWeapon = true,
            ShowGuiOnStartup = true
        };

        public string MenuKey = "Better Ghost Menu";

        public string LastScene = "";
        public List<PlayerSummonInfo> CurrentPlayers = new List<PlayerSummonInfo>();

        public void Init()
        {
            //OLogger.CreateLog(new Rect(525, 5, 450, 150), "Default", true, true);

            LoadSettings();

            gui.showGui = settings.ShowGuiOnStartup;

            //custom keybindings
            AddAction(MenuKey, KeybindingsCategory.Actions, ControlType.Both, 5);

            //Debug.Log("Custom ally init");
        }  

        internal void Update()
        {
            if (NetworkLevelLoader.Instance.IsGameplayPaused)
            {
                return;
            }

            // update character lists
            try
            {
                if (CurrentPlayers.Count != CharacterManager.Instance.PlayerCharacters.Count)
                {
                    CurrentPlayers.Clear();

                    foreach (string uid in CharacterManager.Instance.PlayerCharacters.Values)
                    {
                        Character c = CharacterManager.Instance.GetCharacter(uid);

                        CurrentPlayers.Add(new PlayerSummonInfo { Character = c });
                    }
                }
            }
            catch { }

            // update summon info
            if (CurrentPlayers.Count > 0)
            {
                bool sceneChangeFlag = SceneManagerHelper.ActiveSceneName != LastScene;

                int localID = 0;
                foreach (PlayerSummonInfo player in CurrentPlayers)
                {
                    if (player.Character.IsLocalPlayer)
                    {
                        if (m_playerInputManager[localID].GetButtonDown(MenuKey))
                        {
                            gui.showGui = !gui.showGui;
                        }
                    }

                    if (player.Ally == null && player.Character.CurrentSummon != null && player.Character.CurrentSummon.Health > 0)
                    {
                        AddNewAlly(player);
                    }
                    else if (player.Ally != null)
                    {
                        // update existing ghosts
                        UpdateAlly(player);

                        if (sceneChangeFlag)
                        {
                            StartCoroutine(InitCustomGhost(player));

                            player.Ally.transform.position = player.Character.transform.position;
                        }
                    }

                    localID++;
                }
            }

            LastScene = SceneManagerHelper.ActiveSceneName;
        }

        public void AddNewAlly(PlayerSummonInfo player)
        {
            // add new summoned ghost
            player.Ally = player.Character.CurrentSummon;

            At.SetValue(settings.CustomLifespan, typeof(Character), player.Ally, "Lifetime");
            player.RemainingLifespan = player.Character.CurrentSummon.RemainingLifespan;

            if (settings.GiveGhostWeapon)
            {
                if (player.Character.CurrentWeapon is MeleeWeapon weapon && weapon.EquipSlot == EquipmentSlot.EquipmentSlotIDs.RightHand)
                {
                    player.customWeaponID = player.Character.CurrentWeapon.ItemID;
                }
                else { player.customWeaponID = -1; }
            }

            Stat HP = new Stat { BaseValue = settings.CustomHealth };
            At.SetValue(HP, typeof(CharacterStats), player.Ally.Stats, "m_maxHealthStat");
            At.SetValue(HP.BaseValue, typeof(CharacterStats), player.Ally.Stats, "m_health");

            StartCoroutine(InitCustomGhost(player));
        }

        public void UpdateAlly(PlayerSummonInfo player)
        {
            if (player.RemainingLifespan <= 0 || player.Ally.Health <= 0)
            {
                player.Ally = null;
                player.RemainingLifespan = 0;
            }
            else
            {
                player.RemainingLifespan -= Time.deltaTime;

                if (settings.KeepGhostClose 
                    && Vector3.Distance(player.Character.transform.position, player.Character.CurrentSummon.transform.position) > settings.KeepCloseDistance)
                {
                    //Debug.Log("moving AI to position");
                    player.Character.CurrentSummon.transform.position = player.Character.transform.position + new Vector3(1, 1, 1);
                }
            }
        }

        public IEnumerator InitCustomGhost(PlayerSummonInfo player)
        {
            yield return new WaitForSeconds(0.5f);

            if (At.GetValue(typeof(CharacterStats), player.Ally.Stats, "m_maxHealthStat") is Stat HP)
            {
                At.SetValue(HP.BaseValue, typeof(Stat), HP, "m_currentValue");
            }

            if (settings.GiveGhostWeapon)
            {
                if (player.Ally.CurrentWeapon is Item item)
                {
                    item.transform.parent = null;
                    ItemManager.Instance.DestroyItem(item.UID);
                }

                foreach (KeyValuePair<EquipmentSlot.EquipmentSlotIDs, EquipmentSlotInfo> entry in GhostSlotsHelper)
                {
                    Transform customSlot = null;

                    if (player.Ally.transform.FindInAllChildren("Custom_" + entry.Value.CustomName) is Transform t)
                    {
                        Destroy(t);
                    }

                    if (player.Ally.transform.FindInAllChildren(entry.Value.SlotObjectName) is Transform origSlot)
                    {
                        //Debug.Log("Setting up custom slot " + entry.Value.CustomName);

                        GameObject newObj = new GameObject("Custom_" + entry.Value.CustomName);

                        newObj.transform.parent = origSlot;
                        newObj.transform.position = origSlot.position;
                        newObj.transform.rotation = origSlot.rotation;
                        customSlot = newObj.transform;

                        Quaternion newRot = customSlot.transform.rotation;
                        newRot *= Quaternion.Euler(entry.Value.fixRotation);

                        newRot *= Quaternion.Euler(Vector3.right * 180);

                        EquipmentSlot equipHolder = player.Ally.Inventory.Equipment.GetMatchingEquipmentSlotTransform(entry.Value.slot).GetComponent<EquipmentSlot>();
                        if (equipHolder)
                        {
                            VisualSlot vSlot = customSlot.GetOrAddComponent<MainHandVisualSlot>();

                            At.SetValue(newRot, typeof(MainHandVisualSlot), vSlot, "m_defaultRotation");
                            At.SetValue(false, typeof(MainHandVisualSlot), vSlot, "m_sheathed");

                            vSlot.transform.rotation = newRot;
                            At.SetValue(equipHolder, typeof(VisualSlot), vSlot, "m_linkedEquipmentSlot");
                            At.SetValue(vSlot, typeof(EquipmentSlot), equipHolder, "m_visualSlotHolder");
                        }

                        if (player.customWeaponID != -1 && entry.Value.slot == EquipmentSlot.EquipmentSlotIDs.RightHand)
                        {
                            Item customItem = ItemManager.Instance.GenerateItemNetwork(player.customWeaponID);
                            player.Ally.Inventory.TakeItem(customItem.UID, true);
                            At.Call(player.Ally.Inventory.Equipment, "EquipWithoutAssociating", new object[] { customItem, false });
                        }
                    }
                }
            }
        }

        
        public Dictionary<EquipmentSlot.EquipmentSlotIDs, EquipmentSlotInfo> GhostSlotsHelper = new Dictionary<EquipmentSlot.EquipmentSlotIDs, EquipmentSlotInfo>
        {
            {
                EquipmentSlot.EquipmentSlotIDs.RightHand,

                new EquipmentSlotInfo
                {
                    CustomName = "RightHand",
                    slot = EquipmentSlot.EquipmentSlotIDs.RightHand,
                    fixRotation = new Vector3(0, 0, -94),
                    SlotObjectName = "hand_rightWeapon",
                }
            },
        };

        public void LoadSettings()
        {
            if (Directory.Exists(@"Mods\"))
            {
                if (File.Exists(@"Mods\BetterGhostSettings.json"))
                {
                    JsonUtility.FromJsonOverwrite(File.ReadAllText(@"Mods\BetterGhostSettings.json"), settings);
                }
            }
            else
            {
                Directory.CreateDirectory(@"Mods\");
            }
        }

        internal void OnDisable()
        {
            SaveSettings();
        }

        public void SaveSettings()
        {
            string path = @"Mods\BetterGhostSettings.json";
            if (File.Exists(path)) { File.Delete(path); }
            File.WriteAllText(path, JsonUtility.ToJson(settings, true));
        }
        
    }

    public class PlayerSummonInfo
    {
        public Character Character;
        public Character Ally;
        public float RemainingLifespan;
        public int customWeaponID;
    }

    public class EquipmentSlotInfo
    {
        public string CustomName;
        public EquipmentSlot.EquipmentSlotIDs slot;
        public string SlotObjectName;
        public Vector3 fixRotation;
    }

    public class Settings
    {
        public bool ShowGuiOnStartup = true;

        public float CustomLifespan;
        public float CustomHealth;

        public bool KeepGhostClose;
        public float KeepCloseDistance;

        public bool GiveGhostWeapon;
    }
}