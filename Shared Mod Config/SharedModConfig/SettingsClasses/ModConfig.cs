using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Xml.Serialization;

namespace SharedModConfig
{
    [Serializable]
    public class ModConfig
    {
        public string ModName;
        public double SettingsVersion;
        public List<BBSetting> Settings = new List<BBSetting>();

        [XmlIgnore]
        public GameObject m_linkedPanel;

        public void Register()
        {
            ConfigManager.Instance.RegisterSettings(this);
        }

        public object GetValue(string SettingName)
        {
            if (Settings.Find(x => x.Name == SettingName) is BBSetting setting)
            {
                return setting.GetValue();
            }
            else
            {
                Debug.LogError("[SharedModConfig] A mod requested the value of '" + SettingName + "' on Config '" + this.ModName + "', but such a setting was not found!");
                return null;
            }
        }
    }
}
