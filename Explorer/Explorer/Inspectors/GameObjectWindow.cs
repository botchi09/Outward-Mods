using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Explorer
{
    public class GameObjectWindow : WindowManager.UIWindow
    {
        public override string Name { get => "GameObject Inspector"; set => Name = value; }

        public GameObject m_object;

        // gui element holders
        private string m_name;
        private string m_scene;

        private Vector2 m_transformScroll = Vector2.zero;
        private Transform[] m_children;

        private Vector2 m_compScroll = Vector2.zero;
        private Component[] m_components;

        private float m_translateAmount = 0.3f;
        private float m_rotateAmount = 50f;
        private float m_scaleAmount = 0.1f;

        public override void Init()
        {
            if (!(m_object = Target as GameObject))
            {
                Debug.LogError("Target is not a GameObject!");
                DestroyWindow();
                return;
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
                DestroyWindow();
            }
        }

        private void InspectGameObject(GameObject obj)
        {
            var window = WindowManager.InspectGameObject(obj, out bool created);
            
            if (created)
            {
                window.m_rect = new Rect(this.m_rect.x, this.m_rect.y, this.m_rect.width, this.m_rect.height);
                DestroyWindow();
            }
        }

        private void ReflectObject(object obj)
        {
            var window = WindowManager.ReflectObject(obj, out bool created);

            if (created)
            {
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
        }

        public override void WindowFunction(int windowID)
        {
            Header();

            GUILayout.BeginArea(new Rect(5, 25, m_rect.width - 10, m_rect.height - 35), GUI.skin.box);

            scroll = GUILayout.BeginScrollView(scroll);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Scene: <color=cyan>" + (m_scene == "" ? "n/a" : m_scene) + "</color>");
            if (m_scene == SceneManagerHelper.ActiveSceneName)
            {
                if (GUILayout.Button("<color=#00FF00>< View in Scene Explorer</color>", GUILayout.Width(230)))
                {
                    ScenePage.Instance.SetTransformTarget(m_object);
                    MainMenu.SetCurrentPage(0);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Path:", GUILayout.Width(50));
            string pathlabel = m_object.transform.GetGameObjectPath();
            if (m_object.transform.parent != null)
            {
                if (GUILayout.Button("<-", GUILayout.Width(35)))
                {
                    InspectGameObject(m_object.transform.parent.gameObject);
                }
            }
            GUILayout.TextArea(pathlabel);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Name:", GUILayout.Width(50));
            GUILayout.TextArea(m_name);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("GameObject Active:", GUILayout.Width(150));
            bool m_active = m_object.activeSelf;
            m_active = GUILayout.Toggle(m_active, (m_active ? "<color=lime>Enabled " : "<color=red>Disabled") + "</color>");
            if (m_object.activeSelf != m_active) { m_object.SetActive(m_active); }
            GUILayout.EndHorizontal();

            // --- Horizontal Columns section ---
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(m_rect.width / 2 - 17));
            TransformList();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.Width(m_rect.width / 2 - 17));
            ComponentList();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal(); // end horiz columns

            GameObjectControls();

            GUILayout.EndScrollView();

            m_rect = WindowManager.ResizeWindow(m_rect, windowID);

            GUILayout.EndArea();
        }

        private void TransformList()
        {
            GUILayout.BeginVertical(GUI.skin.box, new GUILayoutOption[] { GUILayout.MaxHeight(450), GUILayout.MinHeight(100), GUILayout.ExpandHeight(true) });
            m_transformScroll = GUILayout.BeginScrollView(m_transformScroll);

            GUILayout.Label("<b>Children:</b>");
            if (m_children != null && m_children.Length > 0)
            {
                foreach (var obj in m_children.Where(x => x.childCount > 0))
                {
                    UIStyles.GameobjButton(obj.gameObject, InspectGameObject, false, this.m_rect.width / 2 - 60);
                }
                foreach (var obj in m_children.Where(x => x.childCount == 0))
                {
                    UIStyles.GameobjButton(obj.gameObject, InspectGameObject, false, this.m_rect.width / 2 - 60);
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
            GUILayout.BeginVertical(GUI.skin.box, new GUILayoutOption[] { GUILayout.MaxHeight(450), GUILayout.MinHeight(100), GUILayout.ExpandHeight(true) });
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
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(500));
            GUILayout.Label("<b><size=15>GameObject Controls</size></b>");

            if (CharacterManager.Instance.GetFirstLocalCharacter() is Character c)
            {
                CharacterCheats(c);
            }

            GUILayout.BeginVertical(GUI.skin.box);

            var t = m_object.transform;
            TranslateControl(t, TranslateType.Position, ref m_translateAmount,  false);
            TranslateControl(t, TranslateType.Rotation, ref m_rotateAmount,     true);
            TranslateControl(t, TranslateType.Scale,    ref m_scaleAmount,      false);

            GUILayout.EndVertical();

            if (GUILayout.Button("<color=red><b>Destroy</b></color>"))
            {
                Destroy(m_object);
                DestroyWindow();
                return;
            }

            GUILayout.EndVertical();
        }

        public enum TranslateType
        {
            Position,
            Rotation,
            Scale
        }

        private void TranslateControl(Transform transform, TranslateType mode, ref float amount, bool multByTime)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("<color=cyan><b>" + mode + "</b></color>:", GUILayout.Width(65));

            Vector3 vector = Vector3.zero;
            switch (mode)
            {
                case TranslateType.Position: vector = transform.position; break;
                case TranslateType.Rotation: vector = transform.rotation.eulerAngles; break;
                case TranslateType.Scale:    vector = transform.localScale; break;
            }
            GUILayout.Label(vector.ToString());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Force:", GUILayout.Width(50));
            var input = amount.ToString();
            input = GUILayout.TextField(input, GUILayout.Width(50));
            if (float.TryParse(input, out float f))
            {
                amount = f;
            }

            GUI.skin.label.alignment = TextAnchor.MiddleRight;

            GUILayout.Label("X:");
            PlusMinusFloat(ref vector.x, amount, multByTime);

            GUILayout.Label("Y:");
            PlusMinusFloat(ref vector.y, amount, multByTime);

            GUILayout.Label("Z:");
            PlusMinusFloat(ref vector.y, amount, multByTime);

            switch (mode)
            {
                case TranslateType.Position: transform.position = vector; break;
                case TranslateType.Rotation: transform.rotation = Quaternion.Euler(vector); break;
                case TranslateType.Scale:    transform.localScale = vector; break;
            }

            GUI.skin.label.alignment = TextAnchor.UpperLeft;
            GUILayout.EndHorizontal();
        }

        private void PlusMinusFloat(ref float f, float amount, bool multByTime)
        {
            if (GUILayout.RepeatButton("-", GUILayout.Width(30)))
            {
                f -= multByTime ? amount * Time.deltaTime : amount;
            }
            if (GUILayout.RepeatButton("+", GUILayout.Width(30)))
            {
                f += multByTime ? amount * Time.deltaTime : amount;
            }
        }

        private void CharacterCheats(Character player)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<color=lime>Teleport SELF to</color>"))
            {
                player.Teleport(m_object.transform.position, Quaternion.identity);
            }
            if (GUILayout.Button("<color=red>Teleport TO self</color>"))
            {
                var pos = player.transform.position + new Vector3(0f, 1f, 0f);
                if (m_object.GetComponent<Character>() is Character other)
                {
                    other.Teleport(pos, Quaternion.identity);
                }
                else if (m_object.GetComponent<Item>() is Item item)
                {
                    item.ChangeParent(item.transform.parent, pos);
                }
                else
                {
                    m_object.transform.position = pos;
                }
            }
            GUILayout.EndHorizontal();
        }
    }
}
