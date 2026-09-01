using DAL.Models;
using DAL.Sales.BO;
using DAL.Sales.TaxSalesReport;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace LegacyApp.Controllers.Sales
{
    /// <summary>
    /// Generates sales tax and Input Tax Credit (ITC) reports, manages tax code history, and provides GL tax exports.
    /// Analyzes taxable revenue, tax collected, vendor tax activity, and General Ledger tax account reconciliations.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class SalesTaxReportController : ControllerBase
    {
        private readonly ISalesTaxReport _salesTaxReportDA;

        public SalesTaxReportController(ISalesTaxReport salesTaxReportDA)
        {
            _salesTaxReportDA = salesTaxReportDA;
        }

        /// <summary>
        /// Loads sales invoice transactions into staging tables for tax calculation across a date range.
        /// Extracts taxable sales lines from the database.
        /// </summary>
        [HttpPost("LoadSalesHistory")]
        public async Task<ActionResult<bool>> LoadSalesHistory([FromBody] SalesTaxReportRequest request)
        {
            if (request == null) return BadRequest("Invalid request.");
            
            int userId = 1; 
            var success = await _salesTaxReportDA.LoadSalesTaxHistoryAsync(request, userId);
            return Ok(success);
        }

        /// <summary>
        /// Loads General Ledger tax accounts data into staging for sales tax reconciliation.
        /// Pulls GL debit/credit entries for sales tax and input credit accounts.
        /// </summary>
        [HttpPost("LoadGLData")]
        public async Task<ActionResult<bool>> LoadGLData([FromBody] SalesTaxReportRequest request)
        {
            if (request == null) return BadRequest("Invalid request.");
            
            int userId = 1; 
            var success = await _salesTaxReportDA.LoadGLDataAsync(request, userId);
            return Ok(success);
        }

        /// <summary>
        /// Generates the complete sales tax report summary comparing taxable sales against GL tax balances.
        /// Calculates tax variance and net remittance amounts.
        /// </summary>
        [HttpPost("GetReport")]
        public async Task<ActionResult<SalesTaxReportResponse>> GetReport([FromBody] SalesTaxReportRequest request)
        {
            if (request == null) return BadRequest("Invalid request.");
            
            int userId = 1; 
            var result = await _salesTaxReportDA.GetSalesTaxReportAsync(request, userId);
            return Ok(result);
        }

        /// <summary>
        /// Exports the generated sales tax calculation summary into a formatted Excel (.xlsx) report.
        /// Used for government tax filing documentation and accounting archives.
        /// </summary>
        [HttpPost("ExportExcel")]
        public async Task<IActionResult> ExportExcel([FromBody] SalesTaxReportRequest request)
        {
            if (request == null) return BadRequest("Invalid request.");

            int userId = 1; 
            var fileBytes = await _salesTaxReportDA.ExportToExcelAsync(request, userId);
            
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SalesTaxReport.xlsx");
        }

        /// <summary>
        /// Exports detailed vendor purchase and tax activity within a date range to Excel.
        /// Audits vendor invoices and associated tax amounts paid.
        /// </summary>
        [HttpPost("ExportVendorActivity")]
        public async Task<IActionResult> ExportVendorActivity([FromBody] VendorActivityRequest request)
        {
            string vendor = request.Vendor;
            DateTime start = request.StartDate;
            DateTime end = request.EndDate;

            var fileBytes = await _salesTaxReportDA.ExportVendorActivityAsync(vendor, start, end);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"VendorActivity-{vendor}.xlsx");
        }

        /// <summary>
        /// Exports General Ledger Input Tax Credit (ITC) transaction breakdown to Excel.
        /// Lists eligible input tax credits claimed during the reporting period.
        /// </summary>
        [HttpPost("ExportGLITCExcel")]
        public async Task<IActionResult> ExportGLITCExcel([FromBody] SalesTaxReportRequest request)
        {
            if (request == null) return BadRequest("Invalid request.");
            int userId = 1;
            var fileBytes = await _salesTaxReportDA.ExportGLITCExcelAsync(request, userId);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"ITCCredits-{request.StartDate:yyyy-MM-dd}.xlsx");
        }

        /// <summary>
        /// Exports complete General Ledger tax journal lines and account balances to Excel.
        /// Provides detailed supporting GL schedule for tax filings.
        /// </summary>
        [HttpPost("ExportGLDataExcel")]
        public async Task<IActionResult> ExportGLDataExcel([FromBody] SalesTaxReportRequest request)
        {
            if (request == null) return BadRequest("Invalid request.");
            int userId = 1;
            var fileBytes = await _salesTaxReportDA.ExportGLDataExcelAsync(request, userId);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"GLTaxData-{request.StartDate:yyyy-MM-dd}.xlsx");
        }

        // ─── TAX CODE HISTORY ENDPOINTS ───────────────────────────────────────

        /// <summary>
        /// Retrieves the list of historical tax code rates and effective date ranges.
        /// Displays tax configuration history on the tax settings screen.
        /// </summary>
        [HttpGet("GetTaxCodeHistory")]
        public async Task<ActionResult<List<TaxCodeHistory>>> GetTaxCodeHistory()
        {
            var result = await _salesTaxReportDA.GetTaxCodeHistoryAsync();
            return Ok(result);
        }

        /// <summary>
        /// Saves or updates a tax code rate definition and its active date window.
        /// Used to configure new GST/PST/HST tax rate changes.
        /// </summary>
        [HttpPost("SaveTaxCodeHistory")]
        public async Task<ActionResult<bool>> SaveTaxCodeHistory([FromBody] TaxCodeHistory history)
        {
            if (history == null) return BadRequest("Invalid request.");
            int userId = 1; 
            var result = await _salesTaxReportDA.SaveTaxCodeHistoryAsync(history, userId);
            return Ok(result);
        }

        /// <summary>
        /// Deletes a tax code history entry by ID.
        /// Removes obsolete or incorrect historical tax rate records.
        /// </summary>
        [HttpDelete("DeleteTaxCodeHistory/{id}")]
        public async Task<ActionResult<bool>> DeleteTaxCodeHistory(int id)
        {
            var result = await _salesTaxReportDA.DeleteTaxCodeHistoryAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves the list of active vendors for vendor tax activity reporting.
        /// Populates vendor selection filters on the sales tax dashboard.
        /// </summary>
        [HttpGet("GetVendors")]
        public async Task<ActionResult<List<VendorBO>>> GetVendors()
        {
            var result = await _salesTaxReportDA.GetVendorsAsync();
            return Ok(result);
        }
    }
}
