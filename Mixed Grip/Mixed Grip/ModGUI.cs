using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using SinAPI;

namespace MixedGrip
{
    public class ModGUI : MonoBehaviour
    {
        public MixedGripGlobal global;

        public Rect m_windowRect = Rect.zero;
        public Vector2 scroll = Vector2.zero;

        public bool showGui = false;
        public int guiPage = 0;

        internal void Update()
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

        public bool lastMenuToggle;
    

        internal void OnGUI()
        {
            if (m_windowRect == Rect.zero || m_windowRect == null)
            {
                m_windowRect = new Rect(200, 5, 500, 150);
            }
            else
            {
                if (showGui)
                {
                    m_windowRect = GUI.Window(29322444, m_windowRect, DrawWindow, "Mixed Grip " + global._base.version.ToString("0.00"));
                }
            }
        }

        private void DrawWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, m_windowRect.width - 50, 20));

            if (GUI.Button(new Rect(m_windowRect.width - 50, 0, 45, 20), "X"))
            {
                showGui = false;
            }

            GUILayout.BeginArea(new Rect(3, 25, m_windowRect.width - 8, m_windowRect.height - 5));

            scroll = GUILayout.BeginScrollView(scroll);

            MainPage();

            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private void MainPage()
        {
            GUILayout.BeginVertical(GUI.skin.box);

            GUI.color = Color.white;
            global.settings.Swap_Animations = GUILayout.Toggle(global.settings.Swap_Animations, "Swap animations (Axes, Swords and Maces only)");
            global.settings.Swap_On_Equip_And_Unequip = GUILayout.Toggle(global.settings.Swap_On_Equip_And_Unequip, "Adjust weapon grip automatically when equipping and unequipping items.");
            // global.settings.Remember_Lantern = GUILayout.Toggle(global.settings.Remember_Lantern, "Remember Lanterns and Torches as previous off-hand item");
            //global.settings.Unequip_Offhand_To_Pouch = GUILayout.Toggle(global.settings.Unequip_Offhand_To_Pouch, "Force off-hand items into pouch when swapping grip");

            GUILayout.Space(10);

            global.settings.Balance_Weapons = GUILayout.Toggle(global.settings.Balance_Weapons, "Balance weapons when swapping grip?");

            //if (global.settings.Balance_Weapons)
            //{
            //    GUILayout.BeginHorizontal();
            //    GUILayout.Label("Attack speed (flat): " + global.settings.Weapon_Speed_Balance, GUILayout.Width(160));
            //    global.settings.Weapon_Speed_Balance = (float)Math.Round((decimal)GUILayout.HorizontalSlider(global.settings.Weapon_Speed_Balance, 0, 1f), 2);
            //    GUILayout.EndHorizontal();

            //    GUILayout.BeginHorizontal();
            //    GUILayout.Label("Damages (multiplier): " + global.settings.Weapon_Damage_Balance, GUILayout.Width(160));
            //    global.settings.Weapon_Damage_Balance = (float)Math.Round((decimal)GUILayout.HorizontalSlider(global.settings.Weapon_Damage_Balance, 1, 1.5f), 2);
            //    GUILayout.EndHorizontal();
            //}

            //// =======================================

            GUILayout.EndVertical();
        }
    }
}
