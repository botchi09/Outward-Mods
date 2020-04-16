using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using SinAPI;
using UnityEngine;
using Partiality.Modloader;
using SideLoader;

namespace ImbuedBows
{
    #region Mod Loader
    public class ModBase : PartialityMod
    {
        public double version = 1.0;

        public ModBase()
        {
            this.ModID = "ImbuedBows";
            this.Version = version.ToString("0.00");
            this.author = "Sinai";
        }

        public override void OnEnable()
        {
            base.OnEnable();

            var _obj = new GameObject(this.ModID);
            GameObject.DontDestroyOnLoad(_obj);

            _obj.AddComponent<ImbuedBows>();

            // mana bow
            _obj.AddComponent<ManaBow>();
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }
    }
    #endregion

    #region Actual Mod
    public class ImbuedBows : MonoBehaviour
    {
        internal void Awake()
        {
            SL.OnPacksLoaded += Setup;

            On.InfuseConsumable.Use += UseInfuseHook;

            On.ItemVisual.AddImbueFX += AddImbueFXHook;


        }


        private void Setup()
        {
            // =============== setup infuse skills ===============
            var list = new List<int>
            {
                8200100, // infuse light
                8200101, // infuse wind
                8200102, // infuse frost
                8200103 // infuse fire
            };

            foreach (int id in list)
            {
                var skill = ResourcesPrefabManager.Instance.GetItemPrefab(id) as AttackSkill;
                skill.RequiredWeaponTypes.Add(Weapon.WeaponType.Bow);
            }
        }

        private bool UseInfuseHook(On.InfuseConsumable.orig_Use orig, InfuseConsumable self, Character _character)
        {
            if (_character != null)
            {
                if (_character.CurrentWeapon != null) // && _character.CurrentWeapon.Type != Weapon.WeaponType.Bow)
                {
                    if (self.m_UseSound)
                    {
                        self.m_UseSound.Play(false);
                    }
                    //self.m_characterUsing = _character;
                    At.SetValue(_character, typeof(Item), self as Item, "m_characterUsing");
                    if (self.ActivateEffectAnimType == Character.SpellCastType.NONE)
                    {
                        _character.Inventory.OnUseItem(self.UID);
                    }
                    else
                    {
                        //self.StartEffectsCast(_character);
                        At.Call(self as Item, "StartEffectsCast", new object[] { _character });
                    }
                    return true;
                }
                if (_character.CharacterUI)
                {
                    _character.CharacterUI.ShowInfoNotificationLoc("Notification_Item_InfuseNotCompatible");
                }
            }

            return false;
        }

        // fix for bows imbue FX (they use Skinned Mesh)
        private void AddImbueFXHook(On.ItemVisual.orig_AddImbueFX orig, ItemVisual self, ImbueStack newStack, Weapon _linkedWeapon)
        {
            // if its not a bow, just do orig(self) and return
            if (_linkedWeapon.Type != Weapon.WeaponType.Bow)
            {
                orig(self, newStack, _linkedWeapon);
                return;
            }

            newStack.ImbueFX = ItemManager.Instance.GetImbuedFX(newStack.ImbuedEffectPrefab);
            if (!newStack.ImbueFX.gameObject.activeSelf)
            {
                newStack.ImbueFX.gameObject.SetActive(true);
            }
            newStack.ParticleSystems = newStack.ImbueFX.GetComponentsInChildren<ParticleSystem>();

            if (self.GetComponentInChildren<SkinnedMeshRenderer>() is SkinnedMeshRenderer skinnedMesh)
            {
                for (int j = 0; j < newStack.ParticleSystems.Length; j++)
                {
                    if (newStack.ParticleSystems[j].shape.shapeType == ParticleSystemShapeType.MeshRenderer)
                    {
                        var shape = newStack.ParticleSystems[j].shape;
                        shape.shapeType = ParticleSystemShapeType.SkinnedMeshRenderer;
                        shape.skinnedMeshRenderer = skinnedMesh;
                    }
                    newStack.ParticleSystems[j].Play();
                }
            }

            newStack.ImbueFX.SetParent(self.transform);
            newStack.ImbueFX.ResetLocal(true);
            if (At.GetValue(typeof(ItemVisual), self, "m_linkedImbueFX") is List<ImbueStack> m_linkedImbues)
            {
                m_linkedImbues.Add(newStack);
                At.SetValue(m_linkedImbues, typeof(ItemVisual), self, "m_linkedImbueFX");
            }
        }
        #endregion
    }
}
