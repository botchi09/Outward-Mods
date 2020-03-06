using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using OutwardExplorer;
using UnityEngine.SceneManagement;

/*
 * THIS MOD DESPERATELY NEEDS A REWORK / CLEANUP. VERY OLD. 
*/

namespace OutwardExplorer
{
    public class ExplorerScript : MonoBehaviour
    {
        public static ExplorerScript Instance;

        public Assembly asm;

        // =============== explorer =================
        public Transform explorerTransform; // scene viewer
        public List<Transform> explorerPath;
        public GameObject explorerComponentsObject; // explorer component list

        //=============== prefab manager ===============
        public Dictionary<string, GameObject> allPrefabs;
        public bool prefabsLoaded;
        public GameObject currentPrefab = null;
        public GameObject currentPrefabChild = null;

        // ============== object inspector ===========
        public object inspectorObject; // component inspector
        public List<object> inspectorPath;
        public string inspectorJsonDump = "";
        public string currentJsonPath = @"Mods\Dump.json";
        public bool inspectingPrefab = false;
        public Dictionary<string, KeyValuePair<FieldInfo, Type>> inspectorFields;
        public Dictionary<string, string> inspectorEdits;

        // quest helper
        public Dictionary<string, QuestEventSignature> questEvents;
        public List<SendQuestEventInteraction> questInteractions;

        internal void Awake()
        {
            Instance = this;
            SceneManager.sceneLoaded += OnSceneChange;

            Application.logMessageReceived += Application_logMessageReceived;
        }

        private void Application_logMessageReceived(string condition, string stackTrace, LogType type)
        {
            // useless spam errors (unity warnings)
            string[] blacklist = new string[]
            {
                "Internal: JobTempAlloc",
                "GUI Error:",
                "BoxColliders does not support negative scale or size",
                "is registered with more than one LODGroup",
                "only 0 controls when doing Repaint",
                "it is not close enough to the NavMesh"
            };

            foreach (string s in blacklist)
            {
                if (condition.ToLower().Contains(s.ToLower()))
                {
                    return;
                }
            }

            if (type == LogType.Exception && !condition.Contains("Repaint"))
            {
                OLogger.Error(condition + "\r\nStack Trace: " + stackTrace);
            }
            else if (type == LogType.Warning)
            {
                OLogger.Warning(condition);
            }
            else
            {
                OLogger.Log(condition);
            }
        }

        internal void Start()
        {
            var m_window = new Vector2(600, 260);
            OLogger.CreateLog(new Rect(Screen.width - m_window.x - 5, Screen.height - m_window.y - 5, m_window.x, m_window.y));

            asm = Assembly.GetExecutingAssembly();

            // explorer
            explorerPath = new List<Transform>();
            inspectorPath = new List<object>();

            // prefab explorer
            allPrefabs = new Dictionary<string, GameObject>();

            //// misc tools
            questEvents = new Dictionary<string, QuestEventSignature>();
            questInteractions = new List<SendQuestEventInteraction>();

            ExplorerGUIHelper.Instance.miscEdits = new string[2];
            for (int i = 0; i < ExplorerGUIHelper.Instance.miscEdits.Length; i++)
                ExplorerGUIHelper.Instance.miscEdits[i] = "";

            inspectorEdits = new Dictionary<string, string>();

            ExplorerGUIHelper.Instance.objectTransformEdits = new string[5]; // x/y/z/rot/scale
            for (int i = 0; i < ExplorerGUIHelper.Instance.objectTransformEdits.Length; i++)
                ExplorerGUIHelper.Instance.objectTransformEdits[i] = "";

            // some debug hooks for quests
            On.QuestEventDictionary.Load += QuestLoad;
            On.SendQuestEventInteraction.OnActivate += SendQuestInteractionHook;
            On.NodeCanvas.Tasks.Actions.SendQuestEvent.OnExecute += SendQuestEventHook;

            OLogger.Log("Initialised Explorer. Unity version: " + Application.unityVersion.ToString());
        }

        private void OnSceneChange(Scene scene, LoadSceneMode mode)
        {
            Reset();
        }

        public void Reset()
        {
            explorerComponentsObject = null;
            explorerTransform = null;
            inspectorObject = null;
            currentPrefabChild = null;
            currentPrefab = null;
            inspectingPrefab = false;
        }

        private void QuestLoad(On.QuestEventDictionary.orig_Load orig)
        {
            orig();

            Type t = typeof(QuestEventDictionary);
            FieldInfo fi = t.GetField("m_questEvents", BindingFlags.Static | BindingFlags.NonPublic);
            if (fi.GetValue(null) is Dictionary<string, QuestEventSignature> m_questEvents)
            {
                foreach (QuestEventSignature sig in m_questEvents.Values)
                {
                    if (questEvents.ContainsKey(sig.EventName)) { continue; }
                    questEvents.Add(sig.EventName, sig);
                }
            }
        }

        private void SendQuestInteractionHook(On.SendQuestEventInteraction.orig_OnActivate orig, SendQuestEventInteraction self)
        {
            var _ref = GetValue(typeof(SendQuestEventInteraction), self, "m_questReference") as QuestEventReference;
            var _event = _ref.Event;
            var s = _ref.EventUID;

            if (_event != null && s != null)
            {
                Debug.LogWarning(
                    "------ ADDING QUEST EVENT (trigger) -------" +
                    "\r\nName: " + _event.EventName +
                    "\r\nDescription: " + _event.Description +
                    "\r\n--------------------------------------");
            }

            orig(self);
        }

