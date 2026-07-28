using DAL.Models;
using DAL.Sales.BO;
using DAL.Sales.TaxSalesReport;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace LegacyApp.Controllers.Sales
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalesTaxReportController : ControllerBase
    {
        private readonly ISalesTaxReport _salesTaxReportDA;

        public SalesTaxReportController(ISalesTaxReport salesTaxReportDA)
        {
            _salesTaxReportDA = salesTaxReportDA;
        }

        [HttpPost("LoadSalesHistory")]
        public async Task<ActionResult<bool>> LoadSalesHistory([FromBody] SalesTaxReportRequest request)
        {
            if (request == null) return BadRequest("Invalid request.");
            
            // Getting userId from session/claims (simulated as 1 for now)
            int userId = 1; 
            
            var success = await _salesTaxReportDA.LoadSalesTaxHistoryAsync(request, userId);
            return Ok(success);
        }

        [HttpPost("LoadGLData")]
        public async Task<ActionResult<bool>> LoadGLData([FromBody] SalesTaxReportRequest request)
        {
            if (request == null) return BadRequest("Invalid request.");
            
            int userId = 1; 
            var success = await _salesTaxReportDA.LoadGLDataAsync(request, userId);
            return Ok(success);
        }

        [HttpPost("GetReport")]
        public async Task<ActionResult<SalesTaxReportResponse>> GetReport([FromBody] SalesTaxReportRequest request)
        {
            if (request == null) return BadRequest("Invalid request.");
            
            int userId = 1; 
            var result = await _salesTaxReportDA.GetSalesTaxReportAsync(request, userId);
            return Ok(result);
        }

        [HttpPost("ExportExcel")]
        public async Task<IActionResult> ExportExcel([FromBody] SalesTaxReportRequest request)
        {
            if (request == null) return BadRequest("Invalid request.");

            int userId = 1;
            var fileBytes = await _salesTaxReportDA.ExportToExcelAsync(request, userId);
            
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SalesTaxReport.xlsx");
        }

        [HttpPost("ExportVendorActivity")]
        public async Task<IActionResult> ExportVendorActivity([FromBody] VendorActivityRequest request)
        {
            string vendor = request.Vendor;
            DateTime start = request.StartDate;
            DateTime end = request.EndDate;

            var fileBytes = await _salesTaxReportDA.ExportVendorActivityAsync(vendor, start, end);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"VendorActivity-{vendor}.xlsx");
        }

        [HttpPost("ExportGLITCExcel")]
        public async Task<IActionResult> ExportGLITCExcel([FromBody] SalesTaxReportRequest request)
        {
            if (request == null) return BadRequest("Invalid request.");
            int userId = 1;
            var fileBytes = await _salesTaxReportDA.ExportGLITCExcelAsync(request, userId);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"ITCCredits-{request.StartDate:yyyy-MM-dd}.xlsx");
        }

        [HttpPost("ExportGLDataExcel")]
        public async Task<IActionResult> ExportGLDataExcel([FromBody] SalesTaxReportRequest request)
        {
            if (request == null) return BadRequest("Invalid request.");
            int userId = 1;
            var fileBytes = await _salesTaxReportDA.ExportGLDataExcelAsync(request, userId);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"GLTaxData-{request.StartDate:yyyy-MM-dd}.xlsx");
        }

        // ─── TAX CODE HISTORY ENDPOINTS ───────────────────────────────────────

        [HttpGet("GetTaxCodeHistory")]
        public async Task<ActionResult<List<TaxCodeHistory>>> GetTaxCodeHistory()
        {
            var result = await _salesTaxReportDA.GetTaxCodeHistoryAsync();
            return Ok(result);
        }

        [HttpPost("SaveTaxCodeHistory")]
        public async Task<ActionResult<bool>> SaveTaxCodeHistory([FromBody] TaxCodeHistory history)
        {
            if (history == null) return BadRequest("Invalid request.");
            int userId = 1; // Simulated session user
            var result = await _salesTaxReportDA.SaveTaxCodeHistoryAsync(history, userId);
            return Ok(result);
        }

        [HttpDelete("DeleteTaxCodeHistory/{id}")]
        public async Task<ActionResult<bool>> DeleteTaxCodeHistory(int id)
        {
            var result = await _salesTaxReportDA.DeleteTaxCodeHistoryAsync(id);
            return Ok(result);
        }

        [HttpGet("GetVendors")]
        public async Task<ActionResult<List<VendorBO>>> GetVendors()
        {
            var result = await _salesTaxReportDA.GetVendorsAsync();
            return Ok(result);
        }
    }
}
