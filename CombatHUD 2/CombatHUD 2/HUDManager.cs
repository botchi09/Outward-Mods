using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using SideLoader;

namespace CombatHUD_2
{
    public class HUDManager : MonoBehaviour
    {
        public static HUDManager Instance;

        public GameObject HUDCanvas;

        internal void Awake()
        {
            Instance = this;

            StartCoroutine(SetupCoroutine());
        }

        private IEnumerator SetupCoroutine()
        {
            while (!SL.Instance.IsInitDone())
            {
                yield return new WaitForSeconds(0.1f);
            }

            SetupCanvas();
        }

        private void SetupCanvas()
        {
            Debug.Log(ModBase.ModName + " started, version: " + ModBase.ModVersion);

            var bundle = SL.Instance.LoadedBundles["combathud"];

            if (bundle.LoadAsset("HUDCanvas") is GameObject canvasAsset)
            {
                HUDCanvas = Instantiate(canvasAsset);
                DontDestroyOnLoad(HUDCanvas);

                // setup draw order
                var canvas = HUDCanvas.GetComponent<Canvas>();

                canvas.sortingOrder = 999; // higher = shown above other layers.

            }
            else
            {
                Debug.LogError("[CombatHUD] Fatal error loading the AssetBundle. Make sure SideLoader is enabled, and the asset exists at Mods/SideLoader/CombatHUD/");
                Destroy(this.gameObject);
                return;
            }

            // setup the autonomous components

            // ====== target manager ======
            var targetMgrHolder = HUDCanvas.transform.Find("TargetManager_Holder");

            var mgr_P1 = targetMgrHolder.transform.Find("TargetManager_P1").GetOrAddComponent<TargetManager>();
            mgr_P1.Split_ID = 0;

            var mgr_P2 = targetMgrHolder.transform.Find("TargetManager_P2").GetOrAddComponent<TargetManager>();
            mgr_P2.Split_ID = 1;

            // ====== player manager ======
            var statusTimerHolder = HUDCanvas.transform.Find("PlayerStatusTimers");
            statusTimerHolder.gameObject.AddComponent<PlayersManager>();

            // ====== damage labels ======
            var damageLabels = HUDCanvas.transform.Find("DamageLabels");
            damageLabels.gameObject.AddComponent<DamageLabels>();
        }

        public static float RelativeOffset(float offset, bool height = false) // false for width, true for height
        {
            return offset * (height ? Screen.height : Screen.width) * 100f / (height ? 720f : 1280f) * 0.01f;
        }


        // settings

        internal void OnApplicationQuit()
        {
            ModBase.SaveSettings();
        }
    }
}
