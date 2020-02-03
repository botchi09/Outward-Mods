using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using SinAPI;

namespace CombatHUD
{
    public class GUIManager : MonoBehaviour
    {
        public CombatHudGlobal global;

        public bool showMenu = true;

        private Vector2 m_virtualSize = new Vector2(1920, 1080);
        private Vector2 m_currentSize = Vector2.zero;
        public Matrix4x4 m_scaledMatrix;

        public Rect m_window = Rect.zero;

        public Vector2 scroll = Vector2.zero;

        internal void Update()
        {
            float w = Screen.width;
            float h = Screen.height;

            if (m_currentSize.x != w || m_currentSize.y != h)
            {
                m_scaledMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(w / m_virtualSize.x, h / m_virtualSize.y, 1));
                m_currentSize = new Vector2(w, h);
            }
        }

        internal void OnGUI()
        {
            ResetGUI();

            Matrix4x4 origMatrix = GUI.matrix;
            
            if (!global.settings.disableScaling)
            {
                GUI.matrix = m_scaledMatrix;
            }

            // MENU
            if (showMenu)
            {
                if (m_window == Rect.zero)
                {
                    m_window = new Rect(5, 5, 350, 480);
                }
                else
                {
                    m_window = GUI.Window(941374, m_window, DrawMenu, "Combat HUD " + global._base.version);
                }
            }

            GUI.matrix = origMatrix;

            // COMBAT HUD STUFF
            DrawCombatHUD();

            ResetGUI();
        }

        // ===================== DRAW HUD FUNCTIONS ========================

        public void DrawCombatHUD()
        {
            if (NetworkLevelLoader.Instance.IsGameplayPaused) { return; }

            foreach (PlayerInfo c in global.LocalPlayers)
            {
                try
                {
                    StatusInfo statusInfo = global.StatusMgr.PlayerInfos.Find(x => x.character.UID == c.character.UID);
                    TargetInfo targetInfo = global.TargetMgr.PlayerLockInfos.Find(x => x.playerInfo.character.UID == c.character.UID);

                    // PLAYER VITALS
                    ResetGUI();
                    DrawVitals(statusInfo);

                    // DAMAGE LABELS
                    if ((global.settings.Show_Player_DamageLabels || global.settings.Show_Enemy_DamageLabels))
                    {
                        ResetGUI();
                        DrawDamageLabels(c, c.camera);
                    }

                    // STATUS TIMERS
                    if (global.settings.Show_Player_StatusTimers)
                    {
                        ResetGUI();
                        DrawStatusTimers(statusInfo);
                    }

                    // TARGETED ENEMY INFO
                    if (global.settings.Show_TargetEnemy_Health || global.settings.Show_Enemy_Status || global.settings.Show_Target_Detailed)
                    {
                        ResetGUI();
                        DrawTargetInfo(targetInfo);
                    }
                }
                catch //(Exception ex)
                {
                    // OLogger.Log("[CombatHUD::OnGUI] " + ex.Message + "\r\n" + ex.StackTrace);
                }
            }
        }

        public void DrawVitals(StatusInfo player)
        {
            CharacterBarListener manager = player.barManager;

            if (manager == null) { return; } // OLogger.Error("BarManager is null"); return; }

            if (At.GetValue(typeof(CharacterBarListener), manager, "m_healthBar") is Bar healthBar
                && At.GetValue(typeof(Bar), healthBar, "m_lblValue") is Text healthText
                && At.GetValue(typeof(CharacterBarListener), manager, "m_manaBar") is Bar manaBar
                && At.GetValue(typeof(Bar), manaBar, "m_lblValue") is Text manaText
                && At.GetValue(typeof(CharacterBarListener), manager, "m_staminaBar") is Bar stamBar
                && At.GetValue(typeof(Bar), stamBar, "m_lblValue") is Text stamText)
            {
                healthText.fontSize = 13;
                manaText.fontSize = 13;
                stamText.fontSize = 13;

                if (global.settings.Show_Player_Vitals)
                {
                    healthBar.TextValueDisplayed = true;
                    manaBar.TextValueDisplayed = true;
                    stamBar.TextValueDisplayed = true;
                }
                else
                {
                    healthBar.TextValueDisplayed = false;
                    manaBar.TextValueDisplayed = false;
                    stamBar.TextValueDisplayed = false;
                }
            }
        }

