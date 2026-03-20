using DAL.Common.Login;
using DAL.Inventory.IMEI.Report;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LegacyApp.Controllers.IEMI
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly IReports _reports;

        public ReportsController(IReports reports)
        {
            _reports = reports;
        }

        [HttpGet("stock-status")]
        public async Task<IActionResult> GetInventoryStockStatus()
        {
            var data = await _reports.GetInventoryStockStatus();
            return Ok(data);
        }



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




        [HttpGet("GetReceipts")]
        public async Task<IActionResult> GetReceipts([FromQuery] DateOnly startDate, [FromQuery] DateOnly endDate, [FromQuery] string whse = "CO")
        {
            if (startDate == default || endDate == default)
                return BadRequest("StartDate and EndDate are required.");

            var data = await _reports.GetSpireReceipts(startDate, endDate, whse);
            return Ok(data);
        }

        [HttpGet("receipts")]
        public async Task<ActionResult<List<HardwareReceiptBO>>> GetHardwareReceipts([FromQuery] string? receiptNo, [FromQuery] string? poNumber)
        {
            if (string.IsNullOrEmpty(receiptNo) && string.IsNullOrEmpty(poNumber))
                return BadRequest("Please provide either ReceiptNo or PO Number");

            var data = await _reports.GetHardwareReceipts(receiptNo, poNumber); // Pass both to DAL
            return Ok(data);
        }

    }
}

