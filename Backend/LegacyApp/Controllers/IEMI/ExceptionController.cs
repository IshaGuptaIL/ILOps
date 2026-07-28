using System.Collections.Generic;
using System.Threading.Tasks;
using DAL.Common.Login;
using DAL.Inventory.IMEI.Exceptions;
using DAL.Models;
using Microsoft.AspNetCore.Mvc;

namespace LegacyApp.Controllers.IEMI
{
    [Route("api/Exception")]
    [ApiController]
    public class ExceptionController : ControllerBase
    {
        private readonly IExceptions _exceptions;

        public ExceptionController(IExceptions exceptions)
        {
            _exceptions = exceptions;
        }

        [HttpGet("GetExceptions")]
        public async Task<ApiResposne> GetExceptions(string poNumber = null)
        {
            var result = await _exceptions.GetExceptionsAsync(poNumber);
            return new ApiResposne
            {
                Success = true,
                Message = "Exceptions retrieved successfully",
                Result = result
            };
        }

        [HttpPost("ResolveException")]
        public async Task<ApiResposne> ResolveException([FromBody] ResolveRequest request)
        {
            var success = await _exceptions.ResolveExceptionAsync(request.Id, request.UserId);
            return new ApiResposne
            {
                Success = success,
                Message = success ? "Exception resolved" : "Failed to resolve exception"
            };
        }

        [HttpDelete("DeleteException/{id}")]
        public async Task<ApiResposne> DeleteException(int id)
        {
            var success = await _exceptions.DeleteExceptionAsync(id);
            return new ApiResposne
            {
                Success = success,
                Message = success ? "Exception deleted" : "Failed to delete exception"
            };
        }

        [HttpDelete("ClearAllExceptions")]
        public async Task<ApiResposne> ClearAllExceptions()
        {
            var success = await _exceptions.ClearAllExceptionsAsync();
            return new ApiResposne
            {
                Success = true,
                Message = "Exceptions cleared"
            };
        }

        [HttpGet("GetIMEILengthExceptions")]
        public async Task<ApiResposne> GetIMEILengthExceptions()
        {
            var result = await _exceptions.GetIMEILengthExceptionsAsync();
            return new ApiResposne
            {
                Success = true,
                Message = "IMEI Length Exceptions retrieved successfully",
                Result = result
            };
        }

        [HttpPost("SaveIMEILengthException")]
        public async Task<ApiResposne> SaveIMEILengthException([FromBody] tblIMEILengthExceptions request)
        {
            var success = await _exceptions.SaveIMEILengthExceptionAsync(request);
            return new ApiResposne
            {
                Success = success,
                Message = success ? "IMEI Length Exception saved" : "Failed to save exception"
            };
        }

        [HttpDelete("DeleteIMEILengthException/{part}")]
        public async Task<ApiResposne> DeleteIMEILengthException(string part)
        {
            var success = await _exceptions.DeleteIMEILengthExceptionAsync(part);
            return new ApiResposne
            {
                Success = success,
                Message = success ? "IMEI Length Exception deleted" : "Failed to delete exception"
            };
        }
    }

    public class ResolveRequest
    {
        public int Id { get; set; }
        public string UserId { get; set; }
    }
}
