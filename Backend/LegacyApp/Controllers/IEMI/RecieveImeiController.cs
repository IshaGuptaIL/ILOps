using DAL.Common.Login;
using DAL.Inventory.IMEI.RecieveIMEI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LegacyApp.Controllers.IEMI
{
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

        [HttpPost("ImportPackingSlip")]
        public async Task<ApiResposne> ImportPackingSlip([FromBody] List<RecieveIMEIBO> items)
        {
            int userId = GetUserId();
            await _recieveImei.ClearPackingSlipAsync(userId);
            var result = await _recieveImei.InsertPackingSlipAsync(items, userId);
            return result;
        }

        [HttpGet("GetPurchaseOrdersAsync")]
        public async Task<ApiResposne> GetPurchaseOrdersAsync()
        {
            return await _recieveImei.GetPurchaseOrdersAsync();
        }

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

        [HttpGet("GetIMEIGrids/{poNumber}")]
        public async Task<ApiResposne> GetIMEIGridsAsync(string poNumber)
        {
            int userId = GetUserId();
            return await _recieveImei.GetIMEIGridsAsync(poNumber, userId);
        }

        [HttpPost("PostReceiptsAsync")]
        public async Task<ApiResposne> PostReceiptsAsync([FromBody] PostReceiptsRequest request)
        {
            int userId = GetUserId();
            return await _recieveImei.PostReceiptsAsync(request.PoId, request.PoItemId, request.Cmo, request.IsReversal, userId);
        }

        [HttpGet("CheckErrorsAsync")]
        public async Task<ApiResposne> CheckErrorsAsync(long poId, long poItemId, bool isReversal)
        {
            int userId = GetUserId();
            return await _recieveImei.CheckErrorsAsync(poId, poItemId, isReversal, userId);
        }

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