using DAL.Inventory.OutputInvoice;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;

namespace LegacyApp.Controllers.Inventory
{
    /// <summary>
    /// Generates, matches, and exports sales invoice document batches into downloadable ZIP packages.
    /// Handles invoice queuing, template matching, bulk printing pipelines, and archive generation.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class OutputInvoiceController : ControllerBase
    {
        private readonly IOutputInvoice _repo;

        public OutputInvoiceController(IOutputInvoice repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Retrieves a paginated list of queued invoices awaiting PDF generation or printing.
        /// Displays staged invoice lines in the output queue table.
        /// </summary>
        [HttpGet("list")]
        public async Task<IActionResult> GetInvoiceList([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _repo.GetInvoiceListPaged(page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Clears all queued invoice records from the generation pipeline.
        /// Resets the invoice output staging list.
        /// </summary>
        [HttpDelete("clear")]
        public async Task<IActionResult> ClearInvoices()
        {
            var result = await _repo.ClearInvoiceList();
            return Ok(result);
        }

        /// <summary>
        /// Processes and renders all queued invoices according to selected output criteria and format.
        /// Generates document files on the server for archiving.
        /// </summary>
        [HttpPost("output-all")]
        public async Task<IActionResult> OutputInvoices([FromBody] InvoiceOutputRequest request)
        {
            if (request == null) return BadRequest("Invalid Request");

            var count = await _repo.ProcessAllInvoices("", request.FilePrefix, request.InvoiceType);

            return Ok(new
            {
                Message = "Output Process Complete",
                ProcessedCount = count
            });
        }

        /// <summary>
        /// Uploads an external invoice template spreadsheet and matches invoice numbers against staging.
        /// Identifies matched invoice files for batch processing.
        /// </summary>
        [HttpPost("upload-template")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            var result = await _repo.UploadAndMatchTemplate(stream);
            return Ok(result);
        }

        /// <summary>
        /// Compresses generated invoice PDF documents into a downloadable ZIP archive.
        /// Facilitates single-download distribution of large invoice batches.
        /// </summary>
        [HttpPost("generate-zip")]
        public async Task<IActionResult> GenerateZip([FromBody] InvoiceOutputRequest request)
        {
            var zipBytes = await _repo.GenerateInvoicesZip(request.FilePrefix);

            if (zipBytes == null || zipBytes.Length == 0)
                return BadRequest("No invoices to process.");

            string fileName = $"Invoices_{DateTime.Now:yyyyMMddHHmmss}.zip";
            return File(zipBytes, "application/zip", fileName);
        }
    }
}