using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Partiality.Modloader;
using UnityEngine;
using System.IO;
using SharedModConfig;
using System.Collections;

namespace SlowerTime
{
    public class CustomTimeSpeed : PartialityMod
    {
        public GameObject _obj = null;
        public TimeScript script;

        public CustomTimeSpeed()
        {
            this.ModID = "CustomTimeSpeed";
            this.Version = "1.0";
            this.author = "Sinai";
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (_obj == null)
            {
                _obj = new GameObject("CustomTimeSpeed");
                GameObject.DontDestroyOnLoad(_obj);
            }

            script = _obj.AddComponent<TimeScript>();
            script.Init();
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }
    }

    public class Settings
    {
        public static string Time_Multiplier = "Time_Multiplier";
    }

    public class TimeScript : MonoBehaviour
    {
        public ModConfig config;

        public void Init()
        {
            config = SetupConfig();

            StartCoroutine(SetupCoroutine());

            On.TOD_Time.AddSeconds += TOD_Time_AddSeconds;
        }

        private void TOD_Time_AddSeconds(On.TOD_Time.orig_AddSeconds orig, TOD_Time self, float seconds, bool adjust = true)
        {
            orig(self, seconds * (float)config.GetValue(Settings.Time_Multiplier), adjust);
        }

        private ModConfig SetupConfig()
        {
            var newConfig = new ModConfig
            {
                ModName = "Custom Time Speed",
                SettingsVersion = 1.0,
                Settings = new List<BBSetting>
                {
                    new FloatSetting
                    {
                        Name = Settings.Time_Multiplier,
                        Description = "Time Multiplier (1.0x = normal time, 0.5x = half speed, 2.0x = double)",
                        DefaultValue = 1.0f,
                        MinValue = 0f,
                        MaxValue = 10f,
                        RoundTo = 2,
                        ShowPercent = false
                    }
                }
            };

            return newConfig;
        }

        private IEnumerator SetupCoroutine()
        {
            while (ConfigManager.Instance == null || !ConfigManager.Instance.IsInitDone())
            {
                yield return new WaitForSeconds(0.1f);
            }

            config.Register();
        }
    }
}
