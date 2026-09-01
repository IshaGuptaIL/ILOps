using DAL.Common.Login;
using DAL.Inventory.CustomSearch;
using DAL.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LegacyApp.Controllers.Inventory
{
    /// <summary>
    /// Searches sales activation histories, retrieves invoice details, and generates Spire invoice data.
    /// Provides custom dynamic searches across activation fields, line item details, and linked transactions.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class CustomSearchController : ControllerBase
    {
        private readonly ICustomSearch _repo;

        public CustomSearchController(ICustomSearch repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Searches sales activation headers matching a specified field name and query filter.
        /// Populates the search results grid in the Custom Search module.
        /// </summary>
        [HttpGet("headers")]
        public async Task<ApiResposne> GetHeaders(string fieldName, string value)
        {
            return await _repo.GetSalesActivationHeaders(fieldName, value);
        }

        /// <summary>
        /// Retrieves line-item activation details for a specific invoice number.
        /// Displays item descriptions, serials, and transaction charges in the details pane.
        /// </summary>
        [HttpGet("details")]
        public async Task<ApiResposne> GetDetails(string invoiceNo)
        {
            return await _repo.GetSalesActivationDetails(invoiceNo);
        }

        /// <summary>
        /// Generates Spire invoice entity records corresponding to an activation invoice and sequence.
        /// Prepares invoice entries for ERP posting.
        /// </summary>
        [HttpPost("generate-invoice")]
        public async Task<List<tblSpireInvoice>> GenerateInvoiceAsync(string invoiceNo, int seq)
        {
            return await _repo.GenerateInvoiceAsync(invoiceNo, seq);
        }

        /// <summary>
        /// Retrieves complete transaction details and payment allocations for an invoice number.
        /// Provides full transaction history for auditing activation payments.
        /// </summary>
        [HttpGet("transactions")]
        public async Task<ApiResposne> GetTransactions(string invoiceNo)
        {
            return await _repo.GetTransactionData(invoiceNo);
        }
    }
}
