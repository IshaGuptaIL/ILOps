using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.Count
{
    public class CountBO
    {
    }

    public class CountRequestBO
    {
        public string FileName { get; set; }
    }

    public class InventorySnapshotBO
    {
        public bool LoadACC { get; set; }
        public bool LoadIMEI { get; set; }
    }
}
