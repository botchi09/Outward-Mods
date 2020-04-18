using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Explorer
{
    public class ReflectionWindow : WindowManager.UIWindow
    {
        public override string Name { get => "Object Reflection"; set => Name = value; }

        public object m_object;
        private string m_searchFilter = "";
        private bool m_autoUpdate = false;

        private List<FieldInfoHolder> m_FieldInfos;
        private List<PropertyInfoHolder> m_PropertyInfos;

        public override void Init()
        {
            m_object = Target;

            m_FieldInfos = new List<FieldInfoHolder>();
            m_PropertyInfos = new List<PropertyInfoHolder>();

            GetFields(m_object.GetType());
            GetProperties(m_object.GetType());

            UpdateValues();
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

            foreach (var prop in this.m_PropertyInfos)
            {
                prop.UpdateValue(m_object);
            }
        }

        public override void WindowFunction(int windowID)
        {
            try
            {
                Header();

                GUILayout.BeginArea(new Rect(5, 25, m_rect.width - 10, m_rect.height - 35), GUI.skin.box);

                GUILayout.BeginHorizontal();
                GUILayout.Label("<b>Type:</b> <color=cyan>" + m_object.GetType() + "</color>");
                if (m_object is Component comp && comp.gameObject is GameObject obj)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label("GameObject:");
                    if (GUILayout.Button("<color=#00FF00>" + obj.name + "</color>", GUILayout.MaxWidth(m_rect.width - 350)))
                    {
                        WindowManager.InspectGameObject(obj, out bool _);
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
                GUILayout.Label("<b>Search:</b>", GUILayout.Width(75));
                m_searchFilter = GUILayout.TextField(m_searchFilter);
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                scroll = GUILayout.BeginScrollView(scroll);

                GUILayout.Space(15);

                GUILayout.Label("<size=18><b><color=gold>Fields</color></b></size>");

                foreach (var holder in this.m_FieldInfos)
                {
                    if (m_searchFilter != "" && !holder.fieldInfo.Name.ToLower().Contains(m_searchFilter.ToLower())) 
                    {
                        continue;
                    }

                    GUILayout.BeginHorizontal(GUILayout.Height(25));
                    holder.Draw(this);
                    GUILayout.EndHorizontal();
                }

                GUILayout.Label("<size=18><b><color=gold>Properties</color></b></size>");

                foreach (var holder in this.m_PropertyInfos)
                {
                    if (m_searchFilter != "" && !holder.propInfo.Name.ToLower().Contains(m_searchFilter.ToLower()))
                    {
                        continue;
                    }

                    GUILayout.BeginHorizontal(GUILayout.Height(25));
                    holder.Draw(this);
                    GUILayout.EndHorizontal();
                }

                GUILayout.EndScrollView();

                m_rect = WindowManager.ResizeWindow(m_rect, windowID);

                GUILayout.EndArea();
            }
            catch (Exception e)
            {
                Debug.LogWarning("Exception on window draw. Message: " + e.Message);
                DestroyWindow();
                return;
            }
        }

        private static bool IsList(Type t)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
        }


        private void GetProperties(Type type, List<string> names = null)
        {
            if (names == null)
            {
                names = new List<string>();
            }

            foreach (var pi in type.GetProperties(At.flags))
            {
                if (names.Contains(pi.Name))
                {
                    continue;
                }
                names.Add(pi.Name);

                var piHolder = new PropertyInfoHolder(type, pi);
                m_PropertyInfos.Add(piHolder);
            }
            if (type.BaseType != null)
            {
                GetProperties(type.BaseType, names);
            }
        }

        private void GetFields(Type type, List<string> names = null)
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

                var fiHolder = new FieldInfoHolder(type, fi);
                m_FieldInfos.Add(fiHolder);
            }
            if (type.BaseType != null)
            {
                GetFields(type.BaseType, names);
            }
        }

        // -------------- big ugly draw value function --------------

        public void DrawValue(ref object value, string valueType, string memberName, Action<object> setAction = null)
        {
            GUILayout.Label("<color=cyan>" + memberName + ":</color>", GUILayout.Width(180));

            if (value == null)
            {
                GUILayout.Label("<i>null (" + valueType + ")</i>");
            }
            else
            {
                if (value.GetType().IsPrimitive || value.GetType() == typeof(string))
                {
                    DrawPrimitive(ref value, setAction);
                }
                else if (value.GetType() == typeof(GameObject) || value.GetType() == typeof(Transform))
                {
                    GameObject go;
                    if (value.GetType() == typeof(Transform))
                    {
                        go = (value as Transform).gameObject;
                    }
                    else
                    {
                        go = (value as GameObject);
                    }

                    UIStyles.GameobjButton(go, null, false, m_rect.width - 250);
                }
                else if (value.GetType().IsEnum)
                {
                    if (setAction != null)
                    {
                        if (GUILayout.Button("<", GUILayout.Width(25)))
                        {
                            SetEnum(ref value, -1);
                            setAction.Invoke(this.m_object);
                        }
                        if (GUILayout.Button(">", GUILayout.Width(25)))
                        {
                            SetEnum(ref value, 1);
                            setAction.Invoke(this.m_object);
                        }
                    }

                    GUILayout.Label(value.ToString());

                }
                else if (value.GetType().IsArray || IsList(value.GetType()))
                {
                    Array m_array = null;

                    if (value is Array)
                    {
                        m_array = value as Array;
                    }
                    else if (value is IList list)
                    {
                        m_array = list.Cast<object>().ToArray();
                    }

                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("<color=yellow>[" + m_array.Length + "] " + valueType + "</color>", GUILayout.MaxWidth(m_rect.width - 230)))
                    {
                        WindowManager.ReflectObject(value, out bool _);
                    }
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;

                    foreach (var entry in m_array)
                    {
                        // collapsing the BeginHorizontal called from ReflectionWindow.WindowFunction or previous array entry
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(190);

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
                                if (GUILayout.Button("<color=yellow>" + entry.ToString() + "</color>", GUILayout.MaxWidth(m_rect.width - 230)))
                                {
                                    WindowManager.ReflectObject(entry, out bool _);
                                }
                                GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                            }
                        }
                    }
                }
                else
                {
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("<color=yellow>" + value.ToString() + "</color>", GUILayout.MaxWidth(m_rect.width - 230)))
                    {
                        WindowManager.ReflectObject(value, out bool _);
                    }
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                }
            }
        }

        // Helper for drawing primitive values (with Apply button)

        public void DrawPrimitive(ref object value, Action<object> setAction)
        {
            bool allowSet = setAction != null;

            if (value.GetType() == typeof(bool))
            {
                bool b = (bool)value;
                var color = "<color=";
                if (b) { color += "lime>"; } else { color += "red>"; }
                
                if (allowSet)
                {
                    b = GUILayout.Toggle(b, "", GUILayout.Width(20));
                }

                GUILayout.Label(color + b.ToString() + "</color>");

                if (allowSet && (bool)value != b)
                {
                    value = b;
                    setAction.Invoke(this.m_object);
                }
            }
            else
            {
                if (value.ToString().Length > 37)
                {
                    value = GUILayout.TextArea(value.ToString(), GUILayout.MaxWidth(m_rect.width - 260));
                }
                else
                {
                    value = GUILayout.TextField(value.ToString(), GUILayout.MaxWidth(m_rect.width - 260));
                }

                if (allowSet && GUILayout.Button("<color=#00FF00>Apply</color>", GUILayout.Width(60)))
                {
                    setAction.Invoke(this.m_object);
                }
            }
        }

        // Helper for setting an enum

        public void SetEnum(ref object value, int change)
        {
            var type = value.GetType();
            var names = Enum.GetNames(type).ToList();

            int newindex = names.IndexOf(value.ToString()) + change;

            if (newindex > 0 && newindex < names.Count - 1)
            {
                value = Enum.Parse(type, names[newindex]);
            }
        }

        /* *********************
         *   PROPERTYINFO HOLDER
        */

        public class PropertyInfoHolder
        {
            public Type classType;
            public PropertyInfo propInfo;
            public object m_value;

            public PropertyInfoHolder(Type _type, PropertyInfo _propInfo)
            {
                classType = _type;
                propInfo = _propInfo;
            }

            public void Draw(ReflectionWindow window)
            {
                window.DrawValue(ref m_value, propInfo.PropertyType.Name, propInfo.Name);
            }

            public void UpdateValue(object obj)
            {
                try
                {
                    m_value = this.propInfo.GetValue(obj, null);
                }
                catch { }
            }
        }


        /* *********************
         *   FIELDINFO HOLDER
        */

        public class FieldInfoHolder
        {
            public Type classType;
            public FieldInfo fieldInfo;
            public object m_value;

            public FieldInfoHolder(Type _type, FieldInfo _fieldInfo)
            {
                classType = _type;
                fieldInfo = _fieldInfo;
            }

            public void UpdateValue(object obj)
            {
                m_value = fieldInfo.GetValue(fieldInfo.IsStatic ? null : obj);
            }

            public void Draw(ReflectionWindow window)
            {
                bool canSet = !(fieldInfo.IsLiteral && !fieldInfo.IsInitOnly);

                if (canSet)
                {
                    window.DrawValue(ref m_value, fieldInfo.FieldType.Name, fieldInfo.Name, SetValue);
                }
                else
                {
                    window.DrawValue(ref m_value, fieldInfo.FieldType.Name, fieldInfo.Name);
                }
            }

            public void SetValue(object obj)
            {
                if (fieldInfo.IsLiteral && !fieldInfo.IsInitOnly)
                {
                    Debug.LogWarning("You cannot change the value of a const!");
                    return;
                }

                if (fieldInfo.FieldType.IsPrimitive || fieldInfo.FieldType == typeof(string))
                {
                    if (fieldInfo.FieldType == typeof(string) || fieldInfo.FieldType == typeof(bool))
                    {
                    }
                    else if (fieldInfo.FieldType == typeof(float))
                    {
                        if (float.TryParse(m_value.ToString(), out float f))
                        {
                            m_value = f;
                        }
                        else
                        {
                            Debug.LogWarning("Cannot parse " + m_value.ToString() + " to a float!");
                        }
                    }
                    else if (fieldInfo.FieldType == typeof(double))
                    {
                        if (double.TryParse(m_value.ToString(), out double d))
                        {
                            m_value = d;
                        }
                        else
                        {
                            Debug.LogWarning("Cannot parse " + m_value.ToString() + " to a double!");
                        }
                    }
                    else
                    {
                        if (int.TryParse(m_value.ToString(), out int i))
                        {
                            m_value = i;
                        }
                        else
                        {
                            Debug.LogWarning("Cannot parse " + m_value.ToString() + " to an integer! type: " + fieldInfo.FieldType);
                        }
                    }
                }
                else if (fieldInfo.FieldType.IsEnum)
                {
                    if (Enum.Parse(fieldInfo.FieldType, m_value.ToString()) is object enumValue && enumValue != null)
                    {
                        m_value = enumValue;
                    }
                }

                fieldInfo.SetValue(fieldInfo.IsStatic ? null : obj, m_value);
            }
        }
    }
}
