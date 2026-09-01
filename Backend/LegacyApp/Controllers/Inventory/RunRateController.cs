using DAL.Inventory.RunRate;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LegacyApp.Controllers.Inventory
{
    /// <summary>
    /// Computes inventory run-rate velocity, sales movement patterns, and days-of-supply inventory projections.
    /// Provides WFH stock tracking, date-range sales loading, and supply days filtration.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class RunRateController : ControllerBase
    {
        private readonly RunRateDA _runRateDA;

        public RunRateController(IConfiguration config)
        {
            _runRateDA = new RunRateDA(config);
        }

        /// <summary>
        /// Retrieves active inventory items held in Work-From-Home (WFH) and remote rep locations.
        /// Displays field stock balances for sales rep accountability.
        /// </summary>
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

        /// <summary>
        /// Ingests historical sales movement data and calculates average daily consumption rates across working days.
        /// Prepares velocity metrics for replenishment analysis.
        /// </summary>
        [HttpPost("LoadRunRateData")]
        public async Task<ActionResult<int>> LoadRunRateDataAsync([FromBody] RunRateRequest request)
        {
            try
            {
                // Calls methods with explicitly passed UserId
                var workingDays = await _runRateDA.LoadRunRateDataAsync(request.StartDate, request.EndDate, request.UserId);
                return Ok(new { WorkingDays = workingDays, Message = "Run Rate data loaded successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error loading Run Rate data: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves calculated inventory run rate metrics filtered by minimum and maximum days of stock coverage.
        /// Identifies fast-moving stockouts and slow-moving excess inventory.
        /// </summary>
        [HttpGet("GetRunRate")]
        public async Task<ActionResult<List<RunRateItemBO>>> GetRunRate(int minDays, int maxDays, int userId)
        {
            var data = await _runRateDA.GetRunRateAsync(minDays, maxDays, userId);
            return Ok(data);
        }


        //[HttpGet("export-runrate")]
        //public async Task<IActionResult> ExportRunRate(int minDays, int maxDays)
        //{
        //    try
        //    {
        //        string templatePath = @"V:\InventoryRunRates-Spire\Templates\Stock Status-Template-Accessories.xlsx";

        //        if (!System.IO.File.Exists(templatePath))
        //            return NotFound("Template file not found");

        //        await using var stream = System.IO.File.OpenRead(templatePath);

        //        var fileBytes = await _runRateDA.ExportRunRateExcel(stream, minDays, maxDays);

        //        var fileName = $"RunRate-{DateTime.Now:yyyyMMddHHmmss}.xlsx";

        //        return File(
        //            fileBytes,
        //            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        //            fileName
        //        );
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, ex.Message);
        //    }
        //}

        [HttpGet("view-accessories")]
        public async Task<IActionResult> GetAccessoriesAsyncView(int userId, int pageNumber = 1, int pageSize = 10)
        {
            var data = await _runRateDA.GetAccessoriesAsyncView(pageNumber, pageSize, userId);
            return Ok(data);
        }

        [HttpGet("hardware-view")]
        public async Task<IActionResult> GetHardwareView(int userId, int pageNumber = 1, int pageSize = 10)
        {
            var data = await _runRateDA.GetHardwareViewAsync(pageNumber, pageSize, userId);
            return Ok(data);
        }

        [HttpGet("export-accessories")]
        public async Task<IActionResult> ExportAccessoriesExcel(int userId)
        {
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Templates", "Stock Status-Template-Accessories.xlsx");

            if (!System.IO.File.Exists(templatePath))
                return NotFound("Template file not found.");

            await using var templateStream = System.IO.File.OpenRead(templatePath);
            var fileBytes = await _runRateDA.ExportAccessoriesExcel(templateStream, userId);

            var fileName = $"Stock_Status_Accessories_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("export-hardware")]
        public async Task<IActionResult> ExportHardwareExcel(int userId)
        {
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Templates", "Stock Status-Template.xlsx");

            if (!System.IO.File.Exists(templatePath))
                return NotFound("Template file not found.");

            await using var templateStream = System.IO.File.OpenRead(templatePath);
            var fileBytes = await _runRateDA.ExportHardwareExcel(templateStream, userId);

            var fileName = $"Stock_Status_Hardware_{System.DateTime.Now:yyyy-MM-dd-HH-mm-ss}.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("export-accessories-rogers")]
        public async Task<IActionResult> ExportAccessoriesRogersExcel(int userId)
        {
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Templates", "Stock Status-Template-Accessories-Rogers.xlsx");

            if (!System.IO.File.Exists(templatePath))
                return NotFound("Template file not found.");

            await using var templateStream = System.IO.File.OpenRead(templatePath);
            var fileBytes = await _runRateDA.ExportAccessoriesRogersExcel(templateStream, userId);

            var fileName = $"Stock_Status_Accessories_Rogers_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

    }
}
