using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dataminer_2
{
    public class ContainerSummary
    {
        public string Name;
        public int ItemID;

        public List<string> Locations_Found = new List<string>();
        public List<string> All_DropTables = new List<string>();
    }
}
