using DAL.Common.Login;
using DAL.Inventory.Count;
using DAL.Inventory.CountAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LegacyApp.Controllers.Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    public class CountAnalysisController : ControllerBase
    {

        public readonly ICountAnalysis countAnalysis;


        public CountAnalysisController(ICountAnalysis countService)
        {
            countAnalysis = countService;
        }
        //1 ROW

        [HttpPost("upload-imei")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadIMEICounts( IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                return BadRequest(new ApiResposne { Success = false, Message = "Please select a valid Excel file." });
            }

            using var stream = excelFile.OpenReadStream();
            var response = await countAnalysis.LoadIMEICounts(stream, excelFile.FileName);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        [HttpGet("view-counts")]
        public async Task<IActionResult> GetCounts()
        {
            var result = await countAnalysis.GetAllImportedCounts(); // No parameters
            return Ok(result);
        }

        [HttpGet("onhand-not-counteds")]
        public async Task<IActionResult> GetOnhandNotCounteds()
        {
            var response = await countAnalysis.GetOnhandNotCounteds();
            return Ok(response);
        }

        [HttpGet("duplicate-counts")]
        public async Task<IActionResult> GetDuplicateIMEICounts([FromQuery] int pageNumber , [FromQuery] int pageSize)
        {
            return Ok(await countAnalysis.GetDuplicateIMEICounts(pageNumber, pageSize));
        }
        [HttpGet("system-duplicates")]
        public async Task<IActionResult> GetSystemDuplicateSerials([FromQuery] int pageNumber, [FromQuery] int pageSize)
        {
            var response = await countAnalysis.GetSystemDuplicateSerials(pageNumber, pageSize);
            return Ok(response);
        }

        [HttpPost("process-duplicates")]
        public async Task<IActionResult> ProcessDuplicates()
        {
            var response = await countAnalysis.ProcessDuplicateCounts();
            return Ok(response);
        }
        [HttpGet("cleanup-preview")]
        public async Task<IActionResult> GetDuplicateCleanupPreview()
        {
            var response = await countAnalysis.GetDuplicateCleanupPreview();
            return Ok(response);
        }
        [HttpPost("delete-duplicates")]
        public async Task<IActionResult> DeleteDuplicates()
        {
            var response = await countAnalysis.DeleteDuplicateCounts();
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        [HttpGet("invalid-serials")]
        public async Task<IActionResult> GetInvalidSerials()
        {
            var response = await countAnalysis.GetInvalidSerialCounts();
            return Ok(response);
        }


        [HttpGet("system-serial-verify")]
        public async Task<IActionResult> GetSystemSerialVerify()
        {
            var response = await countAnalysis.GetSystemSerialVerification();
            return Ok(response);
        }

        [HttpGet("discrepancy-report")]
        public async Task<IActionResult> GetDiscrepancyReport()
        {
            var response = await countAnalysis.GetDiscrepancyReport();
            return Ok(response);
        }
        [HttpGet("qty-vs-serial-comparison")]
        public async Task<IActionResult> GetQtyVsSerialComparison()
        {
            var response = await countAnalysis.GetQuantityVsSerialComparison();
            return Ok(response);
        }

        [HttpGet("missing-from-count")]
        public async Task<IActionResult> GetMissingItems()
        {
            var response = await countAnalysis.GetMissingFromPhysicalCount();
            return Ok(response);
        }
        [HttpPost("process-not-onhand")]
        public async Task<IActionResult> ProcessNotOnhand()
        {
            var response = await countAnalysis.ProcessCountedNotOnhandDetails();
            return Ok(response);
        }

        // 2 ROW

        [HttpGet("warehouses")]
        public async Task<IActionResult> GetWarehouses()
        {
            var data = await countAnalysis.GetWarehouses();
            return Ok(data);
        }
        [HttpGet("countFiles")]
        public async Task<IActionResult> GetCountFiles(string type)
        {
            var data = await countAnalysis.GetCountFiles(type);
            return Ok(data);
        }

        [HttpGet("fileSummary")]
        public async Task<IActionResult> GetFileSummary([FromQuery] string fileName, [FromQuery] string type)
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(type))
                return BadRequest("FileName and Type are required.");

            var data = await countAnalysis.GetCountFileSummary(fileName, type);
            if (data == null) return NotFound("File details not found.");

            return Ok(data);
        }
        [HttpPost("assignCounts")]
        public async Task<IActionResult> AssignCounts([FromBody] AssignWarehouseRequest request)
        {
            if (string.IsNullOrEmpty(request.CountFile) || string.IsNullOrEmpty(request.Warehouse))
                return BadRequest("Data missing.");

            var success = await countAnalysis.AssignCountsToWarehouse(request);
            if (success) return Ok(new { Message = "Operation complete." });

            return StatusCode(500, "Error updating warehouse.");
        }

        [HttpPost("upload-acc")]
        [Consumes("multipart/form-data")]
        public async Task<ApiResposne> UploadACCCounts( IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                return new ApiResposne { Success = false, Message = "File is empty" };
            }

            using var stream = excelFile.OpenReadStream();
            return await countAnalysis.ImportACCCounts(stream, excelFile.FileName);
        }

        [HttpPost("upload-backorders")]
        [Consumes("multipart/form-data")]
        public async Task<ApiResposne> UploadBackOrders(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                return new ApiResposne { Success = false, Message = "File is empty" };
            }

            using var stream = excelFile.OpenReadStream();
            return await countAnalysis.ImportBackOrders(stream, excelFile.FileName);
        }



        [HttpGet("acc-counts-edit")]
        public async Task<ActionResult<ACCEditResponse>> GetACCCounts()
        {
            var response = await countAnalysis.GetACCCountsForEdit();
            return Ok(response);
        }

        [HttpPost("update-acc-qty")]
        public async Task<IActionResult> UpdateQty([FromBody] UpdateQtyRequest request)
        {
            var success = await countAnalysis.UpdateACCCount(request.Id, request.NewQty);
            if (!success) return BadRequest("Update failed or record not found.");

            return Ok(new { message = "Quantity updated successfully" });
        }

        

        [HttpPost("sync-spire-data")]
        public async Task<IActionResult> LoadSpireSalesAndReceipts([FromQuery] string type)
        {
            if (string.IsNullOrEmpty(type))
            {
                return BadRequest(new ApiResposne { Success = false, Message = "Type is required (Both/Sales/Receipts)" });
            }

            var response = await countAnalysis.LoadSpireSalesAndReceipts(type);

            if (!response.Success)
            {
                return StatusCode(500, response); 
            }

            return Ok(response);
        }

        [HttpGet("accessory-discrepancies")]
        public async Task<IActionResult> GetDiscrepancies()
        {
            var response = await countAnalysis.GetAccessoryDiscrepancies();

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }


        [HttpGet("counted-not-in-bv")]
        public async Task<IActionResult> GetCountedNotInBV()
        {
            var response = await countAnalysis.GetCountedNotInBV();
            return Ok(response);
        }
        [HttpGet("onhand-not-counted")]
        public async Task<IActionResult> GetOnhandNotCounted(
     )
        {
            var response = await countAnalysis.GetOnhandNotCounted();

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }
        [HttpGet("warehouse-assignments")]
        public async Task<IActionResult> GetWarehouseAssignments([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 25)
        {
            var response = await countAnalysis.GetWarehouseAssignments(pageNumber, pageSize);
            return Ok(response);
        }
        [HttpGet("loaded-stock-status")]
        public async Task<IActionResult> GetLoadedStockStatus()
        {
            var response = await countAnalysis.GetLoadedStockStatus();
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [HttpPost("import-backorders")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportBackorders( IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ApiResposne
                {
                    Success = false,
                    Message = "No file was uploaded or file is empty."
                });
            }

            var response = await countAnalysis.ImportBackorders(file);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }



        // 2ROW









        // 3 ROW

        //[HttpGet("accessory-totals")]
        //public async Task<IActionResult> GetAccessoryTotalsByTerritory(DateTime startDate, DateTime endDate)
        //{
        //    var response = await countAnalysis.GetAccessoryTotalsByTerritory(startDate, endDate);

        //    if (!response.Success)
        //    {
        //        return BadRequest(response); 
        //    }

        //    return Ok(response);
        //}

        [HttpGet("accessory-analysis")]
        public async Task<IActionResult> GetAccessoryAnalysis(
     [FromQuery] DateTime startDate,
     [FromQuery] DateTime endDate
    )  // searchTerm removed
        {
            var response = await countAnalysis.GetAccessoryAnalysisReport(startDate, endDate);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [HttpGet("accessory-sales-channel")]
        public async Task<IActionResult> GetAccessorySalesByChannel(DateTime startDate, DateTime endDate)
        {
            var response = await countAnalysis.GetAccessorySalesByChannel(startDate, endDate);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet("item-sales-summary")]
        public async Task<IActionResult> GetItemSalesSummary()
        {
            var response = await countAnalysis.GetItemSalesSummary();
            return Ok(response);
        }

        [HttpGet("item-receipts-summary")]
        public async Task<IActionResult> GetItemReceiptsSummary(DateTime startDate, DateTime endDate)
        {
            var response = await countAnalysis.GetItemReceiptsSummary(startDate, endDate);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
    }
}