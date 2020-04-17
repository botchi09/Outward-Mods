using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Explorer
{
    public class MenuManager : MonoBehaviour
    {
        public static MenuManager Instance;

        public static bool ShowWindows { get; set; } = false;

        public static List<ExplorerWindow> Windows = new List<ExplorerWindow>();
        public static int CurrentWindowID { get; set; } = 500000;
        private static Rect m_lastWindowRect;
        private static Rect m_lastReflectRect;

        private const int m_mainWindowID = 10;
        public static Rect m_mainRect = new Rect(5, 5, 550, 700);
        private static readonly List<WindowPage> Pages = new List<WindowPage>();
        private static int m_currentPage = 0;

        // ========= Public Helpers =========

        public static bool IsMouseInWindow
        {
            get
            {
                if (!ShowWindows)
                {
                    return false;
                }

                foreach (var window in Windows)
                {
                    if (RectContainsMouse(window.m_rect))
                    {
                        return true;
                    }
                }
                return RectContainsMouse(m_mainRect);
            }
        }

        private static bool RectContainsMouse(Rect rect)
        {
            return rect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
        }

        public static int NextWindowID()
        {
            return CurrentWindowID++;
        }

        public static Rect NewGamebjectRect()
        {
            return GetNewWindowRect(ref m_lastWindowRect);
        }

        public static Rect NewReflectionRect()
        {
            return GetNewWindowRect(ref m_lastReflectRect);
        }

        public static Rect GetNewWindowRect(ref Rect lastRect)
        {
            Rect rect = new Rect(0, 0, 550, 700);

            if (m_mainRect.x <= (Screen.width - m_mainRect.width - 100))
            {
                rect = new Rect(m_mainRect.x + m_mainRect.width + 20, m_mainRect.y, rect.width, rect.height);
            }

            if (lastRect.x == rect.x)
            {
                rect = new Rect(rect.x + 25, rect.y + 25, rect.width, rect.height);
            }

            lastRect = rect;

            return rect;
        }

        public static ExplorerWindow InspectGameObject(GameObject obj, out bool createdNew)
        {
            foreach (GameObjectWindow window in Windows.Where(x => x is GameObjectWindow))
            {
                if (obj == window.m_object)
                {
                    GUI.BringWindowToFront(window.windowID);
                    GUI.FocusWindow(window.windowID);
                    createdNew = false;
                    return window;
                }
            }

            createdNew = true;
            var new_window = ExplorerWindow.CreateWindow<GameObjectWindow>(obj);
            GUI.FocusWindow(new_window.windowID);

            return new_window;
        }

        public static ExplorerWindow ReflectObject(object obj, out bool createdNew)
        {
            foreach (ReflectionWindow window in Windows.Where(x => x is ReflectionWindow))
            {
                if (obj == window.m_object)
                {
                    GUI.BringWindowToFront(window.windowID);
                    GUI.FocusWindow(window.windowID);
                    createdNew = false;
                    return window;
                }
            }

            createdNew = true;
            var new_window = ExplorerWindow.CreateWindow<ReflectionWindow>(obj);
            GUI.FocusWindow(new_window.windowID);

            return new_window;
        }

        public static void SetCurrentPage(int index)
        {
            if (Pages.Count >= index)
            {
                return;
            }
            m_currentPage = index;
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

            // prevent Rewired GUI click-through
            On.UICursor.StateForCursorButton += UICursor_StateForCursorButton;
        }

        private PointerEventData.FramePressState UICursor_StateForCursorButton(On.UICursor.orig_StateForCursorButton orig, UICursor self, int _buttonID)
        {
            if (IsMouseInWindow)
            {
                return PointerEventData.FramePressState.NotChanged;
            }
            else
            {
                return orig(self, _buttonID);
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

            if (ShowWindows)
            {
                m_mainRect = GUI.Window(m_mainWindowID, m_mainRect, MainWindow, "Outward Explorer");
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
            GUI.DragWindow(new Rect(0, 0, m_mainRect.width - 90, 20));

            if (GUI.Button(new Rect(m_mainRect.width - 90, 2, 80, 20), "Hide (F7)"))
            {
                ShowWindows = false;
                return;
            }

            GUILayout.BeginArea(new Rect(5, 25, m_mainRect.width - 10, m_mainRect.height - 35));

            MainHeader();

            var page = Pages[m_currentPage];
            page.scroll = GUILayout.BeginScrollView(page.scroll);
            page.DrawWindow();
            GUILayout.EndScrollView();

            m_mainRect = ResizeWindow(m_mainRect, m_mainWindowID);

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

        // ============= Public helper for GameObject rows ================ //
        
        /// <summary>
        /// Helper for drawing a "GameObject Row" (listing a gameobject entry on a list, with button / details)
        /// </summary>
        /// <param name="obj">the GameObject</param>
        /// <param name="specialInspectMethod">Special optional method for what happens if you click a gameobject, otherwise opens a GameObject window</param>
        public static void DrawGameObjectRow(GameObject obj, Action<GameObject> specialInspectMethod = null, bool showSmallInspectBtn = true, float width = 380)
        {
            if (obj == null) 
            {
                GUILayout.Label("<i><color=red>null</color></i>");
                return; 
            }

            bool enabled = obj.activeInHierarchy;
            bool children = obj.transform.childCount > 0;

            GUILayout.BeginHorizontal();
            GUI.skin.button.alignment = TextAnchor.UpperLeft;

            // ------ build name ------

            string label = children ? "[" + obj.transform.childCount + " children] " : "";
            label += obj.name;

            // ------ Color -------

            if (enabled)
            {
                if (children)
                {
                    GUI.color = Color.green;
                }
                else
                {
                    GUI.color = Global.LIGHT_GREEN;
                }
            }
            else
            {
                GUI.color = Global.LIGHT_RED;
            }

            // ------ toggle active button ------

            enabled = GUILayout.Toggle(enabled, "", GUILayout.Width(18));
            if (obj.activeSelf != enabled)
            {
                obj.SetActive(enabled);
            }

            // ------- actual button ---------

            if (GUILayout.Button(label, new GUILayoutOption[] { GUILayout.Height(22), GUILayout.Width(width) }))
            {
                if (specialInspectMethod != null)
                {
                    specialInspectMethod(obj);
                }
                else
                {
                    InspectGameObject(obj, out bool _);
                }
            }

            // ------ small "Inspect" button on the right ------

            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
            GUI.color = Color.white;

            if (showSmallInspectBtn)
            {
                if (GUILayout.Button("Inspect"))
                {
                    InspectGameObject(obj, out bool _);
                }
            }

            GUILayout.EndHorizontal();
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
                var component = (ExplorerWindow)Instance.gameObject.AddComponent(typeof(T));

                component.Target = target;
                component.windowID = NextWindowID();
                
                if (component is GameObjectWindow)
                {
                    component.m_rect = NewGamebjectRect();
                }
                else
                {
                    component.m_rect = NewReflectionRect();
                }

                Windows.Add(component);

                component.Init();

                return component;
            }

            public void DestroyWindow()
            {
                Windows.Remove(this);
                Destroy(this);
            }

            public abstract void Init();
            public abstract void WindowFunction(int windowID);

            internal void OnGUI()
            {
                if (ShowWindows)
                {
                    m_rect = GUI.Window(windowID, m_rect, WindowFunction, Name);
                }
            }

            public void Header()
            {
                GUI.DragWindow(new Rect(0, 0, m_rect.width - 90, 20));

                if (GUI.Button(new Rect(m_rect.width - 90, 2, 80, 20), "<color=red><b>X</b></color>"))
                {
                    DestroyWindow();
                    return;
                }
            }
        }
    }
}
