using DAL.Inventory.InventoryType;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LegacyApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryTypeController : ControllerBase
    {
        private readonly IInventoryType _repo;

        public InventoryTypeController(IInventoryType repo)
        {
            _repo = repo;
        }

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

        [HttpPost("Add")]
        public async Task<IActionResult> Add([FromBody] InventoryBO model)
        {
            var success = await _repo.AddGroupAsync(model);
            return Ok(new { success });
        }

        [HttpPatch("Update")]
        public async Task<IActionResult> Update([FromBody] InventoryBO model)
        {
            var success = await _repo.UpdateGroupAsync(model);
            return Ok(new { success });
        }
    }
}

