using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
//using SinAPI;

namespace CombatHUD
{
    public class DamageManager : MonoBehaviour
    {
        public CombatHudGlobal global;

        public List<DamageLabel> ActiveLabels = new List<DamageLabel>();

        public void UpdateDamage()
        {
            if (global.sceneChangeFlag) { ActiveLabels.Clear(); }

            if (ActiveLabels.Count > 0)
            {
                float min = global.settings.labelMinTime;
                float max = global.settings.labelMaxTime;
                float ceil = global.settings.damageStrength;

                for (int i = 0; i < ActiveLabels.Count(); i++)
                {
                    DamageLabel label = ActiveLabels.ElementAt(i);

                    float timeLerp = Mathf.Lerp(min, max, (float)((decimal)label.damage.TotalDamage / (decimal)ceil));

                    if (label.creationTime < Time.time - timeLerp || label.target == null)
                    {
                        ActiveLabels.Remove(label);
                        i -= 1; // set iteration count -1, so we dont skip the next element
                    }
                    if (i >= ActiveLabels.Count() - 1) // make sure we dont get argument out of bounds exception
                    {
                        break;
                    }
                }
            }
        }

        internal void OnEnable()
        {
            // register hit sources
            On.PunctualDamage.DealHit += PunctualHook; // non-weapon skill hits, traps, effects, etc
            On.WeaponDamage.DealHit += WeaponSkillHook; // weapon skill hits
            On.Weapon.HasHit += WeaponHitHook; // melee weapon hit
            On.ProjectileWeapon.HasHit += ProjectileHitHook;
        }

        private void TryAddLabel(DamageList damages, Character target, Vector3 position)
        {
            if (!target.Alive || (!target.IsAI && !global.settings.Show_Player_DamageLabels) || (target.IsAI && !global.settings.Show_Enemy_DamageLabels))
            {
                return;
            }

            if (damages.TotalDamage < global.settings.minDamage) { return; }

            DamageLabel label = new DamageLabel()
            {
                creationTime = Time.time,
                creationPos = position,
                damage = damages,
                target = target,

                ranX = UnityEngine.Random.Range(-global.settings.labelRandomX, global.settings.labelRandomX),
                ranY = UnityEngine.Random.Range(-global.settings.labelRandomY, global.settings.labelRandomY)
            };

            //Debug.Log("Added label. Damage: " + damages + ", creation time: " + Time.time + ", target: " + target.Name);

            ActiveLabels.Add(label);
        }

        private void ProjectileHitHook(On.ProjectileWeapon.orig_HasHit orig, ProjectileWeapon self, Character _hitCharacter, Vector3 _hitPos, Vector3 _dir, bool _blocked)
        {
            var target = _hitCharacter;
            Character selfChar = At.GetValue(typeof(Item), self as Item, "m_ownerCharacter") as Character;

            if (At.GetValue(typeof(Weapon), self as Weapon, "m_alreadyHitChars") is List<Character> alreadyhit)
            {
                bool eligible = (target != null) && (target != selfChar) && (self.CanHitEveryoneButOwner || selfChar.TargetingSystem.IsTargetable(target));

                if (eligible && !alreadyhit.Contains(target))
                {
                    if (!_blocked)
                    {
                        DamageList damages = self.GetDamage(0);
                        target.Stats.GetMitigatedDamage(null, ref damages);

                        TryAddLabel(damages, target, _hitPos);
                    }
                    else
                    {
                         // Attack was blocked.
                    }
                }
            }

            orig(self, _hitCharacter, _hitPos, _dir, _blocked);
        }

        private void WeaponHitHook(On.Weapon.orig_HasHit orig, Weapon self, RaycastHit _hit, Vector3 _dir)
        {
            // basically copies the orig function, but instead of applying any damage, it just adds the label
            var target = _hit.collider.GetComponent<Hitbox>();
            var item = self as Item;
            Character selfChar = At.GetValue(typeof(Item), item, "m_ownerCharacter") as Character;

            if (At.GetValue(typeof(Weapon), self, "m_alreadyHitChars") is List<Character> alreadyhit)
            {
                bool eligible = (target.OwnerChar != null) && (target.OwnerChar != selfChar) && (self.CanHitEveryoneButOwner || selfChar.TargetingSystem.IsTargetable(target.OwnerChar));

                if (eligible && !alreadyhit.Contains(target.OwnerChar))
                {
                    float num = Vector3.Angle(target.OwnerChar.transform.forward, selfChar.transform.position - target.OwnerChar.transform.position);

                    if (!self.Unblockable && target.OwnerChar.Blocking && num < (float)((!target.OwnerChar.ShieldEquipped) ? Weapon.BLOCK_ANGLE : Weapon.SHIELD_BLOCK_ANGLE))
                    {
                        // Debug.Log("Blocked!");
                    }
                    else
                    {
                        var getID = At.GetValue(typeof(Weapon), self, "m_attackID");
                        if (getID is int attackID && attackID >= 0)
                        {
                            DamageList damages = self.GetDamage(attackID);

                            target.OwnerChar.Stats.GetMitigatedDamage(null, ref damages);

                            TryAddLabel(damages, target.OwnerChar, _hit.point);
                        }
                    }
                }
            }

            // orig
            orig(self, _hit, _dir);
        }

        private void PunctualHook(On.PunctualDamage.orig_DealHit orig, PunctualDamage self, Character target)
        {
            // orig
            orig(self, target);

            // custom
            if (target.Alive)
            {
                if (At.GetValue(typeof(PunctualDamage), self, "m_tempList") is DamageList damagelist)
                {
                    DamageList damages = damagelist.Clone();
                    target.Stats.GetMitigatedDamage(null, ref damages);

                    TryAddLabel(damages, target, target.CenterPosition);
                }
            }
        }

        private void WeaponSkillHook(On.WeaponDamage.orig_DealHit orig, WeaponDamage self, Character target)
        {
            // orig
            orig(self, target);

            // custom
            if (target.Alive)
            {
                PunctualDamage punctualSelf = self as PunctualDamage;

                if (At.GetValue(typeof(PunctualDamage), punctualSelf, "m_tempList") is DamageList damagelist)
                {
                    DamageList damages = damagelist.Clone();
                    target.Stats.GetMitigatedDamage(null, ref damages);

                    TryAddLabel(damages, target, target.CenterPosition);
                }
            }
        }
    }


    public class DamageLabel
    {
        //public UID uid;
        public float creationTime;
        public Vector3 creationPos;

        public DamageList damage;
        public Character target;

        public float ranX;
        public float ranY;
    }
}
