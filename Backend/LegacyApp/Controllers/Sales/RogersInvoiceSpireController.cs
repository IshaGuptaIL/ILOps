using DAL.Sales.RogersInvoiceSpire;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.IO.Compression;

namespace LegacyApp.Controllers.Sales
{
    /// <summary>
    /// Processes Rogers sales invoice reconciliation against Spire ERP sales records.
    /// Provides cost verification, payment summaries, returns audit, hardware fee checks, and estimate exports.
    /// </summary>
    [Route("api/Sales/[controller]")]
    [ApiController]
    public class RogersInvoiceSpireController : ControllerBase
    {
        private readonly IRogersInvoiceSpireDA _da;

        public RogersInvoiceSpireController(IRogersInvoiceSpireDA da)
        {
            _da = da;
        }

        private int GetUserId(int? requestUserId)
        {
            if (requestUserId.HasValue && requestUserId.Value > 0)
            {
                return requestUserId.Value;
            }
            if (Request.Cookies.TryGetValue("userId", out string userIdStr) && int.TryParse(userIdStr, out int userId))
            {
                return userId;
            }
            return 1; // Fallback for testing
        }

        /// <summary>
        /// Executes staging data processing and cross-reconciliation between Rogers invoices and Spire records.
        /// Identifies matching sales lines, adjustments, and pricing variances.
        /// </summary>
        [HttpPost("ProcessData")]
        public async Task<ActionResult<ProcessDataResult>> ProcessData([FromBody] ProcessDataRequest request, [FromQuery] int? userId)
        {
            if (request == null) return BadRequest("Invalid request.");
            int uId = GetUserId(userId);
            var result = await _da.ProcessDataAsync(request, uId);
            return Ok(result);
        }

        /// <summary>
        /// Generates cost verification report comparing billed amounts against standard costs.
        /// Highlights cost discrepancies across sales invoices within a date range.
        /// </summary>
        [HttpGet("CostVerificationReport")]
        public async Task<ActionResult<List<CostVerificationRow>>> GetCostVerificationReport([FromQuery] string startDate, [FromQuery] string endDate)
        {
            if (string.IsNullOrEmpty(startDate) || string.IsNullOrEmpty(endDate))
            {
                return BadRequest("Start and End dates are required.");
            }
            var result = await _da.GetCostVerificationReportAsync(startDate, endDate);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves daily sales summaries categorized by payment method for a specified date range.
        /// Provides high-level revenue totals across tender types.
        /// </summary>
        [HttpGet("DailySalesSummary")]
        public async Task<ActionResult<List<DailySalesRow>>> GetDailySalesSummary([FromQuery] string startDate, [FromQuery] string endDate)
        {
            if (string.IsNullOrEmpty(startDate) || string.IsNullOrEmpty(endDate))
            {
                return BadRequest("Start and End dates are required.");
            }
            var result = await _da.GetSalesSummaryByPaymentMethodAsync(startDate, endDate);
            return Ok(result);
        }

        /// <summary>
        /// Generates returns verification report auditing return items against Rogers return authorizations.
        /// Identifies missing credits, return date mismatches, and quantity differences.
        /// </summary>
        [HttpGet("ReturnsVerificationReport")]
        public async Task<ActionResult<List<ReturnsVerificationRow>>> GetReturnsVerificationReport(
            [FromQuery] string startDate, 
            [FromQuery] string endDate, 
            [FromQuery] string returnsStart, 
            [FromQuery] string returnsEnd, 
            [FromQuery] int? userId)
        {
            if (string.IsNullOrEmpty(startDate) || string.IsNullOrEmpty(endDate) || 
                string.IsNullOrEmpty(returnsStart) || string.IsNullOrEmpty(returnsEnd))
            {
                return BadRequest("All dates are required.");
            }
            int uId = GetUserId(userId);
            var result = await _da.GetReturnsVerificationReportAsync(startDate, endDate, returnsStart, returnsEnd, uId);
            return Ok(result);
        }

        /// <summary>
        /// Audits hardware fee adjustments to detect missing or mismatched fee calculations.
        /// Outputs report of hardware transactions requiring fee correction.
        /// </summary>
        [HttpGet("HdwFeeCheck")]
        public async Task<ActionResult<List<CostVerificationRow>>> HdwFeeCheck([FromQuery] int? userId)
        {
            int uId = GetUserId(userId);
            var result = await _da.GetHdwFeeReportAsync(uId);
            return Ok(result);
        }

        /// <summary>
        /// Generates and exports Rogers invoice estimate calculations into a downloadable CSV file.
        /// Facilitates external review of expected commission and invoice charges.
        /// </summary>
        [HttpGet("DownloadRogersEstimate")]
        public async Task<IActionResult> DownloadRogersEstimate([FromQuery] int? userId)
        {
            try
            {
                int uId = GetUserId(userId);
                byte[] fileBytes = await _da.GetRogersEstimateCsvAsync(uId);
                return File(fileBytes, "text/csv", $"RogersInvoiceEstimate_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
