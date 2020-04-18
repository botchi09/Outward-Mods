using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Explorer
{
    public class MainMenu : MonoBehaviour
    {
        public static MainMenu Instance;

        public const int MainWindowID = 10;
        public static Rect MainRect = new Rect(5, 5, 550, 700);
        private static readonly List<WindowPage> Pages = new List<WindowPage>();
        private static int m_currentPage = 0;

        internal void Awake()
        {
            Instance = this;

            Pages.Add(new ScenePage());
            Pages.Add(new SearchPage());
            Pages.Add(new ConsolePage());

            foreach (var page in Pages)
            {
                page.Init();
            }
        }

        public static void SetCurrentPage(int index)
        {
            if (Pages.Count >= index)
            {
                return;
            }
            m_currentPage = index;
        }

        internal void Update()
        {
            Pages[m_currentPage].Update();
        }

        internal void OnGUI()
        {
            if (WindowManager.ShowWindows)
            {
                var origSkin = GUI.skin;
                GUI.skin = UIStyles.CustomSkin;

                MainRect = GUI.Window(MainWindowID, MainRect, MainWindow, "Outward Explorer");

                GUI.skin = origSkin;
            }
        }

        private void MainWindow(int id = MainWindowID)
        {
            GUI.DragWindow(new Rect(0, 0, MainRect.width - 90, 20));

            if (GUI.Button(new Rect(MainRect.width - 90, 2, 80, 20), "Hide (F7)"))
            {
                WindowManager.ShowWindows = false;
                return;
            }

            GUILayout.BeginArea(new Rect(5, 25, MainRect.width - 10, MainRect.height - 35), GUI.skin.box);

            MainHeader();

            var page = Pages[m_currentPage];
            page.scroll = GUILayout.BeginScrollView(page.scroll);
            page.DrawWindow();
            GUILayout.EndScrollView();

            MainRect = WindowManager.ResizeWindow(MainRect, MainWindowID);

            GUILayout.EndArea();
        }

        private void MainHeader()
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < Pages.Count; i++)
            {
                if (m_currentPage == i)
                    GUI.color = Color.green;
                else
                    GUI.color = Color.white;

                if (GUILayout.Button(Pages[i].Name))
                {
                    m_currentPage = i;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUI.color = Color.white;
        }

        public abstract class WindowPage
        {
            public virtual string Name { get; set; }

            public Vector2 scroll = Vector2.zero;

            public abstract void Init();

            public abstract void DrawWindow();

            public abstract void Update();
        }
    }
}
