using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.SpareLight
{
    public class SpareLightBO
    {
    }

    public class HardwareTransferBO
    {
        public int Id { get; set; }
        public string WarehouseCodeTransferFrom { get; set; } = string.Empty;
        public string WarehouseCodeTransferTo { get; set; } = string.Empty;
        public string PartNo { get; set; } = string.Empty;
        public string IMEI { get; set; } = string.Empty;
        public string? SimPartNo { get; set; }
        public string? SimNo { get; set; }
        public string? Pin { get; set; }
        public int RowNumber { get; set; }
        public string? ValidationResult { get; set; }
        public bool IsValid => string.IsNullOrEmpty(ValidationResult);
    }

    public class AccessoryTransferBO
    {
        public int Id { get; set; }
        public string WarehouseCodeTransferFrom { get; set; } = string.Empty;
        public string WarehouseCodeTransferTo { get; set; } = string.Empty;
        public string PartNo { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public int RowNumber { get; set; }
        public string? ValidationResult { get; set; }
        public bool IsValid => string.IsNullOrEmpty(ValidationResult);
    }

    public class SpireTransferReference
    {
        public int Id { get; set; }
        public string? TransferNo { get; set; }
        public string? Links { get; set; }
    }

}
