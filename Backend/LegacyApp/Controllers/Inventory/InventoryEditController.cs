using DAL.Inventory.InventoryEdit;
using DAL.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LegacyApp.Controllers.Inventory
{
    /// <summary>
    /// Manages invoice metadata adjustments including payment terms, bulk billing IDs, and customer billing/shipping addresses.
    /// Allows operators to correct sales invoice attributes in historical sales records.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryEditController : ControllerBase
    {
        private readonly IInventoryEdit _inventoryEditDA;

        public InventoryEditController(IInventoryEdit inventoryEditDA)
        {
            _inventoryEditDA = inventoryEditDA;
        }

        // ─── Terms Edit ───────────────────────────────────────────────────────

        /// <summary>
        /// Retrieves the current payment terms assigned to a specific sales invoice.
        /// Displays payment terms on the invoice terms edit screen.
        /// </summary>
        [HttpGet("GetInvoiceTerms")]
        public async Task<ActionResult<sales_history>> GetInvoiceTerms([FromQuery] string invoiceNo)
        {
            if (string.IsNullOrWhiteSpace(invoiceNo))
                return BadRequest("Invoice number is required.");

            var result = await _inventoryEditDA.GetInvoiceTermsAsync(invoiceNo);
            if (result == null) return NotFound("Invoice not found.");
            return Ok(result);
        }

        /// <summary>
        /// Updates the payment terms code and label on an existing sales invoice.
        /// Modifies credit and due date terms in the sales history database.
        /// </summary>
        [HttpPost("UpdateInvoiceTerms")]
        public async Task<ActionResult<bool>> UpdateInvoiceTerms([FromBody] UpdateTermsRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.InvoiceNo))
                return BadRequest("Invalid request.");

            var success = await _inventoryEditDA.UpdateInvoiceTermsAsync(
                request.InvoiceNo, request.TermsLabel, request.ModifiedBy);

            return Ok(success);
        }

        // ─── Bulk ID Edit ─────────────────────────────────────────────────────

        /// <summary>
        /// Returns the count of sales invoices sharing a designated bulk billing identifier.
        /// Used to verify batch size before executing bulk ID reassignments.
        /// </summary>
        [HttpGet("GetBulkIdCount")]
        public async Task<ActionResult> GetBulkIdCount([FromQuery] string bulkId)
        {
            if (string.IsNullOrWhiteSpace(bulkId))
                return BadRequest("Bulk ID is required.");

            var count = await _inventoryEditDA.GetBulkIdCountAsync(bulkId);
            return Ok(new { count });
        }

        /// <summary>
        /// Updates all sales invoice records associated with an old bulk ID to a new bulk ID.
        /// Performs batch ID reassignments across grouped corporate invoices.
        /// </summary>
        [HttpPost("UpdateBulkId")]
        public async Task<ActionResult<bool>> UpdateBulkId([FromBody] UpdateBulkIdRequest request)
        {
            if (request == null)
                return BadRequest("Invalid request.");

            var success = await _inventoryEditDA.UpdateBulkIdAsync(
                request.OldBulkId, request.NewBulkId, request.ModifiedBy);

            return Ok(success);
        }

        /// <summary>
        /// Retrieves the bulk billing identifier assigned to a single specific invoice.
        /// Displays current bulk assignment on the invoice editor.
        /// </summary>
        [HttpGet("GetSingleInvoiceBulkId")]
        public async Task<ActionResult<sales_history>> GetSingleInvoiceBulkId([FromQuery] string invoiceNo)
        {
            if (string.IsNullOrWhiteSpace(invoiceNo))
                return BadRequest("Invoice number is required.");

            var result = await _inventoryEditDA.GetSingleInvoiceBulkIdAsync(invoiceNo);
            if (result == null) return NotFound("Invoice not found.");
            return Ok(result);
        }

        /// <summary>
        /// Reassigns the bulk billing identifier for a single invoice number.
        /// Modifies group billing associations on an individual invoice.
        /// </summary>
        [HttpPost("UpdateSingleInvoiceBulkId")]
        public async Task<ActionResult<bool>> UpdateSingleInvoiceBulkId([FromBody] UpdateSingleInvoiceBulkIdRequest request)
        {
            if (request == null)
                return BadRequest("Invalid request.");

            var success = await _inventoryEditDA.UpdateSingleInvoiceBulkIdAsync(
                request.InvoiceNo, request.NewBulkId, request.ModifiedBy);

            return Ok(success);
        }

        /// <summary>
        /// Updates the bulk billing identifier across an explicit list of multiple invoice numbers.
        /// Groups selected invoices under a common corporate billing ID.
        /// </summary>
        [HttpPost("UpdateMultipleBulkIds")]
        public async Task<ActionResult<bool>> UpdateMultipleBulkIds([FromBody] UpdateMultipleBulkIdsRequest request)
        {
            if (request == null || request.InvoiceNos == null || request.InvoiceNos.Count == 0)
                return BadRequest("Invoice list is empty.");

            var success = await _inventoryEditDA.UpdateMultipleBulkIdsAsync(
                request.InvoiceNos, request.NewBulkId, request.ModifiedBy);

            return Ok(success);
        }

        // ─── Address Edit ─────────────────────────────────────────────────────

        /// <summary>
        /// Retrieves the billing and shipping address records saved on an invoice.
        /// Populates the invoice address correction form.
        /// </summary>
        [HttpGet("GetInvoiceAddress")]
        public async Task<ActionResult<InvoiceAddressEditModel>> GetInvoiceAddress([FromQuery] string invoiceNo)
        {
            if (string.IsNullOrWhiteSpace(invoiceNo))
                return BadRequest("Invoice number is required.");

            var result = await _inventoryEditDA.GetInvoiceAddressAsync(invoiceNo);
            if (result == null) return NotFound("Invoice not found.");
            return Ok(result);
        }

        /// <summary>
        /// Updates the Bill-To and Ship-To address information recorded on a sales invoice.
        /// Corrects customer shipping or billing destinations after invoice creation.
        /// </summary>
        [HttpPost("UpdateInvoiceAddress")]
        public async Task<ActionResult<bool>> UpdateInvoiceAddress([FromBody] UpdateAddressRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.InvoiceNo))
                return BadRequest("Invalid request.");

            var model = new InvoiceAddressEditModel
            {
                InvoiceNo = request.InvoiceNo,
                BillTo = request.BillTo,
                ShipTo = request.ShipTo
            };

            var success = await _inventoryEditDA.UpdateInvoiceAddressAsync(model, request.ModifiedBy);
            return Ok(success);
        }
    }
}
