using Partiality.Modloader;
using UnityEngine;

namespace BetterSummonedGhost
{
    public class ModBase : PartialityMod
    {
        public static GameObject _obj = null;
        public static GhostScript ghostScript;
        public ScriptGUI gui;

        public ModBase()
        {
            this.ModID = "Better Summoned Ghost";
            this.Version = "1.2";
            this.author = "Sinai";
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (_obj == null)
            {
                _obj = new GameObject("BetterSummonedGhost");
                GameObject.DontDestroyOnLoad(_obj);
            }

            ghostScript = _obj.AddComponent<GhostScript>();

            gui = _obj.AddComponent<ScriptGUI>();
            gui.script = ghostScript;
            ghostScript.gui = gui;

            ghostScript.Init();
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }
    }
}
