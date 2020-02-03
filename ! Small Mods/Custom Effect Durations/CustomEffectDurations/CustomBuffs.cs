using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
//using SinAPI;
using Partiality.Modloader;
using System.IO;

namespace CustomEffectDurations
{
    public class CustomBuffs : PartialityMod
    {
        public GameObject _obj = null;
        public CustomBuffScript script;

        public CustomBuffs()
        {
            this.ModID = "CustomBuffDurations";
            this.Version = "1.0";
            this.author = "Sinai";
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (_obj == null)
            {
                _obj = new GameObject("CustomBuffDurations");
                GameObject.DontDestroyOnLoad(_obj);
            }

            script = _obj.AddComponent<CustomBuffScript>();
            script.Init();
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }
    }

    public class CustomBuffScript : MonoBehaviour
    {
        public Settings settings = new Settings();
        private static readonly string savePath = @"Mods\CustomBuffDurations.json";

        public bool Patched = false;

        public void Init()
        {
            LoadSettings();
        }
        
        internal void Update()
        {
            if (!Patched 
                && ResourcesPrefabManager.Instance.Loaded 
                && At.GetValue(typeof(ResourcesPrefabManager), null, "ITEM_PREFABS") is Dictionary<string, Item> items)
            {
                // ========= allow 2H runic blade to work as Lexicon (temp patch) =========

                // get Lexicon tag first
                Tag lexTag = Tag.None;

                if (ResourcesPrefabManager.Instance.GetItemPrefab(2000100) is Weapon runicBlade)
                {
                    TagListSelectorComponent tagComp = runicBlade.GetComponent<TagSource>() as TagListSelectorComponent;
                    List<Tag> tagList = At.GetValue(typeof(TagListSelectorComponent), tagComp, "m_tags") as List<Tag>;                    
                    foreach (Tag tag in tagList)
                    {
                        if (tag.TagName == "Lexicon")
                        {
                            lexTag = tag;
                            break;
                        }
                    }
                }

                // apply Lexicon tag to Great Blade
                if (lexTag != Tag.None && ResourcesPrefabManager.Instance.GetItemPrefab(2100999) is Weapon greatBlade)
                {
                    TagListSelectorComponent tagComp = greatBlade.GetComponent<TagSource>() as TagListSelectorComponent;
                    List<Tag> tagList = At.GetValue(typeof(TagListSelectorComponent), tagComp, "m_tags") as List<Tag>;
                    tagList.Add(lexTag);

                    List<TagSourceSelector> selectorList = At.GetValue(typeof(TagListSelectorComponent), tagComp, "m_tagSelectors") as List<TagSourceSelector>;
                    selectorList.Add(new TagSourceSelector(lexTag));
                }

                // =========================== actual CustomBuffs stuff ===========================
                foreach (Item item in items.Values)
                {
                    // Imbues
                    if (settings.Custom_Imbues_On && item.GetComponentsInChildren<ImbueWeapon>() is ImbueWeapon[] imbues && imbues.Count() > 0)
                    {
                        foreach (ImbueWeapon imbue in imbues)
                        {
                            float f = settings.Custom_Imbue_Durations;
                            if (Weak_Imbues.Contains(imbue.ImbuedEffect.Name))
                            {
                                f *= 0.5f;
                            }
                            imbue.SetLifespanImbue(f);
                        }
                    }

                    // Boons
                    if (settings.Custom_Boons_On && item.GetComponentsInChildren<AddBoonEffect>() is AddBoonEffect[] addBoons && addBoons.Count() > 0)
                    {
                        foreach (AddBoonEffect addBoon in addBoons)
                        {
                            At.SetValue(settings.Custom_Boon_Durations, typeof(StatusData), addBoon.Status.StatusData, "LifeSpan");

                            if (addBoon.BoonAmplification != null)
                            {
                                At.SetValue(settings.Custom_Boon_Durations, typeof(StatusData), addBoon.BoonAmplification.StatusData, "LifeSpan");
                            }
                        }
                    }

                    // Runic spells
                    if (settings.Custom_Runic_On && item.Name.StartsWith("Rune:"))
                    {
                        foreach (AddStatusEffect addStatus in item.GetComponentsInChildren<AddStatusEffect>(true))
                        {
                            At.SetValue(settings.Custom_Runic_Durations, typeof(StatusData), addStatus.Status.StatusData, "LifeSpan");
                        }

                        foreach (RunicBlade blade in item.GetComponentsInChildren<RunicBlade>(true))
                        {
                            At.SetValue(settings.Custom_Runic_Durations, typeof(RunicBlade), blade, "SummonLifeSpan");
                        }
                    }

                    // Sigils
                    if (settings.Custom_Sigils_On && item.Name.StartsWith("Activated") && item.GetComponent<Ephemeral>() is Ephemeral ephemeral)
                    {
                        At.SetValue(settings.Custom_Sigil_Durations, typeof(Ephemeral), ephemeral, "LifeSpan");
                    }
                }

                Patched = true;
            }
            else if (Patched)
            {
                // we are done here. bye bye!
                DestroyImmediate(this.gameObject);
            }
        }

        private static readonly string[] Weak_Imbues = new string[]
        {
            "Fire Imbue",
            "Frost Imbue",
            "Lightning Imbue",
            "Poison Imbue"
        };

        // ====== misc ======

        private void LoadSettings()
        {
            settings = new Settings();

            if (File.Exists(savePath))
            {
                settings = JsonUtility.FromJson<Settings>(File.ReadAllText(savePath));
            }

            SaveSettings();
        }

        private void SaveSettings()
        {
            if (!Directory.Exists(@"Mods")) { Directory.CreateDirectory(@"Mods"); }

            Jt.SaveJsonOverwrite(savePath, settings);
        }

        internal void OnDisable()
        {
            SaveSettings();
        }
    }

    public class Settings
    {
        public bool Custom_Boons_On = true;
        public float Custom_Boon_Durations = 240;

        public bool Custom_Imbues_On = true;
        public float Custom_Imbue_Durations = 180;

        public bool Custom_Runic_On = true;
        public float Custom_Runic_Durations = 240;

        public bool Custom_Sigils_On = true;
        public float Custom_Sigil_Durations = 60;
    }
}
