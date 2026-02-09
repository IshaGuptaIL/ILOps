using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DAL.Inventory.ModifyInventory
{
    public class ModifyInventoryBO
    {

       
            public string SearchTerm { get; set; }
            public int CurrentPage { get; set; }
            public int PageSize { get; set; }
            public int TotalPages { get; set; }
            public long TotalItems { get; set; }
            public List<InventoryItemBO> InventoryItems { get; set; } = new();
        }

        public class InventoryItemBO
        {
            public long InventoryId { get; set; }
            public string Whse { get; set; }
            public string PartNo { get; set; }
            public string Description { get; set; }
            public string ProductCode { get; set; }
            public decimal CurrentCost { get; set; }
            public decimal AverageCost { get; set; }
            public decimal SellPrice { get; set; }
            public long? UomId { get; set; }
        }

        public class WarehousePriceBO
        {
            public string Whse { get; set; }
            public decimal CurrentCost { get; set; }
            public decimal AverageCost { get; set; }
            public decimal SellPrice { get; set; }
        }


    public class PriceUpdateModel
    {
        [Required]
        public string PartNo { get; set; } = string.Empty;
        public string? Whse { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Current Cost cannot be negative")]
        public decimal CurrentCost { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Average Cost cannot be negative")]
        public decimal AverageCost { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Sell Price cannot be negative")]
        public decimal SellPrice { get; set; }

        [Required(ErrorMessage = "UOM ID is required")]
        public long? UomId { get; set; }
    }
}

