using System;

namespace DAL.Inventory.PriceProtection.RogerOverPayments
{
    public class ImportedFileRow
    {
        public string Filename { get; set; } = string.Empty;
        public DateTime? ImportedDate { get; set; }
        public int Count { get; set; }
    }
}
