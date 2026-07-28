using System;
using System.Threading.Tasks;
using DAL.Common.Login;
using DAL.Inventory.PriceProtection.ImeiSearch;
using Microsoft.AspNetCore.Mvc;

namespace LegacyApp.Controllers.Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImeiSearchController : ControllerBase
    {
        private readonly IImeiSearch _da;

        public ImeiSearchController(IImeiSearch da)
        {
            _da = da;
        }

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
