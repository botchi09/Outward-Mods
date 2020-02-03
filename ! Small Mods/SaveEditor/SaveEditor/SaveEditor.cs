using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using SinAPI;
using System.IO;
using System.Xml.Serialization;
using Partiality.Modloader;

namespace SaveEditor
{
    public class SE : PartialityMod
    {
        public static SaveEditor Instance;

        public GameObject obj;
        public string ID = "OTW_SaveEditor";
        public double version = 1.4;

        public SE()
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

            Instance = obj.AddComponent<SaveEditor>();
            Instance._base = this;

            Instance.Init();
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }
    }

    public class CustomSave
    {
        public CharacterSave cSave;
        public WorldSave wSave;
    }

    public class SaveEditor : MonoBehaviour
    {
        public SE _base;

        //public Dictionary<string, string> CustomSavePaths = new Dictionary<string, string>(); // Key: Char UID, Value: filePath of save

        public Dictionary<string, CustomSave> CustomSaves = new Dictionary<string, CustomSave>();

        public void Init()
        {
            LoadAllSavesFromFile();

            // major savemanager hook
            On.SaveInstance.LoadInstance += LoadSaveInstanceHook;

            // minor apply save hooks
            On.SaveInstance.Save += SaveHook;
            On.CharacterSave.ApplyLoadedSaveToChar += ApplyCharacterSave;
            On.WorldSave.ApplyData += ApplyWorldSave;
        }

        // ======================= CUSTOM FUNCTIONS ======================== //

        private void LoadAllSavesFromFile()
        {
            var saveDir = @"Mods\SaveEditor";
            if (!Directory.Exists(saveDir)) { Directory.CreateDirectory(saveDir); }

            foreach (string directory in Directory.GetDirectories(saveDir))
            {
                CustomSave customSave = new CustomSave();

                string charPath = directory + @"\Character.xml";
                if (File.Exists(charPath))
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(CharacterSave));
                    using (StreamReader streamReader = new StreamReader(charPath))
                    {
                        CharacterSave characterSave = (CharacterSave)xmlSerializer.Deserialize(streamReader);
                        streamReader.Close();
                        customSave.cSave = characterSave;
                    }
                }

                string worldPath = directory + @"\World.xml";
                if (File.Exists(worldPath))
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(WorldSave));
                    using (StreamReader streamReader = new StreamReader(worldPath))
                    {
                        WorldSave worldSave = (WorldSave)xmlSerializer.Deserialize(streamReader);
                        streamReader.Close();
                        customSave.wSave = worldSave;
                    }
                }

                if (customSave.cSave != null && customSave.wSave != null)
                {
                    CustomSaves.Add(customSave.cSave.CharacterUID, customSave);
                }

                //foreach (string filePath in Directory.GetFiles(directory, "*.xml"))
                //{
                //    CustomSavePaths.Add(Path.GetFileNameWithoutExtension(filePath), filePath);
                //}
            }
        }

        private void SaveChar(CharacterSave save)
        {
            SaveXML(save, typeof(CharacterSave), save.PSave.Name + "_" + save.PSave.UID, "Character");
        }

        private void SaveWorld(WorldSave save, string charName)
        {
            SaveXML(save, typeof(WorldSave), charName, "World");
        }

        private void SaveXML(object obj, Type t, string charName, string saveName)
        {
            var saveDir = @"Mods\SaveEditor";
            if (!Directory.Exists(saveDir)) { Directory.CreateDirectory(saveDir); }

            var playerDir = saveDir + @"\" + charName;
            if (!Directory.Exists(playerDir)) { Directory.CreateDirectory(playerDir); }

            var path = playerDir + @"\" + saveName + ".xml";
            if (File.Exists(path)) { File.Delete(path); }

            XmlSerializer xml = new XmlSerializer(t);
            FileStream file = File.Create(path);
            xml.Serialize(file, obj);
            file.Close();
        }

        private bool SaveHook(On.SaveInstance.orig_Save orig, SaveInstance self, bool _saveWorld)
        {
            bool flag = orig(self, _saveWorld);

            if (flag
                && CharacterManager.Instance.GetFirstLocalCharacter() is Character c
                && NetworkLevelLoader.Instance.GetCharacterSave(c.UID) is CharacterSave charSave
                && SaveManager.Instance.WorldSave is WorldSave worldSave)
            {
                //Debug.Log(" Saving CharSave for " + c.Name + " to XML...");

                SaveChar(charSave);
                SaveWorld(worldSave, c.Name + "_" + c.UID);

            }

            return flag;
        }

        // ======================= HOOKS ======================== //

        private void LoadSaveInstanceHook(On.SaveInstance.orig_LoadInstance orig, SaveInstance self)
        {
            // call orig self to load the proper save as a fallback, and to load the stuff we aren't overwriting afterwards.
            orig(self);

            // ========= custom save overwrite =========
            if (self.CharSave.LoadFromFile(self.SavePath) && !string.IsNullOrEmpty(self.CharSave.PSave.UID))
            {
                string uid = self.CharSave.PSave.UID;

                if (CustomSaves.ContainsKey(uid))
                {
                    self.CharSave = CustomSaves[uid].cSave;
                    self.WorldSave = CustomSaves[uid].wSave;
                }
            }
        }

        private void ApplyWorldSave(On.WorldSave.orig_ApplyData orig, WorldSave self)
        {
            if (CharacterManager.Instance.GetFirstLocalCharacter() is Character c && CustomSaves.ContainsKey(c.UID))
            {
                //OLogger.Warning("Setting custom world save to " + c.Name);

                WorldSave wSave = CustomSaves[c.UID].wSave;

                self.DefeatCountSaveData = wSave.DefeatCountSaveData;
                self.KillEventSendersList = wSave.KillEventSendersList;
                self.PlayerDeathEvents = wSave.PlayerDeathEvents;
                self.QuestEventList = wSave.QuestEventList;
                self.QuestList = wSave.QuestList;
                self.TOD = wSave.TOD;
            }
            orig(self);
        }

        private void ApplyCharacterSave(On.CharacterSave.orig_ApplyLoadedSaveToChar orig, CharacterSave self, Character _char)
        {
            if (self.PSave.NewSave && GameObject.Find("PvP")) // && SceneManagerHelper.ActiveSceneName == "Monsoon")
            {
                // PvP is running a custom NewSave game mode. Do nothing.
                orig(self, _char);
            }
            else
            {
                if (CustomSaves.ContainsKey(_char.UID))
                {
                    //OLogger.Warning("Applying custom CharSave to " + _char.Name);

                    self.PSave = CustomSaves[_char.UID].cSave.PSave;
                    self.ItemList = CustomSaves[_char.UID].cSave.ItemList;

                    orig(self, _char);

                    self.PSave = CustomSaves[_char.UID].cSave.PSave;
                    self.ItemList = CustomSaves[_char.UID].cSave.ItemList;
                }
                else
                {
                    orig(self, _char);
                }
            }
        }
    }
}
