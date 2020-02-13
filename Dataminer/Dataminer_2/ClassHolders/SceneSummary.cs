using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dataminer
{
    public class SceneSummary
    {
        public string SceneName;

        public List<QuantityHolder> Enemies = new List<QuantityHolder>();
        public List<string> Merchants = new List<string>();

        public List<QuantityHolder> Loot_Containers = new List<QuantityHolder>();
        public List<QuantityHolder> Gatherables = new List<QuantityHolder>();
        public List<ItemSpawnHolder> Item_Spawns = new List<ItemSpawnHolder>();

        public class QuantityHolder
        {
            public string Name;
            public int Quantity;
        }
    }
}
