using System;
using System.Collections.Generic;
using DAL.Models;

namespace DAL.Sales.RMAReporting
{
    // ==========================================
    // IMEI Search DTOs
    // ==========================================
    public class IMEISearchFilterRequest
    {
        public string Criteria { get; set; } = "IMEI";
        public string Query { get; set; } = string.Empty;
    }

    public class IMEISearchResponseDTO
    {
        public List<TblRMA> RmaResults { get; set; } = new();
        public List<TblRMAResponses> RogersResponses { get; set; } = new();
        public List<TblRogersReportCMRMA> CmRmaResults { get; set; } = new();
    }

    // ==========================================
    // File Import & Batch Management DTOs
    // ==========================================
    public class ImportBatchSummaryDTO
    {
        public List<string> CmFiles { get; set; } = new();
        public List<string> RmFiles { get; set; } = new();
        public List<string> ManualFiles { get; set; } = new();
    }

    public class FileImportResultDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int RecordsImported { get; set; }
        public string FileName { get; set; } = string.Empty;
    }

    public class DeleteBatchRequestDTO
    {
        public string? CmFile { get; set; }
        public string? RmFile { get; set; }
        public string? ManualFile { get; set; }
    }

    // ==========================================
    // Reports DTOs (frmReports2)
    // ==========================================
    public class ReportQueryParamsDTO
    {
        public string QueryType { get; set; } = "creditMatches";
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class GenericReportRowDTO
    {
        public int ID { get; set; }
        public string? Col1 { get; set; }
        public string? Col2 { get; set; }
        public string? Col3 { get; set; }
        public string? Col4 { get; set; }
        public string? Col5 { get; set; }
        public string? Col6 { get; set; }
        public string? Col7 { get; set; }
        public string? Col8 { get; set; }
        public string? Col9 { get; set; }
        public string? Col10 { get; set; }
        public decimal? Amount1 { get; set; }
        public decimal? Amount2 { get; set; }
        public DateTime? Date1 { get; set; }
        public DateTime? Date2 { get; set; }
        public string? Status { get; set; }
    }

    public class CMSummaryRowDTO
    {
        public string? ImportFileName { get; set; }
        public string? ReturnReasonCode { get; set; }
        public int TotalRecords { get; set; }
        public decimal TotalAmount { get; set; }
        public int MatchedCount { get; set; }
        public int UnmatchedCount { get; set; }
    }

    // ==========================================
    // Utilities & Users DTOs (frmUtility / frmUsers)
    // ==========================================
    public class RMAUserDTO
    {
        public int ID { get; set; }
        public string? UserName { get; set; }
        public string? UserInitials { get; set; }
        public string? UserRole { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class SaveRMAUserRequestDTO
    {
        public int? ID { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserInitials { get; set; } = string.Empty;
        public string? UserRole { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
