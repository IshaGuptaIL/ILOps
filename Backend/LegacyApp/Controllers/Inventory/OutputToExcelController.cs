using System;
using System.Threading.Tasks;
using DAL.Inventory.PriceProtection.OutputToExcel;
using Microsoft.AspNetCore.Mvc;

namespace LegacyApp.Controllers.Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    public class OutputToExcelController : ControllerBase
    {
        private readonly IOutputToExcel _da;

        public OutputToExcelController(IOutputToExcel da)
        {
            _da = da;
        }

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
