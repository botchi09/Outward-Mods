using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Partiality.Modloader;
using SinAPI;
using static CustomKeybindings;

namespace CharacterEditor
{
    public class Settings
    {
        public bool AlwaysRenderHair;

        public bool ShowMenuOnStartup;
    }

    public class CharSaveData
    {
        public Character character;
        public CharacterVisualsPresets presets;

        public string Name = "";

        public Character.Gender Gender = 0;
        public int HairStyleIndex = 0;
        public int HairColorIndex = 0;
        public int SkinIndex = 0;
        public int HeadVariationIndex = 0;

        public bool isHardcore = false;
    }

    public class CharacterEditor : PartialityMod
    {
        public double version = 1.2;

        public GameObject _obj = null;
        public Script script;

        public CharacterEditor()
        {
            this.ModID = "Change Character Name";
            this.Version = version.ToString("0.00");
            this.author = "Sinai";
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (_obj == null)
            {
                _obj = new GameObject("ChangeCharName");
                GameObject.DontDestroyOnLoad(_obj);
            }

            script = _obj.AddComponent<Script>();
            script._base = this;
            script.Init();
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }
    }

    public class Script : MonoBehaviour
    {
        public CharacterEditor _base;

        public Settings settings = new Settings() { ShowMenuOnStartup = true, };

        public Rect m_window = Rect.zero;
        public bool ShowMenu = true;
        public string MenuKey = "Character Editor Menu";
        public bool lastMenuToggle;

        public CharSaveData currentSaveData;

        //public bool AlwaysRenderHair = true;

        public void Init()
        {
            LoadSettings();

            if (!settings.ShowMenuOnStartup) { ShowMenu = false; }

            AddAction(MenuKey, KeybindingsCategory.Menus, ControlType.Both, 5);
        }

        internal void Update()
        {
            if (Global.Lobby.PlayersInLobbyCount < 1) { return; }

            foreach (PlayerSystem ps in Global.Lobby.PlayersInLobby.Where(x => x.ControlledCharacter.IsLocalPlayer))
            {
                int id = ps.PlayerID;
                Character c = ps.ControlledCharacter;

                // menu toggle
                if (m_playerInputManager[id].GetButtonDown(MenuKey))
                {
                    ShowMenu = !ShowMenu;
                }

                // always render hair fix
                var hair = At.GetValue(typeof(CharacterVisuals), c.Visuals, "m_defaultHairVisuals") as ArmorVisuals;
                if (hair)
                {
                    if (settings.AlwaysRenderHair && !hair.gameObject.activeSelf)
                    {
                        c.Visuals.ShowHairVisual(true, 0);
                    }
                    if (!settings.AlwaysRenderHair && 
                        hair.gameObject.activeSelf
                        && c.Inventory.Equipment.GetEquippedItem(EquipmentSlot.EquipmentSlotIDs.Helmet) is Armor armor 
                        && armor.SpecialVisualPrefab.GetComponent<ArmorVisuals>().HideHair)
                    {
                        c.Visuals.ShowHairVisual(false, 0);
                    }
                }
            }

            // menu mouse control
            if (lastMenuToggle != ShowMenu)
            {
                lastMenuToggle = ShowMenu;

                Character c = CharacterManager.Instance.GetFirstLocalCharacter();

                if (c.CharacterUI.PendingDemoCharSelectionScreen is Panel panel)
                {
                    if (lastMenuToggle)
                        panel.Show();
                    else
                        panel.Hide();
                }
                else if (lastMenuToggle)
                {
                    GameObject obj = new GameObject();
                    obj.transform.parent = c.transform;
                    obj.SetActive(true);

                    Panel newPanel = obj.AddComponent<Panel>();
                    At.SetValue(newPanel, typeof(CharacterUI), c.CharacterUI, "PendingDemoCharSelectionScreen");
                    newPanel.Show();
                }
            }
        }

        private void LoadCharSave()
        {
            Character c = CharacterManager.Instance.GetFirstLocalCharacter();
            CharacterVisualData visData = c.VisualData;

            CharacterVisualsPresets presets = At.GetValue(typeof(CharacterVisuals), c.Visuals, "m_visualsPresets") as CharacterVisualsPresets;

            currentSaveData = new CharSaveData()
            {
                character = c,
                presets = presets,
                Name = c.Name,
                HeadVariationIndex = visData.HeadVariationIndex,
                Gender = visData.Gender,
                HairColorIndex = visData.HairColorIndex,
                HairStyleIndex = visData.HairStyleIndex,
                SkinIndex = visData.SkinIndex,
                isHardcore = c.HardcoreMode,
            };
        }

