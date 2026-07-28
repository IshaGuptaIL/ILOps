using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DAL.Sales.Interface;
using System.IO;

namespace LegacyApp.Controllers
{
    [ApiController]
    [Route("api/sales/rmareporting/import")]
    public class RMAReportingImportController : ControllerBase
    {
        private readonly IRogersReportImportBo _importBo;

        public RMAReportingImportController(IRogersReportImportBo importBo)
        {
            _importBo = importBo;
        }

        [HttpPost("upload/{fileType}")]
        public async Task<IActionResult> UploadFile(string fileType, IFormFile file)
        {
            // 10 minute timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            
            if (file == null || file.Length == 0)
                return BadRequest("No file provided");

            try
            {
                using (var stream = file.OpenReadStream())
                {
                    await _importBo.ProcessAndImportFileAsync(stream, fileType, file.FileName, cts.Token);
                }
                return Ok(new { message = $"{fileType} uploaded and processed successfully." });
            }
            catch (OperationCanceledException)
            {
                return StatusCode(408, "Request Timeout: Processing took longer than 10 minutes.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("template/{fileType}")]
        public async Task<IActionResult> DownloadTemplate(string fileType)
        {
            try
            {
                var content = await _importBo.GenerateTemplateAsync(fileType);
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{fileType}_Template.xlsx");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("cmsummary")]
        public async Task<IActionResult> GenerateCmSummary()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            try
            {
                await _importBo.GenerateCmSummaryAsync(cts.Token);
                return Ok(new { message = "CM Summary Generated" });
            }
            catch (OperationCanceledException)
            {
                return StatusCode(408, "Request Timeout: Processing took longer than 10 minutes.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("manualimport")]
        public async Task<IActionResult> ProcessManualImport()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            try
            {
                await _importBo.ProcessManualRmaImportAsync(cts.Token);
                return Ok(new { message = "Manual RMA Import Completed" });
            }
            catch (OperationCanceledException)
            {
                return StatusCode(408, "Request Timeout: Processing took longer than 10 minutes.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("deletebatch")]
        public async Task<IActionResult> DeleteBatch([FromQuery] string cmFile, [FromQuery] string rmFile, [FromQuery] string manualFile)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            try
            {
                await _importBo.DeleteBatchFilesAsync(cmFile, rmFile, manualFile, cts.Token);
                return Ok(new { message = "Batch deleted successfully." });
            }
            catch (OperationCanceledException)
            {
                return StatusCode(408, "Request Timeout: Processing took longer than 10 minutes.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
