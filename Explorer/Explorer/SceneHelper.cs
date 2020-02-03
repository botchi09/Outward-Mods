using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace OutwardExplorer
{
    public class SceneHelper : MonoBehaviour
    {
        public DumperScript script;

        public Dictionary<string, List<string>> SceneDic = new Dictionary<string, List<string>>
        {
            { "Abrassar", new List<string> {"Abrassar", "Ancient Hive", "Electric Lab", "Levant", "Sand Rose Cave", "Stone Titan Caves", "The Slide", "Undercity Passage" } },
            { "Enmerkar Forest", new List<string> {"Ancestor’s Resting Place", "Berg", "Cabal of Wind Temple", "Enmerkar Forest", "Face of the Ancients", "Forest Hives", "Necropolis", "Royal Manticore’s Lair" } },
            { "Chersonese", new List<string> { "Blister Burrow", "Blue Chamber’s Conflux Path", "Chersonese", "Cierzo", "Cierzo Storage", "Conflux Chambers", "Corrupted Tombs", "Ghost Pass", "Heroic Kingdom’s Conflux Path", "Holy Mission’s Conflux Path", "Montcalm Clan Fort", "Vendavel Fortress", "Voltaic Hatchery" } },
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
            if (region == "")
            {
                if (SceneManagerHelper.ActiveSceneName.Contains("Cherso"))
                    region = "Chersonese";
                else if (SceneManagerHelper.ActiveSceneName.Contains("Hallowed"))
                    region = "Hallowed Marsh";
                else if (SceneManagerHelper.ActiveSceneName.Contains("Emercar"))
                    region = "Enmerkar Forest";
                else if (SceneManagerHelper.ActiveSceneName.Contains("Abrassar"))
                    region = "Abrassar";
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
