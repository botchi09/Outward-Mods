using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CustomHCDeathChance
{
    public class MonoScript : MonoBehaviour
    {
        public BaseMod _BaseMod;

        public int DeathChance = -1;

        public void Initialise()
        {
            PatchHooks();
            LoadSettings();
        }

        public void PatchHooks()
        {
            On.DefeatScenariosManager.ActivateDefeatScenario += new On.DefeatScenariosManager.hook_ActivateDefeatScenario(ActivateDefeatHook);
        }

        private void ActivateDefeatHook(On.DefeatScenariosManager.orig_ActivateDefeatScenario orig, DefeatScenariosManager self, DefeatScenario _scenario)
        {
            if (DeathChance == -1)
            {
                orig(self, _scenario);
                return;
            }

            self.LastActivationNetworkTime = (float)PhotonNetwork.time;
            if (_scenario)
            {
                var _base = self as Photon.MonoBehaviour;
                if (CharacterManager.Instance.HardcoreMode && _scenario.SupportHardcore)
                {
                    int num = UnityEngine.Random.Range(0, 100);
                    if (num >= DeathChance)
                    {
                        _base.photonView.RPC("DefeatHardcoreDeath", PhotonTargets.All, new object[0]);
                        return;
                    }
                }
                ReflectionTools.ReflectionSetValue(_scenario.ID, typeof(DefeatScenariosManager), self, "m_activeDefeatScenarioID");
                _scenario.Activate();
                _base.photonView.RPC("DefeatScenarioActivated", PhotonTargets.All, new object[]
                {
                _scenario.ID,
                false,
                self.LastActivationNetworkTime
                });
            }
            else
            {
                self.FailSafeDefeat();
            }
        }

        private void LoadSettings()
        {
            var data = File
                .ReadAllLines(@"Mods\DeathChance.txt")
                .Select(x => x.Split('='))
                .Where(x => x.Length > 1)
                .ToDictionary(x => x[0].Trim(), x => x[1]);

            DeathChance = 100 - Convert.ToInt32(data["Chance"]);
        }
    }
}
