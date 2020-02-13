using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Dataminer
{
    public class ItemSpawnHolder
    {
        public string Name;
        public int Item_ID;
        public int Quantity;
        public List<Vector3> positions = new List<Vector3>();
    }
}
