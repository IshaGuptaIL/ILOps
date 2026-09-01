using System.Collections.Generic;
using System.Threading.Tasks;
using DAL.Common.Login;
using DAL.Inventory.IMEI.Exceptions;
using DAL.Models;
using Microsoft.AspNetCore.Mvc;

namespace LegacyApp.Controllers.IEMI
{
    /// <summary>
    /// Manages IMEI Length Exceptions and System Error logs.
    /// Handles exception configuration (part length, alpha allow) and resolution of IMEI processing errors.
    /// </summary>
    [Route("api/Exception")]
    [ApiController]
    public class ExceptionController : ControllerBase
    {
        private readonly IExceptions _exceptions;

        public ExceptionController(IExceptions exceptions)
        {
            _exceptions = exceptions;
        }

        /// <summary>
        /// Retrieves un-resolved or PO-specific system errors from tblErrors.
        /// Used by the IMEI Exception dashboard to display validation and import errors.
        /// </summary>
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

        /// <summary>
        /// Marks a specific logged error as resolved by the current user.
        /// Used when an operator addresses and clears an IMEI validation error.
        /// </summary>
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

        /// <summary>
        /// Permanently deletes a specific error log record by its ID.
        /// Used to remove obsolete error records from tblErrors.
        /// </summary>
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

        /// <summary>
        /// Clears all system error records from the database.
        /// Used for bulk cleanup of resolved or temporary validation error logs.
        /// </summary>
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

        /// <summary>
        /// Retrieves all configured IMEI length exceptions by part number.
        /// Allows specific hardware parts to bypass standard 15-digit numeric rules.
        /// </summary>
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

        /// <summary>
        /// Saves or updates an IMEI length exception definition for a part.
        /// Sets custom length and alphanumeric validation flags for the specified part.
        /// </summary>
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

        /// <summary>
        /// Deletes an IMEI length exception entry for a specific part.
        /// Restores default standard IMEI validation behavior for that part.
        /// </summary>
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
