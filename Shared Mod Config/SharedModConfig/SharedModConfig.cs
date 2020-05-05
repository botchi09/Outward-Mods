using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using BepInEx;
using SideLoader;

namespace SharedModConfig
{
    [BepInPlugin(GUID, ModName, ModVersion)]
    [BepInDependency(SL.GUID, BepInDependency.DependencyFlags.HardDependency)]
    public class SharedModConfig : BaseUnityPlugin
    {
        public const string GUID = "com.sinai.SharedModConfig";
        public const string ModName = "SharedModConfig";
        public const string ModVersion = "1.5";

        internal void Awake()
        {
            var obj = new GameObject(ModName);
            GameObject.DontDestroyOnLoad(obj);

            obj.AddComponent<ConfigManager>(); // not sure if this is any faster than Awake(), just trying it
            obj.AddComponent<MenuManager>();
        }
    }
}
