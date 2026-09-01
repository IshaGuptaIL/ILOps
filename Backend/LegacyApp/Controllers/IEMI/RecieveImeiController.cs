using DAL.Common.Login;
using DAL.Inventory.IMEI.RecieveIMEI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LegacyApp.Controllers.IEMI
{
    /// <summary>
    /// Handles staging-based IMEI receiving operations, error checking, and receipt posting.
    /// Manages user-specific staging tables (tblScanList / tblPackingSlip) and Spire inventory receiving transactions.
    /// </summary>
    [Route("api/RecieveImei")]
    [ApiController]
    public class RecieveImeiController : ControllerBase
    {
        private readonly IRecieveImei _recieveImei;

        public RecieveImeiController(IRecieveImei recieveImei)
        {
            _recieveImei = recieveImei;
        }

        private int GetUserId()
        {
            if (Request.Cookies.TryGetValue("userId", out var cookieUserId) && int.TryParse(cookieUserId, out var parsedId))
                return parsedId;
            if (Request.Headers.TryGetValue("userId", out var headerUserId) && int.TryParse(headerUserId.ToString(), out var parsedHeaderId))
                return parsedHeaderId;
            return 1; // Default fallback legacy user ID
        }

        /// <summary>
        /// Clears previous staging and imports new Packing Slip IMEIs for the current user.
        /// Saves records to tblPackingSlip for subsequent verification.
        /// </summary>
        [HttpPost("ImportPackingSlip")]
        public async Task<ApiResposne> ImportPackingSlip([FromBody] List<RecieveIMEIBO> items)
        {
            int userId = GetUserId();
            await _recieveImei.ClearPackingSlipAsync(userId);
            var result = await _recieveImei.InsertPackingSlipAsync(items, userId);
            return result;
        }

        /// <summary>
        /// Retrieves open purchase orders from Spire to populate the receiving dropdown.
        /// Used by the receiving interface to select active purchase order lines.
        /// </summary>
        [HttpGet("GetPurchaseOrdersAsync")]
        public async Task<ApiResposne> GetPurchaseOrdersAsync()
        {
            return await _recieveImei.GetPurchaseOrdersAsync();
        }

        /// <summary>
        /// Inserts scanned IMEI serial numbers into the user's scan list staging table (tblScanList).
        /// Prepares scanned serial numbers for cross-verification against the packing slip.
        /// </summary>
        [HttpPost("InsertScanList")]
        public async Task<IActionResult> InsertScanList([FromBody] List<RecieveIMEIBO> items)
        {
            if (items == null || items.Count == 0)
                return BadRequest(new ApiResposne { Success = false, Message = "No items provided" });

            int userId = GetUserId();
            var result = await _recieveImei.InsertScanListAsync(items, userId);
            if (result.Success)
                return Ok(result);
            else
                return StatusCode(500, result);
        }

        /// <summary>
        /// Compares staged scan list against packing slip and inventory to generate verification grids.
        /// Returns matched IMEIs, scan discrepancies, pack discrepancies, and existing inventory conflicts.
        /// </summary>
        [HttpGet("GetIMEIGrids/{poNumber}")]
        public async Task<ApiResposne> GetIMEIGridsAsync(string poNumber)
        {
            int userId = GetUserId();
            return await _recieveImei.GetIMEIGridsAsync(poNumber, userId);
        }

        /// <summary>
        /// Finalizes verified receiving by posting serial receipts to Spire and logging to HardwareReceived.
        /// Supports normal receiving as well as receipt reversals.
        /// </summary>
        [HttpPost("PostReceiptsAsync")]
        public async Task<ApiResposne> PostReceiptsAsync([FromBody] PostReceiptsRequest request)
        {
            int userId = GetUserId();
            return await _recieveImei.PostReceiptsAsync(request.PoId, request.PoItemId, request.Cmo, request.IsReversal, userId);
        }

        /// <summary>
        /// Executes validation rules against staged IMEIs, verifying format, duplicates, remaining PO qty, and onhand status.
        /// Returns detailed error messages if discrepancies exist.
        /// </summary>
        [HttpGet("CheckErrorsAsync")]
        public async Task<ApiResposne> CheckErrorsAsync(long poId, long poItemId, bool isReversal)
        {
            int userId = GetUserId();
            return await _recieveImei.CheckErrorsAsync(poId, poItemId, isReversal, userId);
        }

        /// <summary>
        /// Generates sample Excel templates for Scan List and Packing Slip imports on local filesystem.
        /// Helper utility for creating correctly formatted import spreadsheets.
        /// </summary>
        [HttpGet("GenerateSampleExcels")]
        public IActionResult GenerateSampleExcels()
        {
            try
            {
                string targetDir = @"c:\Users\DELL\Downloads\My Code";
                if (!System.IO.Directory.Exists(targetDir))
                    System.IO.Directory.CreateDirectory(targetDir);

                string scanPath = System.IO.Path.Combine(targetDir, "Sample_ScanList.xlsx");
                string packPath = System.IO.Path.Combine(targetDir, "Sample_PackingSlip.xlsx");

                using (var package = new ExcelPackage(new System.IO.FileInfo(scanPath)))
                { package.Workbook.Worksheets.Add("ScanList").Cells[1, 1].Value = "359411001234567"; package.Workbook.Worksheets[0].Cells[2, 1].Value = "359411001234568"; package.Workbook.Worksheets[0].Cells[3, 1].Value = "359411001234569"; package.Save(); }
                using (var package = new ExcelPackage(new System.IO.FileInfo(packPath)))
                { package.Workbook.Worksheets.Add("PackingSlip").Cells[1, 1].Value = "359411001234567"; package.Workbook.Worksheets[0].Cells[2, 1].Value = "359411001234568"; package.Workbook.Worksheets[0].Cells[3, 1].Value = "359411001234569"; package.Save(); }

                return Ok(new ApiResposne { Success = true, Message = $"Sample Excel files created successfully at {targetDir}" });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new ApiResposne { Success = false, Message = ex.Message });
            }
        }
    }
}