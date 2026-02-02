namespace ILOps_Inventory.Common.Spire
{
    public class SpireInventoryItemRequest
    {
        public string whse { get; set; } = string.Empty;
        public string partNo { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public decimal currentCost { get; set; }
        public decimal averageCost { get; set; }
        public string? userDef1 { get; set; }
        public Dictionary<string, SpireInventoryPricingDetail> pricing { get; set; } = new();
        public bool allowBackorders { get; set; }
        public string? groupNo { get; set; }
        public int salesDept { get; set; }
        public bool serialized { get; set; }
        public string? PROD { get; set; }  // License specific
    }

    public class SpireInventoryPricingDetail
    {
        public List<decimal> sellPrices { get; set; } = new();
    }

    public class SpireResponse
    {
        public int HttpStatus { get; set; }
        public string? HttpStatusText { get; set; }
        public string? HeaderResponse { get; set; }
        public string? HeaderLocation { get; set; }
        public long HeaderContentLength { get; set; }
        public long ResponseTime { get; set; }
        public long? HeaderKey { get; set; }
        public string? Allow { get; set; }
    }
}
