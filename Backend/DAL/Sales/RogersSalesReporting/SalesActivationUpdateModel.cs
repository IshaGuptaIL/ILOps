using System;

namespace LegacyApp.DAL.Sales.RogersSalesReporting
{
    public class SalesActivationUpdateModel
    {
        public string Invoice10 { get; set; }
        public string TransactionNo { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public DateTime? OrderDate { get; set; }
        public string CustName { get; set; }
        public string CustTerritory { get; set; }
        public string UserName { get; set; }
        public string CellPhoneNo { get; set; }
        public string VoicePlan { get; set; }
        public string DataPlan { get; set; }
        public string WebOrderID { get; set; }
        public string AdjustmentType { get; set; }
        public bool? Supress { get; set; }
        public decimal? Fee { get; set; }
        public decimal? TopUpSDFAcc { get; set; }
        public decimal? TopUpSDF { get; set; }
        public decimal? TopUpSDFLic { get; set; }
        public string OriginalInvoice { get; set; }
        public int? Qty { get; set; }
        public string whse { get; set; }
        public string PartNumber { get; set; }
        public string ProductCode { get; set; }
        public string FreeAccessory { get; set; }
        public string FreeAccessoryPart { get; set; }
        public string IMEIESN { get; set; }
        public decimal? AccessoryCost { get; set; }
        public decimal? AccessoryPrice { get; set; }
        public string CAPHardware { get; set; }
        public int? BVInvoiceLine { get; set; }
        public decimal? InvoiceNet { get; set; }
        public decimal? InvoiceShipping { get; set; }
        public decimal? InvoiceTaxes { get; set; }
        public decimal? InvoiceTotal { get; set; }
        public string PayMeth { get; set; }
        public string TermsText { get; set; }
        public string Channel { get; set; }
        public string SCOA { get; set; }
        public string Customer { get; set; }
        public string SIMCardNo { get; set; }
    }
}
