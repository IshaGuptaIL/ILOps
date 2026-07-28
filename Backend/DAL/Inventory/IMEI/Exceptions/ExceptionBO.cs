using System;

namespace DAL.Inventory.IMEI.Exceptions
{
    public class ExceptionBO
    {
        public int ID { get; set; }
        public string VBCode { get; set; }
        public string VBDescription { get; set; }
        public string PONumber { get; set; }
        public int? RecNo { get; set; }
        public string ErrorWhile { get; set; }
        public int? RowCount { get; set; }
        public bool Resolved { get; set; }

        // Audit Fields
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}
