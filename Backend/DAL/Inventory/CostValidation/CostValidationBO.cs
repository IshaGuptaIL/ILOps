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
        public int hardwareID { get; set; }
        public string spirePartNumber { get; set; }
        public string model { get; set; }
        public string spireDescription { get; set; }
        public decimal rDDealerCost { get; set; }
        public decimal spireCurrentCost { get; set; }
        public string productCode { get; set; }
        public DateTime? lastSaleDate { get; set; }
    }

    public class InvalidRecord
    {
        public int RowNumber { get; set; }
        public string SKU { get; set; }
        public string Column { get; set; }
        public string Value { get; set; }
        public string Reason { get; set; }
    }
    public class CostVarianceCurrentVsAvg
    {
        public string Whse { get; set; }
        public string PartNo { get; set; }
        public string Description { get; set; }
        public decimal CurrentCost { get; set; }
        public decimal AverageCost { get; set; }
    }

    public class CostVarianceAcrossWarehouses
    {
        public string PartNo { get; set; }
        public string Whse { get; set; }
        public string Description { get; set; }
        public decimal CurrentCost { get; set; }
        public decimal AverageCost { get; set; }
    }


}
