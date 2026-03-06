using DAL.Common.Login;
using DAL.Inventory.IMEI.Credit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LegacyApp.Controllers.IEMI
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceCreditController : ControllerBase
    {
        private readonly IInvoiceCredit _invoiceDA;

        public InvoiceCreditController(IInvoiceCredit invoiceDA)
        {
            _invoiceDA = invoiceDA;
        }

        [HttpGet("FindReceipt")]
        public async Task<ApiResposne> FindReceiptByBVNoAsync(string bvReceiptNo)
        {
             return await _invoiceDA.FindReceiptByBVNoAsync(bvReceiptNo);
        }

        [HttpPost("SaveInvoice")]
        public async Task<IActionResult> SaveInvoice([FromBody] SaveInvoiceBO request)
        {
            var result = await _invoiceDA.SaveInvoiceAsync(request);
            return Ok(result);
        }

        [HttpGet("GetInvoices/{receiptNo}")]
        public async Task<IActionResult> GetInvoices(string receiptNo)
        {
            var result = await _invoiceDA.GetRogersInvoicesAsync(receiptNo);
            return Ok(result);
        }



        [HttpGet("GetAllReceipts")]
        public async Task<IActionResult> GetAllReceipts()
        {
            var res = await _invoiceDA.GetAllReceiptsAsync();
            return Ok(res);
        }


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

        [HttpGet("GetMissingReceiptsByPO/{poNumber}")]
        public async Task<IActionResult> GetMissingReceiptsByPO(string poNumber)
        {
            var result = await _invoiceDA.GetMissingReceiptsByPOAsync(poNumber);
            return Ok(result);
        }

        [HttpGet("GetReceiptsByType/{type}")]
        public async Task<IActionResult> GetReceiptsByType(string type)
        {
            var result = await _invoiceDA.GetReceiptsByTypeAsync(type);
            return Ok(result);
        }

        //[HttpGet("FindReceiptByBVNo")]
        //public async Task<IActionResult> FindReceiptByBVNo([FromQuery] string bvReceiptNo, [FromQuery] string type)
        //{
        //    if (string.IsNullOrEmpty(bvReceiptNo) || string.IsNullOrEmpty(type))
        //        return BadRequest(new { success = false, message = "BVReceiptNo and Type are required." });

        //    var result = await _invoiceDA.FindReceiptByBVNoAsync(bvReceiptNo, type);
        //    return Ok(result);
        //}


        [HttpPost("load-acc")]
        public async Task<ApiResposne> LoadAccReceipts()
        {
            return await _invoiceDA.LoadAccReceipts();
        }

    }
}