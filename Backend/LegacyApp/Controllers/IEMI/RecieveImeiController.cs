using DAL.Common.Login;
using DAL.Inventory.IMEI.RecieveIMEI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;

namespace LegacyApp.Controllers.IEMI
{
    [Route("api/RecieveImei")]
    [ApiController]
    public class RecieveImeiController : ControllerBase
    {

        private readonly IRecieveImei _recieveImei;

        public RecieveImeiController(IRecieveImei recieveImei)
        {
            _recieveImei = recieveImei;
        }

        [HttpPost("ImportPackingSlip")]
        public async Task<ApiResposne> ImportPackingSlip([FromBody] List<RecieveIMEIBO> items)
        {
            await _recieveImei.ClearPackingSlipAsync();
            var result = await _recieveImei.InsertPackingSlipAsync(items);
            return result;
        }
        [HttpGet("GetPurchaseOrdersAsync")]
        public async Task<ApiResposne> GetPurchaseOrdersAsync()
        {
            return await _recieveImei.GetPurchaseOrdersAsync();
        }


        [HttpPost("InsertScanList")]
        public async Task<IActionResult> InsertScanList([FromBody] List<RecieveIMEIBO> items)
        {
            if (items == null || items.Count == 0)
                return BadRequest(new ApiResposne { Success = false, Message = "No items provided" });

            var result = await _recieveImei.InsertScanListAsync(items);
            if (result.Success)
                return Ok(result);
            else
                return StatusCode(500, result);
        }


        [HttpGet("GetIMEIGrids/{poNumber}")]
        public async Task<ApiResposne> GetIMEIGridsAsync(string poNumber)
        {
            return await _recieveImei.GetIMEIGridsAsync(poNumber);
        }


        [HttpPost("PostReceiptsAsync")]
        public async Task<ApiResposne> PostReceiptsAsync([FromBody] PostReceiptsRequest request)
        {
            return await _recieveImei.PostReceiptsAsync(request.PoId, request.PoItemId, request.Cmo, request.IsReversal);



        }


        [HttpGet("CheckErrorsAsync")]
        public async Task<ApiResposne> CheckErrorsAsync(long poId, long poItemId, bool isReversal)
        {
            return await _recieveImei.CheckErrorsAsync(poId, poItemId, isReversal);


                }
    }
    }