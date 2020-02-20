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
    // This class is used to parse the original Localizations to custom XML holders, and these are used for saving the user's custom locs too.

    public class LocalizationMiner : MonoBehaviour
    {
        public static Type[] CUSTOM_TYPES = new Type[]
        {
            typeof(ItemLocalizationHolder),
            typeof(DialogueLocalizationHolder),
            typeof(LocalizationEntryHolder)
        };

        // save orig XML
        public static void SaveLocalization(LocalizationReference.Localization loc, string path)
        {
            var locHolder = new LocalizationHolder
            {
                Name = loc.Name,
                DefaultName = loc.DefaultName
            };

            locHolder.DialogueLocalizations = LoadDialogue(loc.DialogueLocalizations);
            locHolder.ItemLocalizations = LoadItems(loc.ItemLocalizations);
            locHolder.MenuLocalizations = LoadMenu(loc.MenuLocalizations);
            locHolder.LoadingTipsLocalization = LoadTips(loc.LoadingTipsLocalization);

            // Serialize

            XmlSerializer xml = new XmlSerializer(typeof(LocalizationHolder), CUSTOM_TYPES);

            FileStream file = File.Create(path);
            xml.Serialize(file, locHolder);
            file.Close();
        }

        // Parse Menu XML (load from game data)
        public static List<LocalizationEntryHolder> LoadMenu(TextAsset[] array)
        {
            var dict = new Dictionary<string, string>();

            XmlDocument xmlDocument = new XmlDocument();

            foreach (var asset in array)
            {
                xmlDocument.LoadXml(asset.text);

                var nodes = xmlDocument.DocumentElement.SelectNodes("/ooo_calc_export/ooo_sheet");

                foreach (XmlNode node in nodes)
                {
                    var nodes2 = node.SelectNodes("ooo_row");

                    foreach (XmlNode node2 in nodes2)
                    {
                        string text = node2["column_1"].InnerText.TrimEnd(new char[0]);

                        if (!dict.ContainsKey(text))
                        {
                            if (node2["column_2"] != null)
                            {
                                dict.Add(text, node2["column_2"].InnerText);
                            }
                            else if (text.Equals("name_unpc_narrator"))
                            {
                                dict.Add(text, string.Empty);
                            }
                        }
                    }
                }
            }

            var list = new List<LocalizationEntryHolder>();

            foreach (var entry in dict)
            {
                list.Add(new LocalizationEntryHolder
                {
                    Key = entry.Key,
                    Value = entry.Value
                });
            }

            return list;
        }

        // Parse Tips XML (load from game data)
        public static List<LocalizationEntryHolder> LoadTips(TextAsset asset)
        {
            var dict = new Dictionary<string, string>();

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(asset.text);

            var nodes = xmlDocument.DocumentElement.SelectNodes("/ooo_calc_export/ooo_sheet");

            foreach (XmlNode node in nodes)
            {
                var nodes2 = node.SelectNodes("ooo_row");

                foreach (XmlNode node2 in nodes2)
                {
                    string text2 = node2["column_1"].InnerText.TrimEnd(new char[0]);
                    if (!string.IsNullOrEmpty(text2) && text2 != "loc_key")
                    {
                        if (!text2.Equals("Test_Tip37"))
                        {
                            if (!dict.ContainsKey(text2) && node2["column_2"] != null)
                            {
                                dict.Add(text2, node2["column_2"].InnerText.Replace("\n\n", "\n"));
                            }
                        }
                    }
                }
            }

            var list = new List<LocalizationEntryHolder>();

            foreach (var entry in dict)
            {
                list.Add(new LocalizationEntryHolder
                {
                    Key = entry.Key,
                    Value = entry.Value
                });
            }

            return list;
        }

        // Parse Items XML (load from game data)
        public static List<ItemLocalizationHolder> LoadItems(TextAsset[] array)
        {
            var dict = new Dictionary<int, ItemLocalizationHolder>();

            XmlDocument xmlDocument = new XmlDocument();

            foreach (var asset in array)
            {
                xmlDocument.LoadXml(asset.text);

                var nodes = xmlDocument.DocumentElement.SelectNodes("/ooo_calc_export/ooo_sheet[@num='1']/ooo_row");

                int key = -1;
                foreach (XmlNode node in nodes)
                {
                    if (!string.IsNullOrEmpty(node["column_1"].InnerText) && int.TryParse(node["column_1"].InnerText, out key)
                        && !dict.ContainsKey(key))
                    {
                        string name = (node["column_2"] == null) ? string.Empty : node["column_2"].InnerText;
                        string desc = (node["column_3"] == null) ? string.Empty : node["column_3"].InnerText;

                        dict.Add(key, new ItemLocalizationHolder
                        {
                            Name = name,
                            Desc = desc.Replace("\n\n", "\n")
                        });
                    }
                }
            }

            var list = new List<ItemLocalizationHolder>();

            foreach (var entry in dict)
            {
                list.Add(new ItemLocalizationHolder
                {
                    KeyID = entry.Key,
                    Name = entry.Value.Name,
                    Desc = entry.Value.Desc
                });
            }

            return list;
        }

        // Parse Dialogue XML (load from game data)
        public static List<DialogueLocalizationHolder> LoadDialogue(TextAsset[] array)
        {
            var dict = new Dictionary<string, DialogueLocalizationHolder>();

            XmlDocument xmlDocument = new XmlDocument();

            foreach (var asset in array)
            {
                xmlDocument.LoadXml(asset.text);
                XmlNodeList nodes = xmlDocument.DocumentElement.SelectNodes("/ooo_calc_export/ooo_sheet");
                foreach (XmlNode node in nodes)
                {
                    XmlNodeList nodes2 = node.SelectNodes("ooo_row");
                    foreach (XmlNode node2 in nodes2)
                    {
                        string key = (node2["column_1"] == null) ? string.Empty : node2["column_1"].InnerText.TrimEnd(new char[0]);
                        if (!string.IsNullOrEmpty(key))
                        {
                            string general = (node2["column_2"] == null) ? string.Empty : node2["column_2"].InnerText.Replace("\n\n", "\n");
                            general = general.Replace(" !", "\u00a0!");
                            general = general.Replace(" ?", "\u00a0?");
                            string female = (node2["column_3"] == null) ? string.Empty : node2["column_3"].InnerText.Replace("\n\n", "\n");
                            if (!string.IsNullOrEmpty(key) || !string.IsNullOrEmpty(general))
                            {
                                string uniqueAudioName = (node2["column_4"] == null) ? string.Empty : node2["column_4"].InnerText;
                                string emoteData = (node2["column_5"] == null) ? string.Empty : node2["column_5"].InnerText;
                                string animData = (node2["column_6"] == null) ? string.Empty : node2["column_6"].InnerText;

                                if (!string.IsNullOrEmpty(key) && key != "loc_key" && !dict.ContainsKey(key))
                                {
                                    dict.Add(key, new DialogueLocalizationHolder
                                    {
                                        Key = key,
                                        General = general,
                                        Female = female,
                                        UniqueAudioName = uniqueAudioName,
                                        EmoteTags = emoteData,
                                        AnimTags = animData
                                    });
                                }
                            }
                        }
                    }
                }
            }

            return dict.Values.ToList();
        }
    }

    public class LocalizationHolder
    {
        public string Name;
        public string DefaultName;

        public List<ItemLocalizationHolder> ItemLocalizations = new List<ItemLocalizationHolder>();
        public List<DialogueLocalizationHolder> DialogueLocalizations = new List<DialogueLocalizationHolder>();
        public List<LocalizationEntryHolder> MenuLocalizations = new List<LocalizationEntryHolder>();
        public List<LocalizationEntryHolder> LoadingTipsLocalization = new List<LocalizationEntryHolder>();
    }

    public class ItemLocalizationHolder
    {
        public int KeyID;
        public string Name;
        public string Desc;
    }

    public class DialogueLocalizationHolder
    {
        public string Key;
        public string General;
        public string Female;
        public string UniqueAudioName;
        public string AnimTags;
        public string EmoteTags;
    }

    public class LocalizationEntryHolder
    {
        public string Key;
        public string Value;
    }
}
