using System;
using System.Threading.Tasks;
using DAL.Common.Login;
using DAL.Inventory.PriceProtection.ImeiSearch;
using Microsoft.AspNetCore.Mvc;

namespace LegacyApp.Controllers.Inventory
{
    /// <summary>
    /// Searches Price Protection claims, credits, and overpayments associated with specific IMEI numbers.
    /// Provides consolidated serial number lookup across all price protection tables.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ImeiSearchController : ControllerBase
    {
        private readonly IImeiSearch _da;

        public ImeiSearchController(IImeiSearch da)
        {
            _da = da;
        }

        /// <summary>
        /// Retrieves all Price Protection claims, issued credits, and overpayments linked to an IMEI serial number.
        /// Consolidates claim and credit history into a unified response.
        /// </summary>
        [HttpGet("search")]
        [HttpGet("search/{imei?}")]
        public async Task<ApiResposne> SearchImei(string imei = null)
        {
            try
            {
                var claims = await _da.GetClaimsByImeiAsync(imei);
                var credits = await _da.GetCreditsByImeiAsync(imei);
                var overpayments = await _da.GetOverpaymentsByImeiAsync(imei);

                return new ApiResposne
                {
                    Success = true,
                    Result = new
                    {
                        Claims = claims,
                        Credits = credits,
                        Overpayments = overpayments
                    },
                    Message = "IMEI search completed successfully."
                };
            }
            catch (Exception ex)
            {
                return new ApiResposne { Success = false, Message = ex.Message };
            }
        }
    }
}
