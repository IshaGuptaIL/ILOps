using DAL.Inventory.InventoryEdit;
using DAL.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LegacyApp.Controllers.Inventory
{
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

        /// <summary>GET /api/InventoryEdit/GetInvoiceTerms?invoiceNo=...</summary>
        [HttpGet("GetInvoiceTerms")]
        public async Task<ActionResult<sales_history>> GetInvoiceTerms([FromQuery] string invoiceNo)
        {
            if (string.IsNullOrWhiteSpace(invoiceNo))
                return BadRequest("Invoice number is required.");

            var result = await _inventoryEditDA.GetInvoiceTermsAsync(invoiceNo);
            if (result == null) return NotFound("Invoice not found.");
            return Ok(result);
        }

        /// <summary>POST /api/InventoryEdit/UpdateInvoiceTerms</summary>
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

        /// <summary>GET /api/InventoryEdit/GetBulkIdCount?bulkId=...</summary>
        [HttpGet("GetBulkIdCount")]
        public async Task<ActionResult> GetBulkIdCount([FromQuery] string bulkId)
        {
            if (string.IsNullOrWhiteSpace(bulkId))
                return BadRequest("Bulk ID is required.");

            var count = await _inventoryEditDA.GetBulkIdCountAsync(bulkId);
            return Ok(new { count });
        }

        /// <summary>POST /api/InventoryEdit/UpdateBulkId</summary>
        [HttpPost("UpdateBulkId")]
        public async Task<ActionResult<bool>> UpdateBulkId([FromBody] UpdateBulkIdRequest request)
        {
            if (request == null)
                return BadRequest("Invalid request.");

            var success = await _inventoryEditDA.UpdateBulkIdAsync(
                request.OldBulkId, request.NewBulkId, request.ModifiedBy);

            return Ok(success);
        }

        /// <summary>GET /api/InventoryEdit/GetSingleInvoiceBulkId?invoiceNo=...</summary>
        [HttpGet("GetSingleInvoiceBulkId")]
        public async Task<ActionResult<sales_history>> GetSingleInvoiceBulkId([FromQuery] string invoiceNo)
        {
            if (string.IsNullOrWhiteSpace(invoiceNo))
                return BadRequest("Invoice number is required.");

            var result = await _inventoryEditDA.GetSingleInvoiceBulkIdAsync(invoiceNo);
            if (result == null) return NotFound("Invoice not found.");
            return Ok(result);
        }

        /// <summary>POST /api/InventoryEdit/UpdateSingleInvoiceBulkId</summary>
        [HttpPost("UpdateSingleInvoiceBulkId")]
        public async Task<ActionResult<bool>> UpdateSingleInvoiceBulkId([FromBody] UpdateSingleInvoiceBulkIdRequest request)
        {
            if (request == null)
                return BadRequest("Invalid request.");

            var success = await _inventoryEditDA.UpdateSingleInvoiceBulkIdAsync(
                request.InvoiceNo, request.NewBulkId, request.ModifiedBy);

            return Ok(success);
        }

        /// <summary>POST /api/InventoryEdit/UpdateMultipleBulkIds</summary>
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

        /// <summary>GET /api/InventoryEdit/GetInvoiceAddress?invoiceNo=...</summary>
        [HttpGet("GetInvoiceAddress")]
        public async Task<ActionResult<InvoiceAddressEditModel>> GetInvoiceAddress([FromQuery] string invoiceNo)
        {
            if (string.IsNullOrWhiteSpace(invoiceNo))
                return BadRequest("Invoice number is required.");

            var result = await _inventoryEditDA.GetInvoiceAddressAsync(invoiceNo);
            if (result == null) return NotFound("Invoice not found.");
            return Ok(result);
        }

        /// <summary>POST /api/InventoryEdit/UpdateInvoiceAddress</summary>
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
