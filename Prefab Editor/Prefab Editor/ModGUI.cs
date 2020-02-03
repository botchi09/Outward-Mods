using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using SinAPI;

namespace PrefabEditor
{
    public class ModGUI : MonoBehaviour
    {
        public EditorGlobal global;

        // main menu
        public bool ShowEditor = true;
        private Rect m_window = Rect.zero;

        public Vector2 editemItemScroll;
        public Vector2 itemListScroll;
        public Vector2 editorScroll;

        public string itemSearch = "";
        public bool showAllItems;

        public List<string> editFields = new List<string>(); // arbitrary list of strings for use with the GUI edit fields

        // scaling
        private Vector2 m_virtualSize = new Vector2(1920, 1080);
        private Vector2 m_currentSize = Vector2.zero;
        public Matrix4x4 m_scaledMatrix;

        // drop downs
        public Vector3 lastMousePos;
        public DropDown WeaponDrop = new DropDown();
        public DropDown StatusDrop = new DropDown();
        public DropDown AnimTypeDrop = new DropDown();

        // colors
        Color lightRed = new Color() { r = 1, b = 0.7f, g = 0.7f, a = 1 };
        Color lightGreen = new Color() { r = 0.7f, b = 0.7f, g = 1.0f, a = 1.0f };

        internal void Update()
        {
            if (m_currentSize.x != Screen.width || m_currentSize.y != Screen.height)
            {
                m_scaledMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / m_virtualSize.x, Screen.height / m_virtualSize.y, 1));
                m_currentSize = new Vector2(Screen.width, Screen.height);
            }
            
