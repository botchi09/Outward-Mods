using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;
using UnityEngine.UI;

namespace Explorer_2
{
    public class ReflectionWindow : MenuManager.ExplorerWindow
    {
        public override string Name { get => "Object Reflection"; set => Name = value; }

        private object m_object;

        private List<FieldInfoHolder> m_FieldInfos;

        private bool m_autoUpdate = false;

        public override void Init()
        {
            m_object = Target;

            m_FieldInfos = new List<FieldInfoHolder>();
            GetFieldsRecursive(m_object.GetType());
            UpdateValues();
        }

        private void GetFieldsRecursive(Type type)
        {
            foreach (var fi in type.GetFields(At.flags))
            {
                m_FieldInfos.Add(FieldInfoHolder.ParseFieldInfo(type, fi));
            }
            if (type.BaseType != null)
            {
                GetFieldsRecursive(type.BaseType);
            }
        }

        internal void Update()
        {
            if (m_autoUpdate)
            {
                UpdateValues();
            }
        }

        private void UpdateValues()
        {
            foreach (var holder in this.m_FieldInfos)
            {
                holder.UpdateValue(m_object);
            }
        }

        public override void WindowFunction(int windowID)
        {
            try
            {
                Header();

                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                GUILayout.Label("<b>Type:</b> <color=cyan>" + m_object.GetType() + "</color>");
                if (m_object is Component comp && comp.gameObject is GameObject obj)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label("GameObject:");
                    if (GUILayout.Button("<color=#00FF00>" + obj.name + "</color>", GUILayout.MaxWidth(200)))
                    {
                        MenuManager.InspectGameObject(obj);
                    }
                    GUI.skin.label.alignment = TextAnchor.UpperLeft;
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (!m_autoUpdate && GUILayout.Button("Update values"))
                {
                    UpdateValues();
                }
                m_autoUpdate = GUILayout.Toggle(m_autoUpdate, "Auto-update values?");
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                scroll = GUILayout.BeginScrollView(scroll);

                GUILayout.Space(15);

                foreach (var holder in this.m_FieldInfos)
                {
                    GUILayout.BeginHorizontal();
                    holder.Draw(m_object);
                    GUILayout.EndHorizontal();
                }

                GUILayout.EndScrollView();
            }
            catch
            {
                Destroy(this);
                return;
            }
        }

        // ============ FIELDINFO HOLDERS ============

        public abstract class FieldInfoHolder
        {
            public Type classType;
            public FieldInfo fieldInfo;
            public object Value;

            public static FieldInfoHolder ParseFieldInfo(Type type, FieldInfo fi)
            {
                FieldInfoHolder holder;

                if (fi.FieldType.IsPrimitive || fi.FieldType == typeof(string))
                    holder = new PrimitiveHolder();
                else if (fi.FieldType == typeof(GameObject) || typeof(Transform).IsAssignableFrom(fi.FieldType))
                    holder = new GameObjectFieldHolder();
                else
                    holder = new UnsupportedHolder();

                // todo enum and list

                holder.classType = type;
                holder.fieldInfo = fi;
                return holder;
            }

            public abstract void UpdateValue(object obj);
            public abstract void SetValue(object value, object obj);
            public abstract void Draw(object obj);
        }

        public class PrimitiveHolder : FieldInfoHolder
        {
            private object m_value;

            public override void Draw(object obj)
            {
                if (fieldInfo.FieldType == typeof(bool))
                {
                    bool value = (bool)m_value;
                    var color = "<color=";
                    if (value) { color += "lime>"; } else { color += "red>"; }
                    value = GUILayout.Toggle(value, color + fieldInfo.Name + "</color>");
                    m_value = value;
                    SetValue(m_value, obj);
                }
                else
                {
                    GUILayout.Label("<color=cyan>" + fieldInfo.Name + ":</color>", GUILayout.Width(150));
                    m_value = GUILayout.TextField(m_value?.ToString() ?? "");

                    if (GUILayout.Button("<color=#00FF00>Apply</color>", GUILayout.Width(60)))
                    {
                        SetValue(m_value, obj);
                    }
                }
            }

