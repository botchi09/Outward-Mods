using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Partiality.Modloader;
//using SinAPI;

namespace ImprovedChakrams
{
    public class ModBase : PartialityMod
    {
        public GameObject obj;
        public string ID = "ImrpovedChakrams";
        public double version = 1.2;

        public ModBase()
        {
            this.author = "Sinai";
            this.ModID = ID;
            this.Version = version.ToString("0.00");
        }

        public override void OnEnable()
        {
            base.OnEnable();

            obj = new GameObject(ID);
            GameObject.DontDestroyOnLoad(obj);

            obj.AddComponent<Script>();
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }
    }

    public class Script : MonoBehaviour
    {

        internal void Awake()
        {
            StartCoroutine(SetupCoroutine());
        }

        private IEnumerator SetupCoroutine()
        {
            while (!ResourcesPrefabManager.Instance.Loaded)
            {
                yield return new WaitForSeconds(0.1f);
            }

            var list = new List<int> // chakram spells
            {
                8100250,
                8100251,
                8100252
            };

            foreach (int id in list)
            {
                if (ResourcesPrefabManager.Instance.GetItemPrefab(id) is Skill skill)
                {
                    if (skill.transform.Find("AdditionalActivationConditions") is Transform t 
                        && t.GetComponentsInChildren<HasStatusEffectEffectCondition>() is HasStatusEffectEffectCondition[] conditions)
                    {
                        foreach (var condition in conditions)
                        {
                            Destroy(condition.gameObject);
                        }
                    }
                }
            }
        }
    }
}