            if (Global.Lobby.PlayersInLobbyCount > 0)
            {
                // menu mouse fix
                if (lastMenuToggle != !global.GlobalHideGUI)
                {
                    lastMenuToggle = !global.GlobalHideGUI;

                    Character c = CharacterManager.Instance.GetFirstLocalCharacter();

                    if (c.CharacterUI.PendingDemoCharSelectionScreen is Panel panel)
                    {
                        if (lastMenuToggle)
                            panel.Show();
                        else
                            panel.Hide();
                    }
                    else
                    {
                        GameObject obj = new GameObject();
                        obj.transform.parent = c.transform;
                        obj.SetActive(true);

                        Panel newPanel = obj.AddComponent<Panel>();
                        At.SetValue(newPanel, typeof(CharacterUI), c.CharacterUI, "PendingDemoCharSelectionScreen");

                        if (lastMenuToggle) { newPanel.Show(); }
                        else { newPanel.Hide(); }
                    }
                }
            }
        }

        public bool lastMenuToggle;

        internal void OnGUI()
        {
            if (global.GlobalHideGUI)
            {
                return;
            }

            // scaling
            Matrix4x4 orig = GUI.matrix;
            if (!global.cfg.Disable_Scaling)
            {
                GUI.matrix = m_scaledMatrix;
            }

            // main gui
            if (m_window == Rect.zero && ResolutionController.Instance.enabled)
            {
                m_window = new Rect(5, 5, 565, 600);
            }
            else if (ResolutionController.Instance.enabled)
            {
                if (ShowEditor)
                {
                    m_window.height = 600;
                    m_window = GUI.Window(2, m_window, this.DrawMenu, "Outward Prefab Editor 1.6");
                }
                else
                {
                    m_window.height = 30;
                    m_window = GUI.Window(90187263, m_window, this.DrawMenu, "");
                }

                if (m_window.height > Screen.height)
                {
                    m_window = new Rect(m_window.x, m_window.y, m_window.width, m_window.height - 100);
                }
            }

            if (ShowEditor)
            {
                // weapon selector drop down
                if (WeaponDrop.show)
                {
                    if (WeaponDrop.m_dropRect == Rect.zero)
                    {
                        WeaponDrop.m_dropRect = new Rect(lastMousePos.x, Screen.height - lastMousePos.y, 130, 150);
                    }
                    else
                    {
                        WeaponDrop.m_dropRect = GUI.Window(90187264, WeaponDrop.m_dropRect, DrawWeaponDrop, "");
                        GUI.BringWindowToFront(90187264);
                    }
                }

                // status effect selector dropdown
                if (StatusDrop.show)
                {
                    if (StatusDrop.m_dropRect == Rect.zero)
                    {
                        StatusDrop.m_dropRect = new Rect(lastMousePos.x, Screen.height - lastMousePos.y, 200, 200);
                    }
                    else
                    {
                        StatusDrop.m_dropRect = GUI.Window(4352345, StatusDrop.m_dropRect, DrawStatusDropBox, "");
                        GUI.BringWindowToFront(90187265);
                    }
                }

                // custom anim type dropdown
                if (AnimTypeDrop.show)
                {
                    if (AnimTypeDrop.m_dropRect == Rect.zero)
                    {
                        AnimTypeDrop.m_dropRect = new Rect(lastMousePos.x, Screen.height - lastMousePos.y, 250, 200);
                    }
                    else
                    {
                        AnimTypeDrop.m_dropRect = GUI.Window(7234, AnimTypeDrop.m_dropRect, DrawAnimTypeBox, "");
                        GUI.BringWindowToFront(7234);
                    }
                }
            }

            GUI.matrix = orig;
        }

        //public bool viewingCustomEdits = false;

        private void DrawMenu(int id)
        {
            GUI.DragWindow(new Rect(0, 0, m_window.width - 125, 20));

            if (!ShowEditor)
            {
                GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                if (GUI.Button(new Rect(m_window.width - 125, 2, 75, 25), "Restore"))
                {
                    ShowEditor = true;
                }
                if (GUI.Button(new Rect(m_window.width - 50, 2, 40, 25), "X"))
                {
                    global.GlobalHideGUI = true;
                }
                return;
            }
            else
            {
                GUILayout.Space(35);
            }

            if (GUI.Button(new Rect(m_window.width - 90, 3, 40, 20), "-"))
            {
                ShowEditor = false;
            }
            if (GUI.Button(new Rect(m_window.width - 50, 2, 40, 25), "X"))
            {
                global.GlobalHideGUI = true;
            }

            GUILayout.BeginArea(new Rect(10, 20, m_window.width - 20, m_window.height - 30), GUI.skin.box);

            if (global.currentTemplate == null)
            {
                // current edits

                GUILayout.BeginVertical(GUI.skin.box, GUILayout.Height(100));

                GUILayout.Label("Current Edits:");

                if (global.editedItems.Count > 0)
                {
                    editemItemScroll = GUILayout.BeginScrollView(editemItemScroll, GUILayout.Height(170));
                    for (int i = 0; i < global.editedItems.Count(); i++)
                    {
                        GUILayout.BeginHorizontal();

                        var item = global.editedItems[i];

                        GUI.color = lightRed;
                        if (GUILayout.Button("Delete", GUILayout.Width(60)))
                        {
                            ResetStats(item, item.ItemID);
                        }

                        GUI.color = Color.white;
                        if (GUILayout.Button(item.Name + " (" + item.gameObject.name + ")"))
                        {
                            global.SetItemPrefab(item);
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndScrollView();
                }
                else
                {
                    GUILayout.Label("No items edited yet!");
                }

                GUILayout.EndVertical();

                // ========= item list ===========

                GUILayout.BeginHorizontal();
                GUILayout.Label("Search for an item: ");
                itemSearch = GUILayout.TextField(itemSearch, new GUILayoutOption[] { GUILayout.Width(300) }); // current selected item text field
                showAllItems = GUILayout.Toggle(showAllItems, "Show all");
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                itemListScroll = GUILayout.BeginScrollView(itemListScroll);
                if (itemSearch != "" || showAllItems)
                {
                    foreach (KeyValuePair<string, Item> entry in global.allItems.Where(x => x.Key.ToLower().Contains(itemSearch.ToLower())))
                    {
                        GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                        if (GUILayout.Button(entry.Key, new GUILayoutOption[] { GUILayout.Height(22), GUILayout.MaxWidth(510) }))
                        {
                            global.SetItemPrefab(entry.Value);
                        }
                        GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    }
                }
                GUILayout.EndScrollView();
            }
            else if (global.currentTarget != null && global.currentTemplate != null)
            {
                ItemEditorWindow();
            }

            GUILayout.BeginHorizontal();
            global.cfg.Disable_Scaling = GUILayout.Toggle(global.cfg.Disable_Scaling, "Disable GUI scaling");
            global.cfg.Hide_On_Startup = GUILayout.Toggle(global.cfg.Hide_On_Startup, "Hide GUI on Startup");
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
            GUI.color = Color.white;
        }

        private void ItemEditorWindow()
        {
            // =========== item editor window ===========

            if (GUILayout.Button("< Back", GUILayout.Width(100)))
            {
                global.currentTemplate = null;
                global.currentTarget = null;
                GUILayout.EndArea();
                return;
            }

            editorScroll = GUILayout.BeginScrollView(editorScroll, GUILayout.Height(m_window.height - 100));
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.MaxWidth(m_window.width - 45));

            GUILayout.BeginHorizontal();
            GUI.skin.label.alignment = TextAnchor.UpperCenter;
            GUILayout.Label("Editing " + global.currentTemplate.Name + " (" + global.currentTemplate.ItemID + ")",
                new GUILayoutOption[] { GUILayout.Height(30), GUILayout.Width(250) });
            GUI.skin.label.alignment = TextAnchor.UpperLeft;

            if (CharacterManager.Instance.GetFirstLocalCharacter() is Character c)
            {
                if (global.currentTarget is Skill)
                {
                    if (GUILayout.Button("Learn Skill", GUILayout.Width(100)) && !c.Inventory.SkillKnowledge.IsItemLearned(global.currentTarget.ItemID))
                    {
                        Item item = ItemManager.Instance.CloneItem(global.currentTarget as Skill);
                        item.ChangeParent(c.Inventory.SkillKnowledge.transform);
                    }
                    if (GUILayout.Button("Unlearn Skill", GUILayout.Width(100)) && c.Inventory.SkillKnowledge.IsItemLearned(global.currentTarget.ItemID))
                    {
                        Item itemFromItemID = c.Inventory.SkillKnowledge.GetItemFromItemID(global.currentTarget.ItemID);
                        if (itemFromItemID) { ItemManager.Instance.DestroyItem(itemFromItemID.UID); }
                    }
                }
                else
                {
                    if (GUILayout.Button("Add to bag", GUILayout.Width(100)))
                    {
                        c.Inventory.GenerateItem(ResourcesPrefabManager.Instance.GetItemPrefab(global.currentTemplate.ItemID), 1, false);
                    }
                    if (GUILayout.Button("Spawn object", GUILayout.Width(100)))
                    {
                        global.SpawnObject();
                    }
                }
            }
            GUILayout.EndHorizontal();
            // ----------- ITEM EDITOR -----------

            GUILayout.BeginVertical();

            global.currentTemplate.Name = GUILayout.TextField(global.currentTemplate.Name, GUILayout.Width(200));
            global.currentTemplate.Description = GUILayout.TextArea(global.currentTemplate.Description, GUILayout.Height(50));

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            var template = global.currentTemplate;
            var flag = false;
            if (template.CastModifier == 1) { flag = true; }
            flag = GUILayout.Toggle(flag, "Cast Mobile:", GUILayout.Width(100));
            if (flag)
            {
                template.CastModifier = (int)Character.SpellCastModifier.Mobile;
            }
            else
            {
                template.CastModifier = (int)Character.SpellCastModifier.Immobilized;
            }

            SimpleEditField("Cast Movespeed:", "MobileCastMovementMult", 99, false);

            GUILayout.Label("Cast Anim:", GUILayout.Width(80));
            var spelltype = (Character.SpellCastType)template.m_activateEffectAnimType;
            if (GUILayout.Button(spelltype.ToString(), GUILayout.Width(120)))
            {
                AnimTypeDrop.show = !AnimTypeDrop.show;
                lastMousePos = Input.mousePosition;
            }
            template.m_activateEffectAnimType = AnimTypeDrop.selected;
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            if (global.currentTarget.Stats != null)
            {
                GUITitle("Item Stats");

                GUILayout.BeginHorizontal();

                SimpleEditField("Max Durability:", "MaxDurability", 0, false);
                SimpleEditField("Base Value:", "m_baseValue", 1, false);
                SimpleEditField("Weight:", "m_rawWeight", 2, false);

                GUILayout.EndHorizontal();
            }

            if (global.currentTarget is Equipment)
            {
                SimpleEquipmentPage();
            }
            else if (global.currentTarget is Skill)
            {
                if (global.currentTarget is PassiveSkill)
                {
                    PassiveSkillPage();
                }
                else
                {
                    SkillPage();
                }
            }

            GUILayout.EndVertical();

            // -------------------------------------

            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            GUILayout.Space(15);

            GUILayout.BeginHorizontal();

            if (global.editedItems.Contains(global.currentTarget.ItemID) && global.origStats.ContainsKey(global.currentTarget.ItemID))
            {
                GUI.color = lightRed;

                if (GUILayout.Button("Reset stats", GUILayout.Width(130)))
                {
                    ResetStats(global.currentTarget, global.currentTarget.ItemID);

                    global.SetItemPrefab(global.currentTarget);
                }
            }
            else
            {
                GUILayout.Space(130);
            }

            GUI.color = lightGreen;

            if (GUILayout.Button("Apply and Save"))
            {
                global.ApplyEdits(global.currentTemplate, global.currentTarget);
                global.SaveTemplate(global.currentTemplate, global.currentTarget);
            }

            GUILayout.EndHorizontal();
        }

        private void ResetStats(Item item, int ID)
        {
            ItemTemplate template = global.origStats[ID];

            global.ApplyEdits(template, item);

            string path = EditorGlobal.saveDir + "/" + item.gameObject.name + ".json";

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            if (global.editedItems.Contains(ID))
            {
                global.editedItems.Remove(item);
            }

            //script.SaveTemplate(script.currentTemplate, script.currentTarget);
        }

        public void PassiveSkillPage()
        {
            int offset = 3;
            foreach (PassiveEffect pe in (global.currentTemplate as PassiveTemplate).Effects)
            {
                GUITitle(pe.Type);

                if (pe.Type == "AffectStat")
                {
                    string stat = TagSourceManager.Instance.GetTag(pe.AffectedStat.ToString()).TagName;
                    GUILayout.Label("Affected Stat Tag: " + stat);
                }

                GUILayout.BeginHorizontal();
                GUI.skin.label.alignment = TextAnchor.UpperRight;

                GUILayout.Label("Value:", GUILayout.Width(115));
                if (editFields[offset] == "") { editFields[offset] = pe.Value.ToString(); }
                editFields[offset] = TextAreaEdit(offset, false);
                if (Single.TryParse(editFields[offset], out float value))
                {
                    pe.Value = value;
                }
                else
                {
                    Invalid();
                }
                offset++;

                if (pe.Type != "AffectStamina")
                {
                    GUILayout.Label("Is Modifier?", GUILayout.Width(115));
                    pe.IsModifier = GUILayout.Toggle(pe.IsModifier, "", GUILayout.Width(25));
                }

                GUILayout.EndHorizontal();
                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }
        }

        public void SkillPage()
        {
            GUITitle("Skill");
            SkillTemplate template = global.currentTemplate as SkillTemplate;

            GUILayout.BeginHorizontal();

            GUI.skin.label.alignment = TextAnchor.UpperRight;

            SimpleEditField("Cooldown:", "Cooldown", 0, false);
            SimpleEditField("Mana Cost:", "ManaCost", 1, false);
            SimpleEditField("Stamina Cost:", "StaminaCost", 2, false);

            GUILayout.EndHorizontal();

            int offset = 4;

            if (template == null || template.Damages == null) {   return; }

            foreach (SkillDamage damages in template.Damages)
            {
                GUITitle(damages.transform);

                GUILayout.BeginHorizontal();
                GUI.skin.label.alignment = TextAnchor.UpperRight;

                GUI.color = Color.white;
                GUILayout.Label("Impact", GUILayout.Width(115));
                if (editFields[offset] == "") { editFields[offset] = damages.Impact.ToString(); }
                editFields[offset] = TextAreaEdit(offset, false);
                if (float.TryParse(editFields[offset], out float f))
                {
                    damages.Impact = f;
                }
                else { Invalid(); }
                offset++;

                if (damages.PunctualType == "WeaponDamage")
                {
                    GUI.color = Color.white;
                    GUILayout.Label("Impact Multiplier", GUILayout.Width(115));
                    if (editFields[offset] == "") { editFields[offset] = damages.ImpactMultiplier.ToString(); }
                    editFields[offset] = TextAreaEdit(offset, false);
                    if (float.TryParse(editFields[offset], out float f2))
                    {
                        damages.ImpactMultiplier = f2;
                    }
                    else { Invalid(); }
                    offset++;

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();

                    GUI.color = Color.white;
                    GUILayout.Label("Damage Multiplier", GUILayout.Width(115));
                    if (editFields[offset] == "") { editFields[offset] = damages.DamageMultiplier.ToString(); }
                    editFields[offset] = TextAreaEdit(offset, false);
                    if (float.TryParse(editFields[offset], out float f3))
                    {
                        damages.DamageMultiplier = f3;
                    }
                    else { Invalid(); }
                    offset++;

                    GUI.color = Color.white;
                    GUILayout.Label("Damage Override", GUILayout.Width(115));
                    if (editFields[offset] == "") { editFields[offset] = Enum.GetName(typeof(DamageType.Types), damages.DamageOverride); }
                    editFields[offset] = GUILayout.TextField(editFields[offset], GUILayout.Width(80));
                    try
                    {
                        damages.DamageOverride = (int)Enum.Parse(typeof(DamageType.Types), editFields[offset]);
                    }
                    catch
                    {
                        if (editFields[offset] == "Lightning")
                        {
                            editFields[offset] = "Electric";
                        }
                        Invalid();
                    }
                    offset++;

                }

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUI.skin.label.alignment = TextAnchor.UpperLeft;

                DamageList damageList = new DamageList();
                foreach (DamageType type in damages.Damages)
                {
                    damageList.Add(type);
                }
                GUI.color = Color.white;
                int j = 0;
                for (int i = 0; i < 6; i++)
                {
                    var type = (DamageType.Types)i;

                    if (damageList.NotListedTypes.Contains(type))
                    {
                        continue;
                    }

                    DamageType damage = damageList[type];

                    if (j == 3)
                    {
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                    }

                    GUILayout.Label(global.statHlpr.DamageNames[(int)damage.Type], GUILayout.Width(80));
                    if (editFields[offset + i] == "")
                    {
                        editFields[offset + i] = damage.Damage.ToString();
                    }

                    editFields[offset + i] = TextAreaEdit(offset + i, false); GUI.color = Color.white;

                    if (Single.TryParse(editFields[offset + i], out float f4))
                    {
                        damage.Damage = f4;
                    }
                    else { Invalid(); }

                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        damageList.Remove(damage.Type);
                    }
                    GUILayout.Space(10);

                    j++;
                }
                GUILayout.EndHorizontal();

                if (damageList.NotListedTypes.Count > 0)
                {
                    GUILayout.BeginHorizontal();
                    GUI.color = Color.white;

                    GUILayout.Label("Add: ", GUILayout.Width(40));
                    int k = 0;
                    foreach (DamageType.Types type in damageList.NotListedTypes)
                    {
                        if (type == DamageType.Types.DarkOLD || type == DamageType.Types.LightOLD)
                            break;
                        
                        if (k == 5) { GUILayout.EndHorizontal(); GUILayout.BeginHorizontal(); }

                        if (GUILayout.Button(global.statHlpr.DamageNames[(int)type], GUILayout.Width(80)))
                        {
                            DamageList dl = new DamageList();
                            dl.Add(type);
                            dl[type].Damage = 0.0f;
                            editFields[offset + (int)type] = dl[type].Damage.ToString();
                            damageList.Add(dl);
                        }
                        k++;
                    }
                    GUI.skin.label.alignment = TextAnchor.UpperLeft;
                    GUILayout.EndHorizontal();
                }

                offset += 10;

                damages.Damages = damageList.List.ToArray();
            }
        }

        private void SimpleEquipmentPage()
        {
            if (global.currentTarget == null)
                return;

            if (global.currentTarget.Stats != null)
            {
                if (global.currentTarget.Stats is WeaponStats)
                {
                    WeaponStatsPage();
                }

                if (global.currentTarget.Stats is EquipmentStats)
                {
                    EquipStatsPage();
                }
            }
        }

        private void WeaponStatsPage()
        {
            if (global.currentTarget is Weapon)
            {
                WeaponTemplate _template = global.currentTemplate as WeaponTemplate;

                GUILayout.BeginVertical();
                GUITitle("Weapon Stats");

                GUILayout.BeginHorizontal();

                SelectWeaponType();

                SimpleEditField("Attack Speed:", "AttackSpeed", 24, false);
                SimpleEditField("Impact:", "Impact", 25, false);

                GUILayout.EndHorizontal();

                GUILayout.Space(15);

                GUILayout.BeginHorizontal();

                GUILayout.Space(20);

                _template.TwoHand = GUILayout.Toggle(_template.TwoHand, "Two Handed?", GUILayout.Width(130));

                _template.OffHanded = GUILayout.Toggle(_template.OffHanded, "Off-Handed?", GUILayout.Width(130));

                GUILayout.EndHorizontal();

                GUILayout.Space(15);

                GUITitle("Base Damage");

                GUILayout.BeginHorizontal();

                int j = 0;
                for (int i = 0; i < 6; i++)
                {
                    var type = (DamageType.Types)i;

                    if (_template.BaseDamage.NotListedTypes.Contains(type))
                    {
                        continue;
                    }

                    DamageType damage = _template.BaseDamage[type];

                    if (j == 3)
                    {
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                    }

                    GUILayout.Label(global.statHlpr.DamageNames[(int)damage.Type], GUILayout.Width(80));
                    if (editFields[26 + i] == "")
                    {
                        editFields[26 + i] = damage.Damage.ToString();
                    }

                    editFields[26 + i] = TextAreaEdit(26 + i, false); GUI.color = Color.white;

                    if (Single.TryParse(editFields[26 + i], out float f))
                    {
                        damage.Damage = f;
                    }
                    else { Invalid(); }

                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        _template.BaseDamage.Remove(damage.Type);
                    }
                    GUILayout.Space(10);

                    j++;
                }
                GUILayout.EndHorizontal();

                if (_template.BaseDamage.NotListedTypes.Count > 0)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Add: ", GUILayout.Width(40));
                    foreach (DamageType.Types type in _template.BaseDamage.NotListedTypes)
                    {
                        if (type == DamageType.Types.DarkOLD || type == DamageType.Types.LightOLD)
                            break;

                        int k = 0;
                        if (GUILayout.Button(global.statHlpr.DamageNames[((int)type)], GUILayout.Width(80)))
                        {
                            if (k == 5) { GUILayout.EndHorizontal(); GUILayout.BeginHorizontal(); }

                            DamageList dl = new DamageList();
                            dl.Add(type);
                            dl[type].Damage = 0.0f;
                            editFields[26 + (int)type] = dl[type].Damage.ToString();
                            _template.BaseDamage.Add(dl);

                            k++;
                        }

                    }
                    GUI.skin.label.alignment = TextAnchor.UpperLeft;
                    GUILayout.EndHorizontal();
                }

                GUITitle("Status Effects");

                GUILayout.Label("Remove Effects:", GUILayout.Width(120));

                List<string> notListedStatuses = new List<string>();
                foreach (string s in global.statHlpr.StatusNames)
                    notListedStatuses.Add(s);

                for (int i = 0; i < _template.hitEffects.Count(); i++)
                {
                    string status = _template.hitEffects[i];
                    for (int k = 0; k < notListedStatuses.Count - 1; k++)
                    {
                        if (k >= notListedStatuses.Count)
                            break;

                        if (notListedStatuses[k] == status)
                        {
                            notListedStatuses.RemoveAll(x => x == status);
                            k -= 1;
                        }
                    }

                    GUILayout.BeginHorizontal();

                    GUI.color = lightRed;
                    if (GUILayout.Button("X", GUILayout.Width(50)))
                    {
                        StatusDrop.show = false;
                        if (_template.hitEffects.Find(x => x == status) is string y)
                        {
                            _template.hitEffects.Remove(y);
                        }
                    }
                    GUI.color = Color.white;

                    GUILayout.Label(status);

                    GUILayout.EndHorizontal();

                    if (i >= _template.hitEffects.Count() - 1)
                        break;
                }

                GUILayout.BeginHorizontal();

                GUILayout.Label("Add an Effect:", GUILayout.Width(120));
                GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                if (GUILayout.Button(global.statHlpr.StatusNames[StatusDrop.selected], GUILayout.Width(160)))
                {
                    if (!StatusDrop.show)
                    {
                        StatusDrop.show = true;
                        lastMousePos = Input.mousePosition;
                    }
                    else
                    {
                        StatusDrop.show = false;
                    }
                }
                GUI.skin.button.alignment = TextAnchor.MiddleCenter;

                if (GUILayout.Button("Add", GUILayout.Width(60)))
                {
                    StatusDrop.show = false;

                    if (!_template.hitEffects.Contains(global.statHlpr.StatusNames[StatusDrop.selected]))
                    {
                        _template.hitEffects.Add(global.statHlpr.StatusNames[StatusDrop.selected]);
                    }

                    for (int i = 0; i < global.statHlpr.StatusNames.Count; i++)
                    {
                        if (_template.hitEffects.Contains(global.statHlpr.StatusNames[i]))
                            continue;
                        StatusDrop.selected = i;
                        break;
                    }

                }
                GUILayout.EndHorizontal();



                GUILayout.EndVertical();
            }
        }

        private void EquipStatsPage()
        {
            EquipmentTemplate _template = global.currentTemplate as EquipmentTemplate;

            // damage attack
            GUILayout.BeginVertical();
            GUITitle("Damage Bonus %");
            DamageListEdits(3); // 3 = phys bonus offset
            for (int i = 0; i < 6; i++)
            {
                if (editFields[i + 3] == "")
                {
                    editFields[i + 3] = _template.m_damageAttack[i].ToString();
                }
                if (Single.TryParse(editFields[i + 3], out float j))
                {
                    _template.m_damageAttack[i] = j;
                }
                else { Invalid(); GUILayout.Label("Invalid damage list", GUILayout.Width(150)); }
            }
            GUILayout.EndVertical();

            // damage resistance
            GUILayout.BeginVertical();
            GUITitle("Damage Resistance %");
            DamageListEdits(9); // 9 = phys resist offset
            for (int i = 0; i < 6; i++)
            {
                if (editFields[i + 9] == "")
                {
                    editFields[i + 9] = _template.m_damageResistance[i].ToString();
                }
                if (Single.TryParse(editFields[i + 9], out float j))
                {
                    _template.m_damageResistance[i] = j;
                }
                else { Invalid(); GUILayout.Label("Invalid damage list", GUILayout.Width(150)); }
            }
            GUILayout.EndVertical();

            GUITitle("Other Equipment Stats");
            // other resist stats
            GUILayout.BeginHorizontal();
            GUI.skin.label.alignment = TextAnchor.UpperRight;
            GUILayout.Label("Protection", GUILayout.Width(110));
            editFields[15] = TextAreaEdit(15, false); GUI.color = Color.white;
            if (editFields[15] == "")
                editFields[15] = _template.m_damageProtection[0].ToString();
            if (Single.TryParse(editFields[15], out float prot))
            {
                _template.m_damageProtection[0] = prot;
            }
            else { Invalid(); }

            SimpleEditField("Impact Res.:", "m_impactResistance", 16, false);
            SimpleEditField("HP Bonus:", "m_maxHealthBonus", 18, false);

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            SimpleEditField("Mana Modifier %:", "m_manaUseModifier", 17, true);
            SimpleEditField("Stam Penalty %:", "m_staminaUsePenalty", 21, true);
            SimpleEditField("Speed Penalty %:", "m_movementPenalty", 19, true);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            SimpleEditField("Pouch Bonus:", "m_pouchCapacityBonus", 20, false);
            SimpleEditField("Cold Protect:", "m_coldProtection", 22, false);
            SimpleEditField("Heat Protect:", "m_heatProtection", 23, false);
            GUILayout.EndHorizontal();
        }

        private void SimpleEditField(string label, string fieldname, int id, bool invertedType, Type type = null) // inverted type means a negative value is desired (ie move speed penalty)
        {
            GUI.skin.label.alignment = TextAnchor.UpperRight;
            GUILayout.Label(label, GUILayout.Width(115));

            Type t = type ?? global.currentTemplate.GetType();

            if (editFields[id] == "")
            {
                editFields[id] = At.GetValue(t, global.currentTemplate, fieldname).ToString();
            }

            editFields[id] = TextAreaEdit(id, invertedType); GUI.color = Color.white;
            if (Single.TryParse(editFields[id], out float value))
            {
                if (id <= 1) // first two edit fields use int values (durability and base value)
                {
                    At.SetValue(Convert.ToInt32(value), t, global.currentTemplate, fieldname);
                }
                else
                {
                    At.SetValue(value, t, global.currentTemplate, fieldname);
                }
            }
            else
            {
                Invalid();
            }

            GUI.skin.label.alignment = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        private string TextAreaEdit(int id, bool invertedType)
        {
            if (Single.TryParse(editFields[id], out float i))
            {
                if (!invertedType)
                {
                    if (i > 0)
                        GUI.color = lightGreen;
                    else
                        GUI.color = lightRed;
                }
                else
                {
                    if (i >= 0)
                        GUI.color = lightRed;
                    else
                        GUI.color = lightGreen;
                }
            };
            return editFields[id] = GUILayout.TextField(editFields[id], GUILayout.Width(40));
        }

        private void GUITitle(string label)
        {
            GUI.skin.label.fontSize = 15;
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUI.skin.label.alignment = TextAnchor.LowerCenter;
            GUILayout.Label(label, GUILayout.Height(35));
            GUI.skin.label.fontSize = 13;
            GUI.skin.label.fontStyle = FontStyle.Normal;
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
        }

        private void SelectWeaponType()
        {
            GUILayout.Label("Type:", GUILayout.Width(40));

            if (GUILayout.Button(global.statHlpr.weaponTypes.ElementAt(WeaponDrop.selected).Key, GUILayout.Width(110)))
            {
                if (!WeaponDrop.show)
                {
                    WeaponDrop.show = true;
                    lastMousePos = Input.mousePosition;
                }
                else
                {
                    WeaponDrop.show = false;
                }
            }
        }

        private void DrawWeaponDrop(int id)
        {
            if (WeaponDrop.show)
            {
                GUILayout.BeginArea(new Rect(3, 3, WeaponDrop.m_dropRect.width - 3, WeaponDrop.m_dropRect.height - 3));

                WeaponDrop.scroll = GUILayout.BeginScrollView(WeaponDrop.scroll);

                for (int i = 0; i < global.statHlpr.weaponTypes.Count; i++)
                {
                    if (GUILayout.Button(global.statHlpr.weaponTypes.ElementAt(i).Key))
                    {
                        WeaponDrop.show = false;
                        WeaponDrop.m_dropRect = Rect.zero;
                        WeaponDrop.selected = i;
                        (global.currentTemplate as WeaponTemplate).WeaponType = (int)global.statHlpr.weaponTypes.ElementAt(WeaponDrop.selected).Value;
                    }
                }

                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }
        }

        private void DrawStatusDropBox(int id)
        {
            if (StatusDrop.show)
            {
                GUILayout.BeginArea(new Rect(3, 3, StatusDrop.m_dropRect.width - 3, StatusDrop.m_dropRect.height - 3));

                StatusDrop.scroll = GUILayout.BeginScrollView(StatusDrop.scroll);
                for (int i = 0; i < global.statHlpr.StatusNames.Count; i++)
                {
                    if ((global.currentTemplate as WeaponTemplate).hitEffects.Count > 0 && (global.currentTemplate as WeaponTemplate).hitEffects.Contains(global.statHlpr.StatusNames[i]))
                    {
                        continue;
                    }

                    if (GUILayout.Button(global.statHlpr.StatusNames[i]))
                    {
                        StatusDrop.show = false;
                        StatusDrop.m_dropRect = Rect.zero;
                        StatusDrop.selected = i;
                    }

                }

                GUILayout.EndScrollView();

                GUILayout.EndArea();
            }
        }

        private void DrawAnimTypeBox(int id)
        {
            if (AnimTypeDrop.show)
            {
                GUILayout.BeginArea(new Rect(3, 3, AnimTypeDrop.m_dropRect.width - 3, AnimTypeDrop.m_dropRect.height - 3));

                AnimTypeDrop.scroll = GUILayout.BeginScrollView(AnimTypeDrop.scroll);
                
                foreach (string name in Enum.GetNames(typeof(Character.SpellCastType)))
                {
                    if (GUILayout.Button(name))
                    {
                        AnimTypeDrop.show = false;
                        AnimTypeDrop.m_dropRect = Rect.zero;
                        AnimTypeDrop.selected = (int)Enum.Parse(typeof(Character.SpellCastType), name);
                    }
                }

                GUILayout.EndScrollView();

                GUILayout.EndArea();
            }
        }

        private void DamageListEdits(int offset)
        {
            GUI.skin.label.alignment = TextAnchor.UpperRight;
            GUILayout.BeginHorizontal();

            for (int i = 0; i < 6; i++)
            {
                if (i == 3)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
                GUILayout.Label(global.statHlpr.DamageNames[i], GUILayout.Width(115));
                editFields[offset + i] = TextAreaEdit(offset + i, false); GUI.color = Color.white;
            }

            GUILayout.EndHorizontal();
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
        }

        private void Invalid()
        {
            GUI.skin.label.fontSize = 16;
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUI.color = lightRed;
            GUILayout.Label("!", GUILayout.Width(10));
            GUI.color = Color.white;
            GUI.skin.label.fontStyle = FontStyle.Normal;
            GUI.skin.label.fontSize = 13;
        }
    }

    public class DropDown
    {
        public Rect m_dropRect = Rect.zero;
        public Vector2 scroll = Vector2.zero;
        public bool show = false;
        public int selected = 0;
    }
}
