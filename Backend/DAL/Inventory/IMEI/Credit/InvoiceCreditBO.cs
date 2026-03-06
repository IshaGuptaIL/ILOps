using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.IMEI.Credit
{
    public class InvoiceCreditBO
    {
    }


    public class SearchReceiptsBO
    {
        public string ReceiptNo { get; set; }
        public string PONumber { get; set; }
        public string Type { get; set; }
    }
    public class FindReceiptBO
    {
        public string ReceiptNo { get; set; }
    }

    public class HardwareReceivedVM
    {
        public string BVReceiptNo { get; set; } = "";
        public string VendorName { get; set; } = "";
        public DateTime ReceiptDate { get; set; } = DateTime.MinValue;
        public string CMO { get; set; } = "";
        public string PONumber { get; set; } = "";
        public string PartNo { get; set; } = "";
        public int QtyReceived { get; set; } = 0;
        public decimal UnitCost { get; set; } = 0;
        public string Type { get; set; } = "";
        public string Whse { get; set; } = ""; // Optional warehouse info
    }
    public class AccReceiptTransferModel
    {
        public string ID { get; set; }
        public string PartNo { get; set; }
        public DateTime ReceiveDate { get; set; }
        public string LinkNo { get; set; }
        public decimal Cost { get; set; }
        public double Qty { get; set; }
        public string VendorNo { get; set; }
    }
    public class SaveInvoiceBO
    {
        public string BVReceiptNo { get; set; }
        public string TransType { get; set; }
        public string RefNo { get; set; }
        public DateTime TransDate { get; set; }
        public decimal PerUnitAmount { get; set; }
        public string Remarks { get; set; }
    }

}