        private void SendQuestEventHook(On.NodeCanvas.Tasks.Actions.SendQuestEvent.orig_OnExecute orig, NodeCanvas.Tasks.Actions.SendQuestEvent self)
        {
            var _event = self.QuestEventRef.Event;
            var s = self.QuestEventRef.EventUID;

            if (_event != null && s != null)
            {
                Debug.LogWarning(
                    "------ ADDING QUEST EVENT -------" +
                    "\r\nName: " + _event.EventName +
                    "\r\nDescription: " + _event.Description +
                    "\r\nStack: " + self.StackAmount +
                    "\r\n---------------------------");
            }

            orig(self);            
        }

        // On update
        internal void Update()
        {
            if (Input.GetKeyUp(KeyCode.F7))
            {
                ExplorerGUIHelper.Instance.ShowGui = !ExplorerGUIHelper.Instance.ShowGui;
            }

            if (!prefabsLoaded && ResourcesPrefabManager.Instance.Loaded)
            {
                prefabsLoaded = true;

                for (int i = 0; i < ResourcesPrefabManager.AllPrefabs.Count; i++)
                {
                    if (ResourcesPrefabManager.AllPrefabs[i] is GameObject gameObject)
                    {
                        if (gameObject.GetComponent<Item>() is Item item)
                        {
                            allPrefabs.Add(item.Name + " (" + item.gameObject.name + ")", gameObject);
                        }
                        else
                        {
                            allPrefabs.Add(gameObject.name, gameObject);
                        }
                    }
                }

                UnityEngine.Object[] array = Resources.LoadAll("_StatusEffects", typeof(EffectPreset));
                if (array != null && array.Length > 0)
                {
                    foreach (EffectPreset effect in array)
                    {
                        allPrefabs.Add(effect.name, effect.gameObject);
                    }
                }
            }

            if ((NetworkLevelLoader.Instance.InLoading
                || MenuManager.Instance.InFade
                || MenuManager.Instance.IsMasterLoadingDisplayed)
                && !MenuManager.Instance.IsInMainMenuScene)
            {
                explorerPath.Clear();
                explorerTransform = null;
                explorerComponentsObject = null;
                if (inspectingPrefab == false)
                {
                    inspectorObject = null;
                }
                ExplorerGUIHelper.Instance.tempHideGui = true;
                return;
            }
            else
            {
                ExplorerGUIHelper.Instance.tempHideGui = false;
            }
        }

        // =============================== INSPECTOR WINDOW ================================
        // The inspector window, which uses recursive Reflection and JSONutility.

        // Reflection will allow you to write to primitive fields (int, float, bool, string, etc)

        // JSONUtility allows you to dump a Mono component to .json, and try to overwrite a component with FromJsonOverwrite. 
        // The overwrite should ignore missing or invalid fields.
        // However, JsonUtility cannot get private or protected fields unless they have a [Serializable] flag. Most do not.

