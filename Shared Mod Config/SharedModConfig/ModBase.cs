using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Partiality.Modloader;
using UnityEngine;
using System.IO;

namespace SharedModConfig
{
    public class ModBase : PartialityMod
    {
        public static string ModName = "SharedModConfig";
        public static string ModVersion = "1.3";
        public static string ModAuthor = "Sinai";

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

            obj.AddComponent<ConfigManager>(); // not sure if this is any faster than Awake(), just trying it
            obj.AddComponent<MenuManager>();
        }
    }
}
