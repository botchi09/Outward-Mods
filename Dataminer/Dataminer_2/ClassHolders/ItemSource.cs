using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dataminer
{
    public class ItemSource
    {
        public string ItemName;
        public int ItemID;

        public List<string> Container_Sources = new List<string>();
        public List<string> Spawn_Sources = new List<string>();
    }
}
