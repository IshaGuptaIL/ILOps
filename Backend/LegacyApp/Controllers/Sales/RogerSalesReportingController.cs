using ClosedXML.Excel;
using LegacyApp.DAL.Sales.RogersSalesReporting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LegacyApp.Controllers.Sales
{
    /// <summary>
    /// Executes Rogers sales reporting queries, outputs sales reports to Excel, and updates sales activation records.
    /// Supports dynamic action routing (dealer, territory, activation types), inline line adjustments, and file exports.
    /// </summary>
    [Route("api/sales/rogerssalesreporting")]
    [ApiController]
    public class RogerSalesReportingController : ControllerBase
    {
        private readonly IRogerSalesReportingDAL _dal;

        public RogerSalesReportingController(IRogerSalesReportingDAL dal)
        {
            _dal = dal;
        }

        private string GetUserFromCookie()
        {
            // Extract the user from cookies as requested
            if (Request.Cookies.TryGetValue("UserLogin", out var user))
            {
                return user;
            }
            return "System";
        }

        /// <summary>
        /// Executes a dynamic sales reporting query action and returns tabular JSON data with department revenue columns.
        /// Used by the Rogers Sales Reporting grid for live data viewing.
        /// </summary>
        [HttpGet("{endpoint}/view")]
        public async Task<IActionResult> ExecuteViewAction(
            string endpoint,
            [FromQuery] string startDate,
            [FromQuery] string endDate,
            [FromQuery] string criteria,
            [FromQuery] string territory = "")
        {
            try
            {
                string user = GetUserFromCookie();

                DataTable dt = await _dal.ExecuteActionAsync(endpoint, "view", startDate, endDate, criteria, territory, user);

                // Convert DataTable to JSON with department columns
                var list = new List<Dictionary<string, object>>();
                foreach (DataRow row in dt.Rows)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (DataColumn col in dt.Columns)
                    {
                        dict[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
                    }

                    // Ensure department columns exist with default values
                    EnsureDepartmentColumns(dict);

                    list.Add(dict);
                }
                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Executes a specified sales query and generates a downloadable Excel (.xlsx) workbook.
        /// Produces customized reporting spreadsheets directly from database queries.
        /// </summary>
        [HttpGet("{endpoint}/output")]
        public async Task<IActionResult> ExecuteOutputAction(
            string endpoint,
            [FromQuery] string startDate,
            [FromQuery] string endDate,
            [FromQuery] string criteria,
            [FromQuery] string territory = "")
        {
            try
            {
                string user = GetUserFromCookie();

                DataTable dt = await _dal.ExecuteActionAsync(endpoint, "output", startDate, endDate, criteria, territory, user);

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add(dt, "Report");
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();
                        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{endpoint}_{startDate}.xlsx");
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Updates line-item attributes on a Rogers sales activation record (e.g. fees, suppress flag, adjustments).
        /// Modifies commission calculations and invoice details in the sales ledger.
        /// </summary>
        [HttpPut("update")]
        public async Task<IActionResult> UpdateSalesActivationRow([FromBody] System.Text.Json.JsonElement jsonRow)
        {
            try
            {
                // Manually map to avoid 400 Bad Request from strict model binding
                var row = new SalesActivationUpdateModel();

                if (jsonRow.TryGetProperty("Invoice10", out var inv10)) row.Invoice10 = inv10.ValueKind == System.Text.Json.JsonValueKind.Null ? null : inv10.GetString();
                if (jsonRow.TryGetProperty("TransactionNo", out var transNo)) row.TransactionNo = transNo.ValueKind == System.Text.Json.JsonValueKind.Null ? null : transNo.GetString();
                if (jsonRow.TryGetProperty("BVInvoiceLine", out var bvLine) && bvLine.ValueKind != System.Text.Json.JsonValueKind.Null) row.BVInvoiceLine = bvLine.TryGetInt32(out int val) ? val : (int?)null;

                if (jsonRow.TryGetProperty("CustTerritory", out var custTerr)) row.CustTerritory = custTerr.ValueKind == System.Text.Json.JsonValueKind.Null ? null : custTerr.GetString();
                if (jsonRow.TryGetProperty("AdjustmentType", out var adjType)) row.AdjustmentType = adjType.ValueKind == System.Text.Json.JsonValueKind.Null ? null : adjType.GetString();

                if (jsonRow.TryGetProperty("Fee", out var fee) && fee.ValueKind != System.Text.Json.JsonValueKind.Null)
                {
                    row.Fee = fee.TryGetDecimal(out decimal fVal) ? fVal : (decimal?)null;
                }

                if (jsonRow.TryGetProperty("Supress", out var supress) && supress.ValueKind != System.Text.Json.JsonValueKind.Null)
                {
                    row.Supress = supress.ValueKind == System.Text.Json.JsonValueKind.True || supress.ValueKind == System.Text.Json.JsonValueKind.Number && supress.GetInt32() != 0;
                }

                string user = GetUserFromCookie();
                bool success = await _dal.UpdateSalesActivationRowAsync(row, user);
                if (success)
                {
                    return Ok(new { message = "Row updated successfully." });
                }
                return NotFound(new { message = "Row not found or no changes made." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Converts client-side filtered sales grid rows into a formatted Excel export file.
        /// Retains UI column arrangements and applied filters in the resulting spreadsheet.
        /// </summary>
        [HttpPost("export-filtered")]
        public async Task<IActionResult> ExportFilteredData([FromBody] FilteredDataRequest request)
        {
            try
            {
                // Convert the filtered data to Excel
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add(request.Title);

                    if (request.Data != null && request.Data.Any())
                    {
                        // Add headers
                        var headers = new[]
                        {
                            "Invoice10", "TransactionNo", "InvoiceDate", "OrderDate", "CustName", "CustTerritory",
                            "UserName", "CellPhoneNo", "VoicePlan", "DataPlan", "WebOrderID", "Type",
                            "AdjustmentType", "Supress", "Fee", "FeeCount", "TopUpOwing",
                            "Co-Op Advertising - HO", "Miscellaneous GBM NDS Inc/Exp", "Other Revenue - HO",
                            "Other Revenue - CO", "Receivable - Upfront Edge - RV", "SALES - Accessories - CO",
                            "SALES - Hardware - CO", "Staging and Deployment", "Unallocated Sales", "Web Hosting",
                            "PartNumber", "ProductCode", "IMEIESN", "CostPrice", "SellPrice", "InvoiceNet", "InvoiceTotal"
                        };

                        for (int i = 0; i < headers.Length; i++)
                        {
                            worksheet.Cell(1, i + 1).Value = headers[i];
                        }

                        // Add data rows
                        for (int row = 0; row < request.Data.Count; row++)
                        {
                            var item = request.Data[row];
                            worksheet.Cell(row + 2, 1).Value = (XLCellValue)item.GetValueOrDefault("Invoice10", "");
                            worksheet.Cell(row + 2, 2).Value = (XLCellValue)item.GetValueOrDefault("TransactionNo", "");
                            worksheet.Cell(row + 2, 3).Value = (XLCellValue)item.GetValueOrDefault("InvoiceDate", "");
                            worksheet.Cell(row + 2, 4).Value = (XLCellValue)item.GetValueOrDefault("OrderDate", "");
                            worksheet.Cell(row + 2, 5).Value = (XLCellValue)item.GetValueOrDefault("CustName", "");
                            worksheet.Cell(row + 2, 6).Value = (XLCellValue)item.GetValueOrDefault("CustTerritory", "");
                            worksheet.Cell(row + 2, 7).Value = (XLCellValue)item.GetValueOrDefault("UserName", "");
                            worksheet.Cell(row + 2, 8).Value = (XLCellValue)item.GetValueOrDefault("CellPhoneNo", "");
                            worksheet.Cell(row + 2, 9).Value = (XLCellValue)item.GetValueOrDefault("VoicePlan", "");
                            worksheet.Cell(row + 2, 10).Value = (XLCellValue)item.GetValueOrDefault("DataPlan", "");
                            worksheet.Cell(row + 2, 11).Value = (XLCellValue)item.GetValueOrDefault("WebOrderID", "");
                            worksheet.Cell(row + 2, 12).Value = (XLCellValue)item.GetValueOrDefault("Type", "");
                            worksheet.Cell(row + 2, 13).Value = (XLCellValue)item.GetValueOrDefault("AdjustmentType", "");
                            worksheet.Cell(row + 2, 14).Value = (XLCellValue)item.GetValueOrDefault("Supress", false);
                            worksheet.Cell(row + 2, 15).Value = Convert.ToDecimal(item.GetValueOrDefault("Fee", 0));
                            worksheet.Cell(row + 2, 16).Value = Convert.ToInt32(item.GetValueOrDefault("FeeCount", 0));
                            worksheet.Cell(row + 2, 17).Value = Convert.ToDecimal(item.GetValueOrDefault("TopUpOwing", 0));

                            // Department columns
                            worksheet.Cell(row + 2, 18).Value = Convert.ToDecimal(item.GetValueOrDefault("CoOpAdvertisingHO", 0));
                            worksheet.Cell(row + 2, 19).Value = Convert.ToDecimal(item.GetValueOrDefault("MiscellaneousGBMNDSIncExp", 0));
                            worksheet.Cell(row + 2, 20).Value = Convert.ToDecimal(item.GetValueOrDefault("OtherRevenueHO", 0));
                            worksheet.Cell(row + 2, 21).Value = Convert.ToDecimal(item.GetValueOrDefault("OtherRevenueCO", 0));
                            worksheet.Cell(row + 2, 22).Value = Convert.ToDecimal(item.GetValueOrDefault("ReceivableUpfrontEdgeRV", 0));
                            worksheet.Cell(row + 2, 23).Value = Convert.ToDecimal(item.GetValueOrDefault("SalesAccessoriesCO", 0));
                            worksheet.Cell(row + 2, 24).Value = Convert.ToDecimal(item.GetValueOrDefault("SalesHardwareCO", 0));
                            worksheet.Cell(row + 2, 25).Value = Convert.ToDecimal(item.GetValueOrDefault("StagingAndDeployment", 0));
                            worksheet.Cell(row + 2, 26).Value = Convert.ToDecimal(item.GetValueOrDefault("UnallocatedSales", 0));
                            worksheet.Cell(row + 2, 27).Value = Convert.ToDecimal(item.GetValueOrDefault("WebHosting", 0));

                            worksheet.Cell(row + 2, 28).Value = (XLCellValue)item.GetValueOrDefault("PartNumber", "");
                            worksheet.Cell(row + 2, 29).Value = (XLCellValue)item.GetValueOrDefault("ProductCode", "");
                            worksheet.Cell(row + 2, 30).Value = (XLCellValue)item.GetValueOrDefault("IMEIESN", "");
                            worksheet.Cell(row + 2, 31).Value = Convert.ToDecimal(item.GetValueOrDefault("CostPrice", 0));
                            worksheet.Cell(row + 2, 32).Value = Convert.ToDecimal(item.GetValueOrDefault("SellPrice", 0));
                            worksheet.Cell(row + 2, 33).Value = Convert.ToDecimal(item.GetValueOrDefault("InvoiceNet", 0));
                            worksheet.Cell(row + 2, 34).Value = Convert.ToDecimal(item.GetValueOrDefault("InvoiceTotal", 0));
                        }

                        worksheet.Columns().AdjustToContents();
                    }

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();
                        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{request.Title}_Filtered.xlsx");
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Unified legacy dispatcher endpoint supporting backwards-compatible view and export requests.
        /// Routes report actions based on endpoint parameter.
        /// </summary>
        [HttpGet("{endpoint}")]
        public async Task<IActionResult> ExecuteAction(
            string endpoint,
            [FromQuery] string startDate,
            [FromQuery] string endDate,
            [FromQuery] string criteria,
            [FromQuery] string territory = "")
        {
            try
            {
                string user = GetUserFromCookie();

                DataTable dt = await _dal.ExecuteActionAsync(endpoint, "default", startDate, endDate, criteria, territory, user);

                // If this is an OUTPUT command (frontend expects a Blob for Excel download)
                if (endpoint.Contains("output") || endpoint == "dump-all")
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add(dt, "Report");
                        using (var stream = new MemoryStream())
                        {
                            workbook.SaveAs(stream);
                            var content = stream.ToArray();
                            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{endpoint}.xlsx");
                        }
                    }
                }
                else
                {
                    // Convert DataTable to JSON for view commands
                    var list = new List<Dictionary<string, object>>();
                    foreach (DataRow row in dt.Rows)
                    {
                        var dict = new Dictionary<string, object>();
                        foreach (DataColumn col in dt.Columns)
                        {
                            dict[col.ColumnName] = row[col];
                        }
                        EnsureDepartmentColumns(dict);
                        list.Add(dict);
                    }
                    return Ok(list);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        private void EnsureDepartmentColumns(Dictionary<string, object> dict)
        {
            var departmentColumns = new[]
            {
                "CoOpAdvertisingHO", "MiscellaneousGBMNDSIncExp", "OtherRevenueHO", "OtherRevenueCO",
                "ReceivableUpfrontEdgeRV", "SalesAccessoriesCO", "SalesHardwareCO", "StagingAndDeployment",
                "UnallocatedSales", "WebHosting"
            };

            foreach (var col in departmentColumns)
            {
                if (!dict.ContainsKey(col))
                {
                    dict[col] = 0.0m;
                }
            }
        }
    }

    public class FilteredDataRequest
    {
        public List<Dictionary<string, object>> Data { get; set; }
        public string Title { get; set; }
    }
}
