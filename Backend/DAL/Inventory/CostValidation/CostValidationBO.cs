using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.CostValidation
{
    public class CostValidationBO
    {
    }



    public class HpcRecord
    {
        public string Whse { get; set; }
        public string Part { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public string SpireProdCode { get; set; }
        public decimal RogersCost { get; set; }
        public decimal? SpireCost { get; set; }
        public string ExistInSpire { get; set; }
        public decimal? OnhandQty { get; set; }
        public decimal? PurchaseQty { get; set; }
    }

    public class HardwareVsSpire
    {
        public int HardwareID { get; set; }
        public string BVPartNumber { get; set; }
        public string Model { get; set; }
        public string SpireDescription { get; set; }
        public decimal RDDealerCost { get; set; }
        public decimal SpireCurrentCost { get; set; }
        public string ProductCode { get; set; }
        public DateTime? LastSaleDate { get; set; }
    }

    public class InvalidRecord
    {
        public int RowNumber { get; set; }
        public string SKU { get; set; }
        public string Column { get; set; }
        public string Value { get; set; }
        public string Reason { get; set; }
    }

}
