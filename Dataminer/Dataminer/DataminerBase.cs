using Partiality.Modloader;
using UnityEngine;

/*
 * THIS MOD DESPERATELY NEEDS A REWORK / CLEANUP. VERY OLD. 
*/

namespace OutwardExplorer
{
    public class DataminerBase : PartialityMod
    {
        public static GameObject _obj = null;

        public DumperScript dumperScript;
        public DumperSorter dumperSorter;
        public DumperUtils dumperUtils;

        public DataminerBase()
        {
            ModID = "Outward Dataminer";
            Version = "1.0";
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

            dumperScript = _obj.AddComponent<DumperScript>();
            dumperSorter = _obj.AddComponent<DumperSorter>();
            dumperUtils = _obj.AddComponent<DumperUtils>();

            dumperScript.utils = dumperUtils;
            dumperScript.sorter = dumperSorter;

            dumperSorter.script = dumperScript;

            dumperUtils.script = dumperScript;

            dumperScript.Init();
        }
            

        public override void OnDisable()
        {
            base.OnDisable();
        }
    }
}
