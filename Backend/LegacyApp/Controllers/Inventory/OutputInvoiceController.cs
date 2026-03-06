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

            var count = await _repo.ProcessAllInvoices(request.OutputFolder, request.FilePrefix, request.InvoiceType);

            return Ok(new
            {
                Message = "Output Process Complete",
                ProcessedCount = count
            });
        }
    }

  
}