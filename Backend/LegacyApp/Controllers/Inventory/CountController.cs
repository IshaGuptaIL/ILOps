using DAL.Common.Login;
using DAL.Inventory.Count;
using Microsoft.AspNetCore.Mvc;

namespace LegacyApp.Controllers.Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    public class CountController : ControllerBase
    {
        private readonly ICount _countRepo; // Naming consistent rakhein

        public CountController(ICount count)
        {
            _countRepo = count;
        }

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
        [HttpGet("test-access")]
        public async Task<IActionResult> TestAccess()
        {
            var result = await _countRepo.TestFileAccess();
            return Ok(new { status = result });
        }

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