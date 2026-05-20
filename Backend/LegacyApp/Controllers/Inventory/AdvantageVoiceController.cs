using DAL.Inventory.AdvantageVoice;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace LegacyApp.Controllers.Inventory
{
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

        [HttpPost("ImportExcel")]
        public async Task<ActionResult<bool>> ImportExcel([FromForm] IFormFile file, [FromQuery] int userId)
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

