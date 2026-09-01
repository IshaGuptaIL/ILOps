using DAL.Common.Login;
using DAL.Inventory.SpareLight;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LegacyApp.Controllers.Inventory
{
    /// <summary>
    /// Manages Spare Light hardware and accessory transfers between physical warehouse locations.
    /// Handles transfer spreadsheet uploads, stock validation, transfer execution, and audit log tracking.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class SpareLightController : ControllerBase
    {
        private readonly ISpareLight _spareLightDA;

        public SpareLightController(ISpareLight spareLightDA)
        {
            _spareLightDA = spareLightDA;
        }

        /// <summary>
        /// Uploads an Excel file containing hardware transfer items (IMEI serials, parts, locations).
        /// Parses rows and prepares them in staging for validation.
        /// </summary>
        [HttpPost("UploadHardware")]
        public async Task<IActionResult> UploadHardware(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            using var stream = file.OpenReadStream();
            var items = await _spareLightDA.ParseHardwareExcelAsync(stream);
            return Ok(new ApiResposne { Success = true, Result = items, Message = $"{items.Count} items parsed" });
        }

        /// <summary>
        /// Validates staged hardware transfer items against warehouse on-hand stock and serial status.
        /// Flags any unavailable serials or location mismatches before transfer.
        /// </summary>
        [HttpPost("ValidateHardware")]
        public async Task<ApiResposne> ValidateHardware()
        {
            return await _spareLightDA.ValidateHardwareTransferAsync();
        }

        /// <summary>
        /// Executes the warehouse transfer for validated hardware units on the specified transfer date.
        /// Transfers stock in Spire ERP and logs the movements.
        /// </summary>
        [HttpPost("DoHardwareTransfer")]
        public async Task<ApiResposne> DoHardwareTransfer([FromQuery] DateTime transferDate)
        {
            return await _spareLightDA.DoHardwareTransferAsync(transferDate);
        }

        /// <summary>
        /// Uploads an Excel file containing accessory transfer lines (SKU, quantity, source/target warehouse).
        /// Parses accessory items into staging.
        /// </summary>
        [HttpPost("UploadAccessory")]
        public async Task<IActionResult> UploadAccessory(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            using var stream = file.OpenReadStream();
            var items = await _spareLightDA.ParseAccessoryExcelAsync(stream);
            return Ok(new ApiResposne { Success = true, Result = items, Message = $"{items.Count} items parsed" });
        }

        /// <summary>
        /// Validates staged accessory transfer quantities against current warehouse stock availability.
        /// Ensures sufficient on-hand quantity exists at the source location.
        /// </summary>
        [HttpPost("ValidateAccessory")]
        public async Task<ApiResposne> ValidateAccessory()
        {
            return await _spareLightDA.ValidateAccessoryTransferAsync();
        }

        /// <summary>
        /// Executes the warehouse transfer for validated accessory quantities on the specified transfer date.
        /// Adjusts inventory quantities across warehouses and records audit logs.
        /// </summary>
        [HttpPost("DoAccessoryTransfer")]
        public async Task<ApiResposne> DoAccessoryTransfer([FromQuery] DateTime transferDate)
        {
            return await _spareLightDA.DoAccessoryTransferAsync(transferDate);
        }

        /// <summary>
        /// Retrieves historical hardware and accessory transfer logs filtered by date range and type.
        /// Displays complete audit trail of location movements.
        /// </summary>
        [HttpGet("Log")]
        public async Task<ApiResposne> GetTransferLogAsync(DateTime? startDate, DateTime? endDate, string? type)
        {
            return await _spareLightDA.GetTransferLogAsync( startDate,  endDate,  type);
        }
    }
}