        public void DrawDamageLabels(PlayerInfo player, Camera camera)
        {
            foreach (DamageLabel label in global.DamageMgr.ActiveLabels)
            {
                if (label == null || player == null || camera == null || (player.character.CharacterUI.IsMenuFocused && !global.GUIManager.showMenu)) { continue; }

                Vector3 lockpos = Vector3.zero;

                if (!global.settings.labelsStayAtHitPos) { lockpos = label.target.LockingPoint.transform.position; }
                else { lockpos = label.creationPos; }

                if (Vector3.Distance(lockpos, player.character.transform.position) > global.settings.maxDistance) { continue; }

                float damageStrength = (float)((decimal)label.damage.TotalDamage / (decimal)global.settings.damageStrength); // set damage "strength"
                lockpos.y += Mathf.Lerp(global.settings.labelMaxSpeed, global.settings.labelMinSpeed, damageStrength) * (Time.time - label.creationTime); // vertical time offset

                lockpos = camera.WorldToScreenPoint(lockpos);

                // y axis screen-space fix
                lockpos.y = Screen.height - lockpos.y;

                // check if visible
                if (lockpos.z <= 0 || lockpos.x < 0 || lockpos.x > Screen.width) { continue; }
                if (global.LocalPlayers.Count > 1)
                {
                    if (player.ID == 0)
                    {
                        if (lockpos.y > (Screen.height / 2) || lockpos.y < 0) { continue; }
                    }
                    else if (player.ID == 1)
                    {
                        if (lockpos.y > Screen.height || lockpos.y < (Screen.height / 2)) { continue; }
                    }
                }
                else 
                {
                    if (lockpos.y > Screen.height || lockpos.y < 0) { continue; }
                }


                Color color = Color.white;
                float f = 0;
                if (!global.settings.disableColors)
                {
                    foreach (DamageType type in label.damage.List)
                    {
                        if (type.Damage >= f) // if this damage type is greater than our current highest
                        {
                            f = type.Damage;
                            color = GetDmgColor(type.Type); // get our color for that damage type
                        }
                    }
                }

                // create label area, and add the random x/y position for this label
                float w = 100 * (float)((decimal)Screen.width / 1920);
                float h = 55 * (float)((decimal)Screen.height / 1080);
                float xOffset = 40 * (float)((decimal)Screen.width / 1920);

                GUI.BeginGroup(new Rect(lockpos.x + label.ranX - xOffset, lockpos.y + label.ranY, w, h));

                GUI.skin.label.fontStyle = FontStyle.Bold;
                GUI.skin.label.fontSize = Mathf.RoundToInt(Mathf.Lerp(global.settings.labelMinSize, global.settings.labelMaxSize, damageStrength));

                FloatingText(label.creationTime, label.damage.TotalDamage.ToString("0.0"), color, damageStrength);

                GUI.EndGroup();
            }
        }

        public void DrawStatusTimers(StatusInfo player)
        {
            if (player.character.CharacterUI.IsMenuFocused && !global.GUIManager.showMenu) { return; }
            try
            {
                StatusEffectPanel statusPanel = player.fxPanel;
                StatusEffectManager fxManager = player.fxManager;

                if (!statusPanel || !fxManager)
                {
                    return;
                }
                var _base = statusPanel as UIElement;

                bool anyStatus = true;

                if (fxManager.Statuses.Count() < 1)
                {
                    anyStatus = false;

                    if (_base.LocalCharacter.CurrentWeapon != null)
                    {
                        anyStatus = _base.LocalCharacter.CurrentWeapon.FirstImbue != null || _base.LocalCharacter.CurrentWeapon.SummonedEquipment != null;
                    }
                    if (!anyStatus && _base.LocalCharacter.LeftHandWeapon != null)
                    {
                        anyStatus = _base.LocalCharacter.LeftHandWeapon.FirstImbue != null;
                    }
                    if (!anyStatus && _base.LocalCharacter.CurrentSummon != null)
                    {
                        anyStatus = true;
                    }
                };

                if (!anyStatus)
                {
                    return;
                }

                // get list of cached icons, gives us position of each displayed icon rectTransform
                var activeIcons = new Dictionary<string, StatusEffectIcon>();
                activeIcons = At.GetValue(typeof(StatusEffectPanel), statusPanel, "m_statusIcons") as Dictionary<string, StatusEffectIcon>;

                foreach (KeyValuePair<string, StatusEffectIcon> entry in activeIcons.Where(z => z.Value.IsDisplayed))
                {
                    float time = 0f;

                    StatusEffect status = fxManager.Statuses.Find(s => s.IdentifierName == entry.Key);

                    // some statuses use a identifier tag instead of their own status name for the icon...
                    if (!status)
                    {
                        switch (entry.Key.ToLower())
                        {
                            case "imbuemainweapon":
                                time = _base.LocalCharacter.CurrentWeapon.FirstImbue.RemainingLifespan;
                                break;
                            case "imbueoffweapon":
                                time = _base.LocalCharacter.LeftHandWeapon.FirstImbue.RemainingLifespan;
                                break;
                            case "summonweapon":
                                time = _base.LocalCharacter.CurrentWeapon.SummonedEquipment.RemainingLifespan;
                                break;
                            case "summonghost":
                                time = _base.LocalCharacter.CurrentSummon.RemainingLifespan;
                                break;
                            case "129": // marsh poison uses "129" for its tag, I think that's its effect preset ID?
                                if (fxManager.Statuses.Find(z => z.IdentifierName.Equals("Hallowed Marsh Poison Lvl1")) is StatusEffect marshpoison)
                                    time = marshpoison.RemainingLifespan;
                                break;
                            default:
                                //OLogger.Log(entry.Key);
                                continue;
                        }
                    }
                    else // else we can just get the status lifespan from the name, most statuses work this way
                    {
                        time = status.RemainingLifespan;
                    }

                    if (time >= 1)
                    {   // pass null as the camera to get position of a UI element
                        Vector2 pos = RectTransformUtility.WorldToScreenPoint(null, entry.Value.RectTransform.position);

                        string timeString = GetTimeString(time);
                        GUI.skin.label.fontStyle = FontStyle.Bold;

                        float x = pos.x - 25;
                        float y = Screen.height - pos.y - 50;
                        ShadowText(x, y, timeString, time, false, global.settings.StatusTimerX, global.settings.StatusTimerY, global.settings.StatusTimerScale);
                        GUI.skin.label.fontStyle = FontStyle.Normal;
                    }
                }
            }
            catch //(Exception ex)
            {
                //OLogger.Log("DrawStatusTimers error: " + ex.Message);
            }
        }

