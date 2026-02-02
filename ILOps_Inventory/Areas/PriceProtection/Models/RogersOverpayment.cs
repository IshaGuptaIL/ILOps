namespace ILOps_Inventory.Areas.PriceProtection.Models
{
    public class RogersOverpayment
    {
        public int Id { get; set; }
        public string Dealer { get; set; }
        public decimal Amount { get; set; }
        public string FileName { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
