using DAL.Inventory.RunRate;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LegacyApp.Controllers.Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    public class RunRateController : ControllerBase
    {
        private readonly RunRateDA _runRateDA;

        public RunRateController(IConfiguration config)
        {
            _runRateDA = new RunRateDA(config);
        }
        [HttpGet("GetWfhInventory")]
        public async Task<ActionResult<List<RunRateItem>>> GetWFHInventory()
        {
            try
            {
                var data = await _runRateDA.GetWFHInventoryAsync();
                return Ok(data);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