        private void ApplyVisualData()
        {
            CheckHeadMats();

            Character c = currentSaveData.character;

            At.SetValue(currentSaveData.Name, typeof(Character), c, "m_name");

            int g = (int)currentSaveData.Gender;
            int si = currentSaveData.SkinIndex;

            Material newSkin = (g != 0) ? currentSaveData.presets.FSkins[si] : currentSaveData.presets.MSkins[si];
            if (newSkin) { At.SetValue(newSkin, typeof(CharacterVisuals), c.Visuals, "m_skinMat"); }

            List<string> fieldsToDestroy = new List<string>() { "m_defaultHairVisuals", "m_defaultHeadVisuals", "m_defaultBodyVisuals", "m_defaultFootVisuals", "Head" };
            foreach (string field in fieldsToDestroy)
            {
                if (At.GetValue(typeof(CharacterVisuals), c.Visuals, field) is ArmorVisuals visuals
                    && c.transform.Find("Human_v(Clone)").Find(visuals.name) is Transform t)
                {
                    t.transform.parent = null;
                    t.gameObject.SetActive(false);
                    DestroyImmediate(t);
                }
            }

            // clear editor visual data loader
            ArmorVisuals[] empty = new ArmorVisuals[5];
            At.SetValue(empty, typeof(CharacterVisuals), c.Visuals, "m_editorDefaultVisuals");

            // set character's visual data to our settings
            c.VisualData.Gender = currentSaveData.Gender;
            c.VisualData.SkinIndex = currentSaveData.SkinIndex;
            c.VisualData.HairColorIndex = currentSaveData.HairColorIndex;
            c.VisualData.HairStyleIndex = currentSaveData.HairStyleIndex;
            c.VisualData.HeadVariationIndex = currentSaveData.HeadVariationIndex;

            // InitDefaultVisuals() handles the rest for us
            c.Visuals.InitDefaultVisuals();

            // fix the animator
            Animator a = At.GetValue(typeof(Character), c, "m_animator") as Animator;
            a.SetFloat("Gender", (float)((currentSaveData.Gender != Character.Gender.Male) ? 1 : 0));
        }

        private int CheckHeadMats()
        {
            int headMats = 0;

            if (currentSaveData.SkinIndex == 0) { headMats = currentSaveData.Gender == 0 ? currentSaveData.presets.MHeadsWhite.Count() : currentSaveData.presets.FHeadsWhite.Count(); }
            else if (currentSaveData.SkinIndex == 1) { headMats = currentSaveData.Gender == 0 ? currentSaveData.presets.MHeadsBlack.Count() : currentSaveData.presets.FHeadsBlack.Count(); }
            else if (currentSaveData.SkinIndex == 2) { headMats = currentSaveData.Gender == 0 ? currentSaveData.presets.MHeadsAsian.Count() : currentSaveData.presets.FHeadsAsian.Count(); }

            if (currentSaveData.HeadVariationIndex > headMats - 1) { currentSaveData.HeadVariationIndex = 0; }

            return headMats;
        }



        // =================== MENU ======================


        internal void OnGUI()
        {
            if (NetworkLevelLoader.Instance.IsGameplayPaused || CharacterManager.Instance.PlayerCharacters.Count < 1)
            {
                currentSaveData = null;
                return;
            }

            if (!ShowMenu) { return; }

            if (currentSaveData == null)
            {
                LoadCharSave();
            }

            if (m_window == Rect.zero)
            {
                m_window = new Rect(5, 5, 300, 360);
            }
            else
            {
                m_window = GUI.Window(12417, m_window, DrawMenu, "Outward Character Editor " + _base.version.ToString("0.00"));
            }
        }

