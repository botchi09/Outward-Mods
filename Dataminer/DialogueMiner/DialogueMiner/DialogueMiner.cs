using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Localizer;
using BepInEx;
using HarmonyLib;

namespace DialogueMiner
{
    [BepInPlugin("com.sinai.dialogueminer", "DialogueMiner", "1.0")]
    public class DialogueMiner : BaseUnityPlugin
    {
        public static DialogueMiner Instance;

        //private Dictionary<string, LocalizationHolder> CustomLocalization = new Dictionary<string, LocalizationHolder>();
        //private bool m_loaded = false;

        private static readonly string saveFolder = @"CustomLocalization";

        internal void Awake()
        {
            Instance = this;

            if (!Directory.Exists(saveFolder))
            {
                Directory.CreateDirectory(saveFolder);
            }

            var harmony = new Harmony("com.sinai.dialogueminer");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(LocalizationManager), "Load")]
        public class LocalizationManager_Load
        {
            [HarmonyPostfix]
            public static void Prefix(LocalizationManager __instance)
            {
                var localizationData = At.GetValue(typeof(LocalizationManager), __instance, "m_localizationData") as LocalizationReference;

                foreach (LocalizationReference.Localization loc in localizationData.Languages)
                {
                    string path = saveFolder + "/" + loc.DefaultName + ".xml";
                    LocalizationHolder.SaveLocalization(loc, path);
                }
            }
        }

        //// load custom XML localization from file
        //private void LoadCustomXML(string path)
        //{
        //    XmlSerializer xmlSerializer = new XmlSerializer(typeof(LocalizationHolder), LocalizationMiner.CUSTOM_TYPES);

        //    using (StreamReader streamReader = new StreamReader(path))
        //    {
        //        var holder = (LocalizationHolder)xmlSerializer.Deserialize(streamReader);
        //        streamReader.Close();

        //        Debug.Log("loaded custom XML locs for " + holder.DefaultName);

        //        CustomLocalization.Add(holder.DefaultName, holder);
        //    }
        //}

        //// Overwrite Localization Manager from custom XML file
        //private void LoadCustomLoc(LocalizationHolder holder)
        //{
        //    At.SetValue(false, typeof(LocalizationManager), LocalizationManager.Instance, "m_generalLocalizationLoaded");
        //    At.SetValue(false, typeof(LocalizationManager), LocalizationManager.Instance, "m_itemLocalizationLoaded");
        //    At.SetValue(false, typeof(LocalizationManager), LocalizationManager.Instance, "m_dialogueLocalizationLoaded");

        //    // Item Locs
        //    var itemDict = new Dictionary<int, ItemLocalization>();
        //    foreach (var itemLoc in holder.ItemLocalizations)
        //    {
        //        itemDict.Add(itemLoc.KeyID, new ItemLocalization(itemLoc.Name, itemLoc.Desc));
        //    }
        //    At.SetValue(itemDict, typeof(LocalizationManager), LocalizationManager.Instance, "m_itemLocalization");

        //    // Diag locs
        //    var diaDict = new Dictionary<string, DialogueLocalization>();
        //    foreach (var diaLoc in holder.DialogueLocalizations)
        //    {
        //        diaDict.Add(diaLoc.Key, new DialogueLocalization(diaLoc.Key, diaLoc.General, diaLoc.Female, diaLoc.UniqueAudioName, diaLoc.EmoteTags, diaLoc.AnimTags));
        //    }
        //    At.SetValue(diaDict, typeof(LocalizationManager), LocalizationManager.Instance, "m_dialogueLocalization");

        //    // general locs
        //    var generalDict = new Dictionary<string, string>();
        //    foreach (var generalloc in holder.MenuLocalizations)
        //    {
        //        generalDict.Add(generalloc.Key, generalloc.Value);
        //    }
        //    if (generalDict.ContainsKey("Credits_All"))
        //    {
        //        generalDict["Credits_All"].Replace("\n\n", "\n");
        //    }
        //    At.SetValue(generalDict, typeof(LocalizationManager), LocalizationManager.Instance, "m_generalLocalization");

        //    // Tips
        //    var tipsDict = new Dictionary<string, string>();
        //    foreach (var tipsLoc in holder.LoadingTipsLocalization)
        //    {
        //        tipsDict.Add(tipsLoc.Key, tipsLoc.Value);
        //    }
        //    string[] allTips = new string[tipsDict.Count];
        //    tipsDict.Keys.CopyTo(allTips, 0);
        //    At.SetValue(tipsDict, typeof(LocalizationManager), LocalizationManager.Instance, "m_loadingTips");
        //    At.SetValue(allTips, typeof(LocalizationManager), LocalizationManager.Instance, "m_allTips");

        //    // set loaded true
        //    At.SetValue(true, typeof(LocalizationManager), LocalizationManager.Instance, "m_generalLocalizationLoaded");
        //    At.SetValue(true, typeof(LocalizationManager), LocalizationManager.Instance, "m_itemLocalizationLoaded");
        //    At.SetValue(true, typeof(LocalizationManager), LocalizationManager.Instance, "m_dialogueLocalizationLoaded");
        //}
    }
}
