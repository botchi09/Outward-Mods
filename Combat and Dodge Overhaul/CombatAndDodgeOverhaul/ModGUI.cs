using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using SinAPI;

namespace CombatAndDodgeOverhaul
{
    public class ModGUI : MonoBehaviour
    {
        public static ModGUI Instance;

        public Rect m_windowRect = Rect.zero;
        public Vector2 scroll = Vector2.zero;

        private Vector2 m_virtualSize = new Vector2(1920, 1080);
        private Vector2 m_currentSize = Vector2.zero;
        public Matrix4x4 m_scaledMatrix;

        public bool showGui = false;
        public int guiPage = 0;

        internal void Awake()
        {
            Instance = this;
        }

        internal void Update()
        {
            if (m_currentSize.x != Screen.width || m_currentSize.y != Screen.height)
            {
                m_scaledMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / m_virtualSize.x, Screen.height / m_virtualSize.y, 1));
                m_currentSize = new Vector2(Screen.width, Screen.height);
            }

            MenuMouseFix();
        }

        public bool lastMenuToggle;


        internal void OnGUI()
        {
            Matrix4x4 orig = GUI.matrix;

            if (!OverhaulGlobal.settings.Disable_Scaling)
            {
                GUI.matrix = m_scaledMatrix;
            }

            if (m_windowRect == Rect.zero || m_windowRect == null)
            {
                m_windowRect = new Rect(200, 5, 530, 470);
            }
            else
            {
                if (showGui)
                {
                    m_windowRect = GUI.Window(543535, m_windowRect, DrawWindow, "Combat and Dodge Overhaul");
                }
            }
            GUI.matrix = orig;
        }


        private void DrawWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, m_windowRect.width - 50, 20));

            if (GUI.Button(new Rect(m_windowRect.width - 50, 0, 45, 20), "X"))
            {
                showGui = false;
            }

            GUILayout.BeginArea(new Rect(3, 25, m_windowRect.width - 8, m_windowRect.height - 5));

            GUILayout.BeginHorizontal();
            if (guiPage == 0) { GUI.color = Color.green; } else { GUI.color = Color.white; }
            if (GUILayout.Button("Player"))
            {
                guiPage = 0;
            }
            if (guiPage == 1) { GUI.color = Color.green; } else { GUI.color = Color.white; }
            if (GUILayout.Button("Stability"))
            {
                guiPage = 1;
            }
            if (guiPage == 2) { GUI.color = Color.green; } else { GUI.color = Color.white; }
            if (GUILayout.Button("Enemies"))
            {
                guiPage = 2;
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            OverhaulGlobal.settings.Disable_Scaling = GUILayout.Toggle(OverhaulGlobal.settings.Disable_Scaling, "Disable Menu Scaling");
            GUILayout.Space(10);

            scroll = GUILayout.BeginScrollView(scroll, GUI.skin.box);

            switch (guiPage)
            {
                case 0:
                    MainPage();
                    break;
                case 1:
                    StabilityPage(); 
                    break;
                case 2:
                    EnemyPage();
                    break;
                default:
                    break;
            }

            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private void MainPage()
        {
            GUILayout.BeginVertical(GUI.skin.box);

            OverhaulGlobal.settings.Attack_Cancels_Blocking = GUILayout.Toggle(OverhaulGlobal.settings.Attack_Cancels_Blocking, "Attack Cancels Blocking");

            GUILayout.Space(10);
            GUI.skin.label.fontSize = 17;
            GUILayout.Label("Dodging", GUILayout.Height(30));
            GUI.skin.label.fontSize = 12;

            OverhaulGlobal.settings.Dodge_Cancelling = GUILayout.Toggle(OverhaulGlobal.settings.Dodge_Cancelling, "Cancel Animations with Dodge");

            GUILayout.Space(10);
            GUILayout.Label("Bag Dodge Burdens:");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Min Slow from Bag: " + (OverhaulGlobal.settings.min_slow_effect * 100) + "%", GUILayout.Width(250));
            OverhaulGlobal.settings.min_slow_effect = (float)Math.Round((decimal)GUILayout.HorizontalSlider(OverhaulGlobal.settings.min_slow_effect, 0f, 1f), 2);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Max Slow from Bag: " + (OverhaulGlobal.settings.max_slow_effect * 100) + "%", GUILayout.Width(250));
            OverhaulGlobal.settings.max_slow_effect = (float)Math.Round((decimal)GUILayout.HorizontalSlider(OverhaulGlobal.settings.max_slow_effect, 0f, 1f), 2);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Min Bag Weight for Slowed Dodge: " + OverhaulGlobal.settings.min_burden_weight + "%", GUILayout.Width(250));
            OverhaulGlobal.settings.min_burden_weight = (float)Math.Round((decimal)GUILayout.HorizontalSlider(OverhaulGlobal.settings.min_burden_weight, 0f, 125f), 0);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUI.skin.label.fontSize = 17;
            GUILayout.Label("Stamina", GUILayout.Height(30));
            GUI.skin.label.fontSize = 12;

            GUILayout.BeginHorizontal();
            GUILayout.Label("Extra Stamina Regen: " + OverhaulGlobal.settings.Extra_Stamina_Regen, GUILayout.Width(250));
            OverhaulGlobal.settings.Extra_Stamina_Regen = (float)Math.Round((decimal)GUILayout.HorizontalSlider(OverhaulGlobal.settings.Extra_Stamina_Regen, 0, 100f), 2);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Delay for Exta Regen: " + OverhaulGlobal.settings.Stamina_Regen_Delay + " secs", GUILayout.Width(250));
            OverhaulGlobal.settings.Stamina_Regen_Delay = (float)Math.Round((decimal)GUILayout.HorizontalSlider(OverhaulGlobal.settings.Stamina_Regen_Delay, 0f, 10f), 2);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void StabilityPage()
        {
            GUILayout.BeginVertical(GUI.skin.box);

            if (OverhaulGlobal.settings.Enable_StabilityMods) { GUI.color = Color.green; } else { GUI.color = Color.red; }
            OverhaulGlobal.settings.Enable_StabilityMods = GUILayout.Toggle(OverhaulGlobal.settings.Enable_StabilityMods, "Enable Stability / Stagger / Collision Mods");

            if (OverhaulGlobal.settings.Enable_StabilityMods)
            {
                GUILayout.Space(10);
                GUI.color = Color.white;

                OverhaulGlobal.settings.Blocking_Staggers_Attacker = GUILayout.Toggle(OverhaulGlobal.settings.Blocking_Staggers_Attacker, "Blocking staggers the attacker");

                OverhaulGlobal.settings.No_Stability_Regen_When_Blocking = GUILayout.Toggle(OverhaulGlobal.settings.No_Stability_Regen_When_Blocking, "No Stability Regen While Blocking");

                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Collision SlowDown modifier: x" + OverhaulGlobal.settings.SlowDown_Modifier, GUILayout.Width(250));
                OverhaulGlobal.settings.SlowDown_Modifier = Convert.ToSingle(Math.Round((decimal)GUILayout.HorizontalSlider(OverhaulGlobal.settings.SlowDown_Modifier, 0.2f, 2.0f), 2));
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                GUI.color = Color.green;
                GUILayout.Label("Stability / Stagger modifiers:");

                GUI.color = Color.white;
                GUILayout.BeginHorizontal();
                GUILayout.Label("Stability Regen Delay: " + OverhaulGlobal.settings.Stability_Regen_Delay + " secs", GUILayout.Width(250));
                OverhaulGlobal.settings.Stability_Regen_Delay = Convert.ToSingle(Math.Round((decimal)GUILayout.HorizontalSlider(OverhaulGlobal.settings.Stability_Regen_Delay, 0f, 10.0f), 2));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Stability Regen Speed: x" + OverhaulGlobal.settings.Stability_Regen_Speed, GUILayout.Width(250));
                OverhaulGlobal.settings.Stability_Regen_Speed = Convert.ToSingle(Math.Round((decimal)GUILayout.HorizontalSlider(OverhaulGlobal.settings.Stability_Regen_Speed, 0.2f, 10.0f), 2));
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Stagger Threshold: " + OverhaulGlobal.settings.Stagger_Threshold + "%", GUILayout.Width(250));
                OverhaulGlobal.settings.Stagger_Threshold = Convert.ToSingle(Math.Round((decimal)GUILayout.HorizontalSlider(OverhaulGlobal.settings.Stagger_Threshold, 0f, 100.0f), 2));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Stagger Immunity Time: " + OverhaulGlobal.settings.Stagger_Immunity_Period + " secs", GUILayout.Width(250));
                OverhaulGlobal.settings.Stagger_Immunity_Period = Convert.ToSingle(Math.Round((decimal)GUILayout.HorizontalSlider(OverhaulGlobal.settings.Stagger_Immunity_Period, 0f, 10.0f), 2));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Knockdown Threshold: " + OverhaulGlobal.settings.Knockdown_Threshold + "%", GUILayout.Width(250));
                OverhaulGlobal.settings.Knockdown_Threshold = Convert.ToSingle(Math.Round((decimal)GUILayout.HorizontalSlider(OverhaulGlobal.settings.Knockdown_Threshold, 0f, 10.0f), 2));
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Enemy Auto-KD: " + OverhaulGlobal.settings.Enemy_AutoKD_Count + " staggers", GUILayout.Width(250));
                OverhaulGlobal.settings.Enemy_AutoKD_Count = Convert.ToSingle(Math.Round((decimal)GUILayout.HorizontalSlider(OverhaulGlobal.settings.Enemy_AutoKD_Count, 0f, 100.0f), 0));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Enemy Auto-KD Reset: " + OverhaulGlobal.settings.Enemy_AutoKD_Reset_Time + " secs", GUILayout.Width(250));
                OverhaulGlobal.settings.Enemy_AutoKD_Reset_Time = Convert.ToSingle(Math.Round((decimal)GUILayout.HorizontalSlider(OverhaulGlobal.settings.Enemy_AutoKD_Reset_Time, 0f, 10.0f), 1));
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }

        private void EnemyPage()
        {
            GUILayout.BeginVertical(GUI.skin.box);

            if (OverhaulGlobal.settings.Enable_Enemy_Mods) { GUI.color = Color.green; }
            else { GUI.color = Color.red; }
            
            OverhaulGlobal.settings.Enable_Enemy_Mods = GUILayout.Toggle(OverhaulGlobal.settings.Enable_Enemy_Mods, "Enable Enemy Mods");
            GUI.color = Color.white;
            GUILayout.Space(10);

            if (OverhaulGlobal.settings.Enable_Enemy_Mods)
            {
                OverhaulGlobal.settings.All_Enemies_Allied = GUILayout.Toggle(OverhaulGlobal.settings.All_Enemies_Allied, "All enemies are allied with each other (requires scene reload)");

                GUILayout.Space(20);

                OverhaulGlobal.settings.Enemy_Balancing = GUILayout.Toggle(OverhaulGlobal.settings.Enemy_Balancing, "Enable Custom Enemy Stats (requires scene reload)");
                GUILayout.Space(10);

                if (OverhaulGlobal.settings.Enemy_Balancing)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Health Multiplier: " + OverhaulGlobal.settings.Enemy_Health + "x", GUILayout.Width(160));
                    OverhaulGlobal.settings.Enemy_Health = Convert.ToSingle(Math.Round((decimal)GUILayout.HorizontalSlider(OverhaulGlobal.settings.Enemy_Health, 1, 10), 1));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Damage Resistances: +" + OverhaulGlobal.settings.Enemy_Resistances, GUILayout.Width(160));
                    OverhaulGlobal.settings.Enemy_Resistances = Convert.ToSingle(Math.Round((decimal)GUILayout.HorizontalSlider(OverhaulGlobal.settings.Enemy_Resistances, -100, 100), 0));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Impact Resistance: +" + OverhaulGlobal.settings.Enemy_ImpactRes, GUILayout.Width(160));
                    OverhaulGlobal.settings.Enemy_ImpactRes = Convert.ToSingle(Math.Round((decimal)GUILayout.HorizontalSlider(OverhaulGlobal.settings.Enemy_ImpactRes, -100, 100), 0));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Damages Bonus: +" + OverhaulGlobal.settings.Enemy_Damages + "%", GUILayout.Width(160));
                    OverhaulGlobal.settings.Enemy_Damages = Convert.ToSingle(Math.Round((decimal)GUILayout.HorizontalSlider(OverhaulGlobal.settings.Enemy_Damages, -100, 500), 0));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Impact Bonus: +" + OverhaulGlobal.settings.Enemy_ImpactDmg + "%", GUILayout.Width(160));
                    OverhaulGlobal.settings.Enemy_ImpactDmg = Convert.ToSingle(Math.Round((decimal)GUILayout.HorizontalSlider(OverhaulGlobal.settings.Enemy_ImpactDmg, -100, 500), 0));
                    GUILayout.EndHorizontal();

                    //GUILayout.BeginHorizontal();
                    //GUILayout.Label("Attack Speed: " + OverhaulGlobal.settings.Enemy_Attack_Speed + "x", GUILayout.Width(160));
                    //OverhaulGlobal.settings.Enemy_Attack_Speed = Convert.ToSingle(Math.Round((decimal)GUILayout.HorizontalSlider(OverhaulGlobal.settings.Enemy_Attack_Speed, -100, 100), 2));
                    //GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndVertical();
        }

        private void MenuMouseFix()
        {
            // menu mouse fix
            bool shouldUpdate = false;
            if (!lastMenuToggle && showGui)
            {
                lastMenuToggle = true;
                shouldUpdate = true;
            }
            else if (lastMenuToggle && !showGui)
            {
                lastMenuToggle = false;
                shouldUpdate = true;
            }
            if (shouldUpdate)
            {
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
    }
}
