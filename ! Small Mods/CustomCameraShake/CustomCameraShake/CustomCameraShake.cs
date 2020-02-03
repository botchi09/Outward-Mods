using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Partiality.Modloader;
using UnityEngine;
using System.IO;
using System.Reflection;

namespace CustomCameraShake
{
    public class ModBase : PartialityMod
    {
        public GameObject _obj = null;
        public static CustomCameraShake Instance;

        public string modID = "CustomCameraShake";
        public double version = 1.0;

        public ModBase()
        {
            this.ModID = modID;
            this.Version = version.ToString("0.00");
            this.author = "Sinai";
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (_obj == null)
            {
                _obj = new GameObject("CustomCameraShake");
                GameObject.DontDestroyOnLoad(_obj);
            }

            Instance = _obj.AddComponent<CustomCameraShake>();
            Instance._base = this;
            Instance.Init();
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }
    }

    public class Settings
    {
        public float CameraShakeModifier = 0.0f;
    }

    public class CustomCameraShake : MonoBehaviour
    {
        public ModBase _base;

        public Settings settings;
        public static string savePath = @"Mods\CustomCameraShake.json";

        public List<CameraShaker> CurrentShakers = new List<CameraShaker>();

        public void Init()
        {
            if (!Directory.Exists("Mods"))
            {
                Directory.CreateDirectory("Mods");
            }

            bool flag = true;
            if (File.Exists(savePath))
            {
                if (JsonUtility.FromJson<Settings>(File.ReadAllText(savePath)) is Settings s2)
                {
                    settings = s2;
                    flag = false;
                }
            }
            if (flag)
            {
                settings = new Settings();
            }

            On.CharacterCamera.LinkCamera += CharacterCamera_LinkCamera;
        }

        internal void Update()
        {
            if (!NetworkLevelLoader.Instance.IsGameplayPaused && Global.Lobby.PlayersInLobbyCount > 0)
            {
                if (Input.GetKeyDown(KeyCode.F5))
                {
                    //OLogger.Log("Reducing shake. Current: " + settings.CameraShakeModifier + ", new: " + (settings.CameraShakeModifier - 0.1f));
                    settings.CameraShakeModifier -= 0.1f;
                }
                if (Input.GetKeyDown(KeyCode.F6))
                {
                    //OLogger.Log("Increasing shake. Current: " + settings.CameraShakeModifier + ", new: " + (settings.CameraShakeModifier + 0.1f));
                    settings.CameraShakeModifier += 0.1f;
                }

                for (int i = 0; i < CurrentShakers.Count; i++)
                {
                    if (CurrentShakers.Count < 1) { break; }

                    var shaker = CurrentShakers[i];

                    if (shaker == null)
                    {
                        CurrentShakers.RemoveAt(i);
                        i--;
                        continue;
                    }

                    if (shaker.AmplitudeMultiplier != settings.CameraShakeModifier)
                    {
                        //OLogger.Log("actual shaker is not set to our setting! updating...");
                        shaker.AmplitudeMultiplier = settings.CameraShakeModifier;
                        shaker.SingleShakeAmplitude = settings.CameraShakeModifier * 0.01f;
                        shaker.SlowShakeAmplitude = settings.CameraShakeModifier * 0.0005f;
                    }
                }
            }
        }

        public void CharacterCamera_LinkCamera(On.CharacterCamera.orig_LinkCamera orig, CharacterCamera self, Camera _camera)
        {
            orig(self, _camera);

            FieldInfo field = self.GetType().GetField("m_shaker", BindingFlags.Instance | BindingFlags.NonPublic);
            CameraShaker cameraShaker = (CameraShaker)field.GetValue(self);
            CurrentShakers.Add(cameraShaker);
        }

        internal void OnDisable()
        {
            SaveSettings();
        }

        public void SaveSettings()
        {
            if (File.Exists(savePath)) { File.Delete(savePath); }

            File.WriteAllText(savePath, JsonUtility.ToJson(settings, true));
        }
    }
}
