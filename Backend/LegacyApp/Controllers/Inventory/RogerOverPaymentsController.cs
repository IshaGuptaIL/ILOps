using System;
using System.Threading.Tasks;
using DAL.Common.Login;
using DAL.Inventory.PriceProtection.RogerOverPayments;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;

namespace LegacyApp.Controllers.Inventory
{
    /// <summary>
    /// Manages Rogers overpayment file imports, batch summaries, deletion by file, and Excel reporting.
    /// Tracks excess carrier disbursements, supports file cleanup, and provides standardized Excel templates.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class RogerOverPaymentsController : ControllerBase
    {
        private readonly IRogerOverPayments _da;

        public RogerOverPaymentsController(IRogerOverPayments da)
        {
            _da = da;
        }

        /// <summary>
        /// Retrieves summary statistics of all uploaded Rogers overpayment files.
        /// Displays file names, import dates, and total record counts on the summary grid.
        /// </summary>
        [HttpGet("imported-files")]
        public async Task<ApiResposne> GetImportedFilesSummary()
        {
            try
            {
                var data = await _da.GetImportedFilesSummaryAsync();
                return new ApiResposne
                {
                    Success = true,
                    Result = data,
                    Count = data.Count,
                    Message = "Imported files summary retrieved successfully."
                };
            }
            catch (Exception ex)
            {
                return new ApiResposne { Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// Imports Rogers overpayment spreadsheet data into the database.
        /// Validates dealer codes, invoice references, and credit memo numbers.
        /// </summary>
        [HttpPost("import")]
        public async Task<ApiResposne> ImportRogersOverpayments(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return new ApiResposne { Success = false, Message = "No file uploaded." };
                }

                using var stream = file.OpenReadStream();
                var success = await _da.ImportRogersOverpaymentsAsync(stream, file.FileName);
                return new ApiResposne
                {
                    Success = success,
                    Message = success ? "Rogers overpayments imported successfully." : "Import failed."
                };
            }
            catch (Exception ex)
            {
                return new ApiResposne { Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// Deletes all overpayment records originating from a specified uploaded file name.
        /// Allows operators to retract an incorrect or duplicate overpayment file upload.
        /// </summary>
        [HttpDelete("remove-file")]
        public async Task<ApiResposne> RemoveRecordsByFile([FromQuery] string filename)
        {
            try
            {
                var success = await _da.RemoveRecordsByFileAsync(filename);
                return new ApiResposne
                {
                    Success = success,
                    Message = success ? $"Records for file {filename} removed successfully." : "Removal failed."
                };
            }
            catch (Exception ex)
            {
                return new ApiResposne { Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// Exports all active Rogers overpayment records into a formatted Excel (.xlsx) file.
        /// Used by finance to reconcile carrier deductions and recovery balances.
        /// </summary>
        [HttpGet("export")]
        public async Task<IActionResult> ExportAllOverpayments()
        {
            try
            {
                var fileBytes = await _da.ExportAllOverpaymentsExcelAsync();
                var fileName = $"RogersOverpayments_All_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Generates and provides the standard Excel template file for importing Rogers overpayments.
        /// Contains required headers (Dealer, Order Number, Invoice Number, IMEI, SKU, Price, CM No).
        /// </summary>
        [HttpGet("template")]
        public IActionResult DownloadTemplate()
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage();
                var ws = package.Workbook.Worksheets.Add("Template");
                
                // Add headers
                ws.Cells[1, 1].Value = "DEALER";
                ws.Cells[1, 2].Value = "ORDER_NUMBER";
                ws.Cells[1, 3].Value = "INVOICE_NUMBER";
                ws.Cells[1, 4].Value = "IMEI";
                ws.Cells[1, 5].Value = "SKU";
                ws.Cells[1, 6].Value = "SKU_DESCRIPTION";
                ws.Cells[1, 7].Value = "NEW_PRICE";
                ws.Cells[1, 8].Value = "DEALER_COST";
                ws.Cells[1, 9].Value = "PP_AMOUNT";
                ws.Cells[1, 10].Value = "CM_No";
                ws.Cells[1, 11].Value = "CM_Date";
                
                ws.Cells[1, 1, 1, 11].Style.Font.Bold = true;
                ws.Cells.AutoFitColumns();
                
                var fileBytes = package.GetAsByteArray();
                var fileName = "RogersOverpayments_Template.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
