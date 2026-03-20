using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.CustomSearch
{
    public class CustomSearchBO
    {
    }
    public class SalesActivationSearchRequest
    {
        public string FieldName { get; set; }
        public string Value { get; set; }
    }
    public class SalesActivationHeaderBO
    {
        public int Seq { get; set; }
        public string Invoice { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string CustomerNo { get; set; }
        public string CustomerName { get; set; }
        public decimal InvoiceTotal { get; set; }
        public string CustTerritory { get; set; }
        public string WebOrderId { get; set; }
        public string OriginalInvoice { get; set; }
        public decimal? UpfrontEdge { get; set; }
        public string? Adjustment { get; set; }
        public string PaymentMethod { get; set; }
        public string TransactionNumber { get; set; }
    }

    public class SalesActivationDetailBO
    {
        public string Whse { get; set; }
        public string PartNo { get; set; }
        public string Description { get; set; }
        public string SerialNo { get; set; }
        public string Comment { get; set; }
        public int Committed { get; set; }
        public decimal UnitPrice { get; set; }
    }

}
