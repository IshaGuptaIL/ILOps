using DAL.Inventory.IMEI.HardwareIMEI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LegacyApp.Controllers.IEMI
{
    [Route("api/[controller]")]
    [ApiController]
    public class HardwareController : ControllerBase
    {
        private readonly IHardwareService _hardwareService;

        public HardwareController(IHardwareService hardwareService)
        {
            _hardwareService = hardwareService;
        }

        [HttpGet("purchase-orders")]
        public async Task<IActionResult> GetPurchaseOrders()
        {
            var response = await _hardwareService.GetPurchaseOrdersAsync();
            return response.Success ? Ok(response.Data) : BadRequest(response.Message);
        }

        [HttpPost("upload-excel")]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded");
            using var stream = file.OpenReadStream();
            var response = await _hardwareService.ParseExcelImeisAsync(stream);
            return response.Success ? Ok(response.Data) : BadRequest(response.Message);
        }

        [HttpPost("check-errors")]
        public async Task<IActionResult> CheckErrors([FromBody] CheckErrorsRequest request)
        {
            var response = await _hardwareService.CheckErrorsAsync(request);
            return response.Success ? Ok(response.Data) : BadRequest(response.Message);
        }

        [HttpPost("receive")]
        public async Task<IActionResult> ReceiveImei([FromBody] ReceiveImeiRequest request)
        {
            var response = await _hardwareService.ReceiveImeiAsync(request);
            return response.Success ? Ok(response.Data) : BadRequest(response.Message);
        }
    }
}


