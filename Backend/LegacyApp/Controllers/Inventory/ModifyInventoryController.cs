using DAL.Inventory.ModifyInventory;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LegacyApp.Controllers.Inventory
{
    /// <summary>
    /// Modifies inventory pricing levels, standard costs, and sell prices across warehouse locations.
    /// Provides search filtering, multi-warehouse stock viewing, and bulk price propagation.
    /// </summary>
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
        /// <summary>
        /// Retrieves a paginated list of inventory items filtered by part number or description query.
        /// Displays item prices, product codes, and primary warehouse status.
        /// </summary>
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
        /// <summary>
        /// Retrieves pricing and on-hand quantities for a specific part number across all active warehouses.
        /// Allows operators to compare location-specific inventory levels.
        /// </summary>
        [HttpGet("warehouses")]
        public async Task<IActionResult> GetAllWarehouses(
            string partNo,
            string skipWhse)
        {
            var data = await _service.GetAllWarehousesAsync(partNo, skipWhse);
            return Ok(data);
        }

        // 🔹 Update Prices
        /// <summary>
        /// Updates retail, wholesale, and special pricing levels for an inventory part.
        /// Supports single-warehouse updates or bulk price synchronization across all warehouses.
        /// </summary>
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
