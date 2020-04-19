using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Explorer
{
    public class SearchPage : MainMenu.WindowPage
    {
        public static SearchPage Instance;

        public override string Name { get => "Advanced Search"; set => base.Name = value; }

        private string m_searchInput = "";
        private string m_typeInput = "";
        private int m_limit = 100;

        private bool m_anyMode = true;
        private bool m_sceneMode;
        private bool m_dontDestroyMode;
        private bool m_noSceneMode;

        private List<object> m_searchResults = new List<object>();
        private Vector2 resultsScroll = Vector2.zero;

        public override void Init() 
        {
            Instance = this;

            SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        }

        private void SceneManager_activeSceneChanged(Scene arg0, Scene arg1)
        {
            m_searchResults.Clear();
        }

        public override void Update() 
        {
        }

        public override void DrawWindow()
        {
            try
            {
                // helpers
                GUILayout.BeginHorizontal();
                GUILayout.Label("<b>Helpers:</b>", GUILayout.Width(80));
                if (GUILayout.Button("Find Instances", GUILayout.Width(120)))
                {
                    m_searchResults = GetInstanceClassScanner().ToList();
                }
                GUILayout.EndHorizontal();

                SearchBox();

                GUILayout.Space(15);

                resultsScroll = GUILayout.BeginScrollView(resultsScroll);

                var _temprect = new Rect(MainMenu.MainRect.x, MainMenu.MainRect.y, MainMenu.MainRect.width + 180, MainMenu.MainRect.height);

                if (m_searchResults.Count > 0)
                {
                    for (int i = 0; i < m_searchResults.Count; i++)
                    {
                        var obj = (object)m_searchResults[i];

                        UIStyles.DrawValue(ref obj, _temprect);
                    }
                }
                else
                {
                    GUILayout.Label("<color=red><i>No results found!</i></color>");
                }

                GUILayout.EndScrollView();
            }
            catch
            {
                m_searchResults.Clear();
            }
        }

        private void SearchBox()
        {
            // ----- GameObject Search -----
            GUILayout.Label("<b>Search Objects:</b>");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Name Contains:", GUILayout.Width(100));
            m_searchInput = GUILayout.TextField(m_searchInput);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Class Type:", GUILayout.Width(100));
            m_typeInput = GUILayout.TextField(m_typeInput);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Result limit:", GUILayout.Width(100));
            var resultinput = m_limit.ToString();
            resultinput = GUILayout.TextField(resultinput);
            if (int.TryParse(resultinput, out int _i) && _i > 0)
            {
                m_limit = _i;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Scene filter:", GUILayout.Width(100));
            SceneModeToggleButton(ref m_anyMode, "Any");
            SceneModeToggleButton(ref m_sceneMode, "This Scene");
            SceneModeToggleButton(ref m_dontDestroyMode, "DontDestroyOnLoad");
            SceneModeToggleButton(ref m_noSceneMode, "No Scene");
            GUILayout.EndHorizontal();

            if (m_sceneMode || m_dontDestroyMode)
            {
                GUILayout.Label("<i><size=12>note: This search mode restricts results to <color=cyan>GameObject</color> or <color=cyan>Component</color> types!</size></i>");
            }

            if (GUILayout.Button("Search"))
            {
                Search();
            }
        }

        private void SceneModeToggleButton(ref bool toggle, string label)
        {
            if (toggle)
            {
                GUI.color = Color.green;
            }
            else
            {
                GUI.color = Color.white;
            }
            if (GUILayout.Button(label))
            {
                SetSceneMode(ref toggle);
            }
            GUI.color = Color.white;
        }


        // -------------- ACTUAL METHODS (not Gui draw) ----------------- //


        private void SetSceneMode(ref bool toggle)
        {
            m_anyMode = false;
            m_noSceneMode = false;
            m_dontDestroyMode = false;
            m_sceneMode = false;
            toggle = true;
        }

        // ====== get instances ======

        public static IEnumerable<object> GetInstanceClassScanner()
        {
            var query = AppDomain.CurrentDomain.GetAssemblies()
                .Where(x => !x.FullName.StartsWith("Mono"))
                .SelectMany(GetTypesSafe)
                .Where(t => t.IsClass && !t.IsAbstract && !t.ContainsGenericParameters);

            foreach (var type in query)
            {
                object obj = null;
                try
                {
                    obj = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)?.GetValue(null, null);
                }
                catch
                {
                    try
                    {
                        obj = type.GetField("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)?.GetValue(null);
                    }
                    catch
                    {
                    }
                }
                if (obj != null && !obj.ToString().StartsWith("Mono"))
                {
                    yield return obj;
                }
            }
        }

        public static IEnumerable<Type> GetTypesSafe(Assembly asm)
        {
            try { return asm.GetTypes(); }
            catch (ReflectionTypeLoadException e) { return e.Types.Where(x => x != null); }
            catch { return Enumerable.Empty<Type>(); }
        }

        // ======= search functions =======

        private void Search()
        {
            m_searchResults = FindAllObjectsOfType(m_searchInput, m_typeInput);
        }

        private List<object> FindAllObjectsOfType(string _search, string _type)
        {
            Type type = typeof(Object);

            if (!string.IsNullOrEmpty(_type.Trim()))
            {
                if (Explorer.GetType(_type) is Type getType)
                {
                    type = getType;
                }
                else
                {
                    Debug.LogWarning("ERROR! Could not find type: " + _type);
                    return new List<object>();
                }
            }

            if (!typeof(Object).IsAssignableFrom(type))
            {
                Debug.LogError("Your Class Type must inherit from UnityEngine.Object! Leave blank to default to UnityEngine.Object");
                return new List<object>();
            }

            var matches = new List<object>();
            int added = 0;

            foreach (var obj in Resources.FindObjectsOfTypeAll(type))
            {
                if (added == m_limit)
                {
                    break;
                }

                if (!m_anyMode)
                {
                    if (m_noSceneMode)
                    {
                        if (!NoSceneFilter(obj, obj.GetType()))
                            continue;
                    }
                    else
                    {
                        GameObject go;

                        var objtype = obj.GetType();
                        if (objtype == typeof(GameObject))
                        {
                            go = obj as GameObject;
                        }
                        else if (typeof(Component).IsAssignableFrom(objtype))
                        {
                            go = (obj as Component).gameObject;
                        }
                        else { continue; }

                        if (!go) { continue; }

                        if (m_sceneMode)
                        {
                            if (go.scene.name != SceneManagerHelper.ActiveSceneName || go.scene.name == "DontDestroyOnLoad")
                            {
                                continue;
                            }
                        }
                        else if (m_dontDestroyMode)
                        {
                            if (go.scene.name != "DontDestroyOnLoad")
                            {
                                continue;
                            }
                        }
                    }
                }

                if (!matches.Contains(obj))
                {
                    matches.Add(obj);
                    added++;
                }
            }

            return matches;
        }

        public static bool ThisSceneFilter(object obj, Type type)
        {
            if (type == typeof(GameObject) || typeof(Component).IsAssignableFrom(type))
            {
                var go = obj as GameObject ?? (obj as Component).gameObject;
                
                if (go != null && go.scene.name == SceneManagerHelper.ActiveSceneName && go.scene.name != "DontDestroyOnLoad")
                {
                    return true;
                }
            }

            return false;
        }

        public static bool DontDestroyFilter(object obj, Type type)
        {
            if (type == typeof(GameObject) || typeof(Component).IsAssignableFrom(type))
            {
                var go = obj as GameObject ?? (obj as Component).gameObject;

                if (go != null && go.scene.name == "DontDestroyOnLoad")
                {
                    return true;
                }
            }

            return false;
        }

        public static bool NoSceneFilter(object obj, Type type)
        {
            if (type == typeof(GameObject))
            {
                var go = obj as GameObject;

                if (go.scene.name != SceneManagerHelper.ActiveSceneName && go.scene.name != "DontDestroyOnLoad")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (typeof(Component).IsAssignableFrom(type))
            {
                var go = (obj as Component).gameObject;

                if (go == null || (go.scene.name != SceneManagerHelper.ActiveSceneName && go.scene.name != "DontDestroyOnLoad"))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }
    }
}
