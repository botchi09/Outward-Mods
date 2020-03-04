using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using System.Xml.Serialization;

namespace Dataminer
{
    public class SceneManager : MonoBehaviour
    {
        public static SceneManager Instance;

        private bool m_parsing;
        private Coroutine m_coroutine;

        internal void Awake()
        {
            Instance = this;

            On.AICEnemyDetection.Update += new On.AICEnemyDetection.hook_Update(AIEnemyDetectionHook);
            On.AIESwitchState.SwitchState += new On.AIESwitchState.hook_SwitchState(AISwitchStateHook);

            SceneHelper.SetupSceneSummaries();
        }

        internal void Update()
        {
            if (Input.GetKeyDown(KeyCode.Pause))
            {
                m_parsing = !m_parsing;

                if (m_parsing)
                {
                    if (CharacterManager.Instance.GetFirstLocalCharacter() != null)
                    {
                        Debug.Log("___________ Starting Scenes Parse ___________");
                        m_coroutine = StartCoroutine(ParseCoroutine());
                    }
                    else
                    {
                        m_parsing = false;
                    }
                }
                else
                {
                    Debug.Log("___________ Stopping Scenes Parse ___________");
                    StopCoroutine(m_coroutine);
                }
            }
        }

        private IEnumerator ParseCoroutine()
        {
            foreach (string sceneName in SceneHelper.SceneBuildNames.Keys)
            {
                Debug.Log("--- Parsing " + sceneName + " ---");

                /*        Load Scene        */

                if (SceneManagerHelper.ActiveSceneName != sceneName)
                {
                    NetworkLevelLoader.Instance.RequestSwitchArea(sceneName, 0, 1.5f);

                    yield return new WaitForSeconds(5f);

                    while (NetworkLevelLoader.Instance.IsGameplayPaused)
                    {
                        NetworkLevelLoader loader = NetworkLevelLoader.Instance;
                        At.SetValue(true, typeof(NetworkLevelLoader), loader, "m_continueAfterLoading");
                        MenuManager.Instance.HideMasterLoadingScreen();

                        yield return new WaitForSeconds(1f);
                    }
                    yield return new WaitForSeconds(2f);
                }

                /*        Parse Scene        */

                //// temp debug: find cheatable dialogues
                //FindCheatableDialogue();

                // Disable the TreeBehaviour Managers while we do stuff with enemies
                DisableCanvases();

                // Parse Enemies
                EnemyHolder.ParseAllEnemies();

                // Parse Merchants
                MerchantHolder.ParseAllMerchants();

                // Parse Loot (+ item sources)
                ParseAllLoot();

                Debug.Log("--- Finished Scene: " + SceneManagerHelper.ActiveSceneName + " ---");
            }

            Debug.Log("___________ Finished Scenes Parse ___________");

            Debug.Log("[Dataminer] Saving lists...");
            ListManager.SaveLists();

            Debug.Log("[Dataminer] Finished.");
        }

        //// temp debug
        //private void FindCheatableDialogue()
        //{
        //    foreach (var tree in Resources.FindObjectsOfTypeAll<NodeCanvas.DialogueTrees.DialogueTreeExt>())
        //    {
        //        var nodes = At.GetValue(typeof(NodeCanvas.Framework.Graph), tree as NodeCanvas.Framework.Graph, "_nodes") as List<NodeCanvas.Framework.Node>;

        //        foreach (var node in nodes.Where(x => x is NodeCanvas.DialogueTrees.MultipleChoiceNodeExt))
        //        {
        //            var multichoice = node as NodeCanvas.DialogueTrees.MultipleChoiceNodeExt;

        //            // make sure previous option had multichoice
        //            if (node.inConnections == null 
        //                || node.inConnections.Count < 1 
        //                || node.inConnections[0].sourceNode.inConnections == null 
        //                || node.inConnections[0].sourceNode.inConnections.Count < 1)
        //            {
        //                continue;
        //            }

        //            var prevMulti = node.inConnections[0].sourceNode.inConnections[0].sourceNode as NodeCanvas.DialogueTrees.MultipleChoiceNodeExt;

        //            if (prevMulti == null)
        //            {
        //                continue;
        //            }

        //            Dictionary<int, int> conditionKeys = new Dictionary<int, int>();
        //            for (int i = 0; i < multichoice.availableChoices.Count; i++)
        //            {
        //                if (multichoice.availableChoices[i].condition != null)
        //                {
        //                    int fix = i - conditionKeys.Count;
        //                    conditionKeys.Add(i, fix);
        //                }
        //            }

