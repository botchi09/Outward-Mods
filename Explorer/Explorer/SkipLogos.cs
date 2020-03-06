using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace OutwardExplorer
{
    public class SkipLogos : MonoBehaviour
    {
        internal void Awake()
        {
            On.StartupVideo.Start += new On.StartupVideo.hook_Start(StartupVideo_Start);
        }

        public void StartupVideo_Start(On.StartupVideo.orig_Start orig, StartupVideo self)
        {
            //StoreManager.Experimental = false;
            StartupVideo.HasPlayedOnce = true;
            orig(self);
        }
    }
}
