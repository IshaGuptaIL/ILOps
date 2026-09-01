using System;
using System.Threading.Tasks;
using DAL.Common.Login;
using DAL.Inventory.PriceProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LegacyApp.Controllers.Inventory
{
    /// <summary>
    /// Manages Price Protection claim processing for on-hand stock and purchase receipt items.
    /// Handles on-hand calculations, receipt-based price drops, manual IMEI adjustments, claim batches, and raw data exports.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PriceProtectionController : ControllerBase
    {
        private readonly IPriceProtection _priceProtection;

        public PriceProtectionController(IPriceProtection priceProtection)
        {
            _priceProtection = priceProtection;
        }

        /// <summary>
        /// Loads on-hand inventory claim records for a specified SKU on a designated effective price drop date.
        /// Extracts qualifying serial numbers and stock counts from the inventory database.
        /// </summary>
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

        /// <summary>
        /// Calculates price protection claim amounts across on-hand inventory units based on old vs new unit prices.
        /// Staged units are credited for the price drop difference.
        /// </summary>
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

        /// <summary>
        /// Looks up vendor purchase receipt details to verify eligibility for price protection claims.
        /// Retrieves received quantities, dates, and billed unit costs.
        /// </summary>
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

        /// <summary>
        /// Processes a price protection claim against units received on a specific purchase receipt number.
        /// Computes rebate difference for all serial numbers attached to the receipt.
        /// </summary>
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

        /// <summary>
        /// Manually appends an individual IMEI serial number into the current price protection staging batch.
        /// Used for manual claim overrides and exception handling.
        /// </summary>
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

        /// <summary>
        /// Removes an IMEI serial number from the pending price protection staging batch.
        /// Excludes erroneous or ineligible serial numbers prior to final batch submission.
        /// </summary>
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

        /// <summary>
        /// Retrieves all claim records currently staged in the active price protection batch.
        /// Displays pending items in the Price Protection batch grid.
        /// </summary>
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

        /// <summary>
        /// Finalizes the active price protection batch, assigns a permanent Batch ID, and appends to master claim records.
        /// Requires supervisor password authorization.
        /// </summary>
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

        /// <summary>
        /// Deletes an entire unposted or invalid price protection claim batch by batch number.
        /// Reverses staged claim lines from the database.
        /// </summary>
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

        /// <summary>
        /// Retrieves summary totals and status for all posted historical Price Protection claim batches.
        /// Displays posted claim records on the historical summary table.
        /// </summary>
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

        /// <summary>
        /// Exports raw price protection claim records across a date range to an Excel (.xlsx) file.
        /// Used for external auditor review and vendor dispute resolution.
        /// </summary>
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

        /// <summary>
        /// Retrieves the next available sequential Batch ID number for new price protection submissions.
        /// Pre-populates the batch ID indicator on the claim creation form.
        /// </summary>
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
