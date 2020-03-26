using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Partiality.Modloader;
using UnityEngine;
using System.Reflection;
using System.Collections;

namespace AttackTimer
{
    public class ModLoader : PartialityMod
    {
        public ModLoader()
        {
            author = "Sinai";
            Version = "1.0";
            ModID = "AttackTimer";
        }

        public override void OnEnable()
        {
            base.OnEnable();

            var obj = new GameObject("AttackTimer");
            GameObject.DontDestroyOnLoad(obj);

            obj.AddComponent<AttackTimer>();
            obj.AddComponent<TimerGUI>();
        }
    }

    public class AttackTimer : MonoBehaviour
    {
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

            On.Character.StartAttack += StartAttackHook;
        }

        internal void Update()
        {
            if (TimerStarted)
            {
                ComboTimer += Time.deltaTime;
            }
        }

        private void StartAttackHook(On.Character.orig_StartAttack orig, Character self, int i, int ii)
        {
            orig(self, i, ii);

            StartCoroutine(GetDamageCoroutine(self));

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
