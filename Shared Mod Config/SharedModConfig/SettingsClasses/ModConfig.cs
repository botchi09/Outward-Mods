using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Xml.Serialization;

namespace SharedModConfig
{
    public class ModConfig
    {
        // Public, Serializable
        public string ModName;
        public double SettingsVersion;
        public List<BBSetting> Settings = new List<BBSetting>();

        // On Settings Loaded callback
        public delegate void SettingsLoaded();
        public event SettingsLoaded OnSettingsLoaded;

        // On Settings Saved Callback
        public delegate void SettingsSaved();
        public event SettingsSaved OnSettingsSaved;

        // internal use only
        [XmlIgnore] private Dictionary<string, BBSetting> m_Settings = new Dictionary<string, BBSetting>();
        [XmlIgnore] public GameObject m_linkedPanel;

        public void Register()
        {
            foreach (var setting in Settings)
            {
                m_Settings.Add(setting.Name, setting);
            }

            ConfigManager.RegisterSettings(this);
        }

        public void INTERNAL_OnSettingsLoaded()
        {
            if (OnSettingsLoaded != null)
            {
                Debug.Log("OnSettingsLoaded Callback for " + this.ModName);
                OnSettingsLoaded.Invoke();
            }
        }

        public void INTERNAL_OnSettingsSaved()
        {
            foreach (var setting in Settings)
            {
                setting.UpdateValue();
            }

            if (OnSettingsSaved != null)
            {
                Debug.Log("OnSettingsSaved Callback for " + this.ModName);
                OnSettingsSaved.Invoke();
            }
        }

        public object GetValue(string SettingName)
        {
            if (m_Settings.ContainsKey(SettingName))
            {
                return m_Settings[SettingName].GetValue();
            }
            else
            {
                Debug.LogError("[SharedModConfig] A mod requested the value of '" + SettingName + "' on Config '" + this.ModName + "', but such a setting was not found!");
                return null;
            }
        }
    }
}
