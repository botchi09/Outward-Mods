using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using SinAPI;
using System.IO;

namespace PvP
{
    public class BattleRoyale_Hooks : MonoBehaviour
    {
        public PvPGlobal global;
        public BattleRoyale BRManager;

        internal void Start()
        {
            // DROP STUFF ON DEATH
            On.Character.Die += CharacterDieHook;

            // SKILL TAKEITEM HOOK
            On.ItemContainer.AddItem_1 += AddItemHook;

            // SKILL REQUIREMENT REMOVAL HOOKS
            On.Skill.OwnerHasAllRequiredItems += RequiredItemsHook;
            On.Skill.HasAllAdditionalConditions += AdditionalConditionsHook;
            On.Skill.ConsumeRequiredItems += ConsumeItemsHook;
            On.RemoveStatusEffect.ActivateLocally += RemoveStatusHook;
            On.RemoveImbueEffects.ActivateLocally += RemoveImbueHook;

            // SAVE OVERRIDE
            //On.NetworkLevelLoader.OnDisconnectedFromPhoton += DisconnectedHook;
            On.NetworkLevelLoader.Save += NetworkLevelSaveHook;
            On.NetworkLevelLoader.Save_1 += NetworkLevelSave_1Hook;
            On.SaveManager.Update += SaveManagerUpdateHook;
            On.SaveManager.LocalCharStarted += LocalCharStartedHook;

            // BUGFIX
            On.ItemListDisplay.SortBySupport += SortBySupportFix;
            On.PlayerCharacterStats.UpdateWeight += UpdateWeightHook;

        }
        // =========== GAMEPLAY HOOKS ================

        private void CharacterDieHook(On.Character.orig_Die orig, Character self, Vector3 _hitVec, bool _loadedDead = false)
        {
            if (global.CurrentGame == PvPGlobal.GameModes.BattleRoyale)
            {
                if (!self.IsAI)
                {
                    //OLogger.Log("Dropping player stuff!");
                    // custom player death (drop items)

                    if (!PhotonNetwork.isNonMasterClientInRoom && At.GetValue(typeof(Character), self, "m_lastDealers") is List<Pair<UID, float>> lastDealers)
                    {
                        float lowest = Time.time;
                        string uid = "";
                        foreach (Pair<UID, float> entry in lastDealers)
                        {
                            if (entry.Value < lowest)
                            {
                                lowest = entry.Value;
                                uid = entry.Key;
                            }
                        }
                        if (uid != "" && CharacterManager.Instance.GetCharacter(uid) is Character lastDealer)
                        {
                            lastDealers.Clear();
                            At.SetValue(lastDealers, typeof(Character), self, "m_lastDealers");
                            global.SendMessageToAll(lastDealer.Name + " has defeated " + self.Name);
                        }
                    }

                    foreach (EquipmentSlot equipslot in self.Inventory.Equipment.EquipmentSlots.Where(x => x != null && x.HasItemEquipped))
                    {
                        self.Inventory.DropItem(equipslot.EquippedItem.UID);
                    }
                }
                else
                {
                    if (self.Inventory != null && self.GetComponent<LootableOnDeath>() != null)
                    {
                        if (self.Inventory.Pouch == null || !self.Inventory.Pouch.FullyInitialized)
                        {
                            self.Inventory.ProcessStart();
                        }

                        // SPECIFIC TO MONSOON MAP

                        if (!PhotonNetwork.isNonMasterClientInRoom)
                        {
                            if (self.Name.ToLower().Contains("butcher"))
                            {
                                BRManager.AddItemsToContainer(Templates.BR_Templates.Skills_High, 3, self.Inventory.Pouch.transform);
                                BRManager.AddItemsToContainer(Templates.BR_Templates.Weapons_High, 1, self.Inventory.Pouch.transform);
                            }
                            else if (self.Name == "Immaculate" || self.name == "Shell Horror")
                            {
                                BRManager.AddItemsToContainer(Templates.BR_Templates.Skills_High, 1, self.Inventory.Pouch.transform);
                                BRManager.AddItemsToContainer(Templates.BR_Templates.Skills_Low, 2, self.Inventory.Pouch.transform);
                            }
                            else
                            {
                                BRManager.AddItemsToContainer(Templates.BR_Templates.Skills_Low, 3, self.Inventory.Pouch.transform);
                            }
                        }
                    }
                    else
                    {
                        //OLogger.Error("lootableondeath or character inventory is null");
                    }
                    
                }
            }

            orig(self, _hitVec, _loadedDead);
        }

        // ============= SKILL HOOKS ===============

