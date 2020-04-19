using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Partiality.Modloader;

namespace NecromancerSkills
{
    public class ModBase : PartialityMod
    {
        public GameObject _obj = null;
        public double version = 1.3;

        public static Settings settings;
        private static readonly string savePath = @"Mods\NecromancySkills.json";

        public ModBase()
        {
            ModID = "NecroSkills";
            Version = version.ToString("0.00");
            author = "Sinai";
        }

        public override void OnEnable()
        {
            base.OnEnable();

            LoadSettings();

            _obj = new GameObject(ModID);
            GameObject.DontDestroyOnLoad(_obj);

            // Add our custom components.
            // These talk to each other by declaring a static Instance of themselves, which other components use as a handle.
            _obj.AddComponent<TrainerManager>();
            _obj.AddComponent<SkillManager>();
            _obj.AddComponent<SummonManager>();
            _obj.AddComponent<RPCManager>();
        }

        private void LoadSettings()
        {
            bool newSettings = true;
            if (File.Exists(savePath))
            {
                string json = File.ReadAllText(savePath);
                var s2 = JsonUtility.FromJson<Settings>(json);
                if (s2 != null)
                {
                    settings = s2;
                    newSettings = false;
                }
            }
            if (newSettings)
            {
                settings = new Settings();
                SaveSettings();
            }
        }

        private void SaveSettings()
        {
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }
            File.WriteAllText(savePath, JsonUtility.ToJson(settings, true));
        }
    }
}
