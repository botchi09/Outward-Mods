using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;
using UnityEngine.SceneManagement;

namespace Explorer_2
{
    public class SearchPage : MenuManager.WindowPage
    {
        public static SearchPage Instance;

        public override string Name { get => "Advanced Search"; set => base.Name = value; }

        private List<GameObject> m_searchResults = new List<GameObject>();

        private string m_searchInput = "";
        private string m_typeInput = "";
        private int m_limit = 100;

        private bool m_anyMode = true;
        private bool m_sceneMode;
        private bool m_noSceneMode;

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
            if (int.TryParse(resultinput, out int i) && i > 0)
            {
                m_limit = i;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Scene filter:", GUILayout.Width(100));
            SceneModeToggleButton(ref m_anyMode, "Any");
            SceneModeToggleButton(ref m_sceneMode, "Scene Objects");
            SceneModeToggleButton(ref m_noSceneMode, "Non-Scene Objects");
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Search"))
            {
                Search();
            }

            GUILayout.Space(15);

            if (m_searchResults.Count > 0)
            {
                foreach (var obj in m_searchResults)
                {
                    DrawObjectRow(obj);
                }
            }
            else
            {
                GUILayout.Label("<color=red><i>No results found!</i></color>");
            }
        }

        private void SetToggleTrue(ref bool toggle)
        {
            m_anyMode = false;
            m_noSceneMode = false;
            m_sceneMode = false;
            toggle = true;
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
                SetToggleTrue(ref toggle);
            }
            GUI.color = Color.white;
        }

        private void DrawObjectRow(GameObject obj)
        {
            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.BeginHorizontal();
            GUILayout.Label(obj.name);

            if (GUILayout.Button("Inspect", GUILayout.Width(100)))
            {
                MenuManager.InspectGameObject(obj);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }


        // -------------- ACTUAL METHODS (not Gui draw) ----------------- //

        private void Search()
        {
            m_searchResults = FindAllObjectsOfType(m_searchInput, m_typeInput);
        }

        private List<GameObject> FindAllObjectsOfType(string _search, string _type)
        {
            var objectsOfType = new List<GameObject>();

            Type type = typeof(GameObject);

            if (!string.IsNullOrEmpty(_type))
            {
                if (Explorer.GetType(_type) is Type getType)
                {
                    type = getType;
                }
                else
                {
                    Debug.LogWarning("ERROR! Could not find type: " + _type);
                    return objectsOfType;
                }
            }

            if (type != typeof(GameObject) && !typeof(Component).IsAssignableFrom(type))
            {
                Debug.LogError("Your Type must inherit from Component! Leave Type blank to find GameObjects.");
                return objectsOfType;
            }

            var matches = new List<GameObject>();
            int added = 0;

            foreach (var obj in Resources.FindObjectsOfTypeAll(type))
            {
                if (added == m_limit)
                {
                    break;
                }

                var go = obj as GameObject ?? (obj as Component).gameObject;

                if (!m_anyMode)
                {
                    if (m_noSceneMode && go.scene != null && go.scene.name == SceneManagerHelper.ActiveSceneName)
                    {
                        continue;
                    }
                    else if (m_sceneMode && (go.scene == null || go.scene.name != SceneManagerHelper.ActiveSceneName))
                    {
                        continue;
                    }
                }

                if (!matches.Contains(go) && (string.IsNullOrEmpty(_search) || obj.name.ToLower().Contains(_search.ToLower())))
                {
                    matches.Add(go);
                    added++;
                }
            }

            return matches;
        }
    }
}
