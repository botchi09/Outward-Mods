using Partiality.Modloader;
using UnityEngine;

namespace CustomHCDeathChance
{
    public class BaseMod : PartialityMod
    {
        public static MonoScript script;

        public BaseMod()
        {
            this.ModID = "Custom Hardcore Death Chance";
            this.Version = "1.0";
            this.author = "Sinai/Outlander";
        }

        public override void OnEnable()
        {
            base.OnEnable();

            GameObject go = new GameObject();
            script = go.AddComponent<MonoScript>();

            script._BaseMod = this;

            script.Initialise();
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }
    }
}
