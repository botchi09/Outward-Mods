using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Partiality.Modloader;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Localizer;

namespace CustomTranslation
{
    public class ModBase : PartialityMod
    {
        public ModBase()
        {
            Version = "1.0.0";
            author = "Sinai";
            ModID = "Custom Translation";
        }

        public override void OnEnable()
        {
            base.OnEnable();

            var obj = new GameObject("CustomTranslation");
            GameObject.DontDestroyOnLoad(obj);

            obj.AddComponent<CustomTranslation>();
        }
    }

    public class CustomTranslation : MonoBehaviour
    {
        public static CustomTranslation Instance;

        private Dictionary<string, LocalizationHolder> CustomLocalization = new Dictionary<string, LocalizationHolder>();
        private bool m_loaded = false;

        private static readonly string saveFolder = @"CustomLocalization";

        internal void Awake()
        {
            Instance = this;

            if (!Directory.Exists(saveFolder))
            {
                Directory.CreateDirectory(saveFolder);
            }

            On.LocalizationManager.Load += LoadLocalizationHook;
        }

        // Hook for overriding localization load, and saving localizations to xml
        private void LoadLocalizationHook(On.LocalizationManager.orig_Load orig, LocalizationManager self)
        {
            if (!m_loaded)
            {
                foreach (string path in Directory.GetFiles(saveFolder))
                {
                    LoadCustomXML(path);
                }
                m_loaded = true;
            }

            if (CustomLocalization.ContainsKey(self.CurrentLanguage))
            {
                Debug.Log("loading custom localization!");
                LoadCustomLoc(CustomLocalization[self.CurrentLanguage]);
            }
            else
            {
                orig(self);

                var localizationData = At.GetValue(typeof(LocalizationManager), self, "m_localizationData") as LocalizationReference;

                foreach (LocalizationReference.Localization loc in localizationData.Languages)
                {
                    string path = saveFolder + "/" + loc.DefaultName + ".xml";
                    LocalizationMiner.SaveLocalization(loc, path);
                }
            }
        }

        // load custom XML localization from file
        private void LoadCustomXML(string path)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(LocalizationHolder), LocalizationMiner.CUSTOM_TYPES);

            using (StreamReader streamReader = new StreamReader(path))
            {
                var holder = (LocalizationHolder)xmlSerializer.Deserialize(streamReader);
                streamReader.Close();

                Debug.Log("loaded custom XML locs for " + holder.DefaultName);

                CustomLocalization.Add(holder.DefaultName, holder);
            }
        }

        // Overwrite Localization Manager from custom XML file
        private void LoadCustomLoc(LocalizationHolder holder)
        {
            At.SetValue(false, typeof(LocalizationManager), LocalizationManager.Instance, "m_generalLocalizationLoaded");
            At.SetValue(false, typeof(LocalizationManager), LocalizationManager.Instance, "m_itemLocalizationLoaded");
            At.SetValue(false, typeof(LocalizationManager), LocalizationManager.Instance, "m_dialogueLocalizationLoaded");

            // Item Locs
            var itemDict = new Dictionary<int, ItemLocalization>();
            foreach (var itemLoc in holder.ItemLocalizations)
            {
                itemDict.Add(itemLoc.KeyID, new ItemLocalization(itemLoc.Name, itemLoc.Desc));
            }
            At.SetValue(itemDict, typeof(LocalizationManager), LocalizationManager.Instance, "m_itemLocalization");

            // Diag locs
            var diaDict = new Dictionary<string, DialogueLocalization>();
            foreach (var diaLoc in holder.DialogueLocalizations)
            {
                diaDict.Add(diaLoc.Key, new DialogueLocalization(diaLoc.Key, diaLoc.General, diaLoc.Female, diaLoc.UniqueAudioName, diaLoc.EmoteTags, diaLoc.AnimTags));
            }
            At.SetValue(diaDict, typeof(LocalizationManager), LocalizationManager.Instance, "m_dialogueLocalization");

            // general locs
            var generalDict = new Dictionary<string, string>();
            foreach (var generalloc in holder.MenuLocalizations)
            {
                generalDict.Add(generalloc.Key, generalloc.Value);
            }
            if (generalDict.ContainsKey("Credits_All"))
            {
                generalDict["Credits_All"].Replace("\n\n", "\n");
            }
            At.SetValue(generalDict, typeof(LocalizationManager), LocalizationManager.Instance, "m_generalLocalization");

            // Tips
            var tipsDict = new Dictionary<string, string>();
            foreach (var tipsLoc in holder.LoadingTipsLocalization)
            {
                tipsDict.Add(tipsLoc.Key, tipsLoc.Value);
            }
            string[] allTips = new string[tipsDict.Count];
            tipsDict.Keys.CopyTo(allTips, 0);
            At.SetValue(tipsDict, typeof(LocalizationManager), LocalizationManager.Instance, "m_loadingTips");
            At.SetValue(allTips, typeof(LocalizationManager), LocalizationManager.Instance, "m_allTips");

            // set loaded true
            At.SetValue(true, typeof(LocalizationManager), LocalizationManager.Instance, "m_generalLocalizationLoaded");
            At.SetValue(true, typeof(LocalizationManager), LocalizationManager.Instance, "m_itemLocalizationLoaded");
            At.SetValue(true, typeof(LocalizationManager), LocalizationManager.Instance, "m_dialogueLocalizationLoaded");
        }
    }
}
