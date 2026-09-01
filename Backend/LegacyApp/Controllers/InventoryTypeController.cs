using DAL.Inventory.InventoryType;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LegacyApp.Controllers
{
    /// <summary>
    /// Manages inventory category classifications and group type assignments (e.g. HCC, accessories).
    /// Provides paginated retrieval, addition, and modification of inventory group metadata.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryTypeController : ControllerBase
    {
        private readonly IInventoryType _repo;

        public InventoryTypeController(IInventoryType repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Retrieves paginated list of inventory items filtered by entry group type (default 'HCC').
        /// Used by the inventory classification screens to display group categories.
        /// </summary>
        [HttpGet("GetData")]
        public async Task<IActionResult> GetData(string entryType, int page = 1, int pageSize = 10)
        {
             var (data, totalCount) = await _repo.GetPagedDataAsync(entryType ?? "HCC", page, pageSize);

            return Ok(new
            {
                data,
                totalCount,
                page,
                pageSize
            });
        }

        /// <summary>
        /// Adds a new inventory group configuration or classification record.
        /// Used for setting up new product groupings in the inventory system.
        /// </summary>
        [HttpPost("Add")]
        public async Task<IActionResult> Add([FromBody] InventoryBO model)
        {
            var success = await _repo.AddGroupAsync(model);
            return Ok(new { success });
        }

        /// <summary>
        /// Updates existing inventory group classification attributes.
        /// Allows modification of category descriptions and grouping properties.
        /// </summary>
        [HttpPatch("Update")]
        public async Task<IActionResult> Update([FromBody] InventoryBO model)
        {
            var success = await _repo.UpdateGroupAsync(model);
            return Ok(new { success });
        }
    }
}

