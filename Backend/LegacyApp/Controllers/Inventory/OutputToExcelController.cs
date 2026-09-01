using System;
using System.Threading.Tasks;
using DAL.Inventory.PriceProtection.OutputToExcel;
using Microsoft.AspNetCore.Mvc;

namespace LegacyApp.Controllers.Inventory
{
    /// <summary>
    /// Exports Price Protection batch details, Rogers overpayment audits, and claims-to-credits reconciliations to Excel.
    /// Generates structured spreadsheet reports for finance verification and supplier submissions.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class OutputToExcelController : ControllerBase
    {
        private readonly IOutputToExcel _da;

        public OutputToExcelController(IOutputToExcel da)
        {
            _da = da;
        }

        /// <summary>
        /// Exports all itemized claim records for a Price Protection batch ID into an Excel (.xlsx) file.
        /// Formats claim details for vendor reconciliation and audit logs.
        /// </summary>
        [HttpGet("export-batch/{batchId}")]
        public async Task<IActionResult> ExportBatch(int batchId)
        {
            try
            {
                var fileBytes = await _da.ExportPriceProtectionBatchAsync(batchId);
                var fileName = $"PriceProtectionBatch_{batchId}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Exports detected Rogers overpayment transactions into an Excel (.xlsx) spreadsheet.
        /// Used by the AR and vendor recovery teams to track excess disbursements.
        /// </summary>
        [HttpGet("export-rogers-overpayments")]
        public async Task<IActionResult> ExportRogersOverpayments()
        {
            try
            {
                var fileBytes = await _da.ExportRogersOverpaymentsAsync();
                var fileName = $"RogersOverpayments_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Exports Claims to Credits reconciliation data comparing expected credit amounts with vendor credits.
        /// Produces downloadable Excel analysis sheet for credit matching.
        /// </summary>
        [HttpGet("export-claims-to-credits")]
        public async Task<IActionResult> ExportClaimsToCredits()
        {
            try
            {
                var fileBytes = await _da.ExportClaimsToCreditsAsync();
                var fileName = $"ClaimsToCredits_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Retrieves Claims to Credits reconciliation rows for live display on the dashboard grid.
        /// Lists pending credits, matched amounts, and variances.
        /// </summary>
        [HttpGet("claims-to-credits-data")]
        public async Task<IActionResult> GetClaimsToCreditsData()
        {
            try
            {
                var data = await _da.GetClaimsToCreditsDataAsync();
                return Ok(new { success = true, result = data });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
