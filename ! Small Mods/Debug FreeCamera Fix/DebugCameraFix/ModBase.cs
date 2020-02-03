using Partiality.Modloader;
using UnityEngine;

namespace DebugCameraFix
{
    public class ModBase : PartialityMod
    {
        public static GameObject _obj = null;
        public static DebugCameraScript camScript;

        public ModBase()
        {
            this.ModID = "Debug Cam Fix";
            this.Version = "1.0";
            this.author = "Sinai";
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (_obj == null)
            {
                _obj = new GameObject("Debug_Camera_Fix");
                GameObject.DontDestroyOnLoad(_obj);
            }

            camScript = _obj.AddComponent<DebugCameraScript>();
            camScript.Initialise();
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }
    }
}
