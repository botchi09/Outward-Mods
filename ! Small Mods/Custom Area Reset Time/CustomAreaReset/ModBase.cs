using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Partiality.Modloader;
using System.IO;
using SharedModConfig;

namespace CustomAreaResetTime
{
    public class ModBase : PartialityMod
    {
        public ModBase()
        {
            this.ModID = "CustomAreaResetTime";
            this.author = "Sinai";
            this.Version = "1.0";
        }

        public override void OnEnable()
        {
            base.OnEnable();

            var obj = new GameObject(this.ModID);
            GameObject.DontDestroyOnLoad(obj);
            obj.AddComponent<CustomAreaReset>();
        }
    }

    public class CustomAreaReset : MonoBehaviour
    {
        public static CustomAreaReset Instance;
        public static ModConfig config;

        internal void Awake()
        {
            Instance = this;

            On.AreaManager.IsAreaExpired += IsAreaExpiredHook;
        }

        internal void Start()
        {
            config = new ModConfig()
            {
                ModName = "Custom Area Reset Time",
                SettingsVersion = 1.0,
                Settings = new List<BBSetting>
                {
                    new FloatSetting
                    {
                        Name = "ResetTime",
                        Description = "Area Reset Time (in hours). Default: 168 (7 days)",
                        DefaultValue = 168f,
                        MinValue = 0f,
                        MaxValue = 300f,
                        RoundTo = 0,
                        ShowPercent = false
                    }
                }
            };

            StartCoroutine(SetupCoroutine());
        }

        private IEnumerator SetupCoroutine()
        {
            while (!ConfigManager.Instance.IsInitDone())
            {
                yield return new WaitForSeconds(0.1f);
            }

            config.Register();
        }

        private bool IsAreaExpiredHook(On.AreaManager.orig_IsAreaExpired orig, AreaManager self, string _areaName, float _diff)
        {
            Area areaFromSceneName = self.GetAreaFromSceneName(_areaName);

            if (areaFromSceneName != null && !self.PermenantAreas.Contains((AreaManager.AreaEnum)areaFromSceneName.ID))
            {
                //Debug.Log("_________________________________");
                //Debug.Log("Custom Area Reset. Resetting: " + (-_diff > (float)config.GetValue("ResetTime")));
                //Debug.Log("_________________________________");

                return -_diff > (float)config.GetValue("ResetTime");
            }
            else
            {
                return orig(self, _areaName, _diff);
            }
            
        }
    }
}
