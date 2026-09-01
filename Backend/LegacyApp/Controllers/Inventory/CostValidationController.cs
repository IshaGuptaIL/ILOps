using DAL.Common.Login;
using DAL.Inventory.CostValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace LegacyApp.Controllers.Inventory
{
    /// <summary>
    /// Audits inventory costs against Hardware Price Catalog (HPC) benchmarks and Spire ERP cost layers.
    /// Detects purchase price variances, cross-warehouse cost discrepancies, and handles HPC master catalog uploads.
    /// </summary>
    [Route("api/cost-validation")]
    [ApiController]
    public class CostValidationController : ControllerBase
    {
        private readonly ICostValidation _costValidation;

        public CostValidationController(ICostValidation costValidation)
        {
            _costValidation = costValidation;
        }

        /// <summary>
        /// Retrieves the most recent Hardware Price Catalog (HPC) benchmark rates.
        /// Displays latest vendor pricing baselines in the cost validation module.
        /// </summary>
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

        /// <summary>
        /// Identifies pricing discrepancies between HPC benchmark costs and current Spire purchase costs.
        /// Highlights potential vendor overcharges or understated inventory values.
        /// </summary>
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

        /// <summary>
        /// Compares Rogers Distribution hardware pricing against Spire ERP item costs.
        /// Audits agreement pricing compliance across carrier inventory lines.
        /// </summary>
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

        /// <summary>
        /// Analyzes cost variance between current replacement cost and historical average inventory cost.
        /// Detects margin compression or sudden price inflation on inventory assets.
        /// </summary>
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

        /// <summary>
        /// Identifies unit cost discrepancies for identical SKU items across different physical warehouses.
        /// Used for inventory transfer revaluations and inter-branch cost equalization.
        /// </summary>
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

        /// <summary>
        /// Imports an updated Hardware Price Catalog (HPC) Excel spreadsheet into the master benchmark store.
        /// Replaces or appends new price lists for subsequent cost audit runs.
        /// </summary>
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

