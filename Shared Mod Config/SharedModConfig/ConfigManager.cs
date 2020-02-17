using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using SideLoader;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;

namespace SharedModConfig
{
    public class ConfigManager : MonoBehaviour
    {
        public static ConfigManager Instance;

        private static Dictionary<string, ModConfig> RegisteredConfigs = new Dictionary<string, ModConfig>();
        private static readonly string saveFolder = @"Mods/ModConfigs";

        public bool IsInitDone()
        {
            return MenuManager.Instance.IsInitDone();
        }

        internal void Awake()
        {
            Instance = this;

            if (!Directory.Exists("Mods")) 
            {
                Directory.CreateDirectory("Mods"); 
            }
            if (!Directory.Exists(saveFolder))
            {
                Directory.CreateDirectory(saveFolder);
            }
        }

        internal void Update()
        {
            foreach (ModConfig config in RegisteredConfigs.Values)
            {
                if (config.m_linkedPanel.activeSelf)
                {
                    foreach (BBSetting setting in config.Settings)
                    {
                        setting.UpdateValue();
                    }
                }
            }
        }

        public void RegisterSettings(ModConfig config)
        {
            if (config == null || config.ModName == null)
            {
                Debug.LogError("[SharedModConfig] A mod is trying to register with a null Name or null ModConfig!");
                return;
            }

            if (!MenuManager.Instance.IsInitDone())
            {
                StartCoroutine(RegisterSettingsDelayed(config));
                return;
            }

            if (RegisteredConfigs.ContainsKey(config.ModName))
            {
                Debug.LogError(config.ModName + " is already registered!");
            }
            else
            {
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

                MenuManager.Instance.AddConfig(config);

                RegisteredConfigs.Add(config.ModName, config);
            }
        }

        private IEnumerator RegisterSettingsDelayed(ModConfig config)
        {
            while (!MenuManager.Instance.IsInitDone())
            {
                yield return new WaitForSeconds(1f);
            }

            RegisterSettings(config);
        }

        private bool LoadXML(string path, ModConfig config)
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

                    //if (tempCfg.SettingsVersion < config.SettingsVersion)
                    //{
                    //    Debug.LogWarning("[SharedModConfig] The settings version on disk (" + 
                    //        tempCfg.SettingsVersion + 
                    //        ") is older than the current mod settings: " + 
                    //        config.SettingsVersion
                    //        + "\r\nThe old file will be renamed to " + path + ".bak");

                    //    streamReader.Close();

                    //    string bakPath = path + ".bak";
                    //    if (File.Exists(bakPath))
                    //    {
                    //        File.Delete(bakPath);
                    //    }

                    //    File.Move(path, bakPath);
                    //    return false;
                    //}
                }
                else
                {
                    Debug.LogError("[SharedModConfig] Fatal error trying to load settings from: " + path);
                    streamReader.Close();
                    return false;
                }

            }
        }

        private void SaveXML(ModConfig config)
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
