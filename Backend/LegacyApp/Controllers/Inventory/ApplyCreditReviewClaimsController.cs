using System;
using System.Threading.Tasks;
using DAL.Common.Login;
using DAL.Inventory.PriceProtection.ApplyCredit_ReviewClaims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LegacyApp.Controllers.Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApplyCreditReviewClaimsController : ControllerBase
    {
        private readonly IApplyCreditReviewClaims _da;

        public ApplyCreditReviewClaimsController(IApplyCreditReviewClaims da)
        {
            _da = da;
        }

        [HttpGet("claims-summary")]
        public async Task<ApiResposne> GetClaimsSummary()
        {
            try
            {
                var data = await _da.GetClaimsSummaryAsync();
                return new ApiResposne
                {
                    Success = true,
                    Result = data,
                    Count = data.Count,
                    Message = "Claims summary retrieved successfully."
                };
            }
            catch (Exception ex)
            {
                return new ApiResposne { Success = false, Message = ex.Message };
            }
        }

        [HttpGet("credit-summary/{batchId}")]
        public async Task<ApiResposne> GetCreditSummary(int batchId)
        {
            try
            {
                var data = await _da.GetCreditSummaryAsync(batchId);
                return new ApiResposne
                {
                    Success = true,
                    Result = data,
                    Count = data.Count,
                    Message = "Credit summary retrieved successfully."
                };
            }
            catch (Exception ex)
            {
                return new ApiResposne { Success = false, Message = ex.Message };
            }
        }

        [HttpGet("unpaid-claims-detail")]
        public async Task<ApiResposne> GetUnpaidClaimsDetail([FromQuery] int batchId, [FromQuery] string? creditNoteNumber)
        {
            try
            {
                var data = await _da.GetUnpaidClaimsDetailAsync(batchId, creditNoteNumber);
                return new ApiResposne
                {
                    Success = true,
                    Result = data,
                    Count = data.Count,
                    Message = "Unpaid claims details retrieved successfully."
                };
            }
            catch (Exception ex)
            {
                return new ApiResposne { Success = false, Message = ex.Message };
            }
        }

        [HttpGet("credit-detail/{claimId}")]
        public async Task<ApiResposne> GetCreditDetail(int claimId)
        {
            try
            {
                var data = await _da.GetCreditDetailAsync(claimId);
                return new ApiResposne
                {
                    Success = true,
                    Result = data,
                    Count = data.Count,
                    Message = "Credit details retrieved successfully."
                };
            }
            catch (Exception ex)
            {
                return new ApiResposne { Success = false, Message = ex.Message };
            }
        }

        [HttpPost("modify-credit-number")]
        public async Task<ApiResposne> ModifyCreditNoteNumber([FromBody] ModifyCreditNumberRequest request)
        {
            try
            {
                var user = Request.Cookies["UserID"] ?? "System";
                var success = await _da.ModifyCreditNoteNumberAsync(request.OldCreditNoteNumber, request.NewCreditNoteNumber, user);
                return new ApiResposne
                {
                    Success = success,
                    Message = success ? "Credit note number modified successfully." : "No credits updated (confirm old number matches)."
                };
            }
            catch (Exception ex)
            {
                return new ApiResposne { Success = false, Message = ex.Message };
            }
        }

        [HttpPost("apply-credit")]
        public async Task<ApiResposne> ApplyCredit([FromBody] ApplyCreditRequest request)
        {
            try
            {
                var user = Request.Cookies["UserID"] ?? "System";
                var success = await _da.ApplyCreditAsync(request, user);
                return new ApiResposne
                {
                    Success = success,
                    Message = "Credits applied successfully."
                };
            }
            catch (Exception ex)
            {
                return new ApiResposne { Success = false, Message = ex.Message };
            }
        }

        [HttpGet("export-claims-summary")]
        public async Task<IActionResult> ExportClaimsSummary()
        {
            try
            {
                var fileBytes = await _da.ExportClaimsSummaryExcelAsync();
                var fileName = $"ClaimsSummary_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
