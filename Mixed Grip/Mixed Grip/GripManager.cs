using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
//using SinAPI;
using static CustomKeybindings;

namespace MixedGrip
{
    public class CharacterInfo
    {
        public string CharacterUID = "";
        public string LastOffhandUID = "";
    }

    public class GripManager : Photon.MonoBehaviour
    {
        public List<CharacterInfo> CurrentPlayers = new List<CharacterInfo>();

        internal void Awake()
        {
            // equip item hook for MixedGrip logic
            On.CharacterEquipment.EquipItem_1 += EquipItemHook;
        }

        // ==================== UPDATE ===================== //

        internal void Update()
        {
            // make sure game is running
            if (Global.Lobby.PlayersInLobbyCount < 1 || NetworkLevelLoader.Instance.IsGameplayPaused) { return; }

            // setup PhotonView (add when needed)
            if (PhotonNetwork.inRoom && photonView == null)
            {
                gameObject.AddComponent(new PhotonView() { viewID = 7263 });
            }

            MixedGripUpdate();            
        }

        private void MixedGripUpdate()
        {
            if (Global.Lobby.PlayersInLobby.Where(x => x.ControlledCharacter.Initialized).Count() != CurrentPlayers.Count())
            {
                RefreshPlayerList();
            }
            else
            {
                foreach (CharacterInfo info in CurrentPlayers)
                {
                    if (CharacterManager.Instance.GetCharacter(info.CharacterUID) is Character c && c.IsLocalPlayer)
                    {
                        UpdatePlayerInput(info, c);

                        UpdateCharacterSlots(info, c);
                    }
                }
            }
        }

        private void RefreshPlayerList()
        {
            // refresh list update
            CurrentPlayers.Clear();

            foreach (PlayerSystem ps in Global.Lobby.PlayersInLobby.Where(x => x.ControlledCharacter.Initialized))
            {
                //Debug.Log("[MG]" + Time.time + " | InternalUpdate :: Added " + ps.ControlledCharacter.Name + " to CurrentPlayers");

                CharacterInfo charInfo = new CharacterInfo() { CharacterUID = ps.ControlledCharacter.UID };
                if (ps.ControlledCharacter.LeftHandEquipment)
                {
                    charInfo.LastOffhandUID = ps.ControlledCharacter.LeftHandEquipment.UID;
                }
                CurrentPlayers.Add(charInfo);
            }
        }

        private void UpdatePlayerInput(CharacterInfo charInfo, Character c)
        {
            // grip hotkey
            if (m_playerInputManager[c.OwnerPlayerSys.PlayerID].GetButtonDown(MixedGrip.Instance.ToggleKey))
            {
                ToggleGripHotkey(charInfo, c);
            }
        }

        private void UpdateCharacterSlots(CharacterInfo charInfo, Character c)
        {
            // bows not supported currently
            if (c.CurrentWeapon != null && c.CurrentWeapon.Type == Weapon.WeaponType.Bow)
            {
                if (charInfo.LastOffhandUID != "") { charInfo.LastOffhandUID = ""; }
                return;
            }

            // update lastOffhand if new off-hand item
            if (c.LeftHandEquipment != null && charInfo.LastOffhandUID != c.LeftHandEquipment.UID)
            {
                charInfo.LastOffhandUID = c.LeftHandEquipment.UID;
            }
            // update previous off-hand item status
            else if (c.LeftHandEquipment == null && charInfo.LastOffhandUID != "" && ItemManager.Instance.GetItem(charInfo.LastOffhandUID) is Item lastOffhand)
            {
                if (lastOffhand.OwnerCharacter == null)
                {
                    // player no longer owns their previous off-hand item.
                    charInfo.LastOffhandUID = "";
                    return;
                }

                // automatic swap to 2H if we have no off-hand item.
                if ((bool)MixedGrip.config.GetValue(Settings.Swap_On_Equip_And_Unequip) && c.CurrentWeapon != null && c.CurrentWeapon.TwoHand == Equipment.TwoHandedType.None)
                {
                    SwapGrip(c, c.CurrentWeapon);
                }
            }
        }

        // ================  GRIP SWAPPING ================= //

        private void ToggleGripHotkey(CharacterInfo charInfo, Character c)
        {
            if (c.CurrentWeapon == null || c.CurrentWeapon.IsSummonedEquipment || c.CurrentWeapon.Type == Weapon.WeaponType.Bow)
            {
                return;
            }

            SwapGrip(c, c.CurrentWeapon);
            
            // re-equip last offhand
            if (!c.CurrentWeapon.TwoHanded && charInfo.LastOffhandUID != "" && ItemManager.Instance.GetItem(charInfo.LastOffhandUID) is Equipment lastOffhand)
            {
                At.Call(c.Inventory.Equipment, "EquipWithoutAssociating", new object[] { lastOffhand, false });
            }
        }

