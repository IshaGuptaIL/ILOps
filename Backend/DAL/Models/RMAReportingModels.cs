using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    [Table("tblRMA")]
    public class TblRMA
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public string? SKU { get; set; }
        public string? IMEI { get; set; }
        public string? ReturnReasonCode { get; set; }
        public string? ExtraInfo { get; set; }
        public bool? OutputCSV { get; set; }
        public DateTime? OutputCSVDate { get; set; }
        public string? OutputCSVBatch { get; set; }
        public string? ValidationResults { get; set; }
        public string? RogersResponse { get; set; }
        public string? InvoiceSold { get; set; }
        public DateTime? InvoiceSoldDate { get; set; }
        public string? WhseSold { get; set; }
        public string? BVCreditOrder { get; set; }
        public string? ReturnedRogers { get; set; }
        public string? ReturnedRogersBVOrder { get; set; }
        public string? Swap { get; set; }
        public string? SwapCMO { get; set; }
        public bool? Pristine { get; set; }
        public bool? RejectedACT { get; set; }
        public bool? Closed { get; set; }
        public string? FinalDisposition { get; set; }
        public string? ReturnWaybill { get; set; }
        public DateTime? LogInDate { get; set; }
        public decimal? CreditAmtClaimed { get; set; }
        public string? User { get; set; }
        public string? Status { get; set; }

        // Audit Fields
        public string CreatedBy { get; set; } = "SYSTEM";
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string ModifiedBy { get; set; } = "SYSTEM";
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
    }

    [Table("tblRMA_Responses")]
    public class TblRMAResponses
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public string? IMEI { get; set; }
        public string? RogersResponse { get; set; }
        public string? RMANumber { get; set; }
        public DateTime? RMADate { get; set; }
        public string? HeaderReturnReason { get; set; }
        public string? FileName { get; set; }
        public string? ITEM { get; set; }
        public int? Qty { get; set; }
        public DateTime? DateReceived { get; set; }
        public DateTime? DateIssued { get; set; }
        public DateTime? VPFLastMoveDate { get; set; }
        public DateTime? VPFAssignDate { get; set; }
        public string? ReturnReason { get; set; }
        public decimal? CreditAmount { get; set; }
        public decimal? RestockFee { get; set; }
        public decimal? TotalCredit { get; set; }
        public string? Status { get; set; }
        public string? LastStatusMessage { get; set; }
        public bool? RMAUpdated { get; set; }
        public string? RejectReason { get; set; }
        public string? RejectReasonComment { get; set; }

        // Audit Fields
        public string CreatedBy { get; set; } = "SYSTEM";
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string ModifiedBy { get; set; } = "SYSTEM";
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
    }

    [Table("tblRogersReportCMRMA")]
    public class TblRogersReportCMRMA
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public string? CMNumber { get; set; }
        public DateTime? CMDate { get; set; }
        public decimal? CMAmount { get; set; }
        public string? RMA { get; set; }
        public string? SKU { get; set; }
        public int? Qty { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? RMAmount { get; set; }
        public decimal? RMAmountTotal { get; set; }
        public string? IMEIRMA { get; set; }
        public string? CMImportFile { get; set; }
        public string? RMImportFile { get; set; }

        // Audit Fields
        public string CreatedBy { get; set; } = "SYSTEM";
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string ModifiedBy { get; set; } = "SYSTEM";
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    }

    [Table("tblRogersReportCM")]
    public class TblRogersReportCM
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public string? Source { get; set; }
        public string? OperatingUnit { get; set; }
        public string? LegalEntityName { get; set; }
        public string? Number { get; set; }
        public string? BillToCustomer { get; set; }
        public string? Class { get; set; }
        public string? Complete { get; set; }
        public decimal? BalanceDue { get; set; }
        public string? Currency { get; set; }
        public DateTime? Date { get; set; }
        public DateTime? GLDate { get; set; }
        public string? Salesperson { get; set; }
        public string? Terms { get; set; }
        public string? Type { get; set; }
        public string? DiscoverComment { get; set; }
        public string? ImportFileName { get; set; }
        public DateTime? DateImported { get; set; }

        // Audit fields
        public string CreatedBy { get; set; } = "SYSTEM";
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string ModifiedBy { get; set; } = "SYSTEM";
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    }

    public class ReconcileFileSummaryDTO
    {
        public string ImportFileName { get; set; } = "";
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Count { get; set; }
    }

    public class ReconcileFileTypeDTO
    {
        public string ImportFileName { get; set; } = "";
        public string? Class { get; set; }
        public string? Type { get; set; }
        public string? Source { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Count { get; set; }
        public decimal TotalOther { get; set; }
        public decimal? CMTotal { get; set; }
        public decimal? RMTotal { get; set; }
    }

    public class RogersReportCMDetailDTO
    {
        public int Id { get; set; }
        public string? Class { get; set; }
        public string? Source { get; set; }
        public string? Type { get; set; }
        public string? OperatingUnit { get; set; }
        public string? LegalEntityName { get; set; }
        public string? Number { get; set; }
        public DateTime? Date { get; set; }
        public decimal? BalanceDue { get; set; }
        public string? DiscoverComment { get; set; }
        public string? ImportFileName { get; set; }
    }

    public class IMEISearchResultDto
    {
        public System.Collections.Generic.List<TblRMA> RmaResults { get; set; } = new();
        public System.Collections.Generic.List<TblRMAResponses> RogersResponses { get; set; } = new();
        public System.Collections.Generic.List<TblRogersReportCMRMA> CmRmaResults { get; set; } = new();
    }
}