        public void DrawTargetInfo(TargetInfo player)
        {
            try
            {
                if (player != null && player.lockedCharacter != null)
                {
                    if (!player.playerInfo.character.CharacterUI.IsMenuFocused || global.GUIManager.showMenu)
                    {
                        if (global.settings.Show_TargetEnemy_Health)
                        {
                            DrawEnemyHealth(player);
                        }

                        if (global.settings.Show_Enemy_Status)
                        {
                            DrawEnemyStatus(player);
                        }
                    }

                    if (global.settings.Show_Target_Detailed)
                    {
                        DrawTargetDetailed(player);
                    }
                }                
            }
            catch //(Exception ex)
            {
                //OLogger.Error("CombatHUD Draw Target Info: " + ex.Message + " | " + ex.StackTrace);
            }
        }

        public void DrawEnemyHealth(TargetInfo player)
        {
            if (player.lockedCharacter is Character lockedCharacter && lockedCharacter.Health > 0)
            {
                //GUI.skin.label.fontSize = Mathf.RoundToInt(13 * global.settings.ManualScaleFix);
                GUI.skin.label.fontStyle = FontStyle.Bold;
                float x = player.UIBarPos.x - 26 + global.settings.StatusTimerY;
                float y = Screen.height - player.UIBarPos.y - 35 - global.settings.StatusTimerX;
                ShadowText(x, y, lockedCharacter.Stats.CurrentHealth.ToString("0"), lockedCharacter.Stats.CurrentHealth, false, global.settings.EnemyHealthX, global.settings.EnemyHealthY, global.settings.EnemyHealthScale);
            }
        }

        public void DrawEnemyStatus(TargetInfo player)
        {
            List<string> labels = new List<string>();

            Camera cam = player.playerInfo.camera;
            Character c = player.lockedCharacter;
            Vector3 pos;

            // get world-to-screen position of enemy (+ offsets)
            Vector3 offset = new Vector3(0, 1.3f + c.CenterHeight, 0);
            offset += c.transform.position;
            pos = cam.WorldToScreenPoint(offset);

            if (player.lockedCharacter.StatusEffectMngr.Statuses.Count > 0)
            {
                foreach (StatusEffect effect in c.StatusEffectMngr.Statuses)
                {
                    if ((effect.StatusIcon != null || effect.OverrideIcon != null) && effect.IdentifierName != null)
                    {
                        labels.Add(effect.IdentifierName);
                        float x = pos.x + 50;
                        float y = Screen.height - pos.y;

                        GUI.skin.label.alignment = TextAnchor.UpperRight;

                        GUI.Label(
                            new Rect(
                                x + global.settings.EnemyStatusX, 
                                y - global.settings.EnemyStatusY,
                                35 * global.settings.EnemyStatusIconScale,
                                35 * global.settings.EnemyStatusIconScale), 
                            effect.StatusIcon == null ? effect.OverrideIcon.texture : effect.StatusIcon.texture
                        );

                        if (global.settings.Show_Enemy_Status_Lifespan)
                        {
                            x += 35 * global.settings.EnemyStatusIconScale;
                            float t = effect.RemainingLifespan;
                            string s = GetTimeString(t);
                            ShadowText(x, y, s, t, true, global.settings.EnemyStatusX, global.settings.EnemyStatusY, global.settings.EnemyStatusTextScale);
                        }

                        pos.y -= 28 * global.settings.EnemyStatusIconScale;
                    }
                }
            }            

            if (global.settings.Show_BuildUps)
            {
                try
                {
                    var m_statusBuildup = At.GetValue(typeof(StatusEffectManager), c.StatusEffectMngr, "m_statusBuildUp");
                    IDictionary dict = m_statusBuildup as IDictionary;
                    FieldInfo buildupField = m_statusBuildup.GetType().GetGenericArguments()[1].GetField("BuildUp");

                    foreach (string name in dict.Keys)
                    {
                        object value = buildupField.GetValue(dict[name]);
                        if (value != null
                            && ResourcesPrefabManager.Instance.GetStatusEffectPrefab(name) is StatusEffect effect
                            && !labels.Contains(effect.IdentifierName))
                        {
                            float.TryParse(value.ToString(), out float buildup);

                            if (buildup <= 0 || buildup >= 100)
                                continue;

                            float x = pos.x + 50;
                            float y = Screen.height - pos.y;

                            GUI.skin.label.alignment = TextAnchor.LowerRight;

                            float alpha = Mathf.Lerp(0, 1, Convert.ToSingle((decimal)buildup / 100));
                            GUI.color = new Color(1, 1, 1, alpha);

                            GUI.Label(
                                new Rect(
                                    x + global.settings.EnemyStatusX,
                                    y - global.settings.EnemyStatusY,
                                    35 * global.settings.EnemyStatusIconScale,
                                    35 * global.settings.EnemyStatusIconScale),
                                effect.StatusIcon == null ? effect.OverrideIcon.texture : effect.StatusIcon.texture
                            );

                            x += 35 * global.settings.EnemyStatusIconScale;
                            ShadowText(x, y, Math.Round(buildup, 0) + "%", effect.RemainingLifespan, false, global.settings.EnemyStatusX, global.settings.EnemyStatusY, global.settings.EnemyStatusTextScale);

                            pos.y -= 28 * global.settings.EnemyStatusIconScale;

                            GUI.color = Color.white;
                        }
                    }
                }
                catch // (Exception ex)
                {
                    //OLogger.Log("[Combat HUD] Enemy Buildups: " + ex.Message);
                }
            }
        }

