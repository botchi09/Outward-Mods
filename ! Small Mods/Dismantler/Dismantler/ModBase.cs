using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Partiality.Modloader;
using UnityEngine;

namespace Dismantler
{
    public class ModBase : PartialityMod
    {
        public override void OnEnable()
        {
            base.OnEnable();

            var obj = new GameObject("Dismantler");
            GameObject.DontDestroyOnLoad(obj);
            obj.AddComponent<Dismantler>();
        }
    }
}
