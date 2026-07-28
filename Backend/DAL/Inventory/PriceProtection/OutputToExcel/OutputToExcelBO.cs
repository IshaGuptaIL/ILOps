using System;

namespace DAL.Inventory.PriceProtection.OutputToExcel
{
    public class ExportRequest
    {
        public int BatchId { get; set; }
        public string? ExportType { get; set; }
    }

    public class ClaimsToCreditsRow
    {
        public string? Sku { get; set; }
        public string? Description { get; set; }
        public string? Imei { get; set; }
        public int? ClaimBatchID { get; set; }
        public DateTime? ClaimDate { get; set; }
        public decimal ClaimAmount { get; set; }
        public decimal UnitCreditAmount { get; set; }
        public string? CreditNoteNumber { get; set; }
        public DateTime? CreditNoteDate { get; set; }
    }
}
