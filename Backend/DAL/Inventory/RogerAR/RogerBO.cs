using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.RogerAR
{
    public class RogerBO
    {
    }
    public class RogerarBO
    {
        public string CustomerNo { get; set; }
        public string Transaction { get; set; }
        public DateTime? Date { get; set; }
        public string InvoiceNo { get; set; }
        public decimal DebitAmt { get; set; }
        public decimal Balance { get; set; }
        public string CustomerName { get; set; }
        public string Territory { get; set; }

        // From RogersARData table
        public string Comments { get; set; }
        public string Remarks { get; set; }
        public DateTime? SentOn { get; set; }
        public string Comments2 { get; set; }
        public string Comments3 { get; set; }
        public string PaymentCode { get; set; }
        public DateTime? PaymentDate { get; set; }

        // Audit Columns
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }

    public class RogerarRequest
    {
        public string SearchTerm { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class RogerarListResponse
    {
        public List<RogerarBO> Items { get; set; }
        public int TotalItems { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
