using DAL.Common.Login;
using DAL.Inventory.IMEI.Credit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LegacyApp.Controllers.IEMI
{
    /// <summary>
    /// Manages Rogers invoice and credit memo entry for hardware and accessory receipts.
    /// Handles receipt searches, missing receipt tracking, invoice line additions, and Spire accessory syncing.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceCreditController : ControllerBase
    {
        private readonly IInvoiceCredit _invoiceDA;

        public InvoiceCreditController(IInvoiceCredit invoiceDA)
        {
            _invoiceDA = invoiceDA;
        }

        /// <summary>
        /// Finds a specific hardware or accessory receipt by its BV Receipt Number.
        /// Loads receipt details and associated Rogers invoice entries for editing.
        /// </summary>
        [HttpGet("FindReceipt")]
        public async Task<ApiResposne> FindReceiptByBVNoAsync(string bvReceiptNo)
        {
             return await _invoiceDA.FindReceiptByBVNoAsync(bvReceiptNo);
        }

        /// <summary>
        /// Saves or updates a Rogers invoice or credit line item against a receipt.
        /// Inserts transaction details into tblRogersInvoice and updates calculated variances.
        /// </summary>
        [HttpPost("SaveInvoice")]
        public async Task<IActionResult> SaveInvoice([FromBody] SaveInvoiceBO request)
        {
            var result = await _invoiceDA.SaveInvoiceAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves all Rogers invoice lines and credits associated with a specific receipt number.
        /// Displays itemized list of invoice transactions in the detail grid.
        /// </summary>
        [HttpGet("GetInvoices/{receiptNo}")]
        public async Task<IActionResult> GetInvoices(string receiptNo)
        {
            var result = await _invoiceDA.GetRogersInvoicesAsync(receiptNo);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves all receipts with missing invoice information from the last 4 months.
        /// Populates the missing invoice summary grid for reconciliation.
        /// </summary>
        [HttpGet("GetAllReceipts")]
        public async Task<IActionResult> GetAllReceipts()
        {
            var res = await _invoiceDA.GetAllReceiptsAsync();
            return Ok(res);
        }

        /// <summary>
        /// Searches receipts dynamically by receipt number, PO number, or item type.
        /// Allows operators to locate specific receipts for credit and invoice processing.
        /// </summary>
        [HttpGet("SearchReceipts")]
        public async Task<IActionResult> SearchReceipts(
            [FromQuery] string? bvReceiptNo = null,
            [FromQuery] string? poNumber = null,
            [FromQuery] string? type = null)
        {
            var request = new SearchReceiptsBO
            {
                ReceiptNo = bvReceiptNo ?? "",
                PONumber = poNumber ?? "",
                Type = type ?? ""
            };

            if (string.IsNullOrWhiteSpace(request.ReceiptNo) && string.IsNullOrWhiteSpace(request.PONumber))
                return BadRequest(new { success = false, message = "Enter Receipt No or PO Number." });

            var result = await _invoiceDA.SearchReceiptsAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves missing receipt lines filtered by a specific PO number.
        /// Used when searching for un-invoiced hardware lines under a known PO.
        /// </summary>
        [HttpGet("GetMissingReceiptsByPO/{poNumber}")]
        public async Task<IActionResult> GetMissingReceiptsByPO(string poNumber)
        {
            var result = await _invoiceDA.GetMissingReceiptsByPOAsync(poNumber);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves receipts filtered by item type category (HDW for Hardware, ACC for Accessory).
        /// Used by the type tab filters on the Invoice / Credit entry screen.
        /// </summary>
        [HttpGet("GetReceiptsByType/{type}")]
        public async Task<IActionResult> GetReceiptsByType(string type)
        {
            var result = await _invoiceDA.GetReceiptsByTypeAsync(type);
            return Ok(result);
        }

        /// <summary>
        /// Syncs latest accessory receipts from Spire PostgreSQL database into local HardwareReceived table.
        /// Updates the last synced accessory receipt tracker in tblSettings.
        /// </summary>
        [HttpPost("load-acc")]
        public async Task<ApiResposne> LoadAccReceipts()
        {
            return await _invoiceDA.LoadAccReceipts();
        }
    }
}