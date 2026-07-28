using System;

namespace DAL.Inventory.PriceProtection
{
    public class PriceProtectionBO
    {
    }

    public class PriceProtectionBatchRow
    {
        public int ID { get; set; }
        public string? ReceiptNo { get; set; }
        public DateTime? ReceiptDate { get; set; }
        public decimal ReceiptCost { get; set; }
        public DateTime? PriceDropDate { get; set; }
        public string? SKU { get; set; }
        public string? Description { get; set; }
        public string? IMEI { get; set; }
        public DateTime? ClaimDate { get; set; }
        public decimal ClaimAmount { get; set; }
        public decimal PriceBeforeDrop { get; set; }
        public decimal PriceAfterDrop { get; set; }
        public decimal PreviousClaim { get; set; }
        public string? Memo { get; set; }
        public string? PONumber { get; set; }
        public decimal ClaimAmountPaid { get; set; }
    }

    public class PriceProtectionClaimRow
    {
        public int ID { get; set; }
        public string? ReceiptNo { get; set; }
        public DateTime? ReceiptDate { get; set; }
        public decimal ReceiptCost { get; set; }
        public DateTime? PriceDropDate { get; set; }
        public decimal PriceBeforeDrop { get; set; }
        public decimal PriceAfterDrop { get; set; }
        public string? SKU { get; set; }
        public string? Description { get; set; }
        public string? IMEI { get; set; }
        public decimal ClaimAmount { get; set; }
        public decimal PreviousClaim { get; set; }
        public string? PONumber { get; set; }
        public DateTime? ClaimDate { get; set; }
        public int ClaimBatchID { get; set; }
        public decimal ClaimAmountPaid { get; set; }
    }

    public class ReceiptInfoBO
    {
        public string? PartNo { get; set; }
        public decimal Cost { get; set; }
        public string? Description { get; set; }
        public decimal Qty { get; set; }
        public string? PONumber { get; set; }
    }

    public class PostedClaimSummaryBO
    {
        public int ClaimBatchID { get; set; }
        public string? SKU { get; set; }
        public string? Description { get; set; }
        public DateTime? ClaimDate { get; set; }
        public int UnitCount { get; set; }
        public decimal TotalClaimAmount { get; set; }
    }
}