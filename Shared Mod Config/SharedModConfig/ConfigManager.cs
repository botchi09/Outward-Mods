using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using System.Xml.Serialization;

namespace SharedModConfig
{
    public class ConfigManager : MonoBehaviour
    {
        public static ConfigManager Instance;

        public bool InitDone { get => MenuManager.Instance.InitDone; }
        public bool IsInitDone() { return InitDone; } // legacy

        public static Dictionary<string, ModConfig> RegisteredConfigs = new Dictionary<string, ModConfig>();
        private static readonly string saveFolder = @"Mods/ModConfigs";

        private static List<ModConfig> m_delayedConfigs = new List<ModConfig>();

        internal void Awake()
        {
            Instance = this;

            if (!Directory.Exists(saveFolder))
            {
                Directory.CreateDirectory(saveFolder);
            }

            MenuManager.OnMenuLoaded += INTERNAL_OnMenuLoaded;
        }

        public static void RegisterSettings(ModConfig config)
        {
            if (string.IsNullOrEmpty(config.ModName))
            {
                Debug.LogError("[SharedModConfig] A mod is trying to register with a null or empty ModName!");
                return;
            }

            if (RegisteredConfigs.ContainsKey(config.ModName))
            {
                Debug.LogError(config.ModName + " is already registered!");
                return;
            }

            string path = saveFolder + "/" + config.ModName + ".xml";
            bool hasSettings = false;

            if (File.Exists(path))
            {
                hasSettings = LoadXML(path, config);
            }

            if (!hasSettings)
            {
                SaveXML(config);

                foreach (var setting in config.Settings)
                {
                    if (setting.DefaultValue != null)
                    {
                        setting.SetValue(setting.DefaultValue);
                    }
                }
            }

            // if MenuManager has done init, add now. Otherwise add to delayed callback list.
            if (MenuManager.Instance != null && MenuManager.Instance.InitDone)
            {
                MenuManager.Instance.AddConfig(config);
            }
            else
            {
                m_delayedConfigs.Add(config);
            }

            RegisteredConfigs.Add(config.ModName, config);

            // Call the Callback for the Settings, now that they're ready.
            config.INTERNAL_OnSettingsLoaded();
        }

        public void INTERNAL_OnMenuLoaded()
        {
            foreach (var config in m_delayedConfigs)
            {
                MenuManager.Instance.AddConfig(config);
            }

            m_delayedConfigs.Clear();
            m_delayedConfigs = null;
        }

        private static bool LoadXML(string path, ModConfig config)
        {
            Type[] extraTypes = { typeof(BBSetting), typeof(BoolSetting), typeof(FloatSetting), typeof(StringSetting) };
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ModConfig), extraTypes);
            using (StreamReader streamReader = new StreamReader(path))
            {
                var tempCfg = (ModConfig)xmlSerializer.Deserialize(streamReader);

                if (tempCfg != null)
                {
                    foreach (BBSetting setting in config.Settings)
                    {
                        if (tempCfg.Settings.Find(x => x.Name == setting.Name) is BBSetting tempSetting)
                        {
                            setting.SetValue(tempSetting.GetValue());
                        }
                        else
                        {
                            setting.SetValue(setting.DefaultValue);
                        }
                    }
                    streamReader.Close();
                    return true;
                }
                else
                {
                    Debug.LogError("[SharedModConfig] Fatal error trying to load settings from: " + path);
                    streamReader.Close();
                    return false;
                }

            }
        }

        private static void SaveXML(ModConfig config)
        {
            if (!Directory.Exists(saveFolder)) { Directory.CreateDirectory(saveFolder); }

            var path = saveFolder + "/" + config.ModName + ".xml";
            if (File.Exists(path)) { File.Delete(path); }

            Type[] extraTypes = { typeof(BBSetting), typeof(BoolSetting), typeof(FloatSetting), typeof(StringSetting) };
            XmlSerializer xml = new XmlSerializer(typeof(ModConfig), extraTypes);
            FileStream file = File.Create(path);
            xml.Serialize(file, config);
            file.Close();
        }

        internal void OnDisable()
        {
            SaveSettings();
        }

        internal void OnApplicationQuit()
        {
            SaveSettings();
        }

        private void SaveSettings()
        {
            foreach (ModConfig config in RegisteredConfigs.Values)
            {
                SaveXML(config);
            }
        }
    }
}
