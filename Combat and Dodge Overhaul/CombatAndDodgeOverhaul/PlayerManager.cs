using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;
//using SinAPI;

namespace CombatAndDodgeOverhaul
{
    public class PlayerManager : MonoBehaviour
    {
        public static PlayerManager Instance;

        private Dictionary<string, float> PlayerLastHitTimes = new Dictionary<string, float>();

        internal void Awake()
        {
            Instance = this;

            // allow dodge cancelling
            On.Character.DodgeInput_1 += DodgeHookFunc;
            On.Character.HasHit += HasHitHook;

            // bag dodge stuff
            On.Character.SendDodgeTriggerTrivial += DodgeTriggerHook;

            // attack cancels blocking
            On.Character.AttackInput += AttackInputHook;

            //stamina hook
            On.CharacterStats.UpdateVitalStats += new On.CharacterStats.hook_UpdateVitalStats(UpdateVitalHook);
        }

        // for disallowing dodge cancel after you hit an enemy
        private void HasHitHook(On.Character.orig_HasHit orig, Character self, Weapon _weapon, float _damage, Vector3 _hitDir, Vector3 _hitPoint, float _angle, bool _blocked, Character _target, float _knockback, int _attackID = -999)
        {
            orig(self, _weapon, _damage, _hitDir, _hitPoint, _angle, _blocked, _target, _knockback, _attackID);

            if (_weapon is MeleeWeapon)
            {
                if (PlayerLastHitTimes.ContainsKey(self.UID))
                {
                    PlayerLastHitTimes[self.UID] = Time.time;
                }
                else
                {
                    PlayerLastHitTimes.Add(self.UID, Time.time);
                }
            }
        }

        // Dodge Cancelling Hook
        private void DodgeHookFunc(On.Character.orig_DodgeInput_1 orig, Character self, Vector3 _direction)
        {
            // only use this hook for local players. return orig everything else, or if the setting is disabled.
            if (self.IsAI || !self.IsPhotonPlayerLocal)
            {
                orig(self, _direction);
                return;
            }

            float staminaCost = (float)OverhaulGlobal.config.GetValue(Settings.Custom_Dodge_Cost);
            if (self.Inventory.SkillKnowledge.GetItemFromItemID(8205130))
            {
                staminaCost *= 0.5f;
            }

            if (!(bool)OverhaulGlobal.config.GetValue(Settings.Dodge_Cancelling))
            {
                if (At.GetValue(typeof(Character), self, "m_currentlyChargingAttack") is bool m_currentlyChargingAttack
                   && At.GetValue(typeof(Character), self, "m_preparingToSleep") is bool m_preparingToSleep
                   && At.GetValue(typeof(Character), self, "m_nextIsLocomotion") is bool m_nextIsLocomotion
                   && At.GetValue(typeof(Character), self, "m_dodgeAllowedInAction") is int m_dodgeAllowedInAction)
                {
                    if (self.Stats.MovementSpeed > 0f
                        && !m_preparingToSleep
                        && (!self.LocomotionAction || m_currentlyChargingAttack)
                        && (m_nextIsLocomotion || m_dodgeAllowedInAction > 0))
                    {
                        if (!self.Dodging)
                        {
                            SendDodge(self, staminaCost, _direction);
                        }
                        return;
                    }
                }
            }
            else
            {
                if (PlayerLastHitTimes.ContainsKey(self.UID) && Time.time - PlayerLastHitTimes[self.UID] < 0.35f)
                {
                    //  Debug.Log("Player has hit within the last 0.35 seconds. Dodge not allowed!");
                    return;
                }

                Character.HurtType hurtType = (Character.HurtType)At.GetValue(typeof(Character), self, "m_hurtType");

                // manual fix (game sometimes does not reset HurtType to NONE when animation ends.
                float timeout = 0.8f;
                if (hurtType == Character.HurtType.Knockdown)
                {
                    timeout = 2.0f;
                }

                if ((float)At.GetValue(typeof(Character), self, "m_timeOfLastStabilityHit") is float lasthit && Time.time - lasthit > timeout)
                {
                    hurtType = Character.HurtType.NONE;
                    At.SetValue(hurtType, typeof(Character), self, "m_hurtType");
                }

                // if we're not currently dodging or staggered, force an animation cancel dodge (provided we have enough stamina).
                if (!self.Dodging && hurtType == Character.HurtType.NONE)
                {
                    SendDodge(self, staminaCost, _direction);
                }
            }
        }

