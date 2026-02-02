namespace ILOps_Inventory.Areas.Inventory.Models
{
    public class ModifyInventory
    {
        public long? EditInventoryId { get; set; }
        public List<InventoryItem> InventoryItems { get; set; } = new();
        public string SearchTerm { get; set; } = "";
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; } = 1;
        public long TotalItems { get; set; } = 0;
    }

    public class InventoryItem
    {
        public long InventoryId { get; set; }
        public string Whse { get; set; } = "";
        public string PartNo { get; set; } = "";
        public string Description { get; set; } = "";
        public string? ProductCode { get; set; }
        public decimal CurrentCost { get; set; }
        public decimal AverageCost { get; set; }
        public decimal? SellPrice { get; set; }
        public long? UomId { get; set; }
    }

    public class PriceUpdateModel
    {
        public long InventoryId { get; set; }
        public long? UomId { get; set; }  // ✅ Nullable long
        public string Whse { get; set; }
        public string PartNo { get; set; }
        public decimal CurrentCost { get; set; }
        public decimal AverageCost { get; set; }
        public decimal SellPrice { get; set; }
    }

    public class WarehousePriceModel
    {
        public long InventoryId { get; set; }
        public string Warehouse { get; set; }
        public decimal CurrentCost { get; set; }
        public decimal AverageCost { get; set; }
        public decimal? SellPrice { get; set; }
        public long? UomId { get; set; }
    }
}
