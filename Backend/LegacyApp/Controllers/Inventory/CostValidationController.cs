using DAL.Common.Login;
using DAL.Inventory.CostValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace LegacyApp.Controllers.Inventory
{
    [Route("api/cost-validation")]
    [ApiController]
    public class CostValidationController : ControllerBase
    {
        private readonly ICostValidation _costValidation;

        public CostValidationController(ICostValidation costValidation)
        {
            _costValidation = costValidation;
        }

        [HttpGet("HpcLatest")]
        public async Task<ApiResposne> GetHpcLatest()
        {
            var response = new ApiResposne();

            try
            {
                var data = await _costValidation.GetHpcLatestAsync();
                response.Success = true;
                response.Message = "HPC Latest retrieved";
                response.Result = data;
                response.Count = data.Count;
                response.StatusCode = 200;
            }
            catch (System.Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }

            return response;
        }

        [HttpGet("HpcDiscrepancies")]
        public async Task<ApiResposne> GetHpcDiscrepancies()
        {
            var response = new ApiResposne();

            try
            {
                var data = await _costValidation.GetHpcDiscrepanciesAsync();
                response.Success = true;
                response.Message = "HPC Discrepancies retrieved";
                response.Result = data;
                response.Count = data.Count;
                response.StatusCode = 200;
            }
            catch (System.Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }

            return response;
        }

        [HttpGet("RDHardwareVsSpire")]
        public async Task<ApiResposne> GetRDHardwareVsSpire()
        {
            var response = new ApiResposne();

            try
            {
                var data = await _costValidation.GetRDHardwareVsSpireAsync();
                response.Success = true;
                response.Message = "RD Hardware vs Spire retrieved";
                response.Result = data;
                response.Count = data.Count;
                response.StatusCode = 200;
            }
            catch (System.Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }

            return response;
        }

        [HttpGet("CostVarianceCurrentVsAvg")]
        public async Task<ApiResposne> GetCostVarianceCurrentVsAvg()
        {
            var response = new ApiResposne();

            try
            {
                var data = await _costValidation.GetCostVarianceCurrentVsAvgAsync();
                response.Success = true;
                response.Message = "Cost Variance Current vs Avg retrieved";
                response.Result = data;
                response.Count = data.Count;
                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }

            return response;
        }
        [HttpGet("CostVarianceAcrossWarehouses")]
        public async Task<ApiResposne> GetCostVarianceAcrossWarehouses()
        {
            var response = new ApiResposne();

            try
            {
                var data = await _costValidation.GetCostVarianceAcrossWarehousesAsync();
                response.Success = true;
                response.Message = "Cost Variance Across Warehouses retrieved";
                response.Result = data;
                response.Count = data.Count;
                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }

            return response;
        }


        [HttpPost("upload-hpc")]
        [AllowAnonymous]
        [Consumes("multipart/form-data")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ApiResposne> LoadHPC( IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                return new ApiResposne
                {
                    Success = false,
                    Message = "Please select a valid Excel file."
                };
            }

            using var stream = excelFile.OpenReadStream();
            return await _costValidation.LoadHPC(stream);
        }


    }
}

