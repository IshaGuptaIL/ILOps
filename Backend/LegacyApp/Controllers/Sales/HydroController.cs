using DAL.Sales.HydroSales;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace LegacyApp.Controllers.Sales
{
    /// <summary>
    /// Handles Hydro sales payment transactions and credit memo generation.
    /// Manages customer payment postings and billing adjustment memo creation.
    /// </summary>
    [Route("api/Sales/[controller]")]
    [ApiController]
    public class HydroController : ControllerBase
    {
        private readonly IHydroSales _hydroSalesDA;

        public HydroController(IHydroSales hydroSalesDA)
        {
            _hydroSalesDA = hydroSalesDA;
        }

        private int GetUserId(int? requestUserId)
        {
            if (requestUserId.HasValue && requestUserId.Value > 0)
            {
                return requestUserId.Value;
            }
            if (Request.Cookies.TryGetValue("userId", out string? userIdStr) && int.TryParse(userIdStr, out int userId))
            {
                return userId;
            }
            return 1; // Fallback for testing
        }

        /// <summary>
        /// Posts customer payment allocations against open Hydro sales invoices.
        /// Updates ledger balances and logs the payment transaction.
        /// </summary>
        [HttpPost("PostPayment")]
        public async Task<ActionResult<PostPaymentResponse>> PostPayment([FromBody] PostPaymentRequest request, CancellationToken cancellationToken)
        {
            if (request == null) return BadRequest("Invalid request.");
            int userId = GetUserId(request.UserId);
            var result = await _hydroSalesDA.PostPaymentAsync(request, userId);
            return Ok(result);
        }

        /// <summary>
        /// Generates credit or debit adjustment memos for Hydro billing differences.
        /// Records adjustment entries into the sales ledger.
        /// </summary>
        [HttpPost("GenerateMemo")]
        public async Task<ActionResult<GenerateMemoResponse>> GenerateMemo([FromBody] GenerateMemoRequest request, CancellationToken cancellationToken)
        {
            if (request == null) return BadRequest("Invalid request.");
            int userId = GetUserId(request.UserId);
            var result = await _hydroSalesDA.GenerateMemoAsync(request, userId);
            return Ok(result);
        }
    }
}
