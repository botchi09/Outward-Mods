using Partiality.Modloader;
using UnityEngine;

/*
 * THIS MOD DESPERATELY NEEDS A REWORK / CLEANUP. VERY OLD. 
*/

namespace OutwardExplorer
{
    public class ModBase : PartialityMod
    {
        public static GameObject _obj = null;

        public static ExplorerScript explorerScript;

        public ExplorerGUIHelper explorerGUI;

        public SkipLogos skipLogos;

        //public static MapMarkers markers;

        public ModBase()
        {
            ModID = "Outward Explorer";
            Version = "0.7";
            author = "Sinai";
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (_obj == null)
            {
                _obj = new GameObject("OTW_EXPLORER");
                GameObject.DontDestroyOnLoad(_obj);
            }

            explorerScript = _obj.AddComponent<ExplorerScript>();
            explorerGUI = _obj.AddComponent<ExplorerGUIHelper>();

            skipLogos = _obj.AddComponent<SkipLogos>();
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }
    }
}