            public override void SetValue(object _value, object obj)
            {
                if (fieldInfo.IsLiteral && !fieldInfo.IsInitOnly)
                {
                    Debug.LogWarning("You cannot change the value of a const, even with reflection!");
                    return;
                }

                if (fieldInfo.FieldType == typeof(string) || fieldInfo.FieldType == typeof(bool))
                {
                    Value = _value;
                }
                else if (fieldInfo.FieldType == typeof(float))
                {
                    if (float.TryParse(_value.ToString(), out float f))
                    {
                        Value = f;
                    }
                    else
                    {
                        Debug.LogWarning("Cannot parse " + _value.ToString() + " to a float!");
                    }
                }
                else if (fieldInfo.FieldType == typeof(double))
                {
                    if (double.TryParse(_value.ToString(), out double d))
                    {
                        Value = d;
                    }
                    else
                    {
                        Debug.LogWarning("Cannot parse " + _value.ToString() + " to a double!");
                    }
                }
                else
                {
                    if (int.TryParse(_value.ToString(), out int i))
                    {
                        Value = i;
                    }
                    else
                    {
                        Debug.LogWarning("Cannot parse " + _value.ToString() + " to an integer! type: " + fieldInfo.FieldType);
                    }
                }

                fieldInfo.SetValue(fieldInfo.IsStatic ? null : obj, Value);
            }

            public override void UpdateValue(object obj)
            {
                Value = fieldInfo.GetValue(fieldInfo.IsStatic ? null : obj);

                if (Value is bool)
                {
                    m_value = (bool)Value;
                }
                else
                {
                    m_value = Value?.ToString() ?? "";
                }
            }
        }

        public class GameObjectFieldHolder : FieldInfoHolder
        {
            private GameObject m_value;

            public override void Draw(object _obj)
            {
                GUILayout.Label("<color=cyan>" + fieldInfo.Name + ":</color>", GUILayout.Width(150));

                if (m_value == null)
                {
                    GUILayout.Label("<i><color=grey>null (" + fieldInfo.FieldType + ")</color></i>");
                }
                else
                {
                    bool enabled = m_value.activeInHierarchy;
                    bool children = m_value.transform.childCount > 0;
                    bool _static = false;

                    GUI.skin.button.alignment = TextAnchor.UpperLeft;

                    if (enabled)
                    {
                        if (m_value.GetComponent<MeshRenderer>() is MeshRenderer m && m.isPartOfStaticBatch)
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
                        label += "[" + m_value.transform.childCount + " children] ";

                    label += m_value.name;

                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button(label, new GUILayoutOption[] { GUILayout.Height(22), GUILayout.MaxWidth(350) }))
                    {
                        MenuManager.InspectGameObject(m_value);
                    }

                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUI.color = Color.white;
                }
            }

            public override void SetValue(object value, object obj)
            {
                // n/a
            }

            public override void UpdateValue(object obj)
            {
                Value = fieldInfo.GetValue(fieldInfo.IsStatic ? null : obj);

                if (Value != null)
                {
                    if (Value is GameObject go)
                    {
                        m_value = go;
                    }
                    else if (Value is Transform t)
                    {
                        m_value = t.gameObject;
                    }
                }
            }
        }

        public class EnumHolder : FieldInfoHolder
        {
            public override void Draw(object obj)
            {
                throw new NotImplementedException();
            }

            public override void SetValue(object value, object obj)
            {
                throw new NotImplementedException();
            }

            public override void UpdateValue(object obj)
            {
                throw new NotImplementedException();
            }
        }

        public class ListHolder : FieldInfoHolder
        {
            public override void Draw(object obj)
            {
                throw new NotImplementedException();
            }

            public override void SetValue(object value, object obj)
            {
                throw new NotImplementedException();
            }

            public override void UpdateValue(object obj)
            {
                throw new NotImplementedException();
            }
        }

        public class UnsupportedHolder : FieldInfoHolder
        {
            public override void Draw(object obj)
            {
                GUILayout.Label("<color=cyan>" + fieldInfo.Name + ":</color>", GUILayout.Width(150));

                if (Value != null)
                {
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("<color=yellow>" + fieldInfo.FieldType.ToString() + "</color>", GUILayout.MaxWidth(350)))
                    {
                        MenuManager.ReflectObject(Value);
                    }
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                }
                else
                {
                    GUILayout.Label("<i><color=grey>null (" + fieldInfo.FieldType + ")</color></i>");
                }
            }

            public override void SetValue(object value, object obj)
            {
                // n/a
            }

            public override void UpdateValue(object obj)
            {
                Value = fieldInfo.GetValue(fieldInfo.IsStatic ? null : obj);
            }
        }
    }
}
