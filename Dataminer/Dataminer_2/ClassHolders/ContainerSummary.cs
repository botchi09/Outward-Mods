using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dataminer
{
    public class ContainerSummary
    {
        public string Name;
        public int ItemID;

        public List<SceneSummary.QuantityHolder> All_Locations = new List<SceneSummary.QuantityHolder>();
        public List<ContainerDroptableSummary> DropTables = new List<ContainerDroptableSummary>();
    }

    public class ContainerDroptableSummary
    {
        public string DropTableName;
        public List<string> Locations = new List<string>();
    }
}
