using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Partiality.Modloader;
using UnityEngine;

namespace Explorer
{
    public class ModLoader : PartialityMod
    {
        public override void OnLoad()
        {
            base.OnLoad();

            var obj = new GameObject("OTW_EXPLORER");
            GameObject.DontDestroyOnLoad(obj);

            obj.AddComponent<Explorer>();
            obj.AddComponent<MenuManager>();
        }
    }
}
