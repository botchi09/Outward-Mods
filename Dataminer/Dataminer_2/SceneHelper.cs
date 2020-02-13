using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Dataminer
{
    public class SceneHelper
    {
        public static void SetupSceneSummaries()
        {
            foreach (KeyValuePair<string, List<string>> entry in ScenesByRegion)
            {
                var region = entry.Key;
                foreach (string location in entry.Value)
                {
                    ListManager.SceneSummaries.Add(region + ":" + location, new SceneSummary { SceneName = location });
                }

                List<string> list = null;
                switch (region)
                {
                    case "Chersonese":
                        list = SceneHelper.ChersoneseDungeons.Keys.ToList();
                        break;
                    case "Abrassar":
                        list = SceneHelper.AbrassarDungeons.Keys.ToList();
                        break;
                    case "Hallowed Marsh":
                        list = SceneHelper.MarshDungeons.Keys.ToList();
                        break;
                    case "Enmerkar Forest":
                        list = SceneHelper.EnmerkarDungeons.Keys.ToList();
                        break;
                    default:
                        break;
                }
                if (list != null)
                {
                    foreach (string location in list)
                    {
                        ListManager.SceneSummaries.Add(region + ":" + location, new SceneSummary { SceneName = location });
                    }
                }
            }
        }

        public static Dictionary<string, string> SceneBuildNames = new Dictionary<string, string>
        {
            { "CierzoTutorial", "Shipwreck Beach" },
            { "CierzoNewTerrain", "Cierzo" },
            { "CierzoDestroyed", "Cierzo (Destroyed)" },
            { "ChersoneseNewTerrain", "Chersonese" },
            { "Chersonese_Dungeon1", "Vendavel Fortress" },
            { "Chersonese_Dungeon2", "Blister Burrow" },
            { "Chersonese_Dungeon3", "Ghost Pass" },
            { "Chersonese_Dungeon4_BlueChamber", "Blue Chamber’s Conflux Path" },
            { "Chersonese_Dungeon4_HolyMission", "Holy Mission’s Conflux Path" },
            { "Chersonese_Dungeon4_Levant", "Heroic Kingdom’s Conflux Path" },
            { "Chersonese_Dungeon5", "Voltaic Hatchery" },
            { "Chersonese_Dungeon4_CommonPath", "Conflux Chambers" },
            { "Chersonese_Dungeon6", "Corrupted Tombs" },
            { "Chersonese_Dungeon8", "Cierzo Storage" },
            { "Chersonese_Dungeon9", "Montcalm Clan Fort" },
            { "ChersoneseDungeonsSmall", "Chersonese Misc. Dungeons" },
            { "ChersoneseDungeonsBosses", "Unknown Arena" },
            { "Monsoon", "Monsoon" },
            { "HallowedMarshNewTerrain", "Hallowed Marsh" },
            { "Hallowed_Dungeon1", "Jade Quarry" },
            { "Hallowed_Dungeon2", "Giants’ Village" },
            { "Hallowed_Dungeon3", "Reptilian Lair" },
            { "Hallowed_Dungeon4_Interior", "Dark Ziggurat Interior" },
            { "Hallowed_Dungeon5", "Spire of Light" },
            { "Hallowed_Dungeon6", "Ziggurat Passage" },
            { "Hallowed_Dungeon7", "Dead Roots" },
            { "HallowedDungeonsSmall", "Marsh Misc. Dungeons" },
            { "HallowedDungeonsBosses", "Unknown Arena" },
            { "Levant", "Levant" },
            { "Abrassar", "Abrassar" },
            { "Abrassar_Dungeon1", "Undercity Passage" },
            { "Abrassar_Dungeon2", "Electric Lab" },
            { "Abrassar_Dungeon3", "The Slide" },
            { "Abrassar_Dungeon4", "Stone Titan Caves" },
            { "Abrassar_Dungeon5", "Ancient Hive" },
            { "Abrassar_Dungeon6", "Sand Rose Cave" },
            { "AbrassarDungeonsSmall", "Abrassar Misc. Dungeons" },
            { "AbrassarDungeonsBosses", "Unknown Arena" },
            { "Berg", "Berg" },
            { "Emercar", "Enmerkar Forest" },
            { "Emercar_Dungeon1", "Royal Manticore’s Lair" },
            { "Emercar_Dungeon2", "Forest Hives" },
            { "Emercar_Dungeon3", "Cabal of Wind Temple" },
            { "Emercar_Dungeon4", "Face of the Ancients" },
            { "Emercar_Dungeon5", "Ancestor’s Resting Place" },
            { "Emercar_Dungeon6", "Necropolis" },
            { "EmercarDungeonsSmall", "Enmerkar Misc. Dungeons" },
            { "EmercarDungeonsBosses", "Unknown Arena" },
            { "DreamWorld", "In Between" },
        };

        public static Dictionary<string, List<string>> ScenesByRegion = new Dictionary<string, List<string>>
        {
            {
                "Abrassar",
                new List<string>
                {
                    "Abrassar",
                    "Ancient Hive",
                    "Electric Lab",
                    "Levant",
                    "Sand Rose Cave",
                    "Stone Titan Caves",
                    "The Slide",
                    "Undercity Passage"
                }
            },
            {
                "Enmerkar Forest",
                new List<string>
                {
                    "Ancestor’s Resting Place",
                    "Berg",
                    "Cabal of Wind Temple",
                    "Enmerkar Forest",
                    "Face of the Ancients",
                    "Forest Hives",
                    "Necropolis",
                    "Royal Manticore’s Lair"
                }
            },
            {
                "Chersonese",
                new List<string>
                {
                    "Blister Burrow",
                    "Blue Chamber’s Conflux Path",
                    "Chersonese",
                    "Cierzo",
                    "Cierzo (Destroyed)",
                    "Cierzo Storage",
                    "Conflux Chambers",
                    "Corrupted Tombs",
                    "Ghost Pass",
                    "Heroic Kingdom’s Conflux Path",
                    "Holy Mission’s Conflux Path",
                    "Montcalm Clan Fort",
                    "Shipwreck Beach",
                    "Vendavel Fortress",
                    "Voltaic Hatchery"
                }
            },
            {
                "Hallowed Marsh",
                new List<string>
                {
                    "Dark Ziggurat Interior",
                    "Dead Roots",
                    "Giants’ Village",
                    "Hallowed Marsh",
                    "Jade Quarry",
                    "Monsoon",
                    "Reptilian Lair",
                    "Spire of Light",
                    "Ziggurat Passage"
                }
            },
            {
                "Other",
                new List<string>
                {
                    "Unknown Arena",
                    "In Between"
                }
            }
        };

        public static Dictionary<string, Vector3> ChersoneseDungeons = new Dictionary<string, Vector3>
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

        public static Dictionary<string, Vector3> MarshDungeons = new Dictionary<string, Vector3>
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

        public static Dictionary<string, Vector3> AbrassarDungeons = new Dictionary<string, Vector3>
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

        public static Dictionary<string, Vector3> EnmerkarDungeons = new Dictionary<string, Vector3>
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
    }
}
