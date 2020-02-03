using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;
using SinAPI;

namespace CombatAndDodgeOverhaul
{
    public class StabilityManager : MonoBehaviour
    {
        public static StabilityManager Instance;

        public Dictionary<string, float> LastStaggerTimes = new Dictionary<string, float>();

        internal void Awake()
        {
            Instance = this;

            // stability mods
            On.Character.StabilityHit += new On.Character.hook_StabilityHit(StabilityHitHook);
            On.Character.UpdateStability += new On.Character.hook_UpdateStability(UpdateStabilityHook);
            On.Character.SlowDown += new On.Character.hook_SlowDown(SlowHook);
            On.Character.AutoKnock += new On.Character.hook_AutoKnock(AutoKnockHook);
        }

        // This is the main Stability Hit function, called whenever a Character receives impact damage.
        // It is mostly a copy+paste of the original, I've just had to use reflection for all the private fields / methods, and patched in our custom values.
        // The only real thing I added was the 'stagger immunity period', which is essentially a timeout period to prevent stun locking after getting staggered.

        private void StabilityHitHook(On.Character.orig_StabilityHit orig, Character self, float _knockValue, float _angle, bool _block, Character _dealerChar)
        {
            var _base = self as Photon.MonoBehaviour;

            if (!OverhaulGlobal.settings.Enable_StabilityMods)
            {
                orig(self, _knockValue, _angle, _block, _dealerChar);
                return;
            }

            if (At.GetValue(typeof(Character), self, "m_impactImmune") is bool m_impactImmune
                && At.GetValue(typeof(Character), self, "m_shieldStability") is float m_shieldStability
                && At.GetValue(typeof(Character), self, "m_stability") is float m_stability
                && At.GetValue(typeof(Character), self, "m_knockbackCount") is float m_knockbackCount
                && At.GetValue(typeof(Character), self, "m_knockHurtAllowed") is bool m_knockHurtAllowed
                && At.GetValue(typeof(Character), self, "m_currentlyChargingAttack") is bool m_currentlyChargingAttack
                && At.GetValue(typeof(Character), self, "m_animator") is Animator m_animator)
            {
                // Begin actual stability hit function
                var hit = _knockValue;
                if (hit < 0)
                    hit = 0;

                if (!m_impactImmune && hit > 0f)
                {
                    //OLogger.Log("--------- " + self.Name + " ---------");

                    // check stagger immunity dictionary (custom)
                    float lastStagger = -1;
                    if (LastStaggerTimes.ContainsKey(self.UID))
                    {
                        lastStagger = LastStaggerTimes[self.UID];
                    }

                    // if you run out of stamina and get hit, you will always get staggered. (unchanged, except to reflect custom stagger threshold)
                    if (self.Stats.CurrentStamina < 1f)
                    {
                        float hitToStagger = m_shieldStability + m_stability - (100 - OverhaulGlobal.settings.Stagger_Threshold);
                        if (hit < hitToStagger)
                        {
                            hit = hitToStagger;
                        }
                        //OLogger.Error("Stamina autostagger called! hitToStagger: " + hitToStagger + ", hit: " + hit);
                    }

                    At.SetValue(Time.time, typeof(Character), self, "m_timeOfLastStabilityHit");
                   // Debug.Log("Set " + Time.time + " as character's last stability hit");

                    if (self.CharacterCamera != null && hit > 0f)
                    {
                        self.CharacterCamera.Hit(hit * 6f);
                    }

                    // check shield stability if blocking (unchanged)
                    if (_block && m_shieldStability > 0f)
                    {
                        if (hit > m_shieldStability)
                        {
                            var num2 = m_stability - (hit - m_shieldStability);
                            At.SetValue(num2, typeof(Character), self, "m_stability");
                            m_stability = num2;
                        }
                        var num3 = Mathf.Clamp(m_shieldStability - hit, 0f, 50f);
                        At.SetValue(num3, typeof(Character), self, "m_shieldStability");
                        m_shieldStability = num3;
                    }
                    // check non-blocking stability (unchanged)
                    else
                    {
                        var num2 = Mathf.Clamp(m_stability - hit, 0f, 100f);
                        At.SetValue(num2, typeof(Character), self, "m_stability");
                        m_stability = num2;
                    }
                    // if hit takes us below knockdown threshold, or if AI auto-knockdown stagger count was reached...
                    if (m_stability <= OverhaulGlobal.settings.Knockdown_Threshold || m_knockbackCount >= OverhaulGlobal.settings.Enemy_AutoKD_Count)
                    {
                        //OLogger.Error("Knockdown! Hit Value: " + _knockValue + ", current stability: " + m_stability);

                        if ((!self.IsAI && _base.photonView.isMine) || (self.IsAI && (_dealerChar == null || _dealerChar.photonView.isMine)))
                        {
                            _base.photonView.RPC("SendKnock", PhotonTargets.All, new object[]
                            {
                                true,
                                m_stability
                            });
                        }
                        else
                        {
                            At.Call(self, "Knock", new object[]
                            {
                                true
                            });
                        }
                        At.SetValue(0f, typeof(Character), self, "m_stability");
                        m_stability = 0f;
                        if (self.IsPhotonPlayerLocal)
                        {
                            self.BlockInput(false);
                        }
                    }
                    // else if hit is a stagger...
                    else if (m_stability <= OverhaulGlobal.settings.Stagger_Threshold && (Time.time - lastStagger > OverhaulGlobal.settings.Stagger_Immunity_Period))
                    {
                        // OLogger.Warning("Stagger! Hit Value: " + _knockValue + ", current stability: " + m_stability);

                        // update Stagger Immunity dictionary
                        if (!LastStaggerTimes.ContainsKey(self.UID))
                        {
                            LastStaggerTimes.Add(self.UID, Time.time);
                        }
                        else
                        {
                            LastStaggerTimes[self.UID] = Time.time;
                        }

                        if ((!self.IsAI && _base.photonView.isMine) || (self.IsAI && (_dealerChar == null || _dealerChar.photonView.isMine)))
                        {
                            _base.photonView.RPC("SendKnock", PhotonTargets.All, new object[]
                            {
                                false,
                                m_stability
                            });
                        }
                        else
                        {
                            At.Call(self, "Knock", new object[]
                            {
                                false
                            });
                        }
                        if (self.IsPhotonPlayerLocal && _block)
                        {
                            self.BlockInput(false);
                        }
                    }
                    // else if we are not blocking...
                    else if (!_block)
                    {
                        // OLogger.Log("Value: " + _knockValue + ", new stability: " + m_stability);
                        if (m_knockHurtAllowed)
                        {
                            At.SetValue(Character.HurtType.Hurt, typeof(Character), self, "m_hurtType");

                            if (m_currentlyChargingAttack)
                            {
                                self.CancelCharging();
                            }

                            m_animator.SetTrigger("Knockhurt");
                            _base.StopCoroutine("KnockhurtRoutine");

                            MethodInfo _knockhurtRoutine = self.GetType().GetMethod("KnockhurtRoutine", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            IEnumerator _knockEnum = (IEnumerator)_knockhurtRoutine.Invoke(self, new object[] { hit });
                            _base.StartCoroutine(_knockEnum);
                        }

                        if (m_stability <= OverhaulGlobal.settings.Stagger_Immunity_Period)
                        {
                            // OLogger.Error(self.Name + " would have staggered. Current delta: " + (Time.time - lastStagger));
                        }
                    }
                    else // hit was blocked and no stagger
                    {
                        StaggerAttacker(self, m_animator, _dealerChar);
                    }
                    m_animator.SetInteger("KnockAngle", (int)_angle);
                    self.StabilityHitCall?.Invoke();
                }
                else if (!m_impactImmune && _block) // hit dealt 0 impact and was blocked
                {
                    StaggerAttacker(self, m_animator, _dealerChar);
                }
            }
        }

        private void StaggerAttacker(Character self, Animator m_animator, Character _dealerChar)
        {
            // OLogger.Log(self.Name + " blocked the attack. Shield stability: " + m_shieldStability);
            At.SetValue(Character.HurtType.NONE, typeof(Character), self, "m_hurtType");
            if (self.InLocomotion)
            {
                m_animator.SetTrigger("BlockHit");
            }

            if (OverhaulGlobal.settings.Enable_StabilityMods && OverhaulGlobal.settings.Blocking_Staggers_Attacker)
            {
                // OLogger.Log("autoknocking " + _dealerChar.Name);
                if (_dealerChar.CurrentWeapon.Type != Weapon.WeaponType.Bow)
                {
                    _dealerChar.AutoKnock(false, new Vector3(0, 0, 0));
                }
            }
        }

        // The update stability function, for regenerating stability and the AI auto-knockdown count
        private void UpdateStabilityHook(On.Character.orig_UpdateStability orig, Character self)
        {
            if (!OverhaulGlobal.settings.Enable_StabilityMods)
            {
                orig(self);
                return;
            }

            if (At.GetValue(typeof(Character), self, "m_stability") is float m_stability
                && At.GetValue(typeof(Character), self, "m_timeOfLastStabilityHit") is float m_timeOfLastStabilityHit
                && At.GetValue(typeof(Character), self, "m_shieldStability") is float m_shieldStability
                && At.GetValue(typeof(Character), self, "m_knockbackCount") is float m_knockbackCount)
            {
                // ----------- original method, unchanged other than to reflect custom values -------------

                if (OverhaulGlobal.settings.No_Stability_Regen_When_Blocking && self.Blocking) // no stability regen while blocking! otherwise too op
                    return;

                float num = Time.time - m_timeOfLastStabilityHit;
                if (num > OverhaulGlobal.settings.Stability_Regen_Delay)
                {
                    if (m_stability < 100f)
                    {
                        var num2 = Mathf.Clamp(m_stability + (self.StabilityRegen * OverhaulGlobal.settings.Stability_Regen_Speed) * Time.deltaTime, 0f, 100f);
                        At.SetValue(num2, typeof(Character), self, "m_stability");

                    }
                    else if (m_shieldStability < 50f)
                    {
                        var num2 = Mathf.Clamp(m_shieldStability + self.StabilityRegen * Time.deltaTime, 0f, 50f);
                        At.SetValue(num2, typeof(Character), self, "m_shieldStability");
                    }
                    if (num > OverhaulGlobal.settings.Enemy_AutoKD_Reset_Time)
                    {
                        bool flag = m_knockbackCount > 0;
                        var num2 = Mathf.Clamp(m_knockbackCount - Time.deltaTime, 0f, 4f);
                        At.SetValue(num2, typeof(Character), self, "m_knockbackCount");
                        if (flag && num2 <= 0)
                        {
                            // OLogger.Log("Resetting AI stagger count for " + self.Name);
                        }
                    }
                }
            }
        }

        // This function is how the game sends the 'slow down' effect to the attacker and receiver when weapons hit. Basically it gives the feeling that your weapon has collided.
        private void SlowHook(On.Character.orig_SlowDown orig, Character self, float _slowVal, float _timeTo, float _timeStay, float _timeFrom)
        {
            if (!OverhaulGlobal.settings.Enable_StabilityMods)
            {
                orig(self, _slowVal, _timeTo, _timeStay, _timeFrom);
                return;
            }

            var num = OverhaulGlobal.settings.SlowDown_Modifier;

            _slowVal = Mathf.Clamp(num * num, 0.1f, 2) * _slowVal;  // apply the custom modifier MORE to the "slowVal" value, seems to get better results. Clamp above 0.1 otherwise the attack can last like 30+ seconds
            _timeTo = ((4 + num) / 5) * _timeTo; // apply MUCH LESS to "timeTo", for same reason.
            _timeStay = num * _timeStay; // apply raw modifier to these two
            _timeFrom = num * _timeFrom;

            orig(self, _slowVal, _timeTo, _timeStay, _timeFrom); // call orig method with modified values
        }

        // autoknock is used by things like Brace, just instantly staggers enemy
        private void AutoKnockHook(On.Character.orig_AutoKnock orig, Character self, bool _down, Vector3 _dir)
        {
            if (!OverhaulGlobal.settings.Enable_StabilityMods)
            {
                orig(self, _down, _dir);
                return;
            }

            var _base = self as Photon.MonoBehaviour;
            if (At.GetValue(typeof(Character), self, "m_stability") is float m_stability)
            {
                //OLogger.Error("Autoknock, m_stability: " + m_stability);

                At.Call(self, "StabilityHit", new object[] {
                    (!_down) ? Mathf.Clamp(m_stability - OverhaulGlobal.settings.Stagger_Threshold, 1f, 100 - OverhaulGlobal.settings.Stagger_Threshold) : m_stability,
                    Vector3.Angle(_base.transform.forward, -_dir),
                    _down,
                    null
                });
            }
            return;
        }
    }
}
