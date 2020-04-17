using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Partiality.Modloader;
using UnityEngine;

namespace CombatHUD
{
    public class CombatHUDLoader : PartialityMod
    {
        public const string ModName = "CombatHUD";
        public const string ModVersion = "4.13";
        public const string ModAuthor = "Sinai";

        public CombatHUDLoader()
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
