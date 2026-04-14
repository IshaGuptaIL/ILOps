using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.OutputInvoice
{
    public class OutputInvoiceBO
    {
    }

    // REQUEST MODEL (API / Service call)
    public class InvoiceOutputRequest
    {
        //public string OutputFolder { get; set; }
        public string FilePrefix { get; set; }
        public string InvoiceType { get; set; } = "Normal";
    }

    // PAGED RESPONSE
    public class PagedInvoiceResponse
    {
        public List<InvoiceItem> Data { get; set; } = new List<InvoiceItem>();
        public int TotalCount { get; set; }
    }

    // INVOICE HEADER + DETAIL MODEL
    public class InvoiceDetail
    {
        public string BillToName { get; set; }
        public string BillToAddress1 { get; set; }
        public string BillToAddress2 { get; set; }
        public string BillToCity { get; set; }
        public string ShipToName { get; set; }
        public string ShipToAddress1 { get; set; }
        public string ShipToCity { get; set; }
        public string CustNo { get; set; }
        public string InvoiceDate { get; set; }
        public string ShipToAddress2 { get; set; }
        public string OrderNo { get; set; }
        public decimal Shipping { get; set; }
        public decimal GST_HST { get; set; }
        public decimal PST_QST { get; set; }
        public decimal RV_Value { get; set; }
        public List<InvoiceItemLine> Lines { get; set; } = new List<InvoiceItemLine>();
    }

    // INVOICE LINE ITEMS
    public class InvoiceItemLine
    {
        public string PartNo { get; set; }

        public string Description { get; set; }

        public decimal Qty { get; set; }

        public decimal Price { get; set; }

        public string SerialNo { get; set; }   // Device Serial Number

        public string SimNo { get; set; }      // SIM Number

        public decimal LineTotal
        {
            get { return Qty * Price; }
        }
    }

    public class InvoiceItem
    {
        public string InvoiceNo { get; set; }

        public string Status { get; set; } = "Pending";
    }
}