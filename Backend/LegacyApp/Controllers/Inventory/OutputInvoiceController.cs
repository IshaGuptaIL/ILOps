using DAL.Inventory.OutputInvoice;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace LegacyApp.Controllers.Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    public class OutputInvoiceController : ControllerBase
    {
        private readonly IOutputInvoice _repo;

        public OutputInvoiceController(IOutputInvoice repo)
        {
            _repo = repo;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetInvoiceList([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _repo.GetInvoiceListPaged(page, pageSize);
            return Ok(result);
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearInvoices()
        {
            var result = await _repo.ClearInvoiceList(); 
            return Ok(result);
        }

        [HttpPost("output-all")]
        public async Task<IActionResult> OutputInvoices([FromBody] InvoiceOutputRequest request)
        {
            if (request == null) return BadRequest("Invalid Request");

            //var count = await _repo.ProcessAllInvoices(request.OutputFolder, request.FilePrefix, request.InvoiceType);

            return Ok(new
            {
                Message = "Output Process Complete",
                //ProcessedCount = count
            });
        }


        [HttpPost("upload-template")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            var result = await _repo.UploadAndMatchTemplate(stream);
            return Ok(result);
        }

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