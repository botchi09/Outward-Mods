using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using SinAPI;

namespace BetterCustomWeight
{
    public class BetterWeightGUI : MonoBehaviour
    {
        public BetterWeightScript script;

        public bool ShowMenu = true;
        private bool lastMenuToggle = true;
        public Rect m_window = Rect.zero;

        internal void Update()
        {
            if (Global.Lobby.PlayersInLobbyCount < 1) { return; }

            // menu mouse control
            if (lastMenuToggle != ShowMenu)
            {
                lastMenuToggle = ShowMenu;

                if (CharacterManager.Instance.GetFirstLocalCharacter() is Character c)
                {
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

        internal void OnGUI()
        {
            if (!ShowMenu) { return; }            

            if (m_window == Rect.zero)
            {
                m_window = new Rect(5, 5, 270, 260);
            }
            else
            {
                m_window = GUI.Window(8883451, m_window, DrawMenu, script._base.ID + " v" + script._base.version.ToString("0.00"));
            }
        }

        private void DrawMenu(int id)
        {
            GUI.DragWindow(new Rect(0, 0, m_window.width - 40, 20));
            if (GUI.Button(new Rect(m_window.width - 35, 3, 32, 20), "X"))
            {
                ShowMenu = false;
            }

            GUILayout.BeginArea(new Rect(5, 25, m_window.width - 10, m_window.height - 35));
            GUILayout.BeginVertical(GUI.skin.box);

            script.settings.NoContainerLimit = GUILayout.Toggle(script.settings.NoContainerLimit, "No Container Limits");
            script.settings.DisableAllBurdens = GUILayout.Toggle(script.settings.DisableAllBurdens, "Disable All Burdens");

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Pouch Bonus:", GUILayout.Width(150));
            try { script.settings.PouchBonus = int.Parse(GUILayout.TextField(script.settings.PouchBonus.ToString())); } catch { }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Bag Bonus (flat):", GUILayout.Width(150));
            try { script.settings.BagBonusFlat = int.Parse(GUILayout.TextField(script.settings.BagBonusFlat.ToString())); } catch { }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Bag Bonus (multi):", GUILayout.Width(150));
            try { script.settings.BagBonusMulti = float.Parse(GUILayout.TextField(script.settings.BagBonusMulti.ToString("0.0"))); } catch { }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            if (GUILayout.Button("Apply Changes"))
            {
                script.PatchedRPM = false;
                script.PatchedCharacters = 0;
            }

            GUILayout.Space(10);

            script.settings.ShowMenuOnStartup = GUILayout.Toggle(script.settings.ShowMenuOnStartup, "Show Menu on Startup");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
