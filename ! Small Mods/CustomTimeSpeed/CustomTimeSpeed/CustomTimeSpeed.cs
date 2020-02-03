using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Partiality.Modloader;
using UnityEngine;
using System.IO;

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
        public float Time_Multiplier;
    }

    public class TimeScript : MonoBehaviour
    {
        public Settings settings;
        private static readonly string savePath = @"Mods\CustomTime.json";

        public void Init()
        {
            LoadSettings();
            if (settings.Time_Multiplier < 0) { settings.Time_Multiplier = 0; }

            On.TOD_Time.AddSeconds += TOD_Time_AddSeconds;
        }

        private void TOD_Time_AddSeconds(On.TOD_Time.orig_AddSeconds orig, TOD_Time self, float seconds, bool adjust = true)
        {
            orig(self, seconds * settings.Time_Multiplier, adjust);
        }

        private void LoadSettings()
        {
            settings = new Settings { Time_Multiplier = 1.0f, };

            if (File.Exists(savePath))
            {
                var s2 = JsonUtility.FromJson<Settings>(File.ReadAllText(savePath));
                if (s2 != null)
                {
                    settings = s2;
                }
            }

            SaveSettings();
        }

        private void SaveSettings()
        {
            if (File.Exists(savePath)) { File.Delete(savePath); }
            File.WriteAllText(savePath, JsonUtility.ToJson(settings, true));
        }

        internal void OnDisable()
        {
            SaveSettings();
        }
    }
}
