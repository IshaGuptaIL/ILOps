using DAL.Inventory.RogerAR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LegacyApp.Controllers.Inventory
{
    /// <summary>
    /// Manages Rogers Accounts Receivable (AR) transactions, user editing authorizations, and AR statement exports.
    /// Provides debtor record tracking, inline editing, batch loading, and Excel exports for Rogers AR.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class RogerController : ControllerBase
    {

        private readonly IRoger _repo;

        public RogerController(IRoger repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Retrieves paginated Rogers AR records filtered by optional customer search keywords.
        /// Used by the Rogers AR ledger grid.
        /// </summary>
        [HttpGet("list")]
        public async Task<IActionResult> GetARData(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var data = await _repo.GetARDataAsync(searchTerm, pageNumber, pageSize);
            return Ok(data);
        }

        /// <summary>
        /// Updates a Rogers AR debtor entry if the modifying user is authorized for the record.
        /// Saves edits to amounts, notes, and payment statuses.
        /// </summary>
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

        /// <summary>
        /// Loads fresh Rogers AR transaction data into staging for the active user session.
        /// Synchronizes open Rogers receivables for review.
        /// </summary>
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

        /// <summary>
        /// Exports Rogers AR records into an Excel (.xlsx) file.
        /// Provides downloadable schedule of outstanding Rogers receivables.
        /// </summary>
        [HttpGet("export")]
        public async Task<IActionResult> ExportARData()
        {
            try
            {
                var fileBytes = await _repo.ExportToExcelAsync();
                var fileName = $"RogerAR_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error exporting AR data: {ex.Message}");
            }
        }
    }
}
