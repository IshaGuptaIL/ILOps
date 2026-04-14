using DAL.Common.Login;
using DAL.Inventory.SpareLight;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LegacyApp.Controllers.Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    public class SpareLightController : ControllerBase
    {
        private readonly ISpareLight _spareLightDA;

        public SpareLightController(ISpareLight spareLightDA)
        {
            _spareLightDA = spareLightDA;
        }

        [HttpPost("UploadHardware")]
        public async Task<IActionResult> UploadHardware(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            using var stream = file.OpenReadStream();
            var items = await _spareLightDA.ParseHardwareExcelAsync(stream);
            return Ok(new ApiResposne { Success = true, Result = items, Message = $"{items.Count} items parsed" });
        }

        [HttpPost("ValidateHardware")]
        public async Task<ApiResposne> ValidateHardware()
        {
            return await _spareLightDA.ValidateHardwareTransferAsync();
        }

        [HttpPost("DoHardwareTransfer")]
        public async Task<ApiResposne> DoHardwareTransfer([FromQuery] DateTime transferDate)
        {
            return await _spareLightDA.DoHardwareTransferAsync(transferDate);
        }

        [HttpPost("UploadAccessory")]
        public async Task<IActionResult> UploadAccessory(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            using var stream = file.OpenReadStream();
            var items = await _spareLightDA.ParseAccessoryExcelAsync(stream);
            return Ok(new ApiResposne { Success = true, Result = items, Message = $"{items.Count} items parsed" });
        }

        [HttpPost("ValidateAccessory")]
        public async Task<ApiResposne> ValidateAccessory()
        {
            return await _spareLightDA.ValidateAccessoryTransferAsync();
        }

        [HttpPost("DoAccessoryTransfer")]
        public async Task<ApiResposne> DoAccessoryTransfer([FromQuery] DateTime transferDate)
        {
            return await _spareLightDA.DoAccessoryTransferAsync(transferDate);
        }

        [HttpGet("Log")]
        public async Task<ApiResposne> GetTransferLogAsync(DateTime? startDate, DateTime? endDate, string? type)
        {
            return await _spareLightDA.GetTransferLogAsync( startDate,  endDate,  type);
        }
    }
}
