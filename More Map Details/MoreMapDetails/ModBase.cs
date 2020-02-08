using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Partiality.Modloader;
using UnityEngine;
using System.IO;

namespace MoreMapDetails
{
    public class ModBase : PartialityMod
    {
        public static string ModName = "MoreMapDetails";
        public static double ModVersion = 1.0;
        public static string ModAuthor = "Sinai";

        public ModBase()
        {
            this.ModID = ModName;
            this.Version = ModVersion.ToString();
            this.author = ModAuthor;
        }

        public override void OnEnable()
        {
            base.OnEnable();

            var obj = new GameObject(ModName);
            GameObject.DontDestroyOnLoad(obj);
            obj.AddComponent<MapManager>();


        }
    }
}
