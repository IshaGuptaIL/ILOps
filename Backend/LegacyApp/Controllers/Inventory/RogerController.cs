using DAL.Inventory.RogerAR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LegacyApp.Controllers.Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    public class RogerController : ControllerBase
    {

        private readonly IRoger _repo;

        public RogerController(IRoger repo)
        {
            _repo = repo;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetARData(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var data = await _repo.GetARDataAsync(searchTerm, pageNumber, pageSize);
            return Ok(data);
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateARData([FromBody] RogerarBO item)
        {
            try
            {
                var userId = Request.Cookies["UserID"] ?? "System";
                var success = await _repo.UpdateARDataAsync(item, userId);
                if (success) return Ok(new { Success = true, Message = "Update successful" });

                return StatusCode(403, new { Success = false, Message = "You are not authorized to edit this record as you are not the owner." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating AR data: {ex.Message}");
            }
        }

        [HttpPost("load")]
        public async Task<IActionResult> LoadARData([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = Request.Cookies["UserID"] ?? "System";

                var result = await _repo.LoadARDataAsync(userId, pageNumber, pageSize);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error loading AR data: {ex.Message}");
            }
        }

        [HttpGet("export")]
        public async Task<IActionResult> ExportToExcel()
        {
            try
            {
                var fileBytes = await _repo.ExportToExcelAsync();
                var fileName = $"RogersAR_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error exporting to Excel: {ex.Message}");
            }
        }
    }
}





