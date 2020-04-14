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
        public const string ModName = "SharedModConfig";
        public const string ModVersion = "1.3";
        public const string ModAuthor = "Sinai";

        public ModBase()
        {
            ModID = ModName;
            author = ModAuthor;
            Version = ModVersion;
            this.loadPriority = -998; // lower number = higher priority
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
