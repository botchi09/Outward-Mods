using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace AttackTimer
{
    public class TimerGUI : MonoBehaviour
    {
        public static TimerGUI Instance;

        private Rect m_window = Rect.zero;

        internal void Awake()
        {
            Instance = this;
        }

        internal void Start()
        {
            m_window = new Rect(5, 5, 275, 350);
        }

        internal void OnGUI()
        {
            m_window = GUI.Window(1001, m_window, TimerGUIFunc, "Attack Timer GUI");
        }

        private void TimerGUIFunc(int id)
        {
            GUI.DragWindow(new Rect(0, 0, m_window.width, 20));

            GUILayout.BeginArea(new Rect(5, 25, m_window.width - 10, m_window.height - 30));

            GUILayout.Label("Current Combo Length: " + AttackTimer.ComboLength);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<") && AttackTimer.ComboLength > 1)
            {
                AttackTimer.ComboLength--;
            }
            if (GUILayout.Button(">"))
            {
                AttackTimer.ComboLength++;
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("Current Combo Step: " + AttackTimer.ComboStep);
            if (GUILayout.Button("Reset"))
            {
                AttackTimer.ComboStep = 0;
                AttackTimer.ComboTimer = 0f;
                AttackTimer.TimerStarted = false;
                AttackTimer.LastAttackID = -1;
                AttackTimer.LastDamage = new DamageList();
            }
            GUILayout.Space(20);

            var time = TimeSpan.FromSeconds(AttackTimer.ComboTimer);
            GUILayout.Label("Timer: " + time.Seconds.ToString("00") + ":" + time.Milliseconds.ToString("000"));
            time = TimeSpan.FromSeconds(AttackTimer.LastComboTime);
            GUILayout.Label("Last Time: " + time.Seconds.ToString("00") + ":" + time.Milliseconds.ToString("000"));

            GUILayout.Space(20);

            GUILayout.Label("Last Attack ID: " + AttackTimer.LastAttackID);
            GUILayout.Label("Last damage: " + AttackTimer.LastDamage.ToString());

            GUILayout.EndArea();
        }
    }
}
