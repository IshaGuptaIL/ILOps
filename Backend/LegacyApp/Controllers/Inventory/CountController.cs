using DAL.Common.Login;
using DAL.Inventory.Count;
using Microsoft.AspNetCore.Mvc;

namespace LegacyApp.Controllers.Inventory
{
    /// <summary>
    /// Manages physical inventory count dataset maintenance, snapshot captures, file deletions, and count exports.
    /// Supports batch data clearing, snapshot generation, hardware/accessory exports, and count synchronization.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class CountController : ControllerBase
    {
        private readonly ICount _countRepo; // Naming consistent rakhein

        public CountController(ICount count)
        {
            _countRepo = count;
        }

        /// <summary>
        /// Deletes all staged count entries imported from a specific file.
        /// Allows operators to retract an incorrect or duplicate count file upload.
        /// </summary>
        [HttpDelete("delete-by-file")]
        public async Task<IActionResult> DeleteByFile(string fileName, bool isACC)
        {
            if (string.IsNullOrEmpty(fileName))
                return BadRequest("File name is required");

            try
            {
                var success = await _countRepo.DeleteCounts(fileName, isACC);
                return Ok(new
                {
                    message = $"Counts deleted from {fileName}",
                    status = success
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Retrieves the list of unique uploaded count file names for hardware or accessories.
        /// Populates file selection dropdowns in the count management interface.
        /// </summary>
        [HttpGet("file-names")]
        public async Task<IActionResult> GetFileNames(bool isACC)
        {
            try
            {
                var list = await _countRepo.GetUniqueFileNames(isACC);
                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Deletes all physical count records for hardware or accessory lines.
        /// Performs full reset of staged count data prior to a new inventory cycle.
        /// </summary>
        [HttpDelete("delete-all/{isACC}")]
        public async Task<IActionResult> DeleteAll(bool isACC)
        {
            try
            {
                var result = await _countRepo.DeleteAllCounts(isACC);
                return Ok(new { message = "All counts deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Takes a frozen snapshot of current Spire on-hand inventory levels for count audit comparison.
        /// Establishes the system baseline for subsequent physical count reconciliation.
        /// </summary>
        [HttpPost("load-snapshot")]
        public async Task<ApiResposne> LoadSnapshot([FromBody] InventorySnapshotBO bo)
        {
            try
            {
                var result = await _countRepo.LoadSnapshot(bo);
                return new ApiResposne
                {
                    Success = true,
                    Message = "Snapshot Loaded Successfully",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                return new ApiResposne
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 500
                };
            }
        }

        /// <summary>
        /// Exports all hardware physical count data to an Excel (.xlsx) spreadsheet.
        /// Used for external validation and finance review of counted stock.
        /// </summary>
        [HttpGet("export-hardware")]
        public async Task<IActionResult> ExportHardware()
        {
            try
            {
                // NOTE: CountDA mein param nahi hai, isliye yahan se hataya gaya hai
                var fileContent = await _countRepo.ExportHardwareCounts();

                if (fileContent == null || fileContent.Length == 0)
                    return NotFound("No hardware data found.");

                string fileName = $"Hardware_ALL_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

                return File(fileContent,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Exports all accessory physical count data to an Excel (.xlsx) spreadsheet.
        /// Used for accessory stocktaking audit records.
        /// </summary>
        [HttpGet("export-accessories")]
        public async Task<IActionResult> ExportAccessories()
        {
            try
            {
                var fileContent = await _countRepo.ExportAccessoryCounts();

                if (fileContent == null || fileContent.Length == 0)
                    return NotFound("No accessory data found.");

                string fileName = $"Accessory_Counts_{DateTime.Now:yyyyMMdd}.xlsx";

                return File(fileContent,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Verifies access permissions and folder path availability for inventory count files.
        /// Diagnostics endpoint for network share accessibility.
        /// </summary>
        [HttpGet("test-access")]
        public async Task<IActionResult> TestAccess()
        {
            var result = await _countRepo.TestFileAccess();
            return Ok(new { status = result });
        }

        /// <summary>
        /// Synchronizes physical count spreadsheets from network drop folders into the count database.
        /// Automates batch count ingestion across scanner stations.
        /// </summary>
        [HttpPost("sync-inventory-files")]
        public async Task<IActionResult> SyncInventoryFiles()
        {
            try
            {
                var result = await _countRepo.SyncInventoryFiles();
                return Ok(new { success = result, message = "Files synced successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Retrieves the synchronization status and timestamps of inventory count data files.
        /// Used to display file freshness on the physical count dashboard.
        /// </summary>
        [HttpGet("file-status")]
        public async Task<ApiResposne> GetFileStatus()
        {
            var status = await _countRepo.GetFileStatus();
            return new ApiResposne
            {
                Success = true,
                Result = status,
                StatusCode = 200
            };
        }
    }
}