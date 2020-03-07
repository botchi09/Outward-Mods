using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Partiality.Modloader;
using UnityEngine;
using System.IO;

namespace CombatHUD
{
    public class ModBase : PartialityMod
    {
        public static string ModName = "CombatHUD";
        public static string ModVersion = "4.12";
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

            obj.AddComponent<HUDManager>();
        }
    }
}
