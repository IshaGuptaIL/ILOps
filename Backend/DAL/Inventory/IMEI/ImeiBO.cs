using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.IMEI
{
    public class ImeiBO
    {
    }
    public class HardwareReceivedVM
    {
        public string BVReceiptNo { get; set; }
        public string VendorName { get; set; } // Added Vendor
        public DateTime ReceiptDate { get; set; }
        public string CMO { get; set; }    // Keep CMO
        public string PONumber { get; set; }
        public string Whse { get; set; }   // Added Warehouse
        public string PartNo { get; set; }
        public int QtyReceived { get; set; }
        public decimal UnitCost { get; set; }
        public string Type { get; set; }
    }


    public class RogersInvoiceVM
    {
        public string TransType { get; set; }  // I, C, D
        public string RefNo { get; set; }      // Invoice / Credit / Debit No
        public DateTime TransDate { get; set; }
        public decimal PerUnitAmount { get; set; }
        public int? Qty { get; set; }
        public string Remarks { get; set; }
    }

}
