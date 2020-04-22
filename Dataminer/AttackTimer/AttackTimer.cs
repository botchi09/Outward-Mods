using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;
using System.Collections;
using BepInEx;
using HarmonyLib;

namespace AttackTimer
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("com.sinai.PartialityWrapper", BepInDependency.DependencyFlags.HardDependency)]
    public class AttackTimer : BaseUnityPlugin
    {
        const string GUID = "com.sinai.dataminer.attacktimer";
        const string NAME = "Attack Timer";
        const string VERSION = "1.1";

        public static AttackTimer Instance;

        public static bool TimerStarted = false;

        public static float ComboTimer = 0f;
        public static float LastComboTime = 0f;
        public static int ComboStep = 0;
        public static int ComboLength = 2;

        public static int LastAttackID = -1;
        public static DamageList LastDamage = new DamageList();

        internal void Awake()
        {
            Instance = this;

            var h = new Harmony(GUID);
            h.PatchAll();
        }

        internal void Update()
        {
            if (TimerStarted)
            {
                ComboTimer += Time.deltaTime;
            }
        }

        [HarmonyPatch(typeof(Character), "ReceiveDamage")]
        public class Character_ReceiveDamage
        {
            [HarmonyPrefix]
            public static bool Prefix(Character __instance, float _damage, Vector3 _hitVec, bool _syncIfClient = true)
            {
                var self = __instance;

                Debug.Log(string.Format("{0} | {1} received {2} damage", Math.Round(Time.time, 1), self.Name, Math.Round(_damage, 2)));

                return true;
            }
        }

        [HarmonyPatch(typeof(Character), "StartAttack")]
        public class Character_StartAttack
        {
            public static void Postfix(Character __instance, int _type, int _id)
            {
                var self = __instance;

                Instance.StartCoroutine(Instance.GetDamageCoroutine(self));

                if (ComboStep < ComboLength)
                {
                    if (!TimerStarted)
                    {
                        LastComboTime = ComboTimer;
                        ComboTimer = 0f;
                        TimerStarted = true;
                    }

                    ComboStep++;
                }
                else
                {
                    LastComboTime = ComboTimer;
                    ComboTimer = 0f;
                    ComboStep = 1;
                    TimerStarted = true;
                }
            }
        }

        private IEnumerator GetDamageCoroutine(Character self)
        {
            float start = Time.time;
            while (Time.time - start < 5f && LastAttackID == (int)typeof(Weapon).GetField("m_attackID", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(self.CurrentWeapon))
            {
                yield return new WaitForSeconds(0.05f);
            }

            LastAttackID = (int)typeof(Weapon).GetField("m_attackID", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(self.CurrentWeapon);
            LastDamage = LastAttackID == 0 ? self.CurrentWeapon.GetDamage(1) : self.CurrentWeapon.GetDamage(LastAttackID);
        }
    }
}
