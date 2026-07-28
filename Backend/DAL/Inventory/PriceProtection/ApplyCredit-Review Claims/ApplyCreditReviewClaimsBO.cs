using System;
using System.Collections.Generic;

namespace DAL.Inventory.PriceProtection.ApplyCredit_ReviewClaims
{
    public class ClaimsSummaryRow
    {
        public int ClaimBatchID { get; set; }
        public DateTime? DatePriceDrop { get; set; }
        public string PartNo { get; set; } = string.Empty;
        public decimal PriceBefore { get; set; }
        public decimal PriceAfter { get; set; }
        public int Count { get; set; }
        public decimal TotalClaimed { get; set; }
        public decimal TotalPaid { get; set; }
        public int MinOfID { get; set; }
        public decimal TotalOutstanding { get; set; }
    }

    public class CreditSummaryRow
    {
        public int ClaimBatchID { get; set; }
        public string? CreditNoteNumber { get; set; }
        public DateTime? DatePriceDrop { get; set; }
        public string PartNo { get; set; } = string.Empty;
        public DateTime? CreditDate { get; set; }
        public decimal MaxOfPriceBeforeDrop { get; set; }
        public decimal MaxOfPriceAfterDrop { get; set; }
        public int Count { get; set; }
        public decimal UnitAmount { get; set; }
        public decimal TotalClaimed { get; set; }
        public decimal TotalPaid { get; set; }
        public int CreditCount { get; set; }
        public int MinOfID { get; set; }
        public decimal TotalOutstanding { get; set; }
    }

    public class UnpaidClaimsDetailRow
    {
        public int ClaimBatchID { get; set; }
        public int ID { get; set; }
        public DateTime? PriceDropDate { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string? CreditNoteNumber { get; set; }
        public string IMEI { get; set; } = string.Empty;
        public DateTime? ReceiptDate { get; set; }
        public decimal ReceiptCost { get; set; }
        public decimal PriceBeforeDrop { get; set; }
        public decimal PriceAfterDrop { get; set; }
        public decimal ClaimAmount { get; set; }
        public decimal ClaimAmountPaid { get; set; }
    }

    public class CreditDetailRow
    {
        public decimal UnitCreditAmount { get; set; }
        public string? CreditNoteNumber { get; set; }
        public DateTime? CreditNoteDate { get; set; }
        public int PPClaimID { get; set; }
        public string IMEI { get; set; } = string.Empty;
    }

    public class ModifyCreditNumberRequest
    {
        public string OldCreditNoteNumber { get; set; } = string.Empty;
        public string NewCreditNoteNumber { get; set; } = string.Empty;
    }

    public class ApplyCreditRequest
    {
        public int ClaimBatchID { get; set; }
        public string? CreditNoteNumber { get; set; }
        public List<int> SelectedClaimIds { get; set; } = new();
        public string ApplyCreditNoteNumber { get; set; } = string.Empty;
        public DateTime ApplyCreditNoteDate { get; set; }
        public decimal CreditUnitAmount { get; set; }
    }
}
