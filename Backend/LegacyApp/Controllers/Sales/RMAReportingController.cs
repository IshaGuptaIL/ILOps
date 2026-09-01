using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DAL.Sales.RMAReporting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LegacyApp.Controllers.Sales
{
    /// <summary>
    /// Manages RMA reporting, Rogers Credit Memo (CM) and RMA file imports, cascading reconciliation grids, and audit reports.
    /// Replicates the Access RMA Reporting module functionality including file batch management and discrepancy auditing.
    /// </summary>
    [ApiController]
    [Route("api/sales/rmareporting")]
    public class RMAReportingController : ControllerBase
    {
        private readonly IRMAReportingDA _da;

        public RMAReportingController(IRMAReportingDA da)
        {
            _da = da;
        }

        private string GetCurrentUser()
        {
            if (Request.Cookies.TryGetValue("UserLogin", out var user) && !string.IsNullOrWhiteSpace(user))
            {
                return user;
            }
            if (User?.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(User.Identity.Name))
            {
                return User.Identity.Name;
            }
            return "System";
        }

        // ==========================================
        // 1. IMEI SEARCH ENDPOINTS
        // ==========================================
        /// <summary>
        /// Searches RMA tracking database by IMEI, waybill, claim number, or SKU criteria.
        /// Returns detailed lifecycle history and current credit claim status.
        /// </summary>
        [HttpGet("imeisearch/search")]
        public async Task<IActionResult> SearchIMEI([FromQuery] string criteria = "IMEI", [FromQuery] string query = "")
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            try
            {
                var result = await _da.SearchIMEIAsync(criteria, query, cts.Token);
                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(408, "Request Timeout: Search took longer than 10 minutes.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ==========================================
        // 2. FILE IMPORT ENDPOINTS (frmRogersReportImport)
        // ==========================================
        /// <summary>
        /// Imports Rogers Credit Memo (CM) Excel files into staging table (tblRogersReportCM).
        /// Parses transaction dates, invoice numbers, legal entity, and balance due.
        /// </summary>
        [HttpPost("import/cm")]
        public async Task<IActionResult> ImportCMFile(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Please select a file to import.");
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            try
            {
                using var stream = file.OpenReadStream();
                string user = GetCurrentUser();
                var result = await _da.ImportCMFileAsync(stream, file.FileName, user, cts.Token);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Imports Rogers RMA response files and performs automated matching against staged CM records.
        /// Populates tblRogersReportCMRMA with matched credit and return records.
        /// </summary>
        [HttpPost("import/rm")]
        public async Task<IActionResult> ImportRMFile(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Please select a file to import.");
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            try
            {
                using var stream = file.OpenReadStream();
                string user = GetCurrentUser();
                var result = await _da.ImportRMFileAsync(stream, file.FileName, user, cts.Token);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Imports Manual RMA spreadsheet files into the RMA database.
        /// Used for handling offline or manual credit adjustments and returns.
        /// </summary>
        [HttpPost("import/manual")]
        public async Task<IActionResult> ImportManualFile(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Please select a file to import.");
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            try
            {
                using var stream = file.OpenReadStream();
                string user = GetCurrentUser();
                var result = await _da.ImportManualRMAAsync(stream, file.FileName, user, cts.Token);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves lists of imported file batch names for CM, RM, and Manual files.
        /// Populates the batch deletion and filter selection dropdowns.
        /// </summary>
        [HttpGet("import/batches")]
        public async Task<IActionResult> GetImportBatches()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            try
            {
                var summary = await _da.GetImportBatchesAsync(cts.Token);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Deletes a specified batch of imported CM or RM records from staging and match tables.
        /// Reverses an accidental or invalid file import batch.
        /// </summary>
        [HttpPost("import/delete-batch")]
        public async Task<IActionResult> DeleteImportBatch([FromBody] DeleteBatchRequestDTO request)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            try
            {
                string user = GetCurrentUser();
                var ok = await _da.DeleteImportBatchAsync(request, user, cts.Token);
                return Ok(new { success = ok, message = "Import batch deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves summary statistics of imported CM files including total records, amounts, and matched counts.
        /// Populates the CM summary view.
        /// </summary>
        [HttpGet("import/cm-summary")]
        public async Task<IActionResult> GetCMSummary()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            try
            {
                var list = await _da.GetCMSummaryAsync(cts.Token);
                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ==========================================
        // 2.1. RECONCILE CASCADING GRIDS (frmFILESReconcile)
        // ==========================================
        /// <summary>
        /// Retrieves top-tier summary list of imported CM files (file name, date ranges, total transaction count).
        /// Represents the primary selection grid on the cascading reconciliation screen.
        /// </summary>
        [HttpGet("reconcile/files")]
        public async Task<IActionResult> GetReconcileFiles()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            try
            {
                var list = await _da.GetReconcileFilesAsync(cts.Token);
                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves second-tier grouped classifications (Class, Type, Source, Total Other, CMTotal, RMTotal) for a file.
        /// Represents the middle cascading grid on the reconciliation screen.
        /// </summary>
        [HttpGet("reconcile/file-types")]
        public async Task<IActionResult> GetReconcileFileTypes([FromQuery] string fileName = "")
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            try
            {
                var list = await _da.GetReconcileFileTypesAsync(fileName, cts.Token);
                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves third-tier itemized transaction records (frmRogersReportCM) filtered by file and classification.
        /// Displays individual invoice lines, balance due, and discovery comments.
        /// </summary>
        [HttpGet("reconcile/details")]
        public async Task<IActionResult> GetReconcileDetails(
            [FromQuery] string fileName = "",
            [FromQuery] string? className = null,
            [FromQuery] string? typeName = null,
            [FromQuery] string? sourceName = null)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            try
            {
                var list = await _da.GetReconcileDetailsAsync(fileName, className, typeName, sourceName, cts.Token);
                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ==========================================
        // 3. REPORTS ENDPOINTS (frmReports2)
        // ==========================================
        /// <summary>
        /// Executes specialized RMA reporting queries (credit matches, credits not expected, returns no credit, price variance).
        /// Returns report dataset for on-screen review.
        /// </summary>
        [HttpGet("reports/query")]
        [HttpPost("reports/query")]
        public async Task<IActionResult> RunReportQuery([FromQuery] string queryType = "creditMatches", [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            try
            {
                var param = new ReportQueryParamsDTO
                {
                    QueryType = queryType,
                    StartDate = startDate,
                    EndDate = endDate
                };
                var list = await _da.RunReportQueryAsync(param, cts.Token);
                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Generates and exports an RMA report query result into a downloadable Excel (.xlsx) file.
        /// Provides formatted spreadsheet export for auditing and finance reconciliation.
        /// </summary>
        [HttpGet("reports/export")]
        public async Task<IActionResult> ExportReport([FromQuery] string queryType = "creditMatches", [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            try
            {
                var param = new ReportQueryParamsDTO
                {
                    QueryType = queryType,
                    StartDate = startDate,
                    EndDate = endDate
                };
                var bytes = await _da.ExportReportExcelAsync(param, cts.Token);
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{queryType}_{DateTime.UtcNow:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Executes the Read Rogers Returns workflow to sync return transaction logs within a date range.
        /// Updates local return tracking records.
        /// </summary>
        [HttpPost("reports/read-returns")]
        public async Task<IActionResult> ReadRogersReturns([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            try
            {
                string user = GetCurrentUser();
                var ok = await _da.ReadRogersReturnsAsync(startDate, endDate, user, cts.Token);
                return Ok(new { success = ok, message = "Read In Rogers Returns process completed successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ==========================================
        // 4. UTILITIES & USERS ENDPOINTS (frmUtility / frmUsers)
        // ==========================================
        /// <summary>
        /// Retrieves the list of active RMA system users.
        /// Displays user accounts on the RMA utilities user management screen.
        /// </summary>
        [HttpGet("utilities/users")]
        public async Task<IActionResult> GetUsers()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            try
            {
                var users = await _da.GetUsersAsync(cts.Token);
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Saves or updates an RMA system user's profile and active status.
        /// Modifies user definitions in the usermaster table.
        /// </summary>
        [HttpPost("utilities/save-user")]
        public async Task<IActionResult> SaveUser([FromBody] SaveRMAUserRequestDTO request)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            try
            {
                string user = GetCurrentUser();
                var ok = await _da.SaveUserAsync(request, user, cts.Token);
                return Ok(new { success = ok, message = "User saved successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Executes administrative data cleanup and staging table reset procedures.
        /// Clears temporary staging data across selected scopes.
        /// </summary>
        [HttpPost("utilities/reset-data")]
        public async Task<IActionResult> ResetData([FromQuery] string scope = "all")
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            try
            {
                string user = GetCurrentUser();
                var success = await _da.ResetDataAsync(scope, user, cts.Token);
                return Ok(new { success, message = "Data reset procedure completed." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
