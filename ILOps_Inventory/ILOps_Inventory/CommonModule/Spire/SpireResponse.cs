namespace ILOps_Inventory.Common.Spire
{
    public class SpireResponse
    {
        public int HttpStatus { get; set; }
        public string HttpStatusText { get; set; }
        public string HeaderResponse { get; set; }
        public string HeaderLocation { get; set; }
        public long HeaderKey { get; set; }
        public long HeaderContentLength { get; set; }
        public long ResponseTime { get; set; }
        public string Allow { get; set; }

    }
}
