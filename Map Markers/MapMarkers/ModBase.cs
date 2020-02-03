using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Partiality.Modloader;
using UnityEngine;
using System.IO;

namespace MapMarkers
{
    public class ModBase : PartialityMod
    {
        public static string ModName = "MapMarkers";
        public static double ModVersion = 1.0;
        public static string ModAuthor = "Sinai";

        public static Settings settings = new Settings();
        private static readonly string savePath = @"Mods/MapMarkers.json";

        public ModBase()
        {
            this.ModID = ModName;
            this.Version = ModVersion.ToString();
            this.author = ModAuthor;
        }

        public override void OnEnable()
        {
            base.OnEnable();

            var obj = new GameObject(ModName);
            GameObject.DontDestroyOnLoad(obj);
            obj.AddComponent<MapManager>();

            // settings
            LoadSettings();
        }

        private void LoadSettings()
        {
            settings = new Settings();
            if (!Directory.Exists("Mods"))
            {
                Directory.CreateDirectory("Mods");
            }

            if (!File.Exists(savePath))
            {
                File.WriteAllText(savePath, JsonUtility.ToJson(settings, true));
            }
            else
            {
                var temp_settings = JsonUtility.FromJson<Settings>(File.ReadAllText(savePath));
                if (temp_settings != null)
                {
                    settings = temp_settings;
                }
            }
        }
    }
}
