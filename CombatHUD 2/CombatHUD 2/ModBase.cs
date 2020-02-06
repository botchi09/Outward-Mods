using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Partiality.Modloader;
using UnityEngine;
using System.IO;

namespace CombatHUD_2
{
    public class ModBase : PartialityMod
    {
        public static string ModName = "CombatHUD_Rework";
        public static string ModVersion = "1.0";
        public static string ModAuthor = "Sinai";

        public static Settings settings = new Settings();
        private static readonly string savePath = @"Mods/" + ModName + ".json";

        public ModBase()
        {
            ModID = ModName;
            author = ModAuthor;
            Version = ModVersion;
        }

        public override void OnEnable()
        {
            base.OnEnable();

            var obj = new GameObject(ModName);
            GameObject.DontDestroyOnLoad(obj);

            LoadSettings();

            obj.AddComponent<HUDManager>();
        }

        public static void LoadSettings()
        {
            if (!Directory.Exists("Mods"))
            {
                Directory.CreateDirectory("Mods");
                SaveSettings();
            }
            else if (File.Exists(savePath))
            {
                try
                {
                    var json = File.ReadAllText(savePath);
                    var tempSettings = JsonUtility.FromJson<Settings>(json);
                    settings = tempSettings;
                }
                catch (Exception e)
                {
                    Debug.LogError(string.Format("[{0}] Couldn't load Settings file!\r\nError message: {1}", ModName, e.Message));
                }
            }
        }

        public static void SaveSettings()
        {
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }
            File.WriteAllText(savePath, JsonUtility.ToJson(settings, true));
        }
    }
}
