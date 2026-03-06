using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.InventoryType
{
    public class InventoryBO
    {
       
            public int Id { get; set; }
            public string Name { get; set; }
            public string InventoryType { get; set; } // HCC or ACC
            public bool IsActive { get; set; }
    }
}