        internal void InspectorWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, ExplorerGUIHelper.Instance.m_inspectorRect.width - 20, 20));

            if (inspectorObject == null) // || (inspectorJsonDump == "" && jsonObjects.Count == 0 && jsonStatus.Count == 0 && jsonTextures.Count == 0))
            {
                GUI.color = ExplorerGUIHelper.Instance.lightRed;
                GUILayout.Label("No component!");
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                return;
            }

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("X", GUILayout.Width(60)))
            {
                ExplorerGUIHelper.Instance.showInspector = false;
                inspectorObject = null;
                inspectorPath.Clear();
                return;
            }

            GUILayout.Label(inspectorObject.GetType() + " (" + inspectorObject + ")", new GUILayoutOption[] { GUILayout.Width(ExplorerGUIHelper.Instance.m_windowRect.width - 80), GUILayout.Height(20) });

            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.BeginHorizontal();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (inspectorPath.Count > 1)
            {
                if (GUILayout.Button("<- ", GUILayout.Width(50)))
                {
                    int count = inspectorPath.Count() - 1;

                    object newobj = inspectorPath.ElementAt(count - 1);
                    SetInspectorObject(newobj);

                    List<object> newlist = new List<object>();
                    for (int i = 0; i < count; i++)
                    {
                        newlist.Add(inspectorPath[i]);
                    }
                    inspectorPath = newlist;
                }
                string s = "";
                foreach (object o in inspectorPath)
                {
                    s += o.ToString() + " / ";
                }
                GUILayout.TextField(s, GUILayout.Width(ExplorerGUIHelper.Instance.m_windowRect.width - 80));
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(15);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reflection", GUILayout.Width(100)))
            {
                ExplorerGUIHelper.Instance.inspectorPage = 0;
            }
            if (GUILayout.Button("JSON Utility", GUILayout.Width(100)))
            {
                ExplorerGUIHelper.Instance.inspectorPage = 1;
            }
            GUILayout.EndHorizontal();

            if (ExplorerGUIHelper.Instance.inspectorPage == 0)
            {
                ExplorerGUIHelper.Instance.rect2scroll = GUILayout.BeginScrollView(ExplorerGUIHelper.Instance.rect2scroll, new GUILayoutOption[] { GUILayout.Width(730) });

                ReflectionPage(inspectorObject);

                GUILayout.EndScrollView();
            }
            else if (ExplorerGUIHelper.Instance.inspectorPage == 1)
            {
                GUIJsonDump();
            }

            GUI.skin.label.alignment = TextAnchor.UpperLeft;
            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
            GUI.skin.button.wordWrap = true;
            GUILayout.EndVertical();
        }


        // gui-driven object dumper
        private void ReflectionPage(object objToDump)
        {
            foreach (KeyValuePair<FieldInfo,Type> entry in inspectorFields.Values)
            {
                //Debug.Log(fi.Name);

                GUILayout.BeginHorizontal(GUI.skin.box, GUILayout.Width(650));

                GUILayout.Label(entry.Key.Name, GUILayout.Width(150));

                Type valueType = entry.Key.FieldType;
                bool isNull = false;
                object value = null;

                if (entry.Key.GetValue(objToDump) != null)
                {
                    value = entry.Key.GetValue(objToDump);
                    GUI.color = ExplorerGUIHelper.Instance.lightGreen;
                }
                else
                {
                    isNull = true;
                    GUI.color = Color.red;
                    GUILayout.Label("null (" + valueType.ToString() + ")");
                }

                GUILayout.BeginVertical();

                GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                GUI.skin.button.wordWrap = false;

                // non-system types
                if (!isNull && (valueType.Namespace == null || !valueType.Namespace.StartsWith("System")))
                {
                    UnityField(value, valueType, entry.Key);
                }
                // dictionaries
                else if (!isNull && (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(Dictionary<,>) && value is IDictionary dict))
                {
                    if (dict.Keys.Count <= 0)
                    {
                        GUILayout.Label("Dictionary (empty)");
                    }
                    else
                    {
                        GUILayout.BeginVertical();

                        GUI.color = Color.white;
                        UnsupportedFieldButton(value);
                        GUI.color = Color.green;

                        //Type dKeyType = valueType.GetGenericArguments()[0];
                        //Type dValueType = valueType.GetGenericArguments()[1];

                        //bool flag = false;
                        //foreach (KeyValuePair<object, object> dEntry in dict)
                        //{
                        //    if (dEntry.Key == null || dEntry.Value == null)
                        //        continue;

                        //    flag = true;

                        //    // GUILayout.Label(dEntry.Value.ToString());

                        //    //if (dEntry.Value.GetType().IsPrimitive || dEntry.Value.GetType() == typeof(string)) // is primitive or string
                        //    //{
                        //    //    // DisplayPrimitive(entry.Value, ref EditID);
                        //    //    string sValue = "";
                        //    //    if (dEntry.Value != null)
                        //    //        sValue = dEntry.Value.ToString();
                        //    //    GUILayout.Label(sValue);
                        //    //}
                        //    //else
                        //    //{
                        //    //    UnsupportedFieldButton(dEntry.Value, dEntry.Value.ToString());
                        //    //}
                        //}
                        //if (!flag)
                        //{
                        //    GUI.color = Color.red;
                        //    GUILayout.Label("No entries");
                        //}

                        GUILayout.EndVertical();
                    }
                }
                // lists
                else if (!isNull && valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(List<>) && value is IList list)
                {
                    GUILayout.BeginVertical();

                    GUI.color = Color.white;
                    UnsupportedFieldButton(value);
                    GUI.color = Color.green;

                    bool flag = false;

                    foreach (object lValue in list)
                    {
                        if (lValue == null)
                            continue;

                        flag = true;

                        if (lValue.GetType().IsPrimitive || lValue.GetType() == typeof(string))
                        {
                            // DisplayPrimitive(lValue, ref EditID);
                            string sValue = "";
                            if (value != null)
                                sValue = lValue.ToString();
                            GUILayout.Label(sValue);
                        }
                        else
                        {
                            UnsupportedFieldButton(lValue);
                        }
                    }
                    if (!flag)
                    {
                        GUI.color = Color.red;
                        GUILayout.Label("No entries");
                    }
                    GUILayout.EndVertical();
                }
                // other arrays (generic[])
                else if (!isNull && valueType.IsArray)
                {
                    GUILayout.BeginVertical();
                    Array array = (Array)value;

                    GUI.color = Color.white;
                    UnsupportedFieldButton(value);
                    GUI.color = Color.green;

                    bool flag = false;

                    foreach (object listObj in array)
                    {
                        if (listObj == null)
                            continue;

                        flag = true;

                        if (listObj.GetType().IsPrimitive || listObj.GetType() == typeof(string))
                        {
                            // DisplayPrimitive(listObj, ref EditID);
                            string sValue = "";
                            if (listObj != null)
                                sValue = listObj.ToString();
                            GUILayout.Label(sValue);
                        }
                        else
                        {
                            UnsupportedFieldButton(listObj);
                        }
                    }
                    if (!flag)
                    {
                        GUI.color = Color.red;
                        GUILayout.Label("No entries");
                    }
                    GUILayout.EndVertical();
                }
                // display primitive values for editing
                else if (valueType.IsPrimitive || valueType == typeof(string))
                {
                    DisplayPrimitive(entry.Key);
                }

                GUILayout.EndVertical();

                GUI.color = Color.white;
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
            }
        }

        private void UnityField(object value, Type valueType, FieldInfo fi)
        {
            if (value is Vector2 || value is Vector3 || value is Quaternion)
            {
                var x = GetValue(valueType, value, "x");
                var y = GetValue(valueType, value, "y");
                string label = "X: " + x + ", Y: " + y;
                object z = null;
                try { z = GetValue(valueType, value, "z"); } catch { };
                if (z != null)
                {
                    label += ", Z: " + z;
                }
                object w = null;
                try { w = GetValue(valueType, value, "w"); } catch { };
                if (w != null)
                {
                    label += ", W: " + w;
                }
                GUILayout.Label(label);
            }
            else if (value.GetType().IsArray && (Array)value is Array array)
            {
                GUILayout.BeginVertical();

                GUI.color = Color.white;
                GUILayout.Label("Array: " + array.GetType());
                GUI.color = Color.green;

                bool flag = false;

                foreach (object listObj in array)
                {
                    if (listObj == null)
                        continue;

                    flag = true;

                    if (listObj.GetType().IsPrimitive || listObj.GetType() == typeof(string))
                    {
                        string sValue = "";
                        if (listObj != null)
                            sValue = listObj.ToString();
                        GUILayout.Label(sValue);
                    }
                    else
                    {
                        UnsupportedFieldButton(listObj);
                    }
                }
                if (!flag)
                {
                    GUI.color = Color.red;
                    GUILayout.Label("No entries");
                }
                GUILayout.EndVertical();
            }
            else if (value.GetType().IsEnum && value is Enum _enum)
            {
                object underlyingValue = Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType()));

                if (underlyingValue.GetType() == typeof(int))
                {
                    EnumEdit((int)underlyingValue, fi, _enum);
                }
                else
                {
                    UnsupportedFieldButton(value);
                }
            }
            else
            {
                UnsupportedFieldButton(value);
            }
        }

        private void EnumEdit(int intValue, FieldInfo field, Enum _enum)
        {
            GUILayout.BeginHorizontal();

            GUILayout.Label(intValue.ToString(), GUILayout.Width(30));

            string[] names = Enum.GetNames(_enum.GetType());
            int enumCount = names.Count();
            var currentObject = Enum.ToObject(_enum.GetType(), intValue);

            if (!(intValue <= 0))
            {
                if (GUILayout.Button("<", GUILayout.Width(20)))
                {
                    int i = intValue - 1;

                    if (Enum.ToObject(_enum.GetType(), i) is object obj)
                    {
                        SetValue(obj, inspectorFields[field.Name].Value, inspectorObject, field.Name);
                    }
                }
            }

            if (!(intValue > (enumCount - 1)))
            {
                if (GUILayout.Button(">", GUILayout.Width(20)))
                {
                    int i = intValue + 1;

                    if (Enum.ToObject(_enum.GetType(), i) is object obj)
                    {
                        SetValue(obj, inspectorFields[field.Name].Value, inspectorObject, field.Name);
                    }
                }
            }

            GUILayout.Label(Enum.GetName(_enum.GetType(), currentObject));

            GUILayout.EndHorizontal();
        }

        private void DisplayPrimitive(FieldInfo field)
        {
            GUILayout.BeginHorizontal();

            //string sValue = "";
            //if (value != null)
            //    sValue = value.ToString();
            //GUILayout.Label(sValue, GUILayout.Width(145));

            GUI.color = Color.white;

            if (inspectorEdits.ContainsKey(field.Name))
            {
                if (field.FieldType == typeof(bool))
                {
                    bool b = false;
                    if (inspectorObject != null
                        && GetValue(inspectorFields[field.Name].Value, inspectorObject, field.Name) is bool b2)
                    {
                        b = b2;
                    }
                    b = GUILayout.Toggle(b, b.ToString());
                    SetValue(b, inspectorFields[field.Name].Value, inspectorObject, field.Name);
                }
                else
                {
                    if (field.FieldType == typeof(string) && inspectorEdits[field.Name].Length > 20)
                    {
                        inspectorEdits[field.Name] = GUILayout.TextArea(inspectorEdits[field.Name]);
                    }
                    else
                    {
                        inspectorEdits[field.Name] = GUILayout.TextField(inspectorEdits[field.Name], GUILayout.Width(135));
                    }

                    if (GUILayout.Button("Apply", GUILayout.Width(50)))
                    {
                        if (field.FieldType == typeof(string))
                        {
                            SetValue(inspectorEdits[field.Name], inspectorFields[field.Name].Value, inspectorObject, field.Name);
                        }
                        else if (field.FieldType == typeof(int))
                        {
                            if (int.TryParse(inspectorEdits[field.Name], out int i))
                            {
                                SetValue(i, inspectorFields[field.Name].Value, inspectorObject, field.Name);
                            }
                        }
                        else if (field.FieldType == typeof(float))
                        {
                            if (Single.TryParse(inspectorEdits[field.Name], out float f))
                            {
                                SetValue(f, inspectorFields[field.Name].Value, inspectorObject, field.Name);
                            }
                        }
                        else
                        {
                            Debug.Log(field.FieldType + " not supported?");
                        }
                    }
                }
            }

            GUILayout.EndHorizontal();
        }

        private void GetFieldsRecursive(Type type, ref List<FieldInfo> fields, object obj)
        {
            FieldInfo[] fiA = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            if (fiA != null)
            {
                foreach (FieldInfo fi in fiA)
                {
                    fields.Add(fi);

                    if (!inspectorFields.ContainsKey(fi.Name))
                    {
                        inspectorFields.Add(fi.Name, new KeyValuePair<FieldInfo, Type>(fi, type));
                    }

                    if (inspectorEdits.ContainsKey(fi.Name))
                    {
                        continue;
                    }

                    if (fi.FieldType != null && (fi.FieldType.IsPrimitive || fi.FieldType == typeof(string)))
                    {
                        if (fi.GetValue(obj) != null)
                        {
                            inspectorEdits.Add(fi.Name, fi.GetValue(obj).ToString());
                        }
                        else
                        {
                            inspectorEdits.Add(fi.Name, "");
                        }
                    }
                }
            }
            if (type.BaseType != null)  // go deeper
            {
                GetFieldsRecursive(type.BaseType, ref fields, obj);
            }
        }

        private void UnsupportedFieldButton(object value)
        {
            string label = value.ToString();

            GUILayout.BeginHorizontal();

            if (value is MonoBehaviour mono)
            {
                if (GUILayout.Button("< Explore"))
                {
                    try
                    {
                        explorerTransform = mono.transform;
                        explorerComponentsObject = mono.gameObject;
                    }
                    catch (Exception ex)
                    {
                        Debug.Log(ex.Message);
                    }
                }
            }
            else if (value is GameObject go)
            {
                if (GUILayout.Button("< Explore"))
                {
                    try
                    {
                        explorerTransform = go.transform;
                        explorerComponentsObject = go;
                    }
                    catch (Exception ex)
                    {
                        Debug.Log(ex.Message);
                    }
                }
            }
            else if (value is Transform t)
            {
                if (GUILayout.Button("< Explore"))
                {
                    try
                    {
                        explorerTransform = t;
                        explorerComponentsObject = t.gameObject;
                    }
                    catch (Exception ex)
                    {
                        Debug.Log(ex.Message);
                    }
                }

            }

            if (GUILayout.Button(label, GUILayout.Width(520)))
            {
                SetInspectorObject(value);
                return;
            }
            GUILayout.EndHorizontal();
        }

        private void GUIJsonDump()
        {
            GUI.skin.label.fontSize = 15;
            GUILayout.Label("JsonUtility Dump:");
            GUI.skin.label.fontSize = 13;

            GUILayout.BeginHorizontal();
            GUILayout.Label("Output path:");
            currentJsonPath = GUILayout.TextField(currentJsonPath, GUILayout.Width(300));
            if (GUILayout.Button("Dump .json") && inspectorJsonDump.Length > 0)
            {
                try
                {
                    File.WriteAllText(currentJsonPath, inspectorJsonDump);
                }
                catch
                {
                    Debug.LogError("Error! Make sure directory exists");
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            ExplorerGUIHelper.Instance.rect2scroll = GUILayout.BeginScrollView(ExplorerGUIHelper.Instance.rect2scroll);

            if (inspectorJsonDump.Length > 0)
            {
                inspectorJsonDump = GUILayout.TextArea(inspectorJsonDump, GUILayout.MaxWidth(ExplorerGUIHelper.Instance.m_windowRect.width - 30));
            }
            GUILayout.EndScrollView();
            GUILayout.Space(10);

            if (GUILayout.Button("Try Apply .json to Component") && inspectorJsonDump.Length > 0)
            {
                try
                {
                    JsonUtility.FromJsonOverwrite(inspectorJsonDump, inspectorObject);
                    Debug.Log("JSON operation finished!");
                }
                catch
                {
                    Debug.LogError("Failed! Invalid .json, or cannot deserialize to this class type");
                    //currentJsonDump = lastExceptionString;
                }
            }
        }

        public int componentPage = 0;

        public void ListComponents(GameObject obj, bool isPrefab)
        {
            if (obj == null) { GUI.color = ExplorerGUIHelper.Instance.lightRed; GUILayout.Label("No GameObject selected!"); GUI.color = Color.white; return; };

            GUILayout.BeginHorizontal();
            if (componentPage == 0)
                GUI.color = ExplorerGUIHelper.Instance.lightGreen;
            if (GUILayout.Button("Components"))
            {
                componentPage = 0;
            }
            GUI.color = Color.white;
            if (componentPage == 1)
                GUI.color = ExplorerGUIHelper.Instance.lightGreen;
            if (GUILayout.Button("Tools"))
            {
                componentPage = 1;
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            // --------------------component viewer------------------------
            GUILayout.BeginVertical(GUI.skin.box, new GUILayoutOption[] { GUILayout.MaxHeight(240), GUILayout.Height(240) });

            GUILayout.BeginHorizontal();

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            GUI.color = ExplorerGUIHelper.Instance.lightRed;
            if (isPrefab && currentPrefabChild != null)
            {
                if (GUILayout.Button("X", GUILayout.Width(30)))
                {
                    currentPrefabChild = null;
                }
            }
            else if (!isPrefab)
            {
                if (GUILayout.Button("X", GUILayout.Width(30)))
                {
                    explorerComponentsObject = null;
                }
            }
            GUI.color = Color.white;

            GUILayout.TextArea(obj.name, GUILayout.Width(310));

            bool isEnabled = obj.GetActive();
            GUI.skin.label.alignment = TextAnchor.UpperRight;
            if (isEnabled)
            {
                GUI.color = ExplorerGUIHelper.Instance.lightGreen;
                if (GUILayout.Button("Try Disable"))
                {
                    obj.SetActive(false);
                }
            }
            else
            {
                GUI.color = ExplorerGUIHelper.Instance.lightRed;
                if (GUILayout.Button("Try Enable"))
                {
                    obj.SetActive(true);
                }
            }

            GUI.color = ExplorerGUIHelper.Instance.lightRed;
            if (GUILayout.Button("Destroy"))
            {
                DestroyImmediate(obj);
            }
            GUI.color = Color.white;

            GUI.skin.label.alignment = TextAnchor.UpperLeft;

            GUILayout.EndHorizontal();

            ExplorerGUIHelper.Instance.scroll3 = GUILayout.BeginScrollView(ExplorerGUIHelper.Instance.scroll3, GUILayout.Height(230));

            // component list

            if (componentPage == 0)
            {
                GUILayout.BeginVertical(GUI.skin.box);

                var components = obj.GetComponents<Component>();
                if (components != null && components.Count() > 0)
                {
                    GUILayout.Label("Components:");
                    foreach (Component c in components)
                    {
                        GUILayout.BeginHorizontal();

                        GUI.color = ExplorerGUIHelper.Instance.lightGreen;
                        if (GUILayout.Button("Inspect Object"))
                        {
                            if (isPrefab)
                            {
                                inspectingPrefab = true;
                            }
                            else
                            {
                                inspectingPrefab = false;
                            }
                            inspectorPath.Clear();
                            SetInspectorObject(c);
                        }
                        GUI.color = Color.white;

                        GUILayout.Label(c.GetType().ToString(), GUILayout.Width(320));

                        GUI.color = ExplorerGUIHelper.Instance.lightRed;
                        if (GUILayout.Button("Remove", GUILayout.Width(60)))
                        {
                            Destroy(c);
                        }
                        GUI.color = Color.white;
                        GUILayout.EndHorizontal();
                    }

                }
                GUILayout.EndVertical();

            }
            else if (componentPage == 1)
            {
                if (!isPrefab && explorerComponentsObject != null)
                {
                    GUILayout.Space(15);

                    GUILayout.BeginHorizontal();

                    if (GUILayout.Button("Clone", GUILayout.Width(70)))
                    {
                        Instantiate(obj, obj.transform.parent);
                    }

                    if (GUILayout.Button("Detach parent", GUILayout.Width(100)))
                    {
                        obj.transform.parent = null;
                    }

                    ExplorerGUIHelper.Instance.teleToSelf = GUILayout.Toggle(ExplorerGUIHelper.Instance.teleToSelf, "Tele to self?", GUILayout.Width(90));

                    string text = "Tele ";
                    if (ExplorerGUIHelper.Instance.teleToSelf)
                    {
                        text += "to self";
                        GUI.color = ExplorerGUIHelper.Instance.lightRed;
                    }
                    else
                    {
                        text += "self to";
                        GUI.color = ExplorerGUIHelper.Instance.lightGreen;
                    }
                    if (GUILayout.Button(text, GUILayout.Width(80)))
                    {
                        ExplorerGUIHelper.Instance.Teleport(obj.transform);
                    }
                    GUI.color = Color.white;

                    if (obj.GetComponent<Character>() is Character character)
                    {
                        if (GUILayout.Button("Try Resurrect"))
                        {
                            character.Resurrect();
                        }
                    }

                    GUILayout.EndHorizontal();

                    GUILayout.Space(10);

                    GUILayout.Label("Transform:");

                    GUILayout.BeginHorizontal();

                    if (ExplorerGUIHelper.Instance.objectTransformEdits[0] == "")
                    {
                        ExplorerGUIHelper.Instance.objectTransformEdits[0] = explorerComponentsObject.transform.position.x.ToString();
                        ExplorerGUIHelper.Instance.objectTransformEdits[1] = explorerComponentsObject.transform.position.y.ToString();
                        ExplorerGUIHelper.Instance.objectTransformEdits[2] = explorerComponentsObject.transform.position.z.ToString();
                        ExplorerGUIHelper.Instance.objectTransformEdits[3] = explorerComponentsObject.transform.localScale.x.ToString();
                    }
                    //if (gui.objectTransformEdits[3] == "") { gui.objectTransformEdits[3] = "50.0"; };

                    GUI.skin.label.alignment = TextAnchor.UpperRight;
                    GUILayout.Label("X:", GUILayout.Width(20));
                    TranslateButtons(0);
                    ExplorerGUIHelper.Instance.objectTransformEdits[0] = GUILayout.TextField(ExplorerGUIHelper.Instance.objectTransformEdits[0], GUILayout.Width(60));

                    GUILayout.Label("Y:", GUILayout.Width(20));
                    TranslateButtons(1);
                    ExplorerGUIHelper.Instance.objectTransformEdits[1] = GUILayout.TextField(ExplorerGUIHelper.Instance.objectTransformEdits[1], GUILayout.Width(60));

                    GUILayout.Label("Z:", GUILayout.Width(20));
                    TranslateButtons(2);
                    ExplorerGUIHelper.Instance.objectTransformEdits[2] = GUILayout.TextField(ExplorerGUIHelper.Instance.objectTransformEdits[2], GUILayout.Width(60));

                    //GUILayout.Label("Scale:", GUILayout.Width(40));
                    //ExplorerGUIHelper.Instance.objectTransformEdits[4] = GUILayout.TextField(ExplorerGUIHelper.Instance.objectTransformEdits[4], GUILayout.Width(60));

                    GUI.skin.label.alignment = TextAnchor.UpperLeft;
                    if (GUILayout.Button("Apply"))
                    {
                        if (Single.TryParse(ExplorerGUIHelper.Instance.objectTransformEdits[0], out float x)
                            && Single.TryParse(ExplorerGUIHelper.Instance.objectTransformEdits[1], out float y)
                            && Single.TryParse(ExplorerGUIHelper.Instance.objectTransformEdits[2], out float z))
                        {
                            explorerComponentsObject.transform.position = new Vector3(x, y, z);
                        }
                        //if (float.TryParse(ExplorerGUIHelper.Instance.objectTransformEdits[4], out float scale))
                        //{
                        //    explorerComponentsObject.transform.localScale = new Vector3(scale, scale, scale);
                        //}
                        
                    }

                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();

                    GUILayout.Label("Rot speed:", GUILayout.Width(90));
                    if (ExplorerGUIHelper.Instance.objectTransformEdits[3] == null || ExplorerGUIHelper.Instance.objectTransformEdits[3] == "")
                    {
                        ExplorerGUIHelper.Instance.objectTransformEdits[3] = "50.0";
                    }
                    ExplorerGUIHelper.Instance.objectTransformEdits[3] = GUILayout.TextField(ExplorerGUIHelper.Instance.objectTransformEdits[3], GUILayout.Width(60));

                    float f = 50.0f;
                    if (Single.TryParse(ExplorerGUIHelper.Instance.objectTransformEdits[3], out float f2))
                    {
                        f = f2;
                    }

                    if (GUILayout.RepeatButton("<- x", GUILayout.Width(50)))
                    {
                        explorerComponentsObject.transform.Rotate(Vector3.up * f * Time.deltaTime);
                    }
                    if (GUILayout.RepeatButton("x ->", GUILayout.Width(50)))
                    {
                        explorerComponentsObject.transform.Rotate(Vector3.up * -f * Time.deltaTime);
                    }

                    if (GUILayout.RepeatButton("<- y", GUILayout.Width(50)))
                    {
                        explorerComponentsObject.transform.Rotate(Vector3.right * f * Time.deltaTime);
                    }
                    if (GUILayout.RepeatButton("y ->", GUILayout.Width(50)))
                    {
                        explorerComponentsObject.transform.Rotate(Vector3.right * -f * Time.deltaTime);
                    }

                    if (GUILayout.RepeatButton("<- z", GUILayout.Width(50)))
                    {
                        explorerComponentsObject.transform.Rotate(Vector3.forward * f * Time.deltaTime);
                    }
                    if (GUILayout.RepeatButton("z ->", GUILayout.Width(50)))
                    {
                        explorerComponentsObject.transform.Rotate(Vector3.forward * -f * Time.deltaTime);
                    }

                    GUILayout.EndHorizontal();
                }

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Scale -"))
                {
                    explorerComponentsObject.transform.localScale *= 0.9f;
                }
                if (GUILayout.Button("Scale +"))
                {
                    explorerComponentsObject.transform.localScale *= 1.1f;
                }
                if (GUILayout.Button("Scale Reset"))
                {
                    explorerComponentsObject.transform.localScale = new Vector3(1,1,1);
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                GUILayout.Label("Misc:");

                GUILayout.BeginHorizontal();

                ExplorerGUIHelper.Instance.addComponentEdit = GUILayout.TextField(ExplorerGUIHelper.Instance.addComponentEdit, GUILayout.Width(156));
                if (GUILayout.Button("Add Comp", GUILayout.Width(90)))
                {
                    if (asm != null && asm is Assembly)
                    {
                        if (asm.GetType(ExplorerGUIHelper.Instance.addComponentEdit) is Type t)
                        {
                            try { obj.AddComponent(t); } catch { Debug.LogError("Could not add component!"); }
                        }
                        else
                            Debug.LogError("That component type does not exist!");
                    }
                    else
                    {
                        Debug.Log("Could not find assembly!");
                    }
                }

                ExplorerGUIHelper.Instance.setParentEdit = GUILayout.TextField(ExplorerGUIHelper.Instance.setParentEdit, GUILayout.Width(156));
                if (GUILayout.Button("Set Parent", GUILayout.Width(90)))
                {
                    if (GameObject.Find(ExplorerGUIHelper.Instance.setParentEdit) is GameObject newParent)
                    {
                        obj.transform.parent = newParent.transform;
                    }
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            GUILayout.EndVertical();
        }

        private void TranslateButtons(int fieldID)
        {
            if (GUILayout.RepeatButton("+", GUILayout.Width(20)))
            {
                if (Single.TryParse(ExplorerGUIHelper.Instance.objectTransformEdits[fieldID], out float f))
                {
                    f += 0.5f * Time.deltaTime;

                    ExplorerGUIHelper.Instance.objectTransformEdits[fieldID] = f.ToString();

                    if (Single.TryParse(ExplorerGUIHelper.Instance.objectTransformEdits[0], out float x)
                        && Single.TryParse(ExplorerGUIHelper.Instance.objectTransformEdits[1], out float y)
                        && Single.TryParse(ExplorerGUIHelper.Instance.objectTransformEdits[2], out float z))
                    {
                        explorerComponentsObject.transform.position = new Vector3(x, y, z);
                    }
                }
            }

            if (GUILayout.RepeatButton("-", GUILayout.Width(20)))
            {
                if (Single.TryParse(ExplorerGUIHelper.Instance.objectTransformEdits[fieldID], out float f))
                {
                    f -= 1.0f * Time.deltaTime;

                    ExplorerGUIHelper.Instance.objectTransformEdits[fieldID] = f.ToString();

                    if (Single.TryParse(ExplorerGUIHelper.Instance.objectTransformEdits[0], out float x)
                        && Single.TryParse(ExplorerGUIHelper.Instance.objectTransformEdits[1], out float y)
                        && Single.TryParse(ExplorerGUIHelper.Instance.objectTransformEdits[2], out float z))
                    {
                        explorerComponentsObject.transform.position = new Vector3(x, y, z);
                    }
                }
            }
        }

        public void ListChildObjects(GameObject obj, bool isPrefab)
        {
            bool enabled = obj.activeInHierarchy;
            bool children = obj.transform.childCount > 0;

            GUILayout.BeginHorizontal();

            GUI.color = ExplorerGUIHelper.Instance.lightRed;
            if (enabled)
            {
                if (children)
                    GUI.color = Color.green;
                else
                    GUI.color = ExplorerGUIHelper.Instance.lightGreen;
            }

            // build name
            string buttonLabel = "";
            if (obj.GetComponent<MeshRenderer>() is MeshRenderer m)
            {
                if (m.isPartOfStaticBatch)
                {
                    buttonLabel += "(Static) ";

                    if (enabled)
                        GUI.color = Color.yellow;
                }                
            }
            if (obj.transform.childCount > 0)
                buttonLabel += "[" + obj.transform.childCount + " children] ";
            buttonLabel += obj.name;

            GUI.skin.label.fontSize = 12;
            GUI.skin.button.alignment = TextAnchor.UpperLeft;

            if (obj.transform.childCount > 0)
            {
                if (GUILayout.Button(buttonLabel, new GUILayoutOption[] { GUILayout.Height(22), GUILayout.Width(380) }))
                {
                    if (isPrefab)
                    {
                        currentPrefab = obj;
                    }
                    else
                    {
                        ExplorerGUIHelper.Instance.scroll2 = Vector2.zero;
                        explorerTransform = obj.transform;
                        explorerPath.Add(obj.transform);
                    }
                }
            }
            else
            {
                if (GUILayout.Button(buttonLabel, new GUILayoutOption[] { GUILayout.Height(22), GUILayout.Width(380) }))
                {
                    if (isPrefab)
                    {
                        currentPrefabChild = obj;
                    }
                    else
                    {
                        explorerComponentsObject = obj;
                        for (int i = 0; i < 4; i++)
                            ExplorerGUIHelper.Instance.objectTransformEdits[i] = "";
                    }
                }
            }

            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
            GUI.skin.label.fontSize = 13;
            GUI.color = Color.white;           

            if (GUILayout.Button("Inspect"))
            {
                if (isPrefab)
                {
                    currentPrefabChild = obj;
                }
                else
                {
                    explorerComponentsObject = obj;
                    for (int i = 0; i < 4; i++)
                        ExplorerGUIHelper.Instance.objectTransformEdits[i] = "";
                }
            }         

            GUILayout.EndHorizontal();
        }

        public void SetInspectorObject(object obj)
        {
            inspectorFields = new Dictionary<string, KeyValuePair<FieldInfo, Type>>();
            inspectorEdits = new Dictionary<string, string>();

            Type type = obj.GetType();
            List<FieldInfo> fields = new List<FieldInfo>();
            GetFieldsRecursive(type, ref fields, obj);

            ExplorerGUIHelper.Instance.showInspector = true;
            inspectorObject = obj;

            inspectorPath.Add(obj);

            inspectorJsonDump = "";

            try
            {
                JsonUtility.ToJson(inspectorObject, true);
                inspectorJsonDump = JsonUtility.ToJson(inspectorObject, true);
            }
            catch
            {
                // Debug.Log("Cannot deserialize class!");
            }
        }

        

        // ------------------- REFLECTION HELPERS ------------------------

        public object Call(object obj, string method, params object[] args)
        {
            var methodInfo = obj.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
            if (methodInfo != null)
            {
                return methodInfo.Invoke(obj, args);
            }
            return null;
        }

        public void SetValue<T>(T value, Type type, object obj, string field)
        {
            FieldInfo fieldInfo = type.GetField(field, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
            fieldInfo.SetValue(obj, value);
        }

        public object GetValue(Type type, object obj, string value)
        {
            FieldInfo fieldInfo = type.GetField(value, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
            return fieldInfo.GetValue(obj);
        }

    }
}