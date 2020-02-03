using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using NodeCanvas.Framework;
using NodeCanvas.DialogueTrees;

namespace OutwardExplorer
{
    public class DumperUtils : MonoBehaviour
    {
        public DumperScript script;

        public Rect m_window = Rect.zero;

        public void OnGUI()
        {
            if (m_window == Rect.zero)
            {
                m_window = new Rect(5, 5, 340, 530);
            }
            else
            {
                m_window = GUI.Window(75135, m_window, DumperGUIPage, "Outward Dumper");
            }
        }

        // =================================== GUI ===================================

        public Color lightRed = new Color() { r = 1.0f, b = 0.41f, g = 0.41f, a = 1.0f };
        public Color lightGreen = new Color() { r = 0.51f, b = 0.51f, g = 1, a = 1.0f };

        public void DumperGUIPage(int id)
        {
            GUI.DragWindow(new Rect(0, 0, m_window.width, 20));

            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label("Prefab Dumper");

            GUILayout.Space(10);

            if (GUILayout.Button("Dump Items/Skills"))
            {
                StartCoroutine(script.DumpItemPrefabs());
            }
            if (GUILayout.Button("Dump Effects"))
            {
                StartCoroutine(script.DumpEffectPrefabs());
            }
            if (GUILayout.Button("Dump Recipes"))
            {
                script.DumpRecipes();
            }

            // ==========================
            GUILayout.Space(20);

            if (CharacterManager.Instance.Characters.Count > 0)
                GUILayout.Label(GetCurrentLocation(CharacterManager.Instance.GetFirstLocalCharacter().transform.position) + " (" + SceneManagerHelper.ActiveSceneName + ")");
            else
                GUILayout.Label("Loading..." + " (" + SceneManagerHelper.ActiveSceneName + ")");

            GUILayout.Space(15);

            //script.IsExperimental = GUILayout.Toggle(script.IsExperimental, "Experimental Branch");

            if (GUILayout.Button("Dump All Scenes"))
            {
                StartCoroutine(script.DumpAllScenes());
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Dump Enemies"))
            {
                StartCoroutine(script.DumpEnemies());
            }

            if (GUILayout.Button("Dump Merchants"))
            {
                StartCoroutine(script.DumpMerchants());
            }

            if (GUILayout.Button("Dump Loot"))
            {
                StartCoroutine(script.DumpLoot());
            }

            GUILayout.Space(20);

            GUI.color =lightRed;
            if (GUILayout.Button("Abort Loop"))
            {
                script.abortLoop = true;
            }

            string label = "";
            if (script.EnemiesAggressive)
            {
                label = "Enemies: Aggressive!";
                GUI.color =lightGreen;
            }
            else
            {
                label = "Enemies: Not Aggressive";
                GUI.color =lightRed;
            }
            if (GUILayout.Button(label))
            {
                script.EnemiesAggressive = !script.EnemiesAggressive;
            }
            GUI.color = Color.white;

            // ==========================

            GUILayout.Space(30);

            GUILayout.Label("Sorting and List Building");

            if (GUILayout.Button("Sort Scene Dumps"))
            {
                script.sorter.SceneSummaries();
            }

            if (GUILayout.Button("Build Lists"))
            {
                script.sorter.BuildLists();
            }

            GUILayout.EndVertical();
        }


        // =========== save / folder stuff =============

        public List<string> sceneBuildNames = new List<string>
        {
            "CierzoTutorial",
            "CierzoNewTerrain",
            "CierzoDestroyed",
            "ChersoneseNewTerrain",
            "Chersonese_Dungeon1",
            "Chersonese_Dungeon2",
            "Chersonese_Dungeon3",
            "Chersonese_Dungeon4_BlueChamber",
            "Chersonese_Dungeon4_HolyMission",
            "Chersonese_Dungeon4_Levant",
            "Chersonese_Dungeon5",
            "Chersonese_Dungeon4_CommonPath",
            "Chersonese_Dungeon6",
            "Chersonese_Dungeon8",
            "Chersonese_Dungeon9",
            "ChersoneseDungeonsSmall",
            "ChersoneseDungeonsBosses",
            "Monsoon",
            "HallowedMarshNewTerrain",
            "Hallowed_Dungeon1",
            "Hallowed_Dungeon2",
            "Hallowed_Dungeon3",
            "Hallowed_Dungeon4_Interior",
            "Hallowed_Dungeon5",
            "Hallowed_Dungeon6",
            "Hallowed_Dungeon7",
            "HallowedDungeonsSmall",
            "HallowedDungeonsBosses",
            "Levant",
            "Abrassar",
            "Abrassar_Dungeon1",
            "Abrassar_Dungeon2",
            "Abrassar_Dungeon3",
            "Abrassar_Dungeon4",
            "Abrassar_Dungeon5",
            "Abrassar_Dungeon6",
            "AbrassarDungeonsSmall",
            "AbrassarDungeonsBosses",
            "Berg",
            "Emercar",
            "Emercar_Dungeon1",
            "Emercar_Dungeon2",
            "Emercar_Dungeon3",
            "Emercar_Dungeon4",
            "Emercar_Dungeon5",
            "Emercar_Dungeon6",
            "EmercarDungeonsSmall",
            "EmercarDungeonsBosses",
            "DreamWorld"
        };

        public Dictionary<string, List<string>> SceneDic = new Dictionary<string, List<string>>
        {
            { "Abrassar", new List<string> {"Abrassar", "Ancient Hive", "Electric Lab", "Levant", "Sand Rose Cave", "Stone Titan Caves", "The Slide", "Undercity Passage" } },
            { "Enmerkar Forest", new List<string> {"Ancestor’s Resting Place", "Berg", "Cabal of Wind Temple", "Enmerkar Forest", "Face of the Ancients", "Forest Hives", "Necropolis", "Royal Manticore’s Lair" } },
            { "Chersonese", new List<string> { "Blister Burrow", "Blue Chamber’s Conflux Path", "Chersonese", "Chersonese (Shipwreck)", "Cierzo", "Cierzo (Destroyed)", "Cierzo Storage", "Conflux Chambers", "Corrupted Tombs", "Ghost Pass", "Heroic Kingdom’s Conflux Path", "Holy Mission’s Conflux Path", "Montcalm Clan Fort", "Vendavel Fortress", "Voltaic Hatchery" } },
            { "Hallowed Marsh",  new List<string> { "Dark Ziggurat Interior", "Dead Roots", "Giants’ Village", "Hallowed Marsh", "Jade Quarry", "Monsoon", "Reptilian Lair", "Spire of Light", "Ziggurat Passage" } },
            { "Other",  new List<string> { "Unknown Arena", "In Between" } }
        };

        public Dictionary<string, Vector3> ChersoneseDungeons = new Dictionary<string, Vector3>
        {
            { "Hyena Burrow", new Vector3(0, 0, 0)},
            { "Bandits' Prison", new Vector3(300, 0, 0)},
            { "Pirates' Hideout", new Vector3(600, 0, 0)},
            { "Vigil Tomb", new Vector3(900, 0, 0)},
            { "Mansion's Cellar", new Vector3(1200, 0, 0)},
            { "Trog Infiltration", new Vector3(1500, 0, 0)},
            { "Starfish Cave", new Vector3(1800, 0, 0)},
            { "Hermit's House", new Vector3(2100, 0, 0)},
            { "Immaculate's Cave", new Vector3(2400, 0, 0)},
        };

        public Dictionary<string, Vector3> MarshDungeons = new Dictionary<string, Vector3>
        {
            { "Abandoned Ziggurat", new Vector3(0, 0, 0)},
            { "Flooded Cellar", new Vector3(300, 0, 0)},
            { "Steakosaur's Burrow", new Vector3(600, 0, 0)},
            { "Hollowed Lotus", new Vector3(900, -86, 0)},
            { "Abandoned Shed", new Vector3(1200,0,0) },
            { "Dead Tree", new Vector3(1500, 0, 0)},
            { "Under Island", new Vector3(1800, 0, 0)},
            { "Immaculate's Camp", new Vector3(2400, 0, 0)},
        };

        public Dictionary<string, Vector3> AbrassarDungeons = new Dictionary<string, Vector3>
        {
            { "Hive Prison", new Vector3(0, 0, 0)},
            { "Captain's Cabin", new Vector3(300, 0, 0)},
            { "Corsair's Headquarters", new Vector3(600, 0, 0) },
            { "River's End", new Vector3(900, 0, 0)},
            { "Ruined Outpost", new Vector3(1200, 0, 0)},
            { "Immaculate's Camp", new Vector3(1500, 0, 0)},
            { "Dock's Storage", new Vector3(1800, 0, 0)},
            { "Cabal of Wind Outpost", new Vector3(2100, 0, 0)},
        };

        public Dictionary<string, Vector3> EnmerkarDungeons = new Dictionary<string, Vector3>
        {
            { "Damp Hunter's Cabin", new Vector3(0, 0, 0)},
            { "Worn Hunter's Cabin", new Vector3(300, 0, 0)},
            { "Old Hunter's Cabin", new Vector3(600, 0, 0)},
            { "Dolmen Crypt", new Vector3(900, 0, 0)},
            { "Hive Trap", new Vector3(1200, 0, 0) },
            { "Burnt Outpost", new Vector3(1500, 0, 0)},
            { "Immaculate's Camp", new Vector3(1800, 0, 0)},
            { "Tree Husk", new Vector3(2100, 0, 0)},
            { "Vigil Pylon", new Vector3(2400, 0, 0)},
        };

        public void SetupDirectories()
        {
            Directory.CreateDirectory(script.saveDir);


            foreach (string path in script.Folders.Values)
            {
                Directory.CreateDirectory(path);
            }

            foreach (KeyValuePair<string, List<string>> entry in SceneDic)
            {
                string regionPath = script.Folders["Scenes"] + "/" + entry.Key;

                Directory.CreateDirectory(regionPath);

                foreach (string s in entry.Value)
                {
                    string scenePath = regionPath + "/" + s;
                    Directory.CreateDirectory(scenePath);

                    Directory.CreateDirectory(scenePath + "/Enemies");
                    Directory.CreateDirectory(scenePath + "/Merchants");
                    Directory.CreateDirectory(scenePath + "/Loot");
                    Directory.CreateDirectory(scenePath + "/Loot/Spawns");
                }
            }

            // create small dungeons folders
            for (int i = 0; i < 4; i++)
            {
                string dungeonRegion = "";
                IDictionary dict = new Dictionary<string, Vector3>();

                switch (i)
                {
                    case 0:
                        dict = ChersoneseDungeons;
                        dungeonRegion = script.Folders["Scenes"] + "/Chersonese"; break;
                    case 1:
                        dict = AbrassarDungeons;
                        dungeonRegion = script.Folders["Scenes"] + "/Abrassar"; break;
                    case 2:
                        dict = MarshDungeons;
                        dungeonRegion = script.Folders["Scenes"] + "/Hallowed Marsh"; break;
                    case 3:
                        dict = EnmerkarDungeons;
                        dungeonRegion = script.Folders["Scenes"] + "/Enmerkar Forest"; break;
                    default: break;
                }

                foreach (string s in dict.Keys)
                {
                    string scenePath = dungeonRegion + "/" + s;
                    Directory.CreateDirectory(scenePath);

                    Directory.CreateDirectory(scenePath + "/Enemies");
                    Directory.CreateDirectory(scenePath + "/Merchants");
                    Directory.CreateDirectory(scenePath + "/Loot");
                    Directory.CreateDirectory(scenePath + "/Loot/Spawns");
                }
            }
        }

        public string GetCurrentRegion()
        {
            string region = "ERROR";
            foreach (KeyValuePair<string, List<string>> entry in SceneDic)
            {
                foreach (string s in entry.Value)
                {
                    if (s == script.CurrentScenePretty)
                    {
                        region = entry.Key;
                    }
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

        public string GetCurrentLocation(Vector3 t)
        {
            if (!SceneManagerHelper.ActiveSceneName.ToLower().Contains("dungeonssmall"))
            {
                if (SceneManagerHelper.ActiveSceneName == "Unknown Arena ") { return "Unknown Arena"; }
                else { return script.CurrentScenePretty; }
            }

            string name = "";
            float currentLowest = 99999;

            if (SceneManagerHelper.ActiveSceneName.ToLower().Contains("cherso"))
            {
                foreach (KeyValuePair<string, Vector3> entry in ChersoneseDungeons)
                {
                    if (Vector3.Distance(t, entry.Value) < currentLowest)
                    {
                        currentLowest = Vector3.Distance(t, entry.Value);
                        name = entry.Key;
                    }
                }
            }
            else if (SceneManagerHelper.ActiveSceneName.ToLower().Contains("emercar"))
            {
                foreach (KeyValuePair<string, Vector3> entry in EnmerkarDungeons)
                {
                    if (Vector3.Distance(t, entry.Value) < currentLowest)
                    {
                        currentLowest = Vector3.Distance(t, entry.Value);
                        name = entry.Key;
                    }
                }
            }
            else if (SceneManagerHelper.ActiveSceneName.ToLower().Contains("hallowed"))
            {
                foreach (KeyValuePair<string, Vector3> entry in MarshDungeons)
                {
                    if (Vector3.Distance(t, entry.Value) < currentLowest)
                    {
                        currentLowest = Vector3.Distance(t, entry.Value);
                        name = entry.Key;
                    }
                }
            }
            else if (SceneManagerHelper.ActiveSceneName.ToLower().Contains("abrassar"))
            {
                foreach (KeyValuePair<string, Vector3> entry in AbrassarDungeons)
                {
                    if (Vector3.Distance(t, entry.Value) < currentLowest)
                    {
                        currentLowest = Vector3.Distance(t, entry.Value);
                        name = entry.Key;
                    }
                }
            }

            return name;
        }
    }
}
