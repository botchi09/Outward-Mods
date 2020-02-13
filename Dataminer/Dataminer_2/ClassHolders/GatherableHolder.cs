using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Dataminer
{ 
    public class GatherableHolder
    {
        public string Name;
        public int ItemID;

        public List<string> DropTables = new List<string>();

        public static GatherableHolder ParseGatherable(Gatherable gatherable)
        {
            var gatherableHolder = new GatherableHolder
            {
                Name = gatherable.Name,
                ItemID = gatherable.ItemID
            };

            if (At.GetValue(typeof(SelfFilledItemContainer), gatherable as SelfFilledItemContainer, "m_drops") is List<Dropable> droppers)
            {
                if (droppers == null || droppers.Count < 1)
                {
                    //Debug.LogWarning("droppers is null or list count is 0!");
                }
                else
                {
                    foreach (Dropable dropper in droppers)
                    {
                        var dropableHolder = DroptableHolder.ParseDropTable(dropper);
                        gatherableHolder.DropTables.Add(dropableHolder.Name);
                    }
                }
            }

            if (gatherableHolder.Name == "Fish")
            {
                gatherableHolder.Name = "Fishing Spot (" + gatherableHolder.DropTables[0] + ")";
            }

            return gatherableHolder;
        }
    }
}
