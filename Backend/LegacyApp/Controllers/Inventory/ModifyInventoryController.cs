using DAL.Inventory.ModifyInventory;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LegacyApp.Controllers.Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    public class ModifyInventoryController : ControllerBase
    {


        private readonly IModifyInventory _service;

        public ModifyInventoryController(IModifyInventory service)
        {
            _service = service;
        }

        // 🔹 Inventory List
        [HttpGet("list")]
        public async Task<IActionResult> GetInventory(
            string search = "",
            int page = 1,
            int size = 10)
        {
            var result = await _service.GetInventoryAsync(search, page, size);
            return Ok(result);
        }

        // 🔹 All Warehouses
        [HttpGet("warehouses")]
        public async Task<IActionResult> GetAllWarehouses(
            string partNo,
            string skipWhse)
        {
            var data = await _service.GetAllWarehousesAsync(partNo, skipWhse);
            return Ok(data);
        }

        // 🔹 Update Prices
        [HttpPost("update-price")]
        public async Task<IActionResult> UpdatePrice(
        [FromBody] PriceUpdateModel model,
        [FromQuery] bool applyToAll = false)
        {
            var result = await _service.UpdatePriceAsync(model, applyToAll);

            // ✅ Always return ApiResponse
            return Ok(result);
        }
    }
}