        public void DrawMenu(int id)
        {
            if (currentSaveData == null) { return; }

            GUI.DragWindow(new Rect(0, 0, m_window.width - 50, 20));
            if (GUI.Button(new Rect(m_window.width - 50, 0, 45, 20), "X"))
            {
                ShowMenu = false;
            }

            GUILayout.BeginArea(new Rect(5, 23, m_window.width - 10, m_window.height - 25), GUI.skin.box);
            GUILayout.BeginVertical(new GUIStyle() { padding = new RectOffset(3, 0, 10, 3) });

            settings.AlwaysRenderHair = GUILayout.Toggle(settings.AlwaysRenderHair, "Always Render Hair");

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Name: ");
            currentSaveData.Name = GUILayout.TextField(currentSaveData.Name, GUILayout.Width(190));
            if (GUILayout.Button("Set"))
            {
                At.SetValue(currentSaveData.Name, typeof(Character), CharacterManager.Instance.GetFirstLocalCharacter(), "m_name");
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            currentSaveData.isHardcore = GUILayout.Toggle(currentSaveData.isHardcore, "Hardcore Mode");
            if (GUILayout.Button("Set"))
            {
                At.SetValue(currentSaveData.isHardcore, typeof(Character), CharacterManager.Instance.GetFirstLocalCharacter(), "HardcoreMode");
            }
            GUILayout.EndHorizontal();              

            GUILayout.Space(10);

            GUIVisualData(); // visual data editor here

            GUILayout.Space(15);

            GUIRandomizeButton(); // randomize

            GUILayout.Space(25);

            settings.ShowMenuOnStartup = GUILayout.Toggle(settings.ShowMenuOnStartup, "Show Menu On Startup");

            GUILayout.EndVertical();
            GUILayout.EndArea();

            GUI.skin.label.wordWrap = true;
            GUI.skin.textField.wordWrap = true;
        }

        private void GUIVisualData()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Gender: " + currentSaveData.Gender.ToString());
            if (GUILayout.Button("<")) { if (currentSaveData.Gender == Character.Gender.Female) { currentSaveData.Gender = Character.Gender.Male; ApplyVisualData(); } }
            if (GUILayout.Button(">")) { if (currentSaveData.Gender == Character.Gender.Male) { currentSaveData.Gender = Character.Gender.Female; ApplyVisualData(); } }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Hair Color: " + currentSaveData.HairColorIndex);
            if (GUILayout.Button("<")) { if (currentSaveData.HairColorIndex > 0) { currentSaveData.HairColorIndex -= 1; ApplyVisualData(); } }
            if (GUILayout.Button(">")) { if (currentSaveData.HairColorIndex < currentSaveData.presets.HairMaterials.Count() - 1) { currentSaveData.HairColorIndex += 1; ApplyVisualData(); } }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Hair Style: " + currentSaveData.HairStyleIndex);
            if (GUILayout.Button("<")) { if (currentSaveData.HairStyleIndex > 0) { currentSaveData.HairStyleIndex -= 1; ApplyVisualData(); } }
            if (GUILayout.Button(">")) { if (currentSaveData.HairStyleIndex < currentSaveData.presets.Hairs.Count() - 1) { currentSaveData.HairStyleIndex += 1; ApplyVisualData(); } }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Skin Color: " + currentSaveData.SkinIndex);
            if (GUILayout.Button("<")) { if (currentSaveData.SkinIndex > 0) { currentSaveData.SkinIndex -= 1; ApplyVisualData(); } }
            if (GUILayout.Button(">")) { if (currentSaveData.SkinIndex < 2) { currentSaveData.SkinIndex += 1; ApplyVisualData(); } }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Head Style: " + currentSaveData.HeadVariationIndex);
            if (GUILayout.Button("<")) { if (currentSaveData.HeadVariationIndex > 0) { currentSaveData.HeadVariationIndex -= 1; ApplyVisualData(); } }
            if (GUILayout.Button(">")) { if (currentSaveData.HeadVariationIndex < CheckHeadMats() - 1) { currentSaveData.HeadVariationIndex += 1; ApplyVisualData(); } }
            GUILayout.EndHorizontal();
        }

        private void GUIRandomizeButton()
        {
            if (GUILayout.Button("Randomize"))
            {
                CharacterVisualData data2 = currentSaveData.presets.Randomize();
                currentSaveData.HairColorIndex = data2.HairColorIndex;
                currentSaveData.HairStyleIndex = data2.HairStyleIndex;
                currentSaveData.HeadVariationIndex = data2.HeadVariationIndex;
                currentSaveData.SkinIndex = data2.SkinIndex;

                // for some reason Randomize Gender doesn't seem to work? idk, just did it this way instead
                Character.Gender g = Character.Gender.Male;
                int r = UnityEngine.Random.Range(0, 9);
                if (r > 4) { g = Character.Gender.Female; }
                currentSaveData.Gender = g;
            }

            ApplyVisualData();
        }


        // ========== save / load ===========

        private void LoadSettings()
        {
            bool flag = true;
            try
            {
                string path = @"Mods\CharVisualsEditor.json";
                if (File.Exists(path))
                {
                    Settings s2 = JsonUtility.FromJson<Settings>(File.ReadAllText(path));
                    if (s2 != null)
                    {
                        settings = s2;
                        flag = false;
                    }
                }
            } catch { }

            if (flag)
            {
                settings = new Settings()
                {
                    //AddBeards = false,
                    AlwaysRenderHair = false,
                    ShowMenuOnStartup = true,
                };

                SaveSettings();
            }
        }

        internal void OnDisable()
        {
            SaveSettings();
        }

        private void SaveSettings()
        {
            string path = @"Mods\CharVisualsEditor.json";

            Directory.CreateDirectory(@"Mods");

            Jt.SaveJsonOverwrite(path, settings);
        }
    }
}
