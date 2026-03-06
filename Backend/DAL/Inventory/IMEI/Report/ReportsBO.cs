using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.IMEI.Report
{
    public class ReportsBO
    {
    }

    public class SpireReceiptBO
    {
        public int Id { get; set; }
        public DateTime ReceiveDate { get; set; }
        public string Whse { get; set; }
        public string PartNo { get; set; }
        public string Description { get; set; }
        public string ProductCode { get; set; }
        public decimal Qty { get; set; }
        public decimal Cost { get; set; }
        public decimal Selling { get; set; }
        public string LinkNo { get; set; }
        public string LinkTable { get; set; }
        public string RefNo { get; set; }
        public decimal NewAverageCost { get; set; }
        public decimal NewOnhandQty { get; set; }
    }



    public class InventoryStockBO
    {
        public string Whse { get; set; }
        public string PartNo { get; set; }
        public string Description { get; set; }
        public string ProductCode { get; set; }
        public string SalesAcct { get; set; }
        public string SerialNumber { get; set; }
        public int Onhand { get; set; }
        public int Committed { get; set; }
        public int Available { get; set; }
        public decimal CurrentCost { get; set; }
        public decimal AverageCost { get; set; }
        public decimal CurrentValue { get; set; }
        public int Backorder { get; set; }
        public string Group { get; set; }
    }



    public class HardwareReceiptBO
    {
        public string Vendor { get; set; }
        public string BVReceiptNo { get; set; }
        public DateTime BVReceiptDate { get; set; }
        public string CMO { get; set; }
        public string PO { get; set; }
        public string Part { get; set; }
        public int Qty { get; set; }
        public decimal ReceiptUnitCost { get; set; }
        public string IMEI { get; set; }

        // From qryRogerInvoiceTotals
        public decimal? RogersTotal { get; set; }
        public int? RogersCount { get; set; }
        public string FirstOfTransType { get; set; }
        public string FirstOfRefNo { get; set; }
        public DateTime? FirstOfTransDate { get; set; }
        public decimal? FirstOfPerUnitAmount { get; set; }
        public string FirstOfRemarks { get; set; }
    }


    public class ReceivedReportBO
    {
        public string Vendor { get; set; }
        public string BVReceiptNo { get; set; }
        public DateTime BVReceiptDate { get; set; }
        public string CMO { get; set; }
        public string PO { get; set; }
        public string Part { get; set; }
        public decimal ReceiptUnitCost { get; set; }
        public string IMEI { get; set; }
        public int? Qty { get; set; }

        public decimal? RogersTotal { get; set; }
        public int? RogersCount { get; set; }
        public string FirstOfTransType { get; set; }
        public string FirstOfRefNo { get; set; }
        public DateTime? FirstOfTransDate { get; set; }
        public decimal? FirstOfPerUnitAmount { get; set; }
        public string FirstOfRemarks { get; set; }
    }


    public class RogersInvoice
    {
        public int ID { get; set; }
        public string BVReceiptNo { get; set; }
        public string TransType { get; set; }
        public string RefNo { get; set; }
        public DateTime? TransDate { get; set; }
        public decimal PerUnitAmount { get; set; }
        public string Remarks { get; set; }
    }
    public class VendorBO
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
