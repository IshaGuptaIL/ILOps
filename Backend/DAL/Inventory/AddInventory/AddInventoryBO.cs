using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.AddInventory
{
    public class AddInventoryBO
    {
        public string Whse { get; set; }
        public string PartNo { get; set; }
        public string Description { get; set; }
        public string FrDescription { get; set; }
        public string Type { get; set; }
        public string ProductCode { get; set; }
        public int SalesDept { get; set; }
        public string? AccessoryGroup { get; set; }
        public decimal CostPrice { get; set; }
        public decimal? SellingPrice { get; set; }
   
    
    }

    public class ManufacturerBO
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string InventoryType { get; set; } = "";
    }

    public class WarehouseBO
    {
        public string Whse { get; set; }
        public string Description { get; set; }
    }


}