        // Local SwapGrip call. Just determines what the swap should do, then calls it via RPC.
        private void SwapGrip(Character c, Weapon weapon)
        {
            bool setTwoHanded = weapon.TwoHand == Equipment.TwoHandedType.None;
            int newWeaponType = (bool)MixedGrip.config.GetValue(Settings.Swap_Animations) ? (int)GetSwappedType(weapon.Type) : (int)weapon.Type;

            // == send RPC swap grip ==
            photonView.RPC("SwapGripRPC", PhotonTargets.All, new object[] { weapon.UID, c.UID.ToString(), setTwoHanded, newWeaponType, (bool)MixedGrip.config.GetValue(Settings.Balance_Weapons) });
            //if (!PhotonNetwork.offlineMode)
            //{
            //    photonView.RPC("SwapGripRPC", PhotonTargets.All, new object[] { weapon.UID, c.UID.ToString(), setTwoHanded, newWeaponType, global.settings.Balance_Weapons });
            //}
            //else
            //{
            //    SwapGripRPC(weapon.UID, c.UID.ToString(), setTwoHanded, newWeaponType, global.settings.Balance_Weapons);
            //}
        }

        [PunRPC]
        private void SwapGripRPC(string weaponUID, string charUID, bool setTwoHanded, int newWeaponType, bool shouldFixStats)
        {
            if (CharacterManager.Instance.GetCharacter(charUID) is Character c && ItemManager.Instance.GetItem(weaponUID) is Weapon weapon)
            {
                // list of items to stop sync (might also add left hand equipment)
                List<Item> itemsToFix = new List<Item> { weapon };

                // set 2H type
                if (setTwoHanded)
                {
                    weapon.TwoHand = Equipment.TwoHandedType.TwoHandedRight;

                    if (c.LeftHandEquipment != null)
                    {
                        c.Inventory.UnequipItem(c.LeftHandEquipment);
                        itemsToFix.Add(c.LeftHandEquipment); // dont sync this (each player unequips locally for same character)
                    }
                }
                else
                {
                    weapon.TwoHand = Equipment.TwoHandedType.None;
                }

                // set Weapon.WeaponType
                weapon.Type = (Weapon.WeaponType)newWeaponType;

                // if we have it equipped, fix the left hand EquipmentSlot.m_lastEquippedItem
                if (c.Inventory.HasEquipped(weapon.ItemID))
                {
                    if (setTwoHanded)
                    {
                        At.SetValue(weapon, typeof(EquipmentSlot), c.Inventory.GetMatchingEquipmentSlot(EquipmentSlot.EquipmentSlotIDs.LeftHand), "m_lastEquippedItem");
                    }
                    else
                    {
                        At.SetValue<Item>(null, typeof(EquipmentSlot), c.Inventory.GetMatchingEquipmentSlot(EquipmentSlot.EquipmentSlotIDs.LeftHand), "m_lastEquippedItem");
                    }
                }
                
                c.Animator.SetInteger("WeaponType", newWeaponType); // fix the character animator
                if (shouldFixStats) { SetWeaponStats(weapon); }     // fix stats                
                StopItemSync(itemsToFix);                           // fix item sync
            } 
        }

        // fix and override Item Sync
        private void StopItemSync(List<Item> items)
        {
            if (items == null || items.Count() == 0) { return; }

            if (At.GetValue(typeof(ItemManager), ItemManager.Instance, "m_itemToSyncToClient") is HashSet<string> masterToClientDict
                && At.GetValue(typeof(ItemManager), ItemManager.Instance, "m_itemToSyncToMaster") is HashSet<string> clientToMasterDict)
            {
                foreach (Item item in items)
                {
                    if (item == null) { continue; }
                    At.SetValue(false, typeof(Item), item, "m_sendHierarchyRequired");
                    masterToClientDict.RemoveWhere(x => x == item.UID);
                    clientToMasterDict.RemoveWhere(x => x == item.UID);
                }

                At.SetValue(masterToClientDict, typeof(ItemManager), ItemManager.Instance, "m_itemToSyncToClient");
                At.SetValue(clientToMasterDict, typeof(ItemManager), ItemManager.Instance, "m_itemToSyncToMaster");
            }
        }

        // hook for swapping 2H weapons to 1H on equip item, if the logic calls for it.
        private void EquipItemHook(On.CharacterEquipment.orig_EquipItem_1 orig, CharacterEquipment self, Equipment _itemToEquip, bool _playAnim = false)
        {
            if (!(bool)MixedGrip.config.GetValue(Settings.Swap_On_Equip_And_Unequip)) { orig(self, _itemToEquip, _playAnim); return; }

            Character c = At.GetValue(typeof(CharacterEquipment), self, "m_character") as Character;

            bool anySwap = false;
            if (!c.IsAI && ((int)_itemToEquip.EquipSlot == 5 || (int)_itemToEquip.EquipSlot == 6) && !_itemToEquip.IsSummonedEquipment && !_itemToEquip.TwoHandedLeft)
            {
                if (_itemToEquip.TwoHanded && c.LeftHandEquipment != null)
                {
                    // we are equipping a 2H weapon but we currently have an off-hand item, swap the grip of the weapon first.
                    anySwap = true;

                    SwapGrip(c, _itemToEquip as Weapon);
                    At.Call(c.Inventory.Equipment, "EquipWithoutAssociating", new object[] { _itemToEquip, false });
                }
                else if (_itemToEquip.EquipSlot == EquipmentSlot.EquipmentSlotIDs.LeftHand && c.CurrentWeapon != null && c.CurrentWeapon.TwoHanded)
                {
                    // we are equipping an off-hand item but our current weapon is 2H. swap our weapon to 1H first.
                    anySwap = true;

                    SwapGrip(c, c.CurrentWeapon);

                    // set the offhand to our C.LeftHandEquipment now to avoid problems with autoswapping
                    At.SetValue(_itemToEquip as Equipment, typeof(Character), c, "m_leftHandEquipment");
                    At.Call(c.Inventory.Equipment, "EquipWithoutAssociating", new object[] { _itemToEquip, false });
                }
            }

            if (!anySwap)
            {
                orig(self, _itemToEquip, _playAnim);
            }
        }

