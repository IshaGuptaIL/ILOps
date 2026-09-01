using DAL.Inventory.AdvantageVoice;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace LegacyApp.Controllers.Inventory
{
    /// <summary>
    /// Manages Advantage Voice bulk order imports, staging validation, and automated Spire order submissions.
    /// Handles Excel file uploads, validation error reporting, order generation, and import template downloads.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AdvantageVoiceController : ControllerBase
    {
        private readonly IAdvantageVoice _advantageVoiceDA;
        private readonly IWebHostEnvironment _env;

        public AdvantageVoiceController(IAdvantageVoice advantageVoiceDA, IWebHostEnvironment env)
        {
            _advantageVoiceDA = advantageVoiceDA;
            _env = env;
        }

        /// <summary>
        /// Retrieves pending imported Advantage Voice rows for the specified user ID.
        /// Displays staged items in the import review grid.
        /// </summary>
        [HttpGet("GetPendingImports")]
        public async Task<ActionResult<List<AdvantageImportVM>>> GetPendingImports([FromQuery] int userId)
        {
            try
            {
                var data = await _advantageVoiceDA.GetPendingImportsAsync(userId);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error fetching pending imports: {ex.Message}");
            }
        }

        /// <summary>
        /// Imports Advantage Voice orders from an uploaded Excel spreadsheet into the user's staging table.
        /// Parses customer, part, quantity, and pricing details.
        /// </summary>
        [HttpPost("ImportExcel")]
        public async Task<ActionResult<bool>> ImportExcel(IFormFile file, [FromQuery] int userId)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("No file uploaded.");

                using (var stream = file.OpenReadStream())
                {
                    var result = await _advantageVoiceDA.ImportExcelDataAsync(stream, userId);
                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error importing data: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates staged Advantage Voice import lines against customer, inventory, and warehouse rules.
        /// Flags invalid accounts, inactive items, or pricing discrepancies.
        /// </summary>
        [HttpPost("ValidateData")]
        public async Task<ActionResult<List<AdvantageImportVM>>> ValidateData([FromQuery] int userId)
        {
            try
            {
                var data = await _advantageVoiceDA.ValidateDataAsync(userId);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error validating data: {ex.Message}");
            }
        }

        /// <summary>
        /// Submits validated Advantage Voice staging lines to Spire to create formal sales orders.
        /// Clears staging records upon successful order generation.
        /// </summary>
        [HttpPost("SubmitOrders")]
        public async Task<ActionResult<bool>> SubmitOrders([FromQuery] int userId)
        {
            try
            {
                var result = await _advantageVoiceDA.SubmitOrdersAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error submitting orders: {ex.Message}");
            }
        }

        /// <summary>
        /// Serves the standardized Advantage Voice Excel import template (ADVImport.xlsx) for download.
        /// Provides users with the correctly formatted spreadsheet structure.
        /// </summary>
        [HttpGet("DownloadTemplate")]
        public IActionResult DownloadTemplate()
        {
            try
            {
                // Serve the physical file from the frontend-code folder if possible
                string filePath = Path.Combine(_env.ContentRootPath, "..", "FrontendCode", "ADVImport.xlsx");

                if (!System.IO.File.Exists(filePath))
                {
                    filePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "FrontendCode", "ADVImport.xlsx");
                }

                if (System.IO.File.Exists(filePath))
                {
                    var fileBytes = System.IO.File.ReadAllBytes(filePath);
                    return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ADVImport.xlsx");
                }
                else
                {
                    // Fallback to generated template
                    var fileBytes = _advantageVoiceDA.GenerateTemplate();
                    return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ADVImport.xlsx");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error downloading template: {ex.Message}");
            }
        }
    }
}

