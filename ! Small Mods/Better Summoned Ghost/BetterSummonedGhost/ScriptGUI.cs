using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;
using SinAPI;

namespace BetterSummonedGhost
{
    public class ScriptGUI : MonoBehaviour
    {
        public GhostScript script;

        // ===================== GUI =======================

        public bool showGui = true;
        public Rect m_window = new Rect(5, 5, 300, 300);

        internal void OnGUI()
        {
            if (showGui)
            {
                m_window = GUI.Window(3463, m_window, GUIWindow, "Better Summoned Ghost 1.2");
            }
        }

        public void GUIWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, m_window.width - 50, 20));

            if (GUI.Button(new Rect(m_window.width - 50, 3, 45, 18), "X"))
            {
                showGui = false;                
            }

            float w = m_window.width - 10;
            float h = m_window.height - 25;
            Rect subRect = new Rect(5, 23, w, h);

            GUILayout.BeginArea(subRect, GUI.skin.box);

            script.settings.ShowGuiOnStartup = GUILayout.Toggle(script.settings.ShowGuiOnStartup, "Show GUI on Startup");
            GUILayout.Space(10);

            SettingsEdit("Custom Lifespan: ", "CustomLifespan", 0);
            if (script.settings.CustomLifespan <= 0) { script.settings.CustomLifespan = 1; }
            SettingsEdit("Custom Health: ", "CustomHealth", 1);

            GUILayout.Space(10);
            script.settings.KeepGhostClose = GUILayout.Toggle(script.settings.KeepGhostClose, "Keep Ghost Close");
            if (script.settings.KeepGhostClose)
            {
                SettingsEdit("Distance: ", "KeepCloseDistance", 2);
            }

            GUILayout.Space(10);
            script.settings.GiveGhostWeapon = GUILayout.Toggle(script.settings.GiveGhostWeapon, "Ghost Uses Player's Weapon (Cloned)");

            GUILayout.Space(30);
            if (GUILayout.Button("Create Soul Spot", GUILayout.Width(150)))
            {
                if (CharacterManager.Instance.PlayerCharacters.Count > 0)
                {
                    Item soul = ItemManager.Instance.GenerateItemNetwork(8000000);
                    soul.transform.position = CharacterManager.Instance.GetFirstLocalCharacter().transform.position;
                }
            }

            GUILayout.EndArea();
        }

        public List<string> editFields = new List<string>() { "", "", "", "" };

        public void SettingsEdit(string label, string fieldName, int fieldID)
        {
            FieldInfo fi = typeof(Settings).GetField(fieldName);
            if (fi != null && fi.GetValue(script.settings) is float value)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(label + value, GUILayout.Width(150));
                editFields[fieldID] = value.ToString();
                try
                {
                    value = float.Parse(GUILayout.TextField(editFields[fieldID]));
                    At.SetValue(value, typeof(Settings), script.settings, fieldName);
                }
                catch { }
                GUILayout.EndHorizontal();
            }
        }

    }
}