        // fix for item containers
        private bool AddItemHook(On.ItemContainer.orig_AddItem_1 orig, ItemContainer self, Item _item, bool _stackIfPossible)
        {
            if (PhotonNetwork.isNonMasterClientInRoom)
            {
                return orig(self, _item, _stackIfPossible);
            }

            if (global.BRManager.IsGameplayStarting || global.CurrentGame == PvPGlobal.GameModes.BattleRoyale)
            {
                if (_item is Skill && self.OwnerCharacter != null && !self.OwnerCharacter.IsAI)
                {
                    if (!self.OwnerCharacter.Inventory.SkillKnowledge.IsItemLearned(_item.ItemID))
                        _item.ChangeParent(self.OwnerCharacter.Inventory.SkillKnowledge.transform);

                    return true;
                }

                if (_item is Weapon && self.OwnerCharacter != null)
                {
                    if (Templates.Weapon_Skills.ContainsKey((int)(_item as Weapon).Type))
                    {
                        int id = Templates.Weapon_Skills[(int)(_item as Weapon).Type];
                        if (self.OwnerCharacter.Inventory.SkillKnowledge is CharacterSkillKnowledge skills && !skills.IsItemLearned(id))
                        {
                            Item skill = ItemManager.Instance.GenerateItemNetwork(id);
                            skill.ChangeParent(skills.transform);
                            List<Item> learnedItems = At.GetValue(typeof(CharacterKnowledge), skills as CharacterKnowledge, "m_learnedItems") as List<Item>;
                            learnedItems.Add(skill as Item);
                            At.SetValue(learnedItems, typeof(CharacterKnowledge), skills as CharacterKnowledge, "m_learnedItems");
                        }
                    }
                }
            }

            return orig(self, _item, _stackIfPossible);
        }

        // fix for activation conditions / consumption
        private void ConsumeItemsHook(On.Skill.orig_ConsumeRequiredItems orig, Skill self)
        {
            if (global.CurrentGame == PvPGlobal.GameModes.BattleRoyale)
            {
                // do nothing
                return;
            }

            orig(self);
        }

        private bool AdditionalConditionsHook(On.Skill.orig_HasAllAdditionalConditions orig, Skill self, bool _tryingToActive)
        {
            if (global.CurrentGame == PvPGlobal.GameModes.BattleRoyale)
            {
                return true;
            }
            else
            {
                return orig(self, _tryingToActive);
            }
        }

        private bool RequiredItemsHook(On.Skill.orig_OwnerHasAllRequiredItems orig, Skill self, bool _tryingToActive)
        {
            if (global.CurrentGame == PvPGlobal.GameModes.BattleRoyale)
            {
                // override sigils
                if (self.ItemID == 8200030 || self.ItemID == 8200031 || self.ItemID == 8200032)
                {
                    return true;
                }
            }

            return orig(self, _tryingToActive);
        }

        private void RemoveStatusHook(On.RemoveStatusEffect.orig_ActivateLocally orig, RemoveStatusEffect self, Character _affectedCharacter, object[] _infos)
        {
            if (global.CurrentGame == PvPGlobal.GameModes.BattleRoyale)
            {
                // do nothing
                return;
            }
            orig(self, _affectedCharacter, _infos);
        }

        private void RemoveImbueHook(On.RemoveImbueEffects.orig_ActivateLocally orig, RemoveImbueEffects self, Character _affectedCharacter, object[] _infos)
        {
            if (global.CurrentGame == PvPGlobal.GameModes.BattleRoyale)
            {
                // do nothing
                return;
            }
            orig(self, _affectedCharacter, _infos);
        }

        // =========== SAVE HOOKS =============

        //private void DisconnectedHook(On.NetworkLevelLoader.orig_OnDisconnectedFromPhoton orig, NetworkLevelLoader self)
        //{
        //    if (!BRManager.IsGameplayStarting && global.CurrentGame == PvPGlobal.GameModes.BattleRoyale)
        //    {
        //        //OLogger.Warning("[BR] TODO! Cleanup / fix on leave room! Setting ForceNoSaves to false etc");
        //        global.StopGameplay("Connection lost! Ending the game...");
        //        global.CurrentGame = PvPGlobal.GameModes.NONE;
        //        MenuManager.Instance.BackToMainMenu();
        //    }
        //    else
        //    {
        //        orig(self);
        //    }
        //}

        private void LocalCharStartedHook(On.SaveManager.orig_LocalCharStarted orig, SaveManager self, Character _char)
        {
            if (!BRManager.ForceNoSaves)
            {
                orig(self, _char);
            }
            else
            {
                //OLogger.Log("Savemanager tried to LocalCharStarted (load save), but we stopped it.");
            }
        }

        private void NetworkLevelSaveHook(On.NetworkLevelLoader.orig_Save orig, NetworkLevelLoader self)
        {
            if (!BRManager.ForceNoSaves)
            {
                orig(self);
            }
            else
            {
                //OLogger.Log("NetworkLevelSaveHook tried to save, but we stopped it.");
            }
        }

        private void NetworkLevelSave_1Hook(On.NetworkLevelLoader.orig_Save_1 orig, NetworkLevelLoader self, bool _async, bool _forceSaveEnvironment = false)
        {
            if (!BRManager.ForceNoSaves)
            {
                orig(self, _async, _forceSaveEnvironment);
            }
            else
            {
                //OLogger.Log("NetworkLevelSaveHook_1 tried to save, but we stopped it.");
            }
        }

        private void SaveManagerUpdateHook(On.SaveManager.orig_Update orig, SaveManager self)
        {
            if (!BRManager.ForceNoSaves)
            {
                orig(self);
            }
        }

        // bug fix hook

        private void UpdateWeightHook(On.PlayerCharacterStats.orig_UpdateWeight orig, PlayerCharacterStats self)
        {
            try { orig(self); } catch { }
        }

        private int SortBySupportFix(On.ItemListDisplay.orig_SortBySupport orig, Item _item1, Item _item2)
        {
            try
            {
                return orig(_item1, _item2);
            }
            catch
            {
                return -1;
            }
        }

    }
}
