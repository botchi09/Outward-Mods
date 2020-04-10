using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Explorer_2
{
    public class GameObjectWindow : MenuManager.ExplorerWindow
    {
        public override string Name { get => "GameObject Inspector"; set => Name = value; }

        private GameObject m_object;

        // gui element holders
        private string m_name;
        private string m_scene;

        private Vector2 m_transformScroll = Vector2.zero;
        private Transform[] m_children;

        private Vector2 m_compScroll = Vector2.zero;
        private Component[] m_components;


        public override void Init()
        {
            if (!(m_object = Target as GameObject))
            {
                Debug.LogError("Target is not a GameObject!");
                Destroy(this);
            }

            m_name = m_object.name;
            m_scene = m_object.scene == null ? "null" : m_object.scene.name;

            m_components = m_object.GetComponents(typeof(Component));

            var list = new List<Transform>();
            for (int i = 0; i < m_object.transform.childCount; i++)
            {
                list.Add(m_object.transform.GetChild(i));
            }
            m_children = list.ToArray();
        }

        internal void Update()
        {
            if (m_object == null)
            {
                Destroy(this);
            }
        }

        private void InspectGameObject(GameObject obj)
        {
            var window = MenuManager.InspectGameObject(obj);
            window.m_rect = new Rect(this.m_rect.x, this.m_rect.y, 550, 700);
            Destroy(this);
        }

        private void ReflectObject(object obj)
        {
            var window = MenuManager.ReflectObject(obj);
            if (this.m_rect.x <= (Screen.width - this.m_rect.width - 100))
            {
                window.m_rect = new Rect(
                    this.m_rect.x + this.m_rect.width + 20,
                    this.m_rect.y,
                    550,
                    700);
            }
            else
            {
                window.m_rect = new Rect(this.m_rect.x + 50, this.m_rect.y + 50, 550, 700);
            }
        }

        public override void WindowFunction(int windowID)
        {
            Header();

            scroll = GUILayout.BeginScrollView(scroll);

            GUILayout.Label("Name: <b><color=cyan>" + m_name + "</color></b>");

            GUILayout.Label("Scene: <color=cyan>" + (m_scene == "" ? "n/a" : m_scene) + "</color>");
            if (m_scene == SceneManagerHelper.ActiveSceneName)
            {
                if (GUILayout.Button("<color=#00FF00>< View in Scene Explorer</color>", GUILayout.Width(230)))
                {
                    ScenePage.Instance.SetTransformTarget(m_object);
                    MenuManager.SetCurrentPage(0);
                }
            }

            GUILayout.BeginHorizontal();
            string pathlabel = "Path: ";
            if (m_object.transform.parent != null)
            {
                pathlabel += m_object.transform.GetGameObjectPath();
                if (GUILayout.Button("<-", GUILayout.Width(35)))
                {
                    InspectGameObject(m_object.transform.parent.gameObject);
                }
            }
            GUILayout.Label(pathlabel);
            GUILayout.EndHorizontal();

            if (m_object.transform.parent != null || m_object.transform.childCount > 0)
            {
                TransformList();
            }

            ComponentList();

            GameObjectControls();

            if (CharacterManager.Instance.GetFirstLocalCharacter() is Character c)
            {
                CharacterCheats(c);
            }

            GUILayout.EndScrollView();
        }

        private void TransformList()
        {
            GUILayout.BeginVertical(GUI.skin.box, new GUILayoutOption[] { GUILayout.MaxHeight(225), GUILayout.MinHeight(100), GUILayout.ExpandHeight(true) });
            m_transformScroll = GUILayout.BeginScrollView(m_transformScroll);

            GUILayout.Label("<b>Children:</b>");
            if (m_children != null && m_children.Length > 0)
            {
                foreach (var obj in m_children.Where(x => x.childCount > 0))
                {
                    DrawGameObjectRow(obj.gameObject);
                }
                foreach (var obj in m_children.Where(x => x.childCount == 0))
                {
                    DrawGameObjectRow(obj.gameObject);
                }
            }
            else
            {
                GUILayout.Label("<i>None</i>");
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void ComponentList()
        {
            GUILayout.BeginVertical(GUI.skin.box, new GUILayoutOption[] { GUILayout.MaxHeight(225), GUILayout.MinHeight(100), GUILayout.ExpandHeight(true) });
            m_compScroll = GUILayout.BeginScrollView(m_compScroll);
            GUILayout.Label("<b><size=15>Components</size></b>");

            foreach (var component in m_components)
            {
                if (component.GetType() == typeof(Transform))
                {
                    continue;
                }

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("<color=cyan>" + component.GetType().ToString() + "</color>"))
                {
                    ReflectObject(component);
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void GameObjectControls()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("<b><size=15>Object Controls</size></b>");

            GUILayout.Label("TODO gameobject controls");

            GUILayout.EndVertical();
        }

        private void CharacterCheats(Character player)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("<b><size=15>Character Cheats</size></b>");

            GUILayout.Label("TODO character cheats");

            GUILayout.EndVertical();
        }

        private void DrawGameObjectRow(GameObject obj)
        {
            bool enabled = obj.activeInHierarchy;
            bool children = obj.transform.childCount > 0;
            bool _static = false;

            GUILayout.BeginHorizontal();
            GUI.skin.button.alignment = TextAnchor.UpperLeft;

            if (enabled)
            {
                if (obj.GetComponent<MeshRenderer>() is MeshRenderer m && m.isPartOfStaticBatch)
                {
                    _static = true;
                    GUI.color = Color.yellow;
                }
                else if (children)
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

            // build name
            string label = "";
            if (_static) { label = "(STATIC) " + label; }

            if (children)
                label += "[" + obj.transform.childCount + " children] ";

            label += obj.name;

            if (GUILayout.Button(label, GUILayout.Height(22)))
            {
                InspectGameObject(obj);
            }

            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
            GUI.color = Color.white;

            GUILayout.EndHorizontal();
        }
    }
}
