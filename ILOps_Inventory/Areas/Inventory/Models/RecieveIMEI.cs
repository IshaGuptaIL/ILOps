namespace ILOps_Inventory.Areas.Inventory.Models
{
    public class RecieveIMEI
    {
    }

    public class TblScanList
    {
        public int Id { get; set; }
        public int PONumber { get; set; }
        public int RecNo { get; set; }
        public string Whse { get; set; }
        public string PartNo { get; set; }
        public string GUID { get; set; }
        public string Vendor { get; set; }
        public string Location { get; set; }
        public string IMEI { get; set; }
        public int XLSRow { get; set; }
    }

    // INVOICE CREDIT 
    public class InvoiceCreditPageVM
    {
        public List<HardwareReceivedVM> MissingReceipts { get; set; } = new();
        public List<RogersInvoiceVM> RogersInvoices { get; set; } = new();
        public string SelectedReceiptNo { get; set; } = "";
        public string FindPO { get; set; }
        public string FindReceiptNo { get; set; }
        public string SelectedType { get; set; } = "Hardware";

        // Pagination
        public int CurrentPage { get; set; }
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }


        // *** Add these for "Add New With Last Info" button ***
        public string LastInvoiceRefNo { get; set; } = "";
        public DateTime? LastInvoiceTransDate { get; set; } = null;
        public string LastInvoiceTransType { get; set; } = "I";  // Default: I (Invoice)
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

    public class SaveInvoiceRequest
    {
        public string BVReceiptNo { get; set; }
        public string TransType { get; set; }
        public string RefNo { get; set; }
        public DateTime TransDate { get; set; }
        public decimal PerUnitAmount { get; set; }
        public string Remarks { get; set; }
        public bool HasMoreEntries { get; set; }
        public string? EditingRefNo { get; set; }

    }

    // INVOICE CREDIT 
}
