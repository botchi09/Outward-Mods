using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using SinAPI;
using UnityEngine;

namespace ImbuedBows
{
    public class ManaBow : MonoBehaviour
    {
        public static readonly int ManaBowID = 2800900;
        public static readonly int ManaArrowID = 2800901;
        public static readonly float ManaBowCost = 5f;

        internal void Awake()
        {
            StartCoroutine(SetupManaBowCoroutine());

            // mana bow hooks
            On.WeaponLoadout.CanBeLoaded += CanBeLoadedHook;
            On.WeaponLoadout.ReduceShotAmount += ReduceShotHook;
            On.CharacterEquipment.GetEquippedAmmunition += GetAmmoHook;

            // ammo sound hook
            On.CharacterInventory.PlayEquipSFX += EquipSoundHook;
            On.CharacterInventory.PlayUnequipSFX += UnequipSoundHook;

            // bow skills
            On.UseLoadoutAmunition.TryTriggerConditions += TryUseAmmoHook;
            On.UseLoadoutAmunition.ActivateLocally += ActivateAmmoHook;
            On.AttackSkill.OwnerHasAllRequiredItems += OwnerHasItemsHook;
        }

        private bool OwnerHasItemsHook(On.AttackSkill.orig_OwnerHasAllRequiredItems orig, AttackSkill self, bool _tryingToActivate)
        {
            if (self.OwnerCharacter && self.OwnerCharacter.CurrentWeapon is ProjectileWeapon bow && bow.ItemID == ManaBowID)
            {
                return true;
            }
            else
            {
                return orig(self, _tryingToActivate);
            }
        }

        private bool TryUseAmmoHook(On.UseLoadoutAmunition.orig_TryTriggerConditions orig, UseLoadoutAmunition self)
        {
            var affectedChar = At.GetValue(typeof(Effect), self as Effect, "m_affectedCharacter") as Character;

            if (self.MainHand && affectedChar.CurrentWeapon is ProjectileWeapon bow && bow.ItemID == ManaBowID)
            {
                return true;
            }
            else
            {
                return orig(self);
            }
        }

        private void ActivateAmmoHook(On.UseLoadoutAmunition.orig_ActivateLocally orig, UseLoadoutAmunition self, Character _affectedCharacter, object[] _infos)
        {
            if (self.MainHand && _affectedCharacter.CurrentWeapon is ProjectileWeapon bow && bow.ItemID == ManaBow.ManaBowID)
            {
                // do nothing.
            }
            else
            {
                orig(self, _affectedCharacter, _infos);
            }
        }

        private void EquipSoundHook(On.CharacterInventory.orig_PlayEquipSFX orig, CharacterInventory self, Equipment _equipment)
        {
            if (_equipment.ItemID == ManaArrowID)
            {
                return;
            }
            orig(self, _equipment);
        }
        private void UnequipSoundHook(On.CharacterInventory.orig_PlayUnequipSFX orig, CharacterInventory self, Equipment _equipment)
        {
            if (_equipment.ItemID == ManaArrowID)
            {
                return;
            }
            orig(self, _equipment);
        }