        private void SendDodge(Character self, float staminaCost, Vector3 _direction)
        {
            float f = (float)At.GetValue(typeof(CharacterStats), self.Stats, "m_stamina");

            if (f >= staminaCost)
            {
                At.SetValue(f - staminaCost, typeof(CharacterStats), self.Stats, "m_stamina");

                At.SetValue(0, typeof(Character), self, "m_dodgeAllowedInAction");

                if (self.CharacterCamera && self.CharacterCamera.InZoomMode)
                {
                    self.SetZoomMode(false);
                }

                self.ForceCancel(false, true);
                self.ResetCastType();

                (self as Photon.MonoBehaviour).photonView.RPC("SendDodgeTriggerTrivial", PhotonTargets.All, new object[] { _direction });

                At.Call(self, "ActionPerformed", new object[] { false });

                (self as MonoBehaviour).Invoke("ResetDodgeTrigger", 0.5f);
            }
        }

        // Dodge Burden hook
        private void DodgeTriggerHook(On.Character.orig_SendDodgeTriggerTrivial orig, Character self, Vector3 _direction)
        {
            if (!(bool)OverhaulGlobal.config.GetValue(Settings.Custom_Bag_Burden))
            {
                orig(self, _direction);
                return;
            }

            if (self.CurrentWeapon)

            if (self.HasDodgeDirection)
            {
                self.Animator.SetFloat("DodgeBlend", !self.DodgeRestricted ? 0.0f : GetDodgeRestriction(self));
            }
            self.Animator.SetTrigger("Dodge");

            if (self.CurrentlyChargingAttack)
            {
                //self.SendCancelCharging();
                At.Call(self, "SendCancelCharging", new object[0]);
            }

            // get sound player with null coalescing operator
            (At.GetValue(typeof(Character), self, "m_dodgeSoundPlayer") as SoundPlayer)?.Play(false);

            //self.m_dodging = true;
            At.SetValue(true, typeof(Character), self, "m_dodging");

            //self.StopBlocking();
            At.Call(self, "StopBlocking", new object[0]);

            // null coalescing OnDodgeEvent invoke
            self.OnDodgeEvent?.Invoke();

            if (At.GetValue(typeof(Character), self, "m_characterSoundManager") is CharacterSoundManager charSounds)
            {
                Global.AudioManager.PlaySoundAtPosition(charSounds.GetDodgeSound(), self.transform, 0f, 1f, 1f, 1f, 1f);
            }

            self.SendMessage("DodgeTrigger", _direction, SendMessageOptions.DontRequireReceiver);
        }

        // dodge burden helper
        private float GetDodgeRestriction(Character self)
        {
            // Handle if our bag doesn't restrict us anyway
            if (!self.DodgeRestricted)
                return 0.0f;

            // Find the currently equipped bag, it should exist
            Bag bag = self.Inventory.EquippedBag;
            if (bag == null) // This shouldn't happen but who knows
                return 0.0f;

            float weight = bag.Weight * 100;
            float ratio = (weight / bag.BagCapacity) * 0.01f;

            if (ratio < ((float)OverhaulGlobal.config.GetValue(Settings.min_burden_weight) * 0.01f))
            {
                return (float)OverhaulGlobal.config.GetValue(Settings.min_slow_effect) * 0.01f;
            }
            else
            {
                return Mathf.Clamp(ratio, (float)OverhaulGlobal.config.GetValue(Settings.min_slow_effect), (float)OverhaulGlobal.config.GetValue(Settings.max_slow_effect)) * 0.01f;
            }
        }

        // attacking cancels blocking hook
        private bool AttackInputHook(On.Character.orig_AttackInput orig, Character self, int _type, int _id = 0)
        {
            if (self.IsLocalPlayer && (bool)OverhaulGlobal.config.GetValue(Settings.Attack_Cancels_Blocking) && !self.IsAI && self.Blocking)
            {
                StartCoroutine(StopBlockingCoroutine(self));
                At.Call(self, "StopBlocking", null);
                At.SetValue(false, typeof(Character), self, "m_blockDesired");
            }

            return orig(self, _type, _id);
        }

        private IEnumerator StopBlockingCoroutine(Character character)
        {
            yield return new WaitForSeconds(0.05f); // 50ms wait (1 or 2 frames)

            At.Call(character, "StopBlocking", null);
            At.SetValue(false, typeof(Character), character, "m_blockDesired");
        }

        // stamina regen buff
        private void UpdateVitalHook(On.CharacterStats.orig_UpdateVitalStats orig, CharacterStats self)
        {
            if (At.GetValue(typeof(CharacterStats), self, "m_timeOfLastStamUse") is float timeOfLast && Time.time - timeOfLast > (float)OverhaulGlobal.config.GetValue(Settings.Stamina_Regen_Delay)
                && At.GetValue(typeof(CharacterStats), self, "m_stamina") is float m_stamina
                && At.GetValue(typeof(CharacterStats), self, "m_character") is Character character
                && !character.Blocking)
            {
                float regen = (float)OverhaulGlobal.config.GetValue(Settings.Extra_Stamina_Regen) * Time.deltaTime;
                float newStamina = Mathf.Clamp(m_stamina + regen, 0, self.ActiveMaxStamina);
                At.SetValue(newStamina, typeof(CharacterStats), self, "m_stamina");
            }

            orig(self);
        }

        
    }
}
