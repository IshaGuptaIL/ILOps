using System;

namespace DAL.Inventory.PriceProtection.ImeiSearch
{
    public class ImeiSearchClaimRow
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
        public DateTime? ClaimDate { get; set; }
        public decimal ClaimAmount { get; set; }
        public bool ClaimPaid { get; set; }
        public bool Flag { get; set; }
        public string? PreviousClaim { get; set; }
        public string? Memo { get; set; }
        public string? PONumber { get; set; }
        public decimal UnitCredit { get; set; }
        public decimal UnitDebit { get; set; }
        public decimal NetUnitCost { get; set; }
        public string? LastInvoice { get; set; }
        public string? LastCredit { get; set; }
        public DateTime? LastInvoiceDate { get; set; }
        public DateTime? LastCreditDate { get; set; }
    }

    public class ImeiSearchCreditRow
    {
        public int PPClaimID { get; set; }
        public string? ReceiptNo { get; set; }
        public string? SKU { get; set; }
        public string? IMEI { get; set; }
        public decimal UnitCreditAmount { get; set; }
        public string? CreditNoteNumber { get; set; }
        public DateTime? CreditNoteDate { get; set; }
    }

    public class ImeiSearchOverpaymentRow
    {
        public string? DEALER { get; set; }
        public string? ORDER_NUMBER { get; set; }
        public string? INVOICE_NUMBER { get; set; }
        public string? IMEI { get; set; }
        public string? SKU { get; set; }
        public string? SKU_DESCRIPTION { get; set; }
        public decimal NEW_PRICE { get; set; }
        public decimal DEALER_COST { get; set; }
        public decimal PP_AMOUNT { get; set; }
        public string? CM_No { get; set; }
        public DateTime? CM_Date { get; set; }
        public DateTime? DateImported { get; set; }
        public string? Filename { get; set; }
    }
}