        // coroutine to setup after sideloader init is done
        private IEnumerator SetupManaBowCoroutine()
        {
            while (!SideLoader.SL.Instance.IsInitDone())
            {
                yield return new WaitForSeconds(0.1f);
            }

            // setup bow
            var bow = ResourcesPrefabManager.Instance.GetItemPrefab(ManaBowID) as ProjectileWeapon;
            if (bow != null && bow.VisualPrefab is Transform bowVisuals)
            {
                var skinnedMesh = bowVisuals.GetComponentInChildren<SkinnedMeshRenderer>();
                if (skinnedMesh)
                {
                    skinnedMesh.material.color = new Color(0.5f, 0.8f, 2.0f);
                }

                var light = bowVisuals.gameObject.AddComponent<Light>();
                light.color = new Color(0.3f, 0.7f, 0.9f);
                light.intensity = 1.5f;
                light.range = 1.3f;
            }

            // setup custom mana projectile
            var manaArrow = ResourcesPrefabManager.Instance.GetItemPrefab(ManaArrowID) as Ammunition;

            manaArrow.IsPickable = false;

            // custom arrow ProjectileItem component (determines the ammunition behaviour as projectile)
            var origObj = manaArrow.ProjectileFXPrefab.gameObject;
            origObj.SetActive(false);
            var newObj = Instantiate(origObj);
            origObj.SetActive(true);
            DontDestroyOnLoad(newObj);
            var projBehaviour = newObj.GetComponent<ProjectileItem>();
            projBehaviour.CollisionBehavior = ProjectileItem.CollisionBehaviorTypes.Destroyed;

            // custom arrow visuals
            var origVisuals = manaArrow.VisualPrefab;
            origVisuals.gameObject.SetActive(false);
            var newVisuals = Instantiate(origVisuals).gameObject;
            manaArrow.VisualPrefab = newVisuals.transform;
            DontDestroyOnLoad(newVisuals);
            foreach (MeshRenderer mesh in newVisuals.GetComponentsInChildren<MeshRenderer>())
            {
                if (mesh.GetComponent<BoxCollider>())
                {
                    mesh.material.color = new Color(0.3f, 0.8f, 1.2f);

                    var light = mesh.gameObject.AddComponent<Light>();
                    light.color = new Color(0.3f, 0.7f, 0.9f);
                    light.intensity = 1.2f;
                    light.range = 0.5f;

                    break;
                }
            }
        }

        // custom mana cost hook (try trigger)
        private bool CanBeLoadedHook(On.WeaponLoadout.orig_CanBeLoaded orig, WeaponLoadout self)
        {
            var item = self.Item;

            if (item.ItemID != ManaBowID)
            {
                return orig(self);
            }
            else
            {
                float currentMana = item.OwnerCharacter.Stats.CurrentMana;
                float manaCost = item.OwnerCharacter.Stats.GetFinalManaConsumption(null, ManaBowCost);
                if (currentMana - manaCost >= 0)
                {
                    return true;
                }
                else
                {
                    item.OwnerCharacter.CharacterUI.ShowInfoNotificationLoc("Notification_Skill_NotEnoughtMana");
                    return false;
                }
            }
        }

        private void ReduceShotHook(On.WeaponLoadout.orig_ReduceShotAmount orig, WeaponLoadout self, bool _destroyOnEmpty = false)
        {
            if (self.Item.ItemID == ManaBowID)
            {
                float manaCost = self.Item.OwnerCharacter.Stats.GetFinalManaConsumption(null, ManaBowCost);
                self.Item.OwnerCharacter.Stats.UseMana(null, manaCost);
                return;
            }
            orig(self, _destroyOnEmpty);
        }

        private Ammunition GetAmmoHook(On.CharacterEquipment.orig_GetEquippedAmmunition orig, CharacterEquipment self)
        {
            if (self.GetEquippedItem(EquipmentSlot.EquipmentSlotIDs.RightHand) is Weapon weapon && weapon.ItemID == ManaBowID)
            {
                var character = At.GetValue(typeof(CharacterEquipment), self, "m_character") as Character;

                if (!character.Inventory.HasEquipped(ManaArrowID))
                {
                    if (!character.Inventory.OwnsItem(ManaArrowID))
                    {
                        var newAmmo = ItemManager.Instance.GenerateItemNetwork(ManaArrowID) as Ammunition;
                        newAmmo.ChangeParent(self.GetMatchingEquipmentSlotTransform(EquipmentSlot.EquipmentSlotIDs.Quiver));
                        return newAmmo;
                    }
                    else
                    {
                        var ammoFromID = character.Inventory.GetOwnedItems(ManaArrowID);
                        ammoFromID[0].ChangeParent(self.GetMatchingEquipmentSlotTransform(EquipmentSlot.EquipmentSlotIDs.Quiver));
                        return ammoFromID[0] as Ammunition;
                    }
                }
                else
                {
                    return self.GetEquippedItem(EquipmentSlot.EquipmentSlotIDs.Quiver) as Ammunition;
                }
            }

            return orig(self);
        }
    }
}

