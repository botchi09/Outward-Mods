using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Partiality.Modloader;
using UnityEngine;

namespace Dataminer
{
    public class ModBase : PartialityMod
    {
        public ModBase()
        {
            this.ModID = "Dataminer_2";
            this.Version = "1.0";
            this.author = "Sinai";
        }

        public override void OnEnable()
        {
            base.OnEnable();

            var obj = new GameObject("Dataminer_2");
            GameObject.DontDestroyOnLoad(obj);

            obj.AddComponent<Dataminer>();
            obj.AddComponent<ListManager>();
            obj.AddComponent<SceneManager>();
        }
    }
}
