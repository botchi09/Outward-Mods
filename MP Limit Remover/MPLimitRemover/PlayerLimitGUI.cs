using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using SinAPI;

namespace MPLimitRemover
{
    public class PlayerLimitGUI : MonoBehaviour
    {
        public PlayerLimitRemover global;

        public Rect m_windowRect = Rect.zero;
        public Vector2 scroll = Vector2.zero;

        private Vector2 m_virtualSize = new Vector2(1920, 1080);
        private Vector2 m_currentSize = Vector2.zero;
        public Matrix4x4 m_scaledMatrix;

        public bool showGui = false;
        public int guiPage = 0;
        public bool lastMenuToggle;

        internal void Update()
        {
            if (m_currentSize.x != Screen.width || m_currentSize.y != Screen.height)
            {
                m_scaledMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / m_virtualSize.x, Screen.height / m_virtualSize.y, 1));
                m_currentSize = new Vector2(Screen.width, Screen.height);
            }

            if (NetworkLevelLoader.Instance.IsGameplayPaused || Global.Lobby.PlayersInLobbyCount <= 0)
            {
                return;
            }

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

        internal void OnGUI()
        {
            Matrix4x4 orig = GUI.matrix;

            if (global.settings.Enable_Menu_Scaling)
            {
                GUI.matrix = m_scaledMatrix;
            }

            if (m_windowRect == Rect.zero || m_windowRect == null)
            {
                m_windowRect = new Rect(10, 10, 260, 200);
            }
            else
            {
                if (showGui)
                {
                    m_windowRect = GUI.Window(5551223, m_windowRect, DrawWindow, global._base.ID + " (v" + global._base.version.ToString("0.00") + ")");
                }
            }

            GUI.matrix = orig;
        }

        private void DrawWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, m_windowRect.width - 30, 20));

            if (GUI.Button(new Rect(m_windowRect.width - 32, 2, 25, 20), "X"))
            {
                showGui = false;
            }

            GUILayout.BeginArea(new Rect(3, 25, m_windowRect.width - 8, m_windowRect.height - 5));
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(m_windowRect.height - 30));
            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Online Multiplayer Limit:", GUILayout.Width(140));
            string s = GUILayout.TextField(global.settings.PlayerLimit.ToString(), GUILayout.Width(40));
            if (int.TryParse(s, out int i))
            {
                global.settings.PlayerLimit = i;
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("This will only apply when you are the host. If you want to join a game in split mode, you should join the game first, then start split.");

            global.settings.Show_Menu_On_Startup = GUILayout.Toggle(global.settings.Show_Menu_On_Startup, "Show Menu On Startup");
            global.settings.Enable_Menu_Scaling = GUILayout.Toggle(global.settings.Enable_Menu_Scaling, "Enable Menu Scaling");

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
