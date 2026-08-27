using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DAL.Models;

namespace DAL.Sales.RMAReporting
{
    public interface IRMAReportingDA
    {
        // 1. IMEI Search
        Task<IMEISearchResponseDTO> SearchIMEIAsync(string criteria, string query, CancellationToken cancellationToken);

        // 2. File Import & Batch Management (frmRogersReportImport & frmFILESReconcile)
        Task<FileImportResultDTO> ImportCMFileAsync(Stream fileStream, string fileName, string user, CancellationToken cancellationToken);
        Task<FileImportResultDTO> ImportRMFileAsync(Stream fileStream, string fileName, string user, CancellationToken cancellationToken);
        Task<FileImportResultDTO> ImportManualRMAAsync(Stream fileStream, string fileName, string user, CancellationToken cancellationToken);
        Task<ImportBatchSummaryDTO> GetImportBatchesAsync(CancellationToken cancellationToken);
        Task<bool> DeleteImportBatchAsync(DeleteBatchRequestDTO request, string user, CancellationToken cancellationToken);
        Task<List<CMSummaryRowDTO>> GetCMSummaryAsync(CancellationToken cancellationToken);
        Task<List<ReconcileFileSummaryDTO>> GetReconcileFilesAsync(CancellationToken cancellationToken);
        Task<List<ReconcileFileTypeDTO>> GetReconcileFileTypesAsync(string fileName, CancellationToken cancellationToken);
        Task<List<RogersReportCMDetailDTO>> GetReconcileDetailsAsync(string fileName, string? className, string? typeName, string? sourceName, CancellationToken cancellationToken);

        // 3. Reports (frmReports2)
        Task<List<GenericReportRowDTO>> RunReportQueryAsync(ReportQueryParamsDTO param, CancellationToken cancellationToken);
        Task<byte[]> ExportReportExcelAsync(ReportQueryParamsDTO param, CancellationToken cancellationToken);
        Task<bool> ReadRogersReturnsAsync(DateTime? startDate, DateTime? endDate, string user, CancellationToken cancellationToken);

        // 4. Utilities & Users (frmUtility / frmUsers)
        Task<List<RMAUserDTO>> GetUsersAsync(CancellationToken cancellationToken);
        Task<bool> SaveUserAsync(SaveRMAUserRequestDTO request, string user, CancellationToken cancellationToken);
        Task<bool> ResetDataAsync(string resetScope, string user, CancellationToken cancellationToken);
    }
}
