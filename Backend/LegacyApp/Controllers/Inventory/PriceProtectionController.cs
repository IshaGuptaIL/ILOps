using System;
using System.Threading.Tasks;
using DAL.Common.Login;
using DAL.Inventory.PriceProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LegacyApp.Controllers.Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    public class PriceProtectionController : ControllerBase
    {
        private readonly IPriceProtection _priceProtection;

        public PriceProtectionController(IPriceProtection priceProtection)
        {
            _priceProtection = priceProtection;
        }

        [HttpPost("load-claim-data")]
        public async Task<ApiResposne> LoadClaimData([FromBody] LoadOnhandClaimRequest request)
        {
            try
            {
                var success = await _priceProtection.LoadClaimDataAsync(request.SKU, request.OnhandDate);
                return new ApiResposne
                {
                    Success = success,
                    Message = success ? "Onhand claim data loaded successfully." : "Failed to load claim data."
                };
            }
            catch (Exception ex)
            {
                return new ApiResposne { Success = false, Message = ex.Message };
            }
        }

        [HttpPost("process-onhand-claim")]
        public async Task<ApiResposne> ProcessOnhandClaim([FromBody] ProcessOnhandClaimRequest request)
        {
            try
            {
                var user = Request.Cookies["UserID"] ?? "System";
                var processedCount = await _priceProtection.ProcessOnhandClaimAsync(
                    request.SKU, request.OnhandDate, request.PriceBefore, request.PriceAfter, user);

                return new ApiResposne
                {
                    Success = processedCount > 0,
                    Count = processedCount,
                    Message = $"Processed {processedCount} units successfully."
                };
            }
            catch (Exception ex)
            {
                return new ApiResposne { Success = false, Message = ex.Message };
            }
        }

        [HttpGet("find-receipt")]
        public async Task<ApiResposne> FindReceipt([FromQuery] string receiptNo)
        {
            try
            {
                var info = await _priceProtection.FindReceiptAsync(receiptNo);
                return new ApiResposne
                {
                    Success = info != null,
                    Result = info,
                    Message = info != null ? "Receipt found." : "Receipt not found."
                };
            }
            catch (Exception ex)
            {
                return new ApiResposne { Success = false, Message = ex.Message };
            }
        }

        [HttpPost("process-receipt-claim")]
        public async Task<ApiResposne> ProcessReceiptClaim([FromBody] ProcessReceiptClaimRequest request)
        {
            try
            {
                var user = Request.Cookies["UserID"] ?? "System";
                var processedCount = await _priceProtection.ProcessReceiptClaimAsync(
                    request.ReceiptNo, request.DropDate, request.PriceBefore, request.PriceAfter, user);

                return new ApiResposne
                {
                    Success = processedCount > 0,
                    Count = processedCount,
                    Message = $"Processed {processedCount} units successfully for receipt."
                };
            }
            catch (Exception ex)
            {
                return new ApiResposne { Success = false, Message = ex.Message };
            }
        }

        [HttpPost("manual-add-imei")]
        public async Task<ApiResposne> ManualAddImei([FromBody] ManualAddImeiRequest request)
        {
            try
            {
                var user = Request.Cookies["UserID"] ?? "System";
                var success = await _priceProtection.ManualAddImeiAsync(
                    request.IMEI, request.PriceBefore, request.PriceAfter, request.OnhandDate, request.SKU, request.Description, user);

                return new ApiResposne
                {
                    Success = success,
                    Message = "IMEI added to claim successfully."
                };
            }
            catch (Exception ex)
            {
                return new ApiResposne { Success = false, Message = ex.Message };
            }
        }

        [HttpPost("manual-remove-imei")]
        public async Task<ApiResposne> ManualRemoveImei([FromBody] string imei)
        {
            try
            {
                var success = await _priceProtection.ManualRemoveImeiAsync(imei);
                return new ApiResposne
                {
                    Success = success,
                    Message = success ? "IMEI removed from claim." : "IMEI not found in batch."
                };
            }
            catch (Exception ex)
            {
                return new ApiResposne { Success = false, Message = ex.Message };
            }
        }

        [HttpGet("batch-data")]
        public async Task<ApiResposne> GetBatchData()
        {
            try
            {
                var list = await _priceProtection.GetBatchDataAsync();
                return new ApiResposne
                {
                    Success = true,
                    Result = list,
                    Count = list.Count
                };
            }
            catch (Exception ex)
            {
                return new ApiResposne { Success = false, Message = ex.Message };
            }
        }

        [HttpPost("append-claim")]
        public async Task<ApiResposne> AppendClaim([FromBody] AppendClaimRequest request)
        {
            try
            {
                var user = Request.Cookies["UserID"] ?? "System";
                var success = await _priceProtection.AppendClaimAsync(request.Password, user);
                return new ApiResposne
                {
                    Success = success,
                    Message = "Claim appended and batch cleared successfully."
                };
            }
            catch (UnauthorizedAccessException ex)
            {
                return new ApiResposne { Success = false, Message = ex.Message, StatusCode = 401 };
            }
            catch (Exception ex)
            {
                return new ApiResposne { Success = false, Message = ex.Message };
            }
        }

        [HttpDelete("remove-batch/{batchNo}")]
        public async Task<ApiResposne> RemoveBatch(int batchNo)
        {
            try
            {
                var success = await _priceProtection.RemoveBatchAsync(batchNo);
                return new ApiResposne
                {
                    Success = success,
                    Message = success ? $"Batch {batchNo} removed successfully." : $"Batch {batchNo} not found."
                };
            }
            catch (Exception ex)
            {
                return new ApiResposne { Success = false, Message = ex.Message };
            }
        }

        [HttpGet("posted-summary")]
        public async Task<ApiResposne> GetPostedSummary()
        {
            try
            {
                var list = await _priceProtection.GetPostedClaimsSummaryAsync();
                return new ApiResposne
                {
                    Success = true,
                    Result = list,
                    Count = list.Count
                };
            }
            catch (Exception ex)
            {
                return new ApiResposne { Success = false, Message = ex.Message };
            }
        }

        [HttpGet("export-raw-data")]
        public async Task<IActionResult> ExportRawData([FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            try
            {
                var fileBytes = await _priceProtection.GetRawClaimDataExcelAsync(start, end);
                var fileName = $"PriceProtectionRawData_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("next-batch-id")]
        public async Task<ApiResposne> GetNextBatchID()
        {
            try
            {
                var nextBatchID = await _priceProtection.GetNextBatchIDAsync();
                return new ApiResposne
                {
                    Success = true,
                    Result = nextBatchID
                };
            }
            catch (Exception ex)
            {
                return new ApiResposne { Success = false, Message = ex.Message };
            }
        }
    }

    public class LoadOnhandClaimRequest
    {
        public string SKU { get; set; } = string.Empty;
        public DateTime OnhandDate { get; set; }
    }

    public class ProcessOnhandClaimRequest
    {
        public string SKU { get; set; } = string.Empty;
        public DateTime OnhandDate { get; set; }
        public decimal PriceBefore { get; set; }
        public decimal PriceAfter { get; set; }
    }

    public class ProcessReceiptClaimRequest
    {
        public string ReceiptNo { get; set; } = string.Empty;
        public DateTime DropDate { get; set; }
        public decimal PriceBefore { get; set; }
        public decimal PriceAfter { get; set; }
    }

    public class ManualAddImeiRequest
    {
        public string IMEI { get; set; } = string.Empty;
        public decimal PriceBefore { get; set; }
        public decimal PriceAfter { get; set; }
        public DateTime OnhandDate { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class AppendClaimRequest
    {
        public string Password { get; set; } = string.Empty;
    }
}