        //            foreach (var key2 in conditionKeys)
        //            {
        //                if (prevMulti.outConnections.Count - 1 >= key2.Value)
        //                {
        //                    Debug.Log("-----------[ " + tree.name + " ]----------");
        //                    Debug.Log("POTENTIAL EXPLOITABLE!");

        //                    var actors = "";
        //                    foreach (var actor in tree.actorParameters)
        //                    {
        //                        if (actors != "") { actors += ", "; }
        //                        actors += actor.name;
        //                    }
        //                    Debug.Log("Actors: " + actors);

        //                    //find which node leads to the second node
        //                    int key1 = 0;
        //                    foreach (var connection in prevMulti.outConnections)
        //                    {
        //                        if (connection.targetNode != null && connection.targetNode.outConnections.Count > 0 && connection.targetNode.outConnections[0].targetNode == multichoice)
        //                        {
        //                            Debug.Log("Key1 is " + (key1 + 1) + ": " + prevMulti.availableChoices[key1].statement);
        //                            break;
        //                        }
        //                        key1++;
        //                    }

        //                    Debug.Log("Key2 is " + (key2.Value + 1) + ": " + multichoice.availableChoices[key2.Key].statement);
        //                    Debug.Log("Real Key2: " + (key2.Key + 1));
        //                    Debug.Log("--------------------------------------------");
        //                }
        //            }
        //        }
        //    }
        //}

        #region PARSE ALL LOOT FUNCTION
        // Parse Loot
        public static void ParseAllLoot()
        {
            var allitems = Resources.FindObjectsOfTypeAll(typeof(Item)) as Item[];

            foreach (Item item in allitems.Where(x => IsValidLoot(x)))
            {
                var summary = ListManager.SceneSummaries[ListManager.GetSceneSummaryKey(item.transform.position)];

                if (item is SelfFilledItemContainer)
                {
                    if (item is TreasureChest)
                    {
                        var lootContainer = LootContainerHolder.ParseLootContainer(item as TreasureChest);
                        AddQuantity(lootContainer.Name, summary.Loot_Containers);
                        ListManager.AddContainerSummary(lootContainer.Name, ListManager.GetSceneSummaryKey(item.transform.position), lootContainer.DropTables);

                        if (!summary.UniqueContainerList.Contains(lootContainer.Name + "_" + lootContainer.UID))
                            summary.UniqueContainerList.Add(lootContainer.Name + "_" + lootContainer.UID);
                    }
                    else if (item is Gatherable)
                    {
                        var gatherableHolder = GatherableHolder.ParseGatherable(item as Gatherable);
                        AddQuantity(gatherableHolder.Name, summary.Gatherables);
                        ListManager.AddContainerSummary(gatherableHolder.Name, ListManager.GetSceneSummaryKey(item.transform.position), gatherableHolder.DropTables);
                    }
                    else
                    {
                        Debug.LogWarning("[ParseLoot] Unsupported ItemContainer: " + item.Name + ", typeof: " + item.GetType());
                    }
                }
                else
                {
                    // item spawn
                    bool newHolder = true;
                    foreach (ItemSpawnHolder holder in summary.Item_Spawns)
                    {
                        if (holder.Item_ID == item.ItemID)
                        {
                            newHolder = false;
                            holder.Quantity++;
                            holder.positions.Add(item.transform.position);
                            break;
                        }
                    }
                    if (newHolder)
                    {
                        summary.Item_Spawns.Add(new ItemSpawnHolder
                        {
                            Name = item.Name,
                            Item_ID = item.ItemID,
                            Quantity = 1,
                            positions = new List<Vector3>
                            {
                                item.transform.position
                            }
                        });
                    }

                    AddItemSpawnSource(item.ItemID, item.Name, item.transform.position);
                }
            }
        }
        #endregion

        #region GLOBAL HELPERS
        // Helpers used globally
        public string GetCurrentRegion()
        {
            string region = "ERROR";
            foreach (KeyValuePair<string, List<string>> entry in SceneHelper.ScenesByRegion)
            {
                if (entry.Value.Contains(SceneHelper.SceneBuildNames[SceneManagerHelper.ActiveSceneName]))
                {
                    region = entry.Key;
                    break;
                }
            }
            if (region == "ERROR")
            {
                if (SceneManagerHelper.ActiveSceneName.ToLower().Contains("cherso"))
                    region = "Chersonese";
                if (SceneManagerHelper.ActiveSceneName.ToLower().Contains("emercar"))
                    region = "Enmerkar Forest";
                if (SceneManagerHelper.ActiveSceneName.ToLower().Contains("abrassar"))
                    region = "Abrassar";
                if (SceneManagerHelper.ActiveSceneName.ToLower().Contains("hallowed"))
                    region = "Hallowed Marsh";

            }
            return region;
        }

