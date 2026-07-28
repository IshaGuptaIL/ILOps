using DAL.Sales.RogersInvoiceSpire;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.IO.Compression;

namespace LegacyApp.Controllers.Sales
{
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

        [HttpPost("ProcessData")]
        public async Task<ActionResult<ProcessDataResult>> ProcessData([FromBody] ProcessDataRequest request, [FromQuery] int? userId)
        {
            if (request == null) return BadRequest("Invalid request.");
            int uId = GetUserId(userId);
            var result = await _da.ProcessDataAsync(request, uId);
            return Ok(result);
        }

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

        [HttpGet("HdwFeeCheck")]
        public async Task<ActionResult<List<CostVerificationRow>>> HdwFeeCheck([FromQuery] int? userId)
        {
            int uId = GetUserId(userId);
            var result = await _da.GetHdwFeeReportAsync(uId);
            return Ok(result);
        }

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
