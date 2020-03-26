using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OutwardExplorer
{
    public class ExplorerGUIHelper : MonoBehaviour
    {
        public static ExplorerGUIHelper Instance;

        internal void Awake()
        {
            Instance = this;
        }

        // main OnGUI
        internal void OnGUI()
        {
            if (!ShowGui)
                return;

            if (m_windowRect == null || m_windowRect.width < 1)
                m_windowRect = _rect;
            else
                m_windowRect = GUI.Window(99, m_windowRect, MainGuiWindow, "Outward Explorer 0.75");

            if (showInspector)
            {
                if (m_inspectorRect == null || m_inspectorRect.width < 1)
                {
                    m_inspectorRect = _rect;
                    m_inspectorRect.width += 200;
                    m_inspectorRect.x = m_windowRect.x + 600;
                }
                else
                {
                    m_inspectorRect = GUI.Window(98, m_inspectorRect, ExplorerScript.Instance.InspectorWindow, "Object Inspector");
                }
            }
        }


        private void NavButton(string label, int page)
        {
            if (guiPage == page) { GUI.color = lightGreen; } else { GUI.color = Color.white; }
            if (GUILayout.Button(label))
                guiPage = page;
        }


        // ------------------ main menu ------------------ //
        internal void MainGuiWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, _rect.width - 90, 20));

            if (GUI.Button(new Rect(_rect.width - 90, 2, 80, 17), "Hide (F7)"))
            {
                ShowGui = false;
                return;
            }

            var labelStyle = GUI.skin.GetStyle("Label");
            labelStyle.alignment = TextAnchor.UpperLeft;
            labelStyle.fontStyle = FontStyle.Normal;
            labelStyle.fontSize = 13;

            GUILayout.BeginArea(new Rect(3, 25, _rect.width - 5, _rect.height - 35));
            GUILayout.BeginVertical(new GUIStyle() { padding = new RectOffset(3, 3, 8, 3) });

            GUILayout.BeginHorizontal();

            NavButton("Explorer", 0);
            NavButton("Prefab Manager", 1);
            NavButton("Quest Events", 2);
            // NavButton("Dumps", 3);

            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUI.color = Color.white;

            scroll = GUILayout.BeginScrollView(scroll);

            try
            {
                if (guiPage == 0)
                    ExplorerPage();

                else if (guiPage == 1)
                    PrefabManagerPage();

                else if (guiPage == 2)
                    MiscPage();
            }
            catch
            {
                ExplorerScript.Instance.Reset();
            }
            //else if (guiPage == 3)
            //    xExplorerScript.Instance.dumper.utils.DumperGUIPage();

            GUILayout.EndScrollView();

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        // ----------------------------- GUI PAGES -------------------------------- //

        public bool ShowGui = true;
        public bool tempHideGui = false;
        public int guiPage = 0;

        // inspector gui
        public int inspectorPage = 0;
        public Rect m_inspectorRect; // inspector window
        public bool showInspector = false; // bool - show inspector window
        public Vector2 rect2scroll;

        // explorer page
        public string explorerAddObjectEdit = "";
        public string[] objectTransformEdits;

        public Rect _rect = new Rect(5, 5, 550, 700);
        public Rect m_windowRect;
        public string exploreSearch = "";
        public Vector2 scroll = Vector2.zero;
        public Vector2 scroll2 = Vector2.zero;
        public Vector2 scroll3 = Vector2.zero;

        // component inspector
        public string addComponentEdit = "";
        public string setParentEdit = "";

        public Color lightRed = new Color() { r = 1.0f, b = 0.41f, g = 0.41f, a = 1.0f };
        public Color lightGreen = new Color() { r = 0.51f, b = 0.51f, g = 1, a = 1.0f };

        // prefab manager gui
        public Vector2 scrollPrefabs = Vector2.zero;
        public string itemSearch = "";
        public string prefabAddObjectEdit = "";

        // misc page gui
        public bool teleToSelf;
        public bool enemyAggressive = false;
        public string[] miscEdits;

        //  =========================== explorer page =========================
        public void ExplorerPage()
        {
            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label("Scene:", GUILayout.Width(60));
            //GUILayout.Label(SceneManagerHelper.ActiveSceneName, GUILayout.Width(140));
            if (GUILayout.Button(SceneManagerHelper.ActiveSceneName, GUILayout.Width(140)))
            {
                Area a = AreaManager.Instance.GetAreaFromSceneName(SceneManagerHelper.ActiveSceneName);
                ExplorerScript.Instance.SetInspectorObject(a);
            }

            // search
            exploreSearch = GUILayout.TextField(exploreSearch);
            if (GUILayout.Button("Search (exact)", GUILayout.Width(120)) && exploreSearch.Length > 0)
            {
                if (GameObject.Find(exploreSearch) is GameObject result)
                {
                    ExplorerScript.Instance.explorerPath.Clear();
                    ExplorerScript.Instance.explorerPath.Add(result.transform);
                    ExplorerScript.Instance.explorerTransform = result.transform;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (ExplorerScript.Instance.explorerTransform != null)
            {
                if (GUILayout.Button("X", GUILayout.Width(30)))
                {
                    ExplorerScript.Instance.explorerTransform = null;
                    ExplorerScript.Instance.explorerPath.Clear();
                    GUILayout.EndHorizontal();
                    return;
                }
                if (ExplorerScript.Instance.explorerTransform.parent != null)
                {
                    if (GUILayout.Button("<-", new GUILayoutOption[] { GUILayout.Height(21), GUILayout.Width(60) }))
                    {
                        //explorerPath = explorerPath.Substring(0, explorerPath.Length - ExplorerScript.Instance.explorerTransform.name.Length - 1);
                        ExplorerScript.Instance.explorerPath.RemoveAt(ExplorerScript.Instance.explorerPath.Count - 1);
                        ExplorerScript.Instance.explorerTransform = ExplorerScript.Instance.explorerTransform.parent;
                        GUILayout.EndHorizontal();
                        return;
                    }
                }
            }

            GUI.skin.label.alignment = TextAnchor.UpperLeft;

            string s = "";
            foreach (Transform t in ExplorerScript.Instance.explorerPath)
            {
                s += t.name + "/";
            }
            GUILayout.Label("~/" + s, GUILayout.Height(23));

            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(GUI.skin.box);

            scroll2 = GUILayout.BeginScrollView(scroll2, GUILayout.MaxHeight(260));

            if (ExplorerScript.Instance.explorerTransform == null)
            {
                foreach (PlayerSystem ps in Global.Lobby.PlayersInLobby)
                {
                    Character c = ps.ControlledCharacter;
                    ExplorerScript.Instance.ListChildObjects(c.gameObject, false);
                }
                foreach (GameObject child in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Where(x => x.transform.childCount > 0))
                {
                    ExplorerScript.Instance.ListChildObjects(child.gameObject, false);
                }
                foreach (GameObject child in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Where(x => x.transform.childCount <= 0))
                {
                    ExplorerScript.Instance.ListChildObjects(child.gameObject, false);
                }
            }
            else
            {
                foreach (Transform child in ExplorerScript.Instance.explorerTransform)
                {
                    if (child.childCount > 0)
                        ExplorerScript.Instance.ListChildObjects(child.gameObject, false);
                }
                foreach (Transform child in ExplorerScript.Instance.explorerTransform)
                {
                    if (child.childCount <= 0)
                        ExplorerScript.Instance.ListChildObjects(child.gameObject, false);
                }
            }
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            explorerAddObjectEdit = GUILayout.TextField(explorerAddObjectEdit, GUILayout.Width(340));
            if (GUILayout.Button("Create Object", GUILayout.Width(120)))
            {
                GameObject obj = new GameObject
                {
                    name = explorerAddObjectEdit,
                    hideFlags = HideFlags.None,
                };
                //DontDestroyOnLoad(obj);
                obj.SetActive(true);
                if (CharacterManager.Instance.GetFirstLocalCharacter() is Character c)
                {
                    obj.transform.position = c.transform.position + new Vector3(0, 1, 0);
                }
                if (ExplorerScript.Instance.explorerTransform != null)
                    obj.transform.parent = ExplorerScript.Instance.explorerTransform;
                else
                    obj.transform.parent = null;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            //GUI.skin.horizontalScrollbar.padding = new RectOffset(0, 0, 0, 0);
            GUILayout.Label("", GUI.skin.horizontalScrollbar);

            if (ExplorerScript.Instance.explorerComponentsObject != null)
            {
                ExplorerScript.Instance.ListComponents(ExplorerScript.Instance.explorerComponentsObject, false);
            }
        }

        //========================== prefab manager page ==========================

        private void PrefabManagerPage()
        {
            GUILayout.BeginVertical();

            if (ExplorerScript.Instance.currentPrefab == null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Search for an item:");
                itemSearch = GUILayout.TextField(itemSearch, new GUILayoutOption[] { GUILayout.Width(200) }); // current selected item text field
                GUILayout.Label("* = show all");
                GUILayout.EndHorizontal();

                GUILayout.Space(10);
                // ========= item list ===========
                if (itemSearch != "" || itemSearch == "*")
                {
                    if (itemSearch == "*") { itemSearch = ""; } // if we're in wildcard, set "*" to "" temporarily to draw the full list

                    scrollPrefabs = GUILayout.BeginScrollView(scrollPrefabs);
                    foreach (KeyValuePair<string, GameObject> entry in ExplorerScript.Instance.allPrefabs.Where(x => x.Key.ToLower().Contains(itemSearch.ToLower())))
                    {
                        GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                        if (GUILayout.Button(entry.Key, new GUILayoutOption[] { GUILayout.Height(22), GUILayout.MaxWidth(510) }))
                        {
                            ExplorerScript.Instance.currentPrefab = entry.Value;
                        }
                        GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    }
                    GUILayout.EndScrollView();

                    if (itemSearch == "") { itemSearch = "*"; } // reset "" to "*"
                }
            }

            if (ExplorerScript.Instance.currentPrefab != null)
            {
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Back", GUILayout.Width(50)))
                {
                    ExplorerScript.Instance.currentPrefabChild = null;
                    if (ExplorerScript.Instance.currentPrefab.transform.parent != null)
                    {
                        ExplorerScript.Instance.currentPrefab = ExplorerScript.Instance.currentPrefab.transform.parent.gameObject;
                    }
                    else
                    {
                        ExplorerScript.Instance.currentPrefab = null;
                    }
                    return;
                }

                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                GUILayout.Label(ExplorerScript.Instance.currentPrefab.name);
                GUI.skin.label.alignment = TextAnchor.UpperLeft;

                GUILayout.Space(50);
                GUILayout.EndHorizontal();

                Item item = ExplorerScript.Instance.currentPrefab.GetComponent<Item>();

                if (GUILayout.Button("Create object (root scene)"))
                {
                    Item spawnedItem = ItemManager.Instance.GenerateItemNetwork(item.ItemID);
                    if (CharacterManager.Instance.GetFirstLocalCharacter() is Character c2)
                    {
                        spawnedItem.transform.position = c2.transform.position;
                    }
                }

                if (CharacterManager.Instance.GetFirstLocalCharacter() is Character c && item != null && !(item is Quest || item is Skill || item is ItemContainer))
                {
                    if (GUILayout.Button("Add to local player bag"))
                    {
                        c.Inventory.GenerateItem(item, 1, false);
                    }

                    if (item is Skill)
                    {
                        if (GUILayout.Button("Learn Skill"))
                        {
                            CharacterManager.Instance.GetFirstLocalCharacter().Inventory.SkillKnowledge.AddItem(item);
                        }
                    }
                }


                GUILayout.Label("Child objects");

                GUILayout.BeginHorizontal();
                prefabAddObjectEdit = GUILayout.TextField(prefabAddObjectEdit, GUILayout.Width(300));
                if (GUILayout.Button("Add Object", GUILayout.Width(120)))
                {
                    GameObject obj = new GameObject
                    {
                        name = prefabAddObjectEdit,
                        hideFlags = HideFlags.None,
                    };
                    obj.transform.parent = ExplorerScript.Instance.currentPrefab.transform;
                }
                GUILayout.EndHorizontal();

                // -----------------------------transform / component viewer -------------------------------

                scroll2 = GUILayout.BeginScrollView(scroll2, GUILayout.MaxHeight(310));
                if (ExplorerScript.Instance.currentPrefab.transform != null)
                {
                    foreach (Transform child in ExplorerScript.Instance.currentPrefab.transform)
                    {
                        if (child.childCount > 0)
                            ExplorerScript.Instance.ListChildObjects(child.gameObject, true);
                    }
                    foreach (Transform child in ExplorerScript.Instance.currentPrefab.transform)
                    {
                        if (child.childCount <= 0)
                            ExplorerScript.Instance.ListChildObjects(child.gameObject, true);
                    }
                }
                GUILayout.EndScrollView();

                GUILayout.Space(20);
                GUILayout.Label("GameObject Components:");

                if (ExplorerScript.Instance.currentPrefabChild != null)
                {
                    ExplorerScript.Instance.ListComponents(ExplorerScript.Instance.currentPrefabChild.gameObject, true);
                }
                else if (ExplorerScript.Instance.currentPrefab != null)
                {
                    ExplorerScript.Instance.ListComponents(ExplorerScript.Instance.currentPrefab.gameObject, true);
                }
            }

            GUILayout.EndVertical();
        }

        public QuestEventSignature currentSig;
        public Vector2 questScroll1;
        public Vector2 questScroll2;
        public string stackEdit = "";

        private void MiscPage()
        {
            // page 4 - other

            // dictionary (all events)
            questScroll1 = GUILayout.BeginScrollView(questScroll1, GUI.skin.box, GUILayout.Height(300));

            GUILayout.BeginHorizontal();
            GUILayout.Label("Search:", GUILayout.Width(80));
            miscEdits[0] = GUILayout.TextField(miscEdits[0], GUILayout.Width(250));
            string s = miscEdits[0];
            GUILayout.EndHorizontal();

            if (currentSig != null)
            {
                if (GUILayout.Button("Back", GUILayout.Width(60)))
                {
                    currentSig = null;
                }
                if (currentSig != null)
                {
                    GUILayout.Label("Name: " + currentSig.EventName);
                    GUILayout.Label("Description: " + currentSig.Description);

                    GUILayout.Space(20);

                    if (stackEdit == "") { stackEdit = "1"; }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Stack value: ", GUILayout.Width(100));
                    stackEdit = GUILayout.TextField(stackEdit, GUILayout.Width(50));
                    GUILayout.EndHorizontal();

                    int stackValue = 1;
                    if (int.TryParse(stackEdit, out int i)) { stackValue = i; }

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Try add event to local player"))
                    {
                        if (CharacterManager.Instance != null && CharacterManager.Instance.GetFirstLocalCharacter() != null)
                        {
                            QuestEventManager.Instance.AddEvent(currentSig, stackValue);
                        }
                    }

                    if (GUILayout.Button("Try remove event"))
                    {
                        if (CharacterManager.Instance != null && CharacterManager.Instance.GetFirstLocalCharacter() != null)
                        {
                            QuestEventManager.Instance.RemoveEvent(currentSig.EventUID);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }
            else if (s != "")
            {
                if (ExplorerScript.Instance.questEvents != null && ExplorerScript.Instance.questEvents.Count > 0)
                {
                    int max = 0;
                    foreach (KeyValuePair<string, QuestEventSignature> entry in ExplorerScript.Instance.questEvents.Where(x => x.Key.ToLower().Contains(s.ToLower())))
                    {
                        max++;

                        if (max > 250)
                            break;

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(entry.Value.EventName);
                        if (GUILayout.Button("Inspect"))
                        {
                            currentSig = entry.Value;
                        }
                        GUILayout.EndHorizontal();
                    }
                }

            }

            GUILayout.EndScrollView();

            // ------------------- other misc ---------------------- //


            // recipes

            //if (GUILayout.Button("Learn all recipes"))
            //{
            //    LearnAllRecipes();
            //}

            //GUILayout.Space(20);

        }

        // -------------------------- OTHER FUNCTIONS ------------------------------ //

        public void Teleport(Transform target)
        {
            if (CharacterManager.Instance.GetFirstLocalCharacter() is Character c)
            {
                if (teleToSelf)
                {
                    target.position = c.transform.position;
                }
                else
                {
                    Vector3 fix = new Vector3(0, 2, 0);
                    c.transform.position = target.position + fix;
                }
            }
        }

        //private void LearnAllRecipes()
        //{
        //    CharacterRecipeKnowledge charRecipes = CharacterManager.Instance.GetFirstLocalCharacter().Inventory.RecipeKnowledge;
        //    Dictionary<string, Recipe> recipes = typeof(RecipeManager).GetField("m_recipes", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(RecipeManager.Instance) as Dictionary<string, Recipe>;

        //    if (recipes != null && charRecipes != null)
        //    {
        //        foreach (KeyValuePair<string, Recipe> entry in recipes)
        //        {
        //            charRecipes.LearnRecipe(entry.Value);
        //        }
        //    }
        //}
    }
}
