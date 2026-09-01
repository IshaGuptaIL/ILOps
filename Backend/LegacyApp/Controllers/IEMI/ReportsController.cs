using DAL.Common.Login;
using DAL.Inventory.IMEI.Report;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LegacyApp.Controllers.IEMI
{
    /// <summary>
    /// Generates hardware/accessory inventory reports, Spire stock status summaries, and receipt exports.
    /// Supports querying receiving data by date ranges, vendors, part numbers, and receipt/PO identifiers.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly IReports _reports;

        public ReportsController(IReports reports)
        {
            _reports = reports;
        }

        /// <summary>
        /// Retrieves complete Spire inventory stock status (onhand, committed, available, costs, values).
        /// Used for generating the Spire Stock Status report and Excel export.
        /// </summary>
        [HttpGet("stock-status")]
        public async Task<IActionResult> GetInventoryStockStatus()
        {
            var data = await _reports.GetInventoryStockStatus();
            return Ok(data);
        }

        /// <summary>
        /// Generates received hardware or accessory reports filtered by date range, vendor, and part.
        /// Maps to qryHardwareReport and qryAccessoryReport in the reporting dashboard.
        /// </summary>
        [HttpGet("received-report")]
        public async Task<IActionResult> GetReceivedReport(
            string itemType,      
            string vendor = null,
            string part = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var data = await _reports.GetReceivedReport(itemType, vendor, part, startDate, endDate);
            return Ok(data);
        }

        /// <summary>
        /// Retrieves the list of active vendors for report filtering dropdowns.
        /// Populates vendor selection filters across the IMEI reporting module.
        /// </summary>
        [HttpGet("vendors")]
        public async Task<IActionResult> GetVendors()
        {
            try
            {
                var response = await _reports.GetVendors();
                if (response.Success)
                    return Ok(response);
                else
                    return BadRequest(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = $"Server error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Retrieves part numbers for a specified item type category (HDW or ACC).
        /// Populates the part number dropdown when generating item-specific reports.
        /// </summary>
        [HttpGet("parts/{itemType}")]
        public async Task<IActionResult> GetParts([FromRoute] string itemType)
        {
            try
            {
                var response = await _reports.GetParts(itemType);
                if (response.Success)
                    return Ok(response);
                else
                    return BadRequest(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = $"Server error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Retrieves Spire inventory receipts within a specified date range and warehouse.
        /// Used for generating and exporting the Spire Receipts report to Excel.
        /// </summary>
        [HttpGet("GetReceipts")]
        public async Task<IActionResult> GetReceipts([FromQuery] DateOnly startDate, [FromQuery] DateOnly endDate, [FromQuery] string whse = "CO")
        {
            if (startDate == default || endDate == default)
                return BadRequest("StartDate and EndDate are required.");

            var data = await _reports.GetSpireReceipts(startDate, endDate, whse);
            return Ok(data);
        }

        /// <summary>
        /// Retrieves hardware receipt records filtered by single Receipt Number or PO Number.
        /// Used by the single receipt/PO search feature on the reporting screen.
        /// </summary>
        [HttpGet("receipts")]
        public async Task<ActionResult<List<HardwareReceiptBO>>> GetHardwareReceipts([FromQuery] string? receiptNo, [FromQuery] string? poNumber)
        {
            if (string.IsNullOrEmpty(receiptNo) && string.IsNullOrEmpty(poNumber))
                return BadRequest("Please provide either ReceiptNo or PO Number");

            var data = await _reports.GetHardwareReceipts(receiptNo, poNumber);
            return Ok(data);
        }
    }
}