        // =================== STAT HELPERS, ETC ====================== //

        public Weapon.WeaponType GetSwappedType(Weapon.WeaponType type)
        {
            switch (type)
            {
                case Weapon.WeaponType.Sword_1H:
                    type = Weapon.WeaponType.Sword_2H;
                    break;
                case Weapon.WeaponType.Sword_2H:
                    type = Weapon.WeaponType.Sword_1H;
                    break;
                case Weapon.WeaponType.Axe_1H:
                    type = Weapon.WeaponType.Axe_2H;
                    break;
                case Weapon.WeaponType.Axe_2H:
                    type = Weapon.WeaponType.Axe_1H;
                    break;
                case Weapon.WeaponType.Mace_1H:
                    type = Weapon.WeaponType.Mace_2H;
                    break;
                case Weapon.WeaponType.Mace_2H:
                    type = Weapon.WeaponType.Mace_1H;
                    break;
                default:
                    break;
            }
            return type;
        }

        private void SetWeaponStats(Weapon newWeapon)
        {
            Weapon origWeapon = ResourcesPrefabManager.Instance.GetItemPrefab(newWeapon.ItemID) as Weapon;

            // if returning to the orig type, just use the orig item stats.
            if (origWeapon.TwoHand == newWeapon.TwoHand)
            {
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(origWeapon.Stats), newWeapon.Stats);
            }
            else
            {
                WeaponStats newStats = new WeaponStats();
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(origWeapon.Stats), newStats);

                float adjustedSpeed = GetWeaponBalance(origWeapon, newWeapon);
                float damageMultiplier = 1.04f * adjustedSpeed;
                if (newWeapon.Type == Weapon.WeaponType.Spear_2H || newWeapon.Type == Weapon.WeaponType.Halberd_2H)
                {
                    damageMultiplier = 0.90f;
                }


                // set visible damage
                for (int i = 0; i < newStats.BaseDamage.List.Count; i++)
                {
                    if (newStats.BaseDamage.List[i].Damage > 0)
                    {
                        newStats.BaseDamage.List[i].Damage *= damageMultiplier;
                        newStats.BaseDamage.List[i].Damage = Mathf.Round(newStats.BaseDamage.List[i].Damage);
                    }
                }

                newStats.Impact *= damageMultiplier;
                newStats.Impact = Mathf.Round(newStats.Impact);

                // set attack speed
                adjustedSpeed += newStats.AttackSpeed;
                newStats.AttackSpeed = (float)Math.Round(100 * adjustedSpeed * 0.01f * 0.5f, 2); // average of "fixed" speed and current speed.

                for (int i = 0; i < newStats.Attacks.Count(); i++)
                {
                    newStats.Attacks[i].Knockback *= damageMultiplier;

                    for (int j = 0; j < newStats.Attacks[i].Damage.Count(); j++)
                    {
                        newStats.Attacks[i].Damage[j] *= damageMultiplier;
                    }
                }

                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(newStats), newWeapon.Stats);
            }
        }

        public float GetWeaponBalance(Weapon originalWeapon, Weapon newWeapon)
        {
            if (originalWeapon.Type == Weapon.WeaponType.Spear_2H || originalWeapon.Type == Weapon.WeaponType.Halberd_2H)
            {
                return 0.9f * originalWeapon.Stats.AttackSpeed;
            }

            return 100f / (float)((decimal)weaponSpeeds[originalWeapon.Type] / (decimal)weaponSpeeds[newWeapon.Type]) / 100;
        }

        public Dictionary<Weapon.WeaponType, float> weaponSpeeds = new Dictionary<Weapon.WeaponType, float>
        {
            { Weapon.WeaponType.Sword_1H,   1.251f},
            { Weapon.WeaponType.Axe_1H,     1.399f},
            { Weapon.WeaponType.Mace_1H,    1.629f},
            { Weapon.WeaponType.Sword_2H,   1.710f},
            { Weapon.WeaponType.Axe_2H,     1.667f},
            { Weapon.WeaponType.Mace_2H,    2.036f},
            { Weapon.WeaponType.Spear_2H,   1.499f},
            { Weapon.WeaponType.Halberd_2H, 1.612f}
        };
    }
}
