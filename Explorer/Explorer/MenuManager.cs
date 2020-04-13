using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Explorer
{
    public class MenuManager : MonoBehaviour
    {
        public static MenuManager Instance;

        private const int m_mainWindowID = 10;

        public static int CurrentWindowID { get; set; } = 500000;
        public static bool ShowMenu { get; set; } = false;

        private static Rect m_rect = new Rect(5, 5, 550, 700);
        private static readonly List<WindowPage> Pages = new List<WindowPage>();
        private static int m_currentPage = 0;
        
        public static int GetNewWindowID()
        {
            return CurrentWindowID++;
        }

        private static Rect m_lastWindowRect;
        private static Rect m_lastReflectRect;

        public static Rect GetNewReflectRect()
        {
            return GetNewWindowRect(ref m_lastReflectRect);
        }

        public static Rect GetNewGameObjectRect()
        {
            return GetNewWindowRect(ref m_lastWindowRect);
        }

        public static Rect GetNewWindowRect(ref Rect lastRect)
        {
            Rect rect = new Rect(0, 0, 550, 700);
            if (m_rect.x <= (Screen.width - m_rect.width - 100))
            {
                rect = new Rect(m_rect.x + m_rect.width + 20, m_rect.y, rect.width, rect.height);
            }
            if (lastRect != null && lastRect.x == rect.x)
            {
                rect = new Rect(rect.x + 25, rect.y + 25, rect.width, rect.height);
            }
            lastRect = rect;
            return rect;
        }

        public static void SetCurrentPage(int index)
        {
            if (Pages.Count >= index)
            {
                return;
            }
            m_currentPage = index;
        }

        public static ExplorerWindow InspectGameObject(GameObject obj)
        {
            return ExplorerWindow.CreateWindow<GameObjectWindow>(obj);
        }

        public static ExplorerWindow ReflectObject(object obj)
        {
            return ExplorerWindow.CreateWindow<ReflectionWindow>(obj);
        }

        // --------------------- INTERNAL MAIN MENU FUNCTIONS ---------------------- //

        private Texture2D m_nofocusTex;
        private Texture2D m_focusTex;

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

        internal void Update()
        {
            Pages[m_currentPage].Update();
        }

        private static bool m_init = false;

        internal void OnGUI()
        {
            if (!m_init)
            {
                GuiInit();
            }

            if (ShowMenu)
            {
                m_rect = GUI.Window(m_mainWindowID, m_rect, MainWindow, "Outward Explorer");
            }
        }

        private void GuiInit()
        {
            m_nofocusTex = MakeTex(550, 700, new Color(0f, 0f, 0f, 0.75f));
            m_focusTex = MakeTex(550, 700, new Color(0f, 0f, 0f, 0.975f));

            GUI.skin.window.normal.background = m_nofocusTex;
            GUI.skin.window.onNormal.background = m_focusTex;

            m_init = true;
        }

        private void MainWindow(int id = m_mainWindowID)
        {
            GUI.DragWindow(new Rect(0, 0, m_rect.width - 90, 20));

            if (GUI.Button(new Rect(m_rect.width - 90, 2, 80, 20), "Hide (F7)"))
            {
                ShowMenu = false;
                return;
            }

            GUILayout.BeginArea(new Rect(5, 25, m_rect.width - 10, m_rect.height - 35));

            MainHeader();

            var page = Pages[m_currentPage];
            page.scroll = GUILayout.BeginScrollView(page.scroll);
            page.DrawWindow();
            GUILayout.EndScrollView();

            m_rect = ResizeWindow(m_rect, m_mainWindowID);

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

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
            {
                pix[i] = col;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        // ============= Resize Window Helper ============

        static readonly GUIContent gcDrag = new GUIContent("///", "drag to resize");

        private static bool isResizing = false;
        private static Rect m_currentResize;
        private static int m_currentWindow;

        public static Rect ResizeWindow(Rect _rect, int ID)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(_rect.width - 25);

            GUILayout.Button(gcDrag, GUI.skin.box, new GUILayoutOption[] { GUILayout.Width(25), GUILayout.Height(25) });

            var r = GUILayoutUtility.GetLastRect();

            Vector2 mouse = GUIUtility.ScreenToGUIPoint(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));

            if (r.Contains(mouse) && Input.GetMouseButtonDown(0))
            {
                isResizing = true;
                m_currentWindow = ID;
                m_currentResize = new Rect(mouse.x, mouse.y, _rect.width, _rect.height);
            }
            else if (!Input.GetMouseButton(0))
            {
                isResizing = false;
            }

            if (isResizing && ID == m_currentWindow)
            {
                _rect.width = Mathf.Max(100, m_currentResize.width + (mouse.x - m_currentResize.x));
                _rect.height = Mathf.Max(100, m_currentResize.height + (mouse.y - m_currentResize.y));
                _rect.xMax = Mathf.Min(Screen.width, _rect.xMax);  // modifying xMax affects width, not x
                _rect.yMax = Mathf.Min(Screen.height, _rect.yMax);  // modifying yMax affects height, not y
            }

            GUILayout.EndHorizontal();

            return _rect;
        }

        // ============ Main Menu Page Holder ============

        public abstract class WindowPage
        {
            public virtual string Name { get; set; }

            public Vector2 scroll = Vector2.zero;

            public abstract void Init();

            public abstract void DrawWindow();

            public abstract void Update();
        }

        // ============ GENERATED WINDOW HOLDER ============

        public abstract class ExplorerWindow : MonoBehaviour
        {
            public abstract string Name { get; set; }

            public object Target;

            public int windowID;
            public Rect m_rect = new Rect(0, 0, 550, 700);

            public Vector2 scroll = Vector2.zero;

            public static ExplorerWindow CreateWindow<T>(object target) where T: ExplorerWindow
            {
                var component = Instance.gameObject.AddComponent(typeof(T)) as ExplorerWindow;

                component.Target = target;
                component.windowID = GetNewWindowID();
                
                if (component is GameObjectWindow)
                {
                    component.m_rect = GetNewGameObjectRect();
                }
                else
                {
                    component.m_rect = GetNewReflectRect();
                }

                component.Init();

                return component;
            }

            public abstract void Init();
            public abstract void WindowFunction(int windowID);

            internal void OnGUI()
            {
                m_rect = GUI.Window(windowID, m_rect, WindowFunction, Name);
            }

            public void Header()
            {
                GUI.DragWindow(new Rect(0, 0, m_rect.width - 90, 20));

                if (GUI.Button(new Rect(m_rect.width - 90, 2, 80, 20), "<color=red><b>X</b></color>"))
                {
                    Destroy(this);
                    return;
                }
            }
        }
    }
}
