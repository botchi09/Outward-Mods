using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using SinAPI;

namespace OutSoulsMod
{
    public class OutSoulsGUI : MonoBehaviour
    {
        public static OutSoulsGUI Instance;

        public Rect m_windowRect = Rect.zero;
        public Vector2 scroll = Vector2.zero;

        private Vector2 m_virtualSize = new Vector2(1920, 1080);
        private Vector2 m_currentSize = Vector2.zero;
        public Matrix4x4 m_scaledMatrix;

        public bool showGui = false;
        public int guiPage = 0;

        // message display stuff
        public Font Philosopher_Font = null;
        public string currentDisplayMessage = "";
        public float lastMessageTime = -1;

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

            // get bonfire menu font
            if (Philosopher_Font == null)
            {
                Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
                if (fonts != null && fonts.Count() > 0)
                {
                    foreach (Font font in fonts)
                    {
                        if (font.name == "Philosopher-Regular")
                        {
                            Philosopher_Font = font;
                        }
                    }
                }                
            }

            // menu mouse fix
            bool shouldUpdate = false;
            if (!lastMenuToggle && (showGui || BonfireManager.Instance.IsBonfireInteracting))
            {
                lastMenuToggle = true;
                shouldUpdate = true;
            }
            else if (lastMenuToggle && (!showGui && !BonfireManager.Instance.IsBonfireInteracting))
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
            Matrix4x4 orig = GUI.matrix;

            if (!OutSouls.settings.Disable_Scaling)
            {
                GUI.matrix = m_scaledMatrix;
            }

            if (currentDisplayMessage != "") { DrawMessage(); }

            if (m_windowRect == Rect.zero || m_windowRect == null)
            {
                m_windowRect = new Rect(200, 5, 400, 470);
            }
            else
            {
                if (showGui)
                {
                    m_windowRect = GUI.Window(2332228, m_windowRect, DrawWindow, "OutSouls " + OutSouls.version.ToString("0.00"));
                }
            }
            GUI.matrix = orig;
        }


        // ===== message display stuff =====

        public IEnumerator SetMessage(string message, float lifespan)
        {
            float startTime = Time.time;
            lastMessageTime = startTime;

            currentDisplayMessage = message;

            while (Time.time - startTime < lifespan)
            {
                yield return null;
            }

            // only reset the message if this coroutine is still the "active" message
            if (currentDisplayMessage == message) { currentDisplayMessage = ""; }
        }

        public void DrawMessage()
        {
            float delta2 = (Time.time - lastMessageTime) * 2;
            float alphaClamp = Mathf.Clamp(delta2, 0.1f, 1);

            float x = 0;
            float y = Screen.height / 4;
            GUI.skin.label.fontSize = 21;
            GUI.skin.label.alignment = TextAnchor.UpperCenter;

            if (Philosopher_Font != null)
            {
                GUI.skin.label.font = Philosopher_Font;
            }

            GUI.color = new Color(0, 0, 0, alphaClamp); // black
            GUI.Label(new Rect(x + 1, y + 1, Screen.width, 100), currentDisplayMessage);

            GUI.color = new Color(0.9f, 0.1f, 0.05f, alphaClamp); // dark red
            GUI.Label(new Rect(x, y, Screen.width, 100), currentDisplayMessage);

            // reset
            GUI.skin.label.fontSize = 13;
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
            GUI.skin.label.font = null;
            GUI.color = Color.white;
        }


        // ============ OUTSOULS MAIN MENU ============
        private void DrawWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, m_windowRect.width - 50, 20));

            if (GUI.Button(new Rect(m_windowRect.width - 50, 0, 45, 20), "X"))
            {
                showGui = false;
            }

            GUILayout.BeginArea(new Rect(3, 25, m_windowRect.width - 8, m_windowRect.height - 5));

            //GUILayout.BeginHorizontal();
            //if (guiPage == 0) { GUI.color = Color.green; } else { GUI.color = Color.white; }
            //if (GUILayout.Button("Main"))
            //{
            //    guiPage = 0;
            //}
            //if (guiPage == 1) { GUI.color = Color.green; } else { GUI.color = Color.white; }
            //if (GUILayout.Button("Enemies"))
            //{
            //    guiPage = 1;
            //}
            //if (guiPage == 2) { GUI.color = Color.green; } else { GUI.color = Color.white; }
            //if (GUILayout.Button("Stability"))
            //{
            //    guiPage = 2;
            //}
            //if (guiPage == 3) { GUI.color = Color.green; } else { GUI.color = Color.white; }
            //if (GUILayout.Button("Bonfires"))
            //{
            //    guiPage = 3;
            //}
            //GUI.color = Color.white; 
            //GUILayout.EndHorizontal();

            scroll = GUILayout.BeginScrollView(scroll);

            switch (guiPage)
            {
                case 0:
                    BonfirePage();
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

            OutSouls.settings.Disable_Scaling = GUILayout.Toggle(OutSouls.settings.Disable_Scaling, "Disable GUI Scaling");

            //// =======================================

            GUILayout.EndVertical();
        }

        private void BonfirePage()
        {
            GUILayout.BeginVertical(GUI.skin.box);

            if (OutSouls.settings.Enable_Bonfire_System) { GUI.color = Color.green; }
            else { GUI.color = Color.red; }
            GUILayout.Label("Bonfire System");
            OutSouls.settings.Enable_Bonfire_System = GUILayout.Toggle(OutSouls.settings.Enable_Bonfire_System, "Enable Bonfires (requires scene reload)");
            GUI.color = Color.white;

            GUILayout.Space(10);

            OutSouls.settings.Bonfires_Heal_Enemies = GUILayout.Toggle(OutSouls.settings.Bonfires_Heal_Enemies, "Resting at Bonfires Heals and Resurrects Enemies");
            OutSouls.settings.Disable_Bonfire_Costs = GUILayout.Toggle(OutSouls.settings.Disable_Bonfire_Costs, "Disable Bonfire Costs");
            OutSouls.settings.Cant_Use_Bonfires_In_Combat = GUILayout.Toggle(OutSouls.settings.Cant_Use_Bonfires_In_Combat, "Can't Use Bonfires In Combat");

            GUILayout.EndVertical();
        }
    }
}