        // ===== DETAILED TARGET INFO GUI ======
        public Texture2D DetailedGUITex;
        public Texture2D HealthIcon;
        public Texture2D ImpactIcon;
        public static Texture2D LoadPNG(string filePath)
        {
            Texture2D tex = null;
            byte[] fileData;

            if (File.Exists(filePath))
            {
                fileData = File.ReadAllBytes(filePath);
                tex = new Texture2D(2, 2);
                tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
            }
            return tex;
        }

        public void DrawTargetDetailed(TargetInfo player)
        {
            ResetGUI();

            Matrix4x4 origMatrix = GUI.matrix;

            if (!global.settings.disableScaling)
            {
                GUI.matrix = m_scaledMatrix;
            }

            float w = 240;
            float h = 132;

            float guiX = 5;
            float guiY = 5;
            if (player.playerInfo.ID == 1)
                guiY += Screen.height / 2;

            float iconWidth = 20;
            float iconHeight = 20;
            float labelWidth = 30;

            bool flag = false;
            if (DetailedGUITex == null)
            {
                try
                {
                    DetailedGUITex = LoadPNG(@"Mods\CombatHUD\enemy_bg.png");
                    HealthIcon = LoadPNG(@"Mods\CombatHUD\Health.png");
                    ImpactIcon = LoadPNG(@"Mods\CombatHUD\Impact.png");
                }
                catch { flag = true; }
            }
            if (flag || DetailedGUITex == null) { Debug.LogError(@"[CombatHUD] OnGUI: Textures and Icons not loaded! Ensure the .png files exist at Mods\CombatHUD\"); return; }

            if (player.playerInfo.ID == 0)
            {
                guiX += global.settings.Player1_Detailed_Pos.x;
                guiY += global.settings.Player1_Detailed_Pos.y;
            }
            else if (player.playerInfo.ID == 1)
            {
                guiX += global.settings.Player2_Detailed_Pos.x;
                guiY += global.settings.Player2_Detailed_Pos.y;
            }

            GUI.BeginGroup(new Rect(guiX, guiY, w, h), DetailedGUITex);
            RectOffset padding = new RectOffset(10, 0, 3, 0);
            GUILayout.BeginArea(new Rect(padding.left, padding.top, w - padding.right, h - padding.bottom));

            // NAME
            GUILayout.Label(player.lockedCharacter.Name);

            // --- row one ---
            GUILayout.BeginHorizontal();

            // HEALTH
            GUI.color = Color.white;
            GUI.skin.label.alignment = TextAnchor.UpperRight;
            GUILayout.Label(HealthIcon, new GUILayoutOption[] { GUILayout.Width(iconWidth), GUILayout.Height(iconHeight) });
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
            GUILayout.Label(Math.Round(player.lockedCharacter.Health) + " / " + Math.Round(player.lockedCharacter.ActiveMaxHealth), GUILayout.Width(75));

            // PROTECTION
            if (player.lockedCharacter.Stats.DamageProtection[0] != 0)
            {
                Texture2D tex = UIUtilities.PhyisicalProtectionIcon.texture;
                string value = player.lockedCharacter.Stats.DamageProtection[0].ToString();
                DisplayDamageStat(tex, value, DamageType.Types.Physical, iconWidth, iconHeight, labelWidth);
            }

            // IMPACT RES
            DisplayDamageStat(ImpactIcon, Math.Round(player.lockedCharacter.Stats.GetImpactResistance()).ToString(), DamageType.Types.Raw, iconWidth, iconHeight, labelWidth);

            GUILayout.EndHorizontal();
            // --- row 2 & 3 ---
            GUILayout.BeginHorizontal();
            // extra padding
            GUILayout.Space(20);

            // DAMAGE RESISTANCES
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            if (At.GetValue(typeof(CharacterStats), player.lockedCharacter.Stats, "m_totalDamageResistance") is float[] damageRes)
            {
                for (int i = 0; i < 6; i++)
                {
                    int res = (int)Mathf.Round(damageRes[i] * 100);
                    if (i == 3) { GUILayout.EndHorizontal(); GUILayout.BeginHorizontal(); }
                    DisplayDamageStat(null, res.ToString(), (DamageType.Types)i, iconWidth, iconHeight, labelWidth);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            // --- row 4+ ---

            // STATUS IMMUNITY DISPLAY
            ResetGUI();

            List<string> Status_Immunities = new List<string>();
            foreach (TagSourceSelector tagSelector in At.GetValue(typeof(CharacterStats), player.lockedCharacter.Stats, "m_statusEffectsNaturalImmunity") as TagSourceSelector[])
                Status_Immunities.Add(tagSelector.Tag.TagName);
            foreach (KeyValuePair<Tag, List<string>> entry in At.GetValue(typeof(CharacterStats), player.lockedCharacter.Stats, "m_statusEffectsImmunity") as Dictionary<Tag, List<string>>)
            {
                if (entry.Value.Count > 0)
                    Status_Immunities.Add(entry.Key.TagName);
            }
            string immune = " None";
            for (int j = 0; j < Status_Immunities.Count(); j++)
            {
                string s = Status_Immunities[j];
                if (j == 0) { immune = ""; }
                immune += s;
                if (j < Status_Immunities.Count - 1) { immune += ", "; }
            }
            GUILayout.Label("Immune To: " + immune);

            GUILayout.EndArea();
            GUI.EndGroup();

            GUI.matrix = origMatrix;
        }

        public void DisplayDamageStat(Texture2D icon, string value, DamageType.Types type, float iconWidth, float iconHeight, float labelWidth)
        {
            DamageType dmgValue = new DamageType { Type = type, Damage = float.Parse(value) };

            if (icon == null) { icon = dmgValue.TypeIcon.texture; }

            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUI.color = Color.white;

            GUI.skin.label.alignment = TextAnchor.UpperRight;
            GUILayout.Label(icon, new GUILayoutOption[] { GUILayout.Width(iconWidth), GUILayout.Height(iconHeight) });
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
            GUILayout.Label(value, GUILayout.Width(labelWidth));
        }

        // =============================== MENU ================================

        public int GuiPage = 0;

        public void DrawMenu(int id)
        {
            ResetGUI();

            GUI.DragWindow(new Rect(0, 0, m_window.width - 50, 20));

            if (GUI.Button(new Rect(m_window.width - 50, 3, 45, 18), "X"))
            {
                showMenu = false;
            }

            float w = m_window.width - 10;
            float h = m_window.height - 25;
            Rect subRect = new Rect(5, 23, w, h);

            GUILayout.BeginArea(subRect, GUI.skin.box);
            GUILayout.BeginVertical(new GUIStyle() { padding = new RectOffset(3, 0, 10, 3) });

            GUILayout.BeginHorizontal();
            if (GuiPage == 0) { GUI.color = Color.green; }
            if (GUILayout.Button("Player / General"))
            {
                GuiPage = 0;
            }
            GUI.color = Color.white;
            if (GuiPage == 1) { GUI.color = Color.green; }
            if (GUILayout.Button("Enemies"))
            {
                GuiPage = 1;
            }
            GUI.color = Color.white;
            if (GuiPage == 2) { GUI.color = Color.green; }
            if (GUILayout.Button("Damage Labels"))
            {
                GuiPage = 2;
            }
            GUI.color = Color.white;
            //if (GUILayout.Button("DPS Calc"))
            //{
            //    global.DPSCounter.showDPSgui = true;
            //}
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            scroll = GUILayout.BeginScrollView(scroll);

            if (GuiPage == 0)
            {
                GUI.color = Color.green;
                GUILayout.Label("Player Settings");
                GUI.color = Color.white;

                global.settings.Show_Player_StatusTimers = GUILayout.Toggle(global.settings.Show_Player_StatusTimers, "Show Status Timers");
                global.settings.Show_Player_DamageLabels = GUILayout.Toggle(global.settings.Show_Player_DamageLabels, "Show Player Damage Received");
                global.settings.Show_Player_Vitals = GUILayout.Toggle(global.settings.Show_Player_Vitals, "Show Numerical Vitals (HP/Stam/Mana)");

                GUILayout.Space(10);

                GUI.color = Color.green;
                GUILayout.Label("Player Status Timers Scale / Position");
                GUI.color = Color.white;
                GUILayout.BeginHorizontal();
                GUILayout.Label("Text Scale: " + global.settings.StatusTimerScale.ToString("0.0"), GUILayout.Width(150));
                global.settings.StatusTimerScale = (float)Math.Round(GUILayout.HorizontalSlider(global.settings.StatusTimerScale, 0.2f, 3.0f), 1);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("X Offset: " + global.settings.StatusTimerX, GUILayout.Width(150));
                global.settings.StatusTimerX = (float)Math.Round(GUILayout.HorizontalSlider(global.settings.StatusTimerX, -100, 100), 0);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Y Offset: " + global.settings.StatusTimerY, GUILayout.Width(150));
                global.settings.StatusTimerY = (float)Math.Round(GUILayout.HorizontalSlider(global.settings.StatusTimerY, -100, 100), 0);
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                GUI.color = lightGrey;
                GUILayout.Space(20);
                global.settings.Show_On_Load = GUILayout.Toggle(global.settings.Show_On_Load, "Show this menu on startup");
                ResetGUI();
                GUI.skin.label.fontSize = 12;
                GUILayout.Label("You can also set a keybinding in your in-game bindings to toggle this menu.");

                global.settings.disableScaling = GUILayout.Toggle(global.settings.disableScaling, "Disable Menu Scaling");
            }
            if (GuiPage == 1)
            {
                GUI.color = Color.green;
                GUILayout.Label("Damage Labels");
                GUI.color = Color.white;
                global.settings.Show_Enemy_DamageLabels = GUILayout.Toggle(global.settings.Show_Enemy_DamageLabels, "Show Enemy Damage Received");

                GUILayout.Space(15);
                GUI.color = Color.green;
                GUILayout.Label("Target Settings");
                GUI.color = Color.white;
                global.settings.Show_TargetEnemy_Health = GUILayout.Toggle(global.settings.Show_TargetEnemy_Health, "Show Health Value (on Health Bar)");
                global.settings.Show_Enemy_Status = GUILayout.Toggle(global.settings.Show_Enemy_Status, "Show Status Effects");

                if (!global.settings.Show_Enemy_Status)
                    GUI.color = lightGrey;

                GUILayout.BeginHorizontal();
                GUILayout.Space(15);
                global.settings.Show_Enemy_Status_Lifespan = GUILayout.Toggle(global.settings.Show_Enemy_Status_Lifespan, "Show Lifespans");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(15);
                global.settings.Show_BuildUps = GUILayout.Toggle(global.settings.Show_BuildUps, "Show Build-Ups");
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                GUI.color = Color.green;
                GUILayout.Label("Detailed Target Info-Boxes");
                GUI.color = Color.white;
                global.settings.Show_Target_Detailed = GUILayout.Toggle(global.settings.Show_Target_Detailed, "Show Detailed Stats Info-Boxes");

                GUILayout.Label("Player 1 Position Offsets:");
                GUILayout.BeginHorizontal();
                GUILayout.Label("X:", GUILayout.Width(30));
                global.settings.Player1_Detailed_Pos.x = GUILayout.HorizontalSlider(global.settings.Player1_Detailed_Pos.x, 0, Screen.width);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Y:", GUILayout.Width(30));
                global.settings.Player1_Detailed_Pos.y = GUILayout.HorizontalSlider(global.settings.Player1_Detailed_Pos.y, 0, Screen.height);
                GUILayout.EndHorizontal();

                GUILayout.Label("Player 2 Position Offsets:");
                GUILayout.BeginHorizontal();
                GUILayout.Label("X:", GUILayout.Width(30));
                global.settings.Player2_Detailed_Pos.x = GUILayout.HorizontalSlider(global.settings.Player2_Detailed_Pos.x, 0, Screen.width);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Y:", GUILayout.Width(30));
                global.settings.Player2_Detailed_Pos.y = GUILayout.HorizontalSlider(global.settings.Player2_Detailed_Pos.y, 0, Screen.height);
                GUILayout.EndHorizontal();

                GUI.color = Color.green;
                GUILayout.Label("Enemy Health Text Scale / Position");
                GUI.color = Color.white;
                GUILayout.BeginHorizontal();
                GUILayout.Label("Text Scale: " + global.settings.EnemyHealthScale.ToString("0.0"), GUILayout.Width(150));
                global.settings.EnemyHealthScale = (float)Math.Round(GUILayout.HorizontalSlider(global.settings.EnemyHealthScale, 0.2f, 3.0f), 1);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("X Offset: " + global.settings.EnemyHealthX, GUILayout.Width(150));
                global.settings.EnemyHealthX = (float)Math.Round(GUILayout.HorizontalSlider(global.settings.EnemyHealthX, -100, 100), 0);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Y Offset: " + global.settings.EnemyHealthY, GUILayout.Width(150));
                global.settings.EnemyHealthY = (float)Math.Round(GUILayout.HorizontalSlider(global.settings.EnemyHealthY, -100, 100), 0);
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                GUI.color = Color.green;
                GUILayout.Label("Enemy Status Icons Scale / Position");

                GUI.color = Color.white;
                GUILayout.BeginHorizontal();
                GUILayout.Label("Icon Scale: " + global.settings.EnemyStatusIconScale.ToString("0.0"), GUILayout.Width(150));
                global.settings.EnemyStatusIconScale = (float)Math.Round(GUILayout.HorizontalSlider(global.settings.EnemyStatusIconScale, 0.2f, 3.0f), 1);
                GUILayout.EndHorizontal();

                GUI.color = Color.white;
                GUILayout.BeginHorizontal();
                GUILayout.Label("Text Scale: " + global.settings.EnemyStatusTextScale.ToString("0.0"), GUILayout.Width(150));
                global.settings.EnemyStatusTextScale = (float)Math.Round(GUILayout.HorizontalSlider(global.settings.EnemyStatusTextScale, 0.2f, 3.0f), 1);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("X Offset: " + global.settings.EnemyStatusX, GUILayout.Width(150));
                global.settings.EnemyStatusX = (float)Math.Round(GUILayout.HorizontalSlider(global.settings.EnemyStatusX, -100, 100), 0);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Y Offset: " + global.settings.EnemyStatusY, GUILayout.Width(150));
                global.settings.EnemyStatusY = (float)Math.Round(GUILayout.HorizontalSlider(global.settings.EnemyStatusY, -100, 100), 0);
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                GUI.color = Color.white;
            }
            else if (GuiPage == 2)
            {
                GUI.skin.label.alignment = TextAnchor.UpperRight;
                global.settings.labelsStayAtHitPos = GUILayout.Toggle(global.settings.labelsStayAtHitPos, "Labels stay at hit position");
                global.settings.disableColors = GUILayout.Toggle(global.settings.disableColors, "Disable Damage Colors");

                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Minimum Damage: " + global.settings.minDamage, GUILayout.Width(150));
                global.settings.minDamage = Mathf.Round(GUILayout.HorizontalSlider(global.settings.minDamage, 0, global.settings.damageStrength));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Damage Ceiling: " + global.settings.damageStrength, GUILayout.Width(150));
                global.settings.damageStrength = Mathf.Round(GUILayout.HorizontalSlider(global.settings.damageStrength, global.settings.minDamage, 500));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Min Transparency: " + (global.settings.MinTransparency * 100) + "%", GUILayout.Width(150));
                global.settings.MinTransparency = (float)Math.Round(GUILayout.HorizontalSlider(global.settings.MinTransparency, 0, 1), 2);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Max Distance: " + global.settings.maxDistance + "m", GUILayout.Width(150));
                global.settings.maxDistance = (float)Math.Round(GUILayout.HorizontalSlider(global.settings.maxDistance, 5, 500), 0);
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                if (global.settings.labelMinSpeed < 0.029 || global.settings.labelMinSpeed > global.settings.labelMaxSpeed)
                {
                    global.settings.labelMinSpeed = 0.03f;
                }
                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                GUI.color = Color.green;
                GUILayout.Label("Label Float-Up Speed:");
                GUI.skin.label.alignment = TextAnchor.UpperRight;
                GUI.color = Color.white;
                GUILayout.BeginHorizontal();
                GUILayout.Label("Min:" + global.settings.labelMinSpeed, GUILayout.Width(70));
                global.settings.labelMinSpeed = (float)Math.Round(GUILayout.HorizontalSlider(global.settings.labelMinSpeed, 0.03f, global.settings.labelMaxSpeed), 2);
                if (global.settings.labelMaxSpeed < global.settings.labelMinSpeed)
                {
                    global.settings.labelMaxSpeed = 0.3f;
                }
                GUILayout.Label("Max:" + global.settings.labelMaxSpeed, GUILayout.Width(70));
                global.settings.labelMaxSpeed = (float)Math.Round(GUILayout.HorizontalSlider(global.settings.labelMaxSpeed, global.settings.labelMinSpeed, 3f), 2);
                GUILayout.EndHorizontal();

                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                GUI.color = Color.green;
                GUILayout.Label("Font Size:");
                GUI.skin.label.alignment = TextAnchor.UpperRight;
                GUI.color = Color.white;
                GUILayout.BeginHorizontal();
                GUILayout.Label("Min: " + global.settings.labelMinSize, GUILayout.Width(70));
                global.settings.labelMinSize = Mathf.Round(GUILayout.HorizontalSlider(global.settings.labelMinSize, 6, global.settings.labelMaxSize));
                GUILayout.Label("Max: " + global.settings.labelMaxSize, GUILayout.Width(70));
                global.settings.labelMaxSize = Mathf.Round(GUILayout.HorizontalSlider(global.settings.labelMaxSize, global.settings.labelMinSize, 26));
                GUILayout.EndHorizontal();

                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                GUI.color = Color.green;
                GUILayout.Label("Lifespan:");
                GUI.skin.label.alignment = TextAnchor.UpperRight;
                GUI.color = Color.white;
                GUILayout.BeginHorizontal();
                GUILayout.Label("Min: " + global.settings.labelMinTime, GUILayout.Width(70));
                global.settings.labelMinTime = (float)Math.Round(GUILayout.HorizontalSlider(global.settings.labelMinTime, 0.2f, global.settings.labelMaxTime), 2);
                GUILayout.Label("Max: " + global.settings.labelMaxTime, GUILayout.Width(70));
                global.settings.labelMaxTime = (float)Math.Round(GUILayout.HorizontalSlider(global.settings.labelMaxTime, global.settings.labelMinTime, 5.0f), 2);
                GUILayout.EndHorizontal();

                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                GUI.color = Color.green;
                GUILayout.Label("Random Position:");
                GUI.skin.label.alignment = TextAnchor.UpperRight;
                GUI.color = Color.white;
                GUILayout.BeginHorizontal();
                GUILayout.Label("X: " + global.settings.labelRandomX, GUILayout.Width(70));
                global.settings.labelRandomX = (float)Math.Round(GUILayout.HorizontalSlider(global.settings.labelRandomX, 0, 100), 1);
                GUILayout.Label("Y: " + global.settings.labelRandomY, GUILayout.Width(70));
                global.settings.labelRandomY = (float)Math.Round(GUILayout.HorizontalSlider(global.settings.labelRandomY, 0, 100), 1);
                GUILayout.EndHorizontal();
                GUI.skin.label.alignment = TextAnchor.UpperLeft;
            }
            else
            {
                GuiPage = 0;
            }

            GUILayout.EndScrollView();

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        // ========== GUI HELPERS ===========

        // floating damage text
        private void FloatingText(float creationTime, string label, Color color, float damageStrength)
        {
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;

            float width = 80;
            float height = 40;
            float fadeRate = Mathf.Lerp(0.7f, 0.3f, damageStrength);
            Vector2 fadelimit = new Vector2(1.0f, global.settings.MinTransparency);

            // set color to black (alpha lerped)
            GUI.color = new Color()
            {
                r = 0,
                b = 0,
                g = 0,
                a = Mathf.Lerp(fadelimit.x, fadelimit.y, fadeRate * (Time.time - creationTime))
            };
            GUI.Label(new Rect(2, 2, width, height), label);

            // set color to damage color (alpha lerped, brightened)
            GUI.color = new Color()
            {
                r = 0.7f + color.r,
                b = 0.7f + color.b,
                g = 0.7f + color.g,
                a = Mathf.Lerp(fadelimit.x, fadelimit.y, fadeRate * (Time.time - creationTime))
            };

            GUI.Label(new Rect(0, 0, width, height), label);
        }

        public void ShadowText(float x, float y, string text, float lifespan, bool leftAlign, float xOffset, float yOffset, float scale)
        {
            if (leftAlign)
                GUI.skin.label.alignment = TextAnchor.MiddleLeft;
            else
                GUI.skin.label.alignment = TextAnchor.MiddleCenter;

            float w = 60 * scale;
            float h = 26 * scale;
            x += xOffset;
            y -= yOffset;

            GUI.skin.label.fontSize = Mathf.RoundToInt(13 * scale);

            // shadow
            GUI.color = Color.black;
            GUI.Label(new Rect(x + 1, y + 1, w, h), text);

            // red if < 10 secs left
            if (lifespan <= 10)
                GUI.color = lightRed;
            else
                GUI.color = Color.white;

            // main label
            GUI.Label(new Rect(x, y, w, h), text);
        }

        // convert seconds to minutes and seconds
        public string GetTimeString(float timespan)
        {
            TimeSpan t = TimeSpan.FromSeconds(Mathf.RoundToInt(timespan));
            string s = "";
            if (t.Minutes > 0)
                s += t.Minutes + "m ";
            s += t.Seconds + "s";
            return s;
        }

        // get damage color switch
        public Color GetDmgColor(DamageType.Types dtype)
        {
            Color color;
            switch (dtype)
            {
                case DamageType.Types.Physical:
                    color = lightGrey;
                    break;
                case DamageType.Types.Ethereal:
                    color = Color.magenta;
                    break;
                case DamageType.Types.Decay:
                    color = Color.green;
                    break;
                case DamageType.Types.Electric:
                    color = Color.yellow;
                    break;
                case DamageType.Types.Frost:
                    color = lightBlue;
                    break;
                case DamageType.Types.Fire:
                    color = Color.red;
                    break;
                case DamageType.Types.Raw:
                default:
                    color = Color.white;
                    break;
            }
            return color;
        }

        public Color lightBlue = new Color()
        {
            r = 0,
            g = 0.26f,
            b = 1.0f,
            a = 1.0f
        };

        public Color lightGrey = new Color()
        {
            r = 0.7f,
            b = 0.7f,
            g = 0.7f,
            a = 1.0f
        };

        public Color lightRed = new Color
        {
            r = 1.0f,
            g = 0.41f,
            b = 0.41f,
            a = 1.0f
        };

        public Color lightGreen = new Color
        {
            r = 0.41f,
            g = 1.00f,
            b = 0.41f,
            a = 1.00f
        };

        private void ResetGUI()
        {
            GUI.color = Color.white;
            GUI.skin.label.fontSize = 13;
            GUI.skin.label.fontStyle = FontStyle.Normal;
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
            GUI.skin = null; // sometimes this resets the gui skin to default, but not always, idk why
        }
    }

}
