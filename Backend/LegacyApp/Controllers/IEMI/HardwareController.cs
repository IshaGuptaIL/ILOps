using DAL.Inventory.IMEI.HardwareIMEI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LegacyApp.Controllers.IEMI
{
    /// <summary>
    /// Handles hardware purchase order processing, IMEI validation, and Spire receiving operations.
    /// Provides endpoints for PO retrieval, Excel IMEI file parsing, error verification, and receipt posting.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class HardwareController : ControllerBase
    {
        private readonly IHardwareService _hardwareService;

        public HardwareController(IHardwareService hardwareService)
        {
            _hardwareService = hardwareService;
        }

        /// <summary>
        /// Retrieves open and active purchase orders (status 'I' / 'R') from Spire.
        /// Used by the Receive IMEI form to populate available PO line items.
        /// </summary>
        [HttpGet("purchase-orders")]
        public async Task<IActionResult> GetPurchaseOrders()
        {
            var response = await _hardwareService.GetPurchaseOrdersAsync();
            return response.Success ? Ok(response.Data) : BadRequest(response.Message);
        }

        /// <summary>
        /// Reads and extracts IMEI serial numbers from an uploaded Excel file stream.
        /// Used to import Scan Lists and Packing Slips for verification.
        /// </summary>
        [HttpPost("upload-excel")]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded");
            using var stream = file.OpenReadStream();
            var response = await _hardwareService.ParseExcelImeisAsync(stream);
            return response.Success ? Ok(response.Data) : BadRequest(response.Message);
        }

        private int GetUserId()
        {
            if (Request.Cookies.TryGetValue("userId", out var cookieUserId) && int.TryParse(cookieUserId, out var parsedId))
                return parsedId;
            if (Request.Headers.TryGetValue("userId", out var headerUserId) && int.TryParse(headerUserId.ToString(), out var parsedHeaderId))
                return parsedHeaderId;
            return 1; // Default fallback legacy user ID
        }

        /// <summary>
        /// Performs complete multi-tier validation checks (duplicates, format, cross-matching, PO remaining qty, Spire onhand).
        /// Returns verification status and lists of matching, missing, or conflicting serial numbers.
        /// </summary>
        [HttpPost("check-errors")]
        public async Task<IActionResult> CheckErrors([FromBody] CheckErrorsRequest request)
        {
            request.UserId = GetUserId();
            var response = await _hardwareService.CheckErrorsAsync(request);
            return response.Success ? Ok(response.Data) : BadRequest(response.Message);
        }

        /// <summary>
        /// Finalizes and posts IMEI receipts or reversals to Spire PO and logs received items to HardwareReceived table.
        /// Updates line serial numbers and generates formal receipt records in Spire.
        /// </summary>
        [HttpPost("receive")]
        public async Task<IActionResult> ReceiveImei([FromBody] ReceiveImeiRequest request)
        {
            request.UserId = GetUserId();
            var response = await _hardwareService.ReceiveImeiAsync(request);
            return response.Success ? Ok(response.Data) : BadRequest(response.Message);
        }
    }
}


