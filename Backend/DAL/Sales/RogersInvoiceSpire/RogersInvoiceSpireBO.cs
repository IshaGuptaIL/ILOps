using System;

namespace DAL.Sales.RogersInvoiceSpire
{
    public class ProcessDataRequest
    {
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public string ReturnsStart { get; set; } = string.Empty;
        public string ReturnsEnd { get; set; } = string.Empty;
    }

    public class ProcessDataResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class CostVerificationRow
    {
        public string? TransactionNo { get; set; }
        public string? Invoice { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string? CustName { get; set; }
        public string? CustTerritory { get; set; }
        public string? Whse { get; set; }
        public string? PartNumber { get; set; }
        public string? FreeAccessory { get; set; }
        public double? Qty { get; set; }
        public string? IMEIESN { get; set; }
        public double? CostPrice { get; set; }
        public double? SellPrice { get; set; }
        public double? TopUpOwing { get; set; }
        public decimal? BVReceiptCost { get; set; }
        public double? NetIMEIReceiveCost { get; set; }
        public double? NetPriceProtection { get; set; }
        public string? PONumber { get; set; }
        public string? BVReceipt { get; set; }
        public string? MISC_1 { get; set; }
    }

    public class DailySalesRow
    {
        public string? InvoiceNo { get; set; }
        public string? WebOrderID { get; set; }
        public DateTime? Date { get; set; }
        public string? PaymentMethod { get; set; }
        public string? TransNo { get; set; }
        public string? CustNo { get; set; }
        public string? CustName { get; set; }
        public decimal? Total { get; set; }
        public string? InvTerr { get; set; }
        public string? CustTerr { get; set; }
    }

    public class ReturnsVerificationRow
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? ChannelName { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Type { get; set; }
        public string? Invoice { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string? CustTerritory { get; set; }
        public string? CellPhoneNo { get; set; }
        public string? WebOrderID { get; set; }
        public double? Qty { get; set; }
        public string? PartNumber { get; set; }
        public string? FreeAccessory { get; set; }
        public string? IMEIESN { get; set; }
        public double? CostPrice { get; set; }
        public double? SellPrice { get; set; }
        public double? TopUpOwing { get; set; }
        public double? AccessoryCost { get; set; }
        public double? AccessoryPrice { get; set; }
        public double? TopUpAcc { get; set; }
        public double? TopUpTotal { get; set; }
        public double? ARAmount { get; set; }
        public double? HDWChargeToCustomer { get; set; }
        public double? TrueHDWTopUp { get; set; }
        public double? ACCChargeToCx { get; set; }
        public double? AccMargin { get; set; }
        public string? Group { get; set; }
        public string? Source { get; set; }
        
        // Match columns
        public string? ChannelName2 { get; set; }
        public string? PaymentMethod2 { get; set; }
        public string? Type2 { get; set; }
        public string? Invoice2 { get; set; }
        public DateTime? InvoiceDate2 { get; set; }
        public string? CustTerritory2 { get; set; }
        public string? CellPhoneNo2 { get; set; }
        public string? WebOrderID2 { get; set; }
        public double? Qty2 { get; set; }
        public string? PartNumber2 { get; set; }
        public string? FreeAccessory2 { get; set; }
        public string? IMEIESN2 { get; set; }
        public double? CostPrice2 { get; set; }
        public double? SellPrice2 { get; set; }
        public double? TopUpOwing2 { get; set; }
        public double? AccessoryCost2 { get; set; }
        public double? AccessoryPrice2 { get; set; }
        public double? TopUpAcc2 { get; set; }
        public double? TopUpTotal2 { get; set; }
        public double? ARAmount2 { get; set; }
        public double? HDWChargeToCustomer2 { get; set; }
        public double? TrueHDWTopUp2 { get; set; }
        public double? ACCChargeToCx2 { get; set; }
        public double? AccMargin2 { get; set; }
        public string? Group2 { get; set; }
    }
}
