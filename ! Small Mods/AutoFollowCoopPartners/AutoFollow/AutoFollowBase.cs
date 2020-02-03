using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Partiality.Modloader;
using UnityEngine;

namespace AutoFollow
{
    public class AutoFollowBase : PartialityMod
    {
        public static GameObject _obj = null;
        public FollowScript script;

        public AutoFollowBase()
        {
            ModID = "AutoFollow";
            Version = "1.0";
            author = "Sinai";
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (_obj == null)
            {
                _obj = new GameObject("AutoFollower");
                GameObject.DontDestroyOnLoad(_obj);
            }

            script = _obj.AddComponent<FollowScript>();
            script.Init();
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }
    }
}
