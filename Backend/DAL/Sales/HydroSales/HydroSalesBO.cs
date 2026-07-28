using System;

namespace DAL.Sales.HydroSales
{
    public class PostPaymentRequest
    {
        public string InvoiceNo { get; set; }
        public int UserId { get; set; }
    }

    public class PostPaymentResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class GenerateMemoRequest
    {
        public string InvoiceNo { get; set; }
        public decimal OriginalAmount { get; set; }
        public string WebOrderID { get; set; }
        public string CardType { get; set; }
        public int UserId { get; set; }
    }

    public class GenerateMemoResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string GeneratedMemo { get; set; }
    }
}