        public string GetCurrentLocation(Vector3 _pos)
        {
            if (!SceneManagerHelper.ActiveSceneName.ToLower().Contains("dungeonssmall"))
            {
                return SceneHelper.SceneBuildNames[SceneManagerHelper.ActiveSceneName];
            }
            else
            {
                string closestRegion = "";
                float lowest = float.MaxValue;

                Dictionary<string, Vector3> dict = null;
                switch (GetCurrentRegion())
                {
                    case "Chersonese":
                        dict = SceneHelper.ChersoneseDungeons; break;
                    case "Enmerkar Forest":
                        dict = SceneHelper.EnmerkarDungeons; break;
                    case "Hallowed Marsh":
                        dict = SceneHelper.MarshDungeons; break;
                    case "Abrassar":
                        dict = SceneHelper.AbrassarDungeons; break;
                    default: break;
                }

                if (dict != null)
                {
                    foreach (KeyValuePair<string, Vector3> entry in dict)
                    {
                        if (Vector3.Distance(_pos, entry.Value) < lowest)
                        {
                            lowest = Vector3.Distance(_pos, entry.Value);
                            closestRegion = entry.Key;
                        }
                    }

                    return closestRegion;
                }
                else
                {
                    Debug.LogWarning("Could not get region!");
                    return SceneManagerHelper.ActiveSceneName;
                }
            }
        }
        #endregion

        #region SMALL HELPERS
        // Small helper functions
        private static void AddItemSpawnSource(int item_ID, string item_Name, Vector3 pos)
        {
            if (ListManager.ItemLootSources.ContainsKey(item_ID.ToString()))
            {
                ListManager.ItemLootSources[item_ID.ToString()].Spawn_Sources.Add(ListManager.GetSceneSummaryKey(pos));
            }
            else
            {
                ListManager.ItemLootSources.Add(item_ID.ToString(), new ItemSource
                {
                    ItemID = item_ID,
                    ItemName = item_Name,
                    Container_Sources = new List<string>(),
                    Spawn_Sources = new List<string>
                    {
                        ListManager.GetSceneSummaryKey(pos)
                    }
                });
            }
        }

        private static void AddQuantity(string name, List<SceneSummary.QuantityHolder> list)
        {
            bool newEntry = true;
            foreach (SceneSummary.QuantityHolder holder in list)
            {
                if (holder.Name == name)
                {
                    holder.Quantity++;
                    newEntry = false;
                    break;
                }
            }
            if (newEntry)
            {
                list.Add(new SceneSummary.QuantityHolder
                {
                    Name = name,
                    Quantity = 1
                });
            }
        }

        public static bool IsValidLoot(Item item)
        {
            if (item.gameObject.scene == null || item.UID == null || item.UID == UID.Empty || ItemManager.Instance.GetItem(item.UID) == null)
            {
                return false;
            }

            if (item.ParentContainer == null && item.OwnerCharacter == null && item.IsInWorld && (item.IsPickable || item is SelfFilledItemContainer || item.IsDeployable))
            {
                return true;
            }

            return false;
        }

        private void ForceObjectActive(GameObject obj)
        {
            obj.SetActive(true);
            if (obj.transform.parent != null)
            {
                ForceObjectActive(obj.transform.parent.gameObject);
            }
        }

        public void DisableCanvases()
        {
            var canvases = Resources.FindObjectsOfTypeAll(typeof(NodeCanvas.BehaviourTrees.BehaviourTreeOwner));

            foreach (NodeCanvas.BehaviourTrees.BehaviourTreeOwner tree in canvases)
            {
                tree.gameObject.SetActive(false);
            }
        }
        #endregion

        #region HOOKS

        // disable AI aggression
        private void AIEnemyDetectionHook(On.AICEnemyDetection.orig_Update orig, AICEnemyDetection self)
        {
            if (!m_parsing)
                orig(self);
        }
        private void AISwitchStateHook(On.AIESwitchState.orig_SwitchState orig, AIESwitchState self)
        {
            if (!m_parsing)
                orig(self);
        }
        #endregion
    }
}
