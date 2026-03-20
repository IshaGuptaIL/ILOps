using DAL.Common.Login;
using DAL.Inventory.CustomSearch;
using DAL.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LegacyApp.Controllers.Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomSearchController : ControllerBase
    {

        private readonly ICustomSearch _repo;

        public CustomSearchController(ICustomSearch repo)
        {
            _repo = repo;
        }

        [HttpGet("headers")]
        public async Task<ApiResposne> GetHeaders(string fieldName, string value)
        {
            return await _repo.GetSalesActivationHeaders(fieldName, value);
        }

        [HttpGet("details")]
        public async Task<ApiResposne> GetDetails(string invoiceNo)
        {
            return await _repo.GetSalesActivationDetails(invoiceNo);
        }
        [HttpPost("generate-invoice")]
        public async Task<List<tblSpireInvoice>> GenerateInvoiceAsync(string invoiceNo, int seq)
        {
            return await _repo.GenerateInvoiceAsync(invoiceNo, seq);
        }
        [HttpGet("transactions")]
        public async Task<ApiResposne> GetTransactions(string invoiceNo)
        {
            return await _repo.GetTransactionData(invoiceNo);
        }

    }
}
