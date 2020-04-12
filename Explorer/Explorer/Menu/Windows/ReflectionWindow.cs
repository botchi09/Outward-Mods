using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;
using UnityEngine.UI;

namespace Explorer
{
    public class ReflectionWindow : MenuManager.ExplorerWindow
    {
        public override string Name { get => "Object Reflection"; set => Name = value; }

        private object m_object;

        private string m_searchFilter = "";

        private List<FieldInfoHolder> m_FieldInfos;

        private bool m_autoUpdate = false;

        public override void Init()
        {
            m_object = Target;

            m_FieldInfos = new List<FieldInfoHolder>();
            GetFieldsRecursive(m_object.GetType());
            UpdateValues();
        }

        private void GetFieldsRecursive(Type type, List<string> names = null)
        {
            if (names == null)
            {
                names = new List<string>();
            }
            
            foreach (var fi in type.GetFields(At.flags))
            {
                if (names.Contains(fi.Name))
                {
                    continue;
                }
                names.Add(fi.Name);
                m_FieldInfos.Add(FieldInfoHolder.ParseFieldInfo(type, fi));
            }
            if (type.BaseType != null)
            {
                GetFieldsRecursive(type.BaseType, names);
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
                if (GUILayout.Button("Update values"))
                {
                    UpdateValues();
                }
                GUI.color = m_autoUpdate ? Color.green : Color.red;
                m_autoUpdate = GUILayout.Toggle(m_autoUpdate, "Auto-update values?");
                GUI.color = Color.white;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Filter field names:", GUILayout.Width(180));
                m_searchFilter = GUILayout.TextField(m_searchFilter);
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                scroll = GUILayout.BeginScrollView(scroll);

                GUILayout.Space(15);

                foreach (var holder in this.m_FieldInfos)
                {
                    if (m_searchFilter != "" && !holder.fieldInfo.Name.ToLower().Contains(m_searchFilter.ToLower())) 
                    {
                        continue;
                    }

                    GUILayout.BeginHorizontal(GUILayout.Height(25));
                    holder.Draw(m_object);
                    GUILayout.EndHorizontal();
                }

                GUILayout.EndScrollView();

                m_rect = MenuManager.ResizeWindow(m_rect, windowID);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Exception on window draw. Message: " + e.Message + "\r\nStack: " + e.StackTrace);
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
                {
                    holder = new PrimitiveHolder();
                }
                else if (fi.FieldType == typeof(GameObject) || typeof(Transform).IsAssignableFrom(fi.FieldType))
                {
                    holder = new GameObjectFieldHolder();
                }
                else if (fi.FieldType.IsEnum)
                {
                    holder = new EnumHolder();
                }
                else if (fi.FieldType.IsArray || IsList(fi.FieldType))
                {
                    holder = new ListHolder();
                }
                else
                {
                    holder = new UnsupportedHolder();
                }

                // todo enum and list

                holder.classType = type;
                holder.fieldInfo = fi;
                return holder;
            }

            private static bool IsList(Type t)
            {
                return t.IsGenericType && t.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
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
                    GUILayout.Label("<color=cyan>" + fieldInfo.Name + ":</color>", GUILayout.Width(180));

                    if (m_value.ToString().Length > 37)
                    {
                        m_value = GUILayout.TextArea(m_value?.ToString() ?? "", GUILayout.MaxWidth(350));
                    }
                    else
                    {
                        m_value = GUILayout.TextField(m_value?.ToString() ?? "", GUILayout.MaxWidth(350));
                    }

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
                GUILayout.Label("<color=cyan>" + fieldInfo.Name + ":</color>", GUILayout.Width(180));

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
                    if (GUILayout.Button(label, new GUILayoutOption[] { GUILayout.Height(22), GUILayout.MaxWidth(320) }))
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
            private bool m_init = false;

            private string[] m_values;
            private int m_selectedValue;

            public override void Draw(object obj)
            {
                GUILayout.Label("<color=cyan>" + fieldInfo.Name + ":</color>", GUILayout.Width(180));

                if (GUILayout.Button("<", GUILayout.Width(25)))
                {
                    if (m_selectedValue > 0)
                    {
                        m_selectedValue--;
                        SetValue(m_values[m_selectedValue], obj);
                    }
                }
                if (GUILayout.Button(">", GUILayout.Width(25)))
                {
                    if (m_selectedValue < m_values.Length - 1)
                    {
                        m_selectedValue++;
                        SetValue(m_values[m_selectedValue], obj);
                    }
                }

                GUILayout.Label("<color=lime>[" + m_selectedValue + "] " + m_values[m_selectedValue] + "</color>");

            }

            public override void SetValue(object value, object obj)
            {
                if (Enum.Parse(fieldInfo.FieldType, value.ToString()) is object enumValue && enumValue != null)
                {
                    fieldInfo.SetValue(obj, enumValue);
                }
            }

            public override void UpdateValue(object obj)
            {
                if (!m_init)
                {
                    Value = fieldInfo.GetValue(fieldInfo.IsStatic ? null : obj);
                    m_values = Enum.GetNames(fieldInfo.FieldType);
                    m_init = true;
                }

                m_selectedValue = m_values.IndexOf(Value.ToString());
            }
        }

        public class ListHolder : FieldInfoHolder
        {
            private Array m_array;

            public override void Draw(object obj)
            {
                GUILayout.Label("<color=cyan>" + fieldInfo.Name + ":</color>", GUILayout.Width(180));

                if (m_array == null || m_array.Length < 1)
                {
                    GUILayout.Label("<i><color=grey>null (" + fieldInfo.FieldType + ")</color></i>");
                }
                else
                {
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("<color=yellow>[" + m_array.Length + "] " + fieldInfo.FieldType + "</color>", GUILayout.MaxWidth(320)))
                    {
                        MenuManager.ReflectObject(Value);
                    }
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;

                    foreach (var entry in m_array)
                    {
                        // collapsing the BeginHorizontal called from ReflectionWindow.WindowFunction or previous array entry
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(180);

                        if (entry == null)
                        {
                            GUILayout.Label("<i><color=grey>null</color></i>");
                        }
                        else
                        {
                            var type = entry.GetType();
                            if (type.IsPrimitive || type == typeof(string))
                            {
                                GUILayout.Label(entry.ToString());
                            }
                            else
                            {
                                GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                                if (GUILayout.Button("<color=yellow>" + entry.ToString() + "</color>", GUILayout.MaxWidth(350)))
                                {
                                    MenuManager.ReflectObject(entry);
                                }
                                GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                            }
                        }
                    }
                }
            }

            public override void SetValue(object value, object obj)
            {
                // n/a
            }

            public override void UpdateValue(object obj)
            {
                var value = fieldInfo.GetValue(fieldInfo.IsStatic ? null : obj);

                if (value != null)
                {
                    if (value is Array)
                    {
                        m_array = value as Array;
                    }
                    else if (value is IList list)
                    {
                        m_array = list.Cast<object>().ToArray();
                    }
                }
            }
        }

        public class UnsupportedHolder : FieldInfoHolder
        {
            public override void Draw(object obj)
            {
                GUILayout.Label("<color=cyan>" + fieldInfo.Name + ":</color>", GUILayout.Width(180));

                if (Value != null)
                {
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("<color=yellow>" + Value.ToString() + "</color>", GUILayout.MaxWidth(320)))
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
