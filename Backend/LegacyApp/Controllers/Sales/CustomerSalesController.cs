using DAL.Sales.CustomerSales;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LegacyApp.Controllers.Sales
{
    /// <summary>
    /// Manages customer group sales reporting, group definition configuration, and multi-format exports.
    /// Provides data generation, customizable field mapping, SunLife reports, and dealer group management.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerSalesController : ControllerBase
    {
        private readonly ICustomerSales _customerSalesDA;

        public CustomerSalesController(ICustomerSales customerSalesDA)
        {
            _customerSalesDA = customerSalesDA;
        }

        private int GetUserId()
        {
            if (Request.Cookies.TryGetValue("userId", out string? userIdStr) && int.TryParse(userIdStr, out int userId))
            {
                return userId;
            }
            return 1; // Fallback for testing
        }

        /// <summary>
        /// Retrieves all configured customer reporting groups.
        /// Populates the customer group selection dropdown.
        /// </summary>
        [HttpGet("GetCustomerGroups")]
        public async Task<ActionResult<List<CustomerGroupBO>>> GetCustomerGroups()
        {
            var result = await _customerSalesDA.GetCustomerGroupsAsync();
            return Ok(result);
        }

        /// <summary>
        /// Retrieves customers belonging to a specified reporting group.
        /// Displays customer membership list for the selected group.
        /// </summary>
        [HttpGet("GetCustomersInGroup/{groupName}")]
        public async Task<ActionResult<List<BVCustomerBO>>> GetCustomersInGroup(string groupName)
        {
            var result = await _customerSalesDA.GetCustomersInGroupAsync(groupName);
            return Ok(result);
        }

        /// <summary>
        /// Generates sales data for customers in a specified group within a date range.
        /// Aggregates sales volume, costs, and profit figures into temporary reporting tables.
        /// </summary>
        [HttpPost("GenerateData")]
        public async Task<ActionResult<bool>> GenerateData([FromBody] CustomerSalesRequest request)
        {
            if (request == null) return BadRequest("Invalid request.");
            int userId = GetUserId();
            var success = await _customerSalesDA.GenerateCustomerSalesDataAsync(request, userId);
            return Ok(success);
        }

        /// <summary>
        /// Retrieves generated sales reporting rows for a customer group.
        /// Displays the results table after sales data generation.
        /// </summary>
        [HttpGet("GetGeneratedData/{groupName}")]
        public async Task<ActionResult<List<CustomerSalesRow>>> GetGeneratedData(string groupName)
        {
            var result = await _customerSalesDA.GetGeneratedDataAsync(groupName);
            return Ok(result);
        }

        /// <summary>
        /// Exports customer group sales data to a formatted Excel (.xlsx) spreadsheet.
        /// Provides downloadable Excel file of generated sales figures.
        /// </summary>
        [HttpPost("ExportExcel")]
        public async Task<IActionResult> ExportExcel([FromBody] CustomerSalesRequest request)
        {
            if (request == null) return BadRequest("Invalid request.");
            int userId = GetUserId();
            var fileBytes = await _customerSalesDA.ExportToExcelAsync(request, userId);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"CustomerSales-{request.CustGroup}-{DateTime.Now:yyyyMMdd}.xlsx");
        }

        /// <summary>
        /// Exports customer group sales records to a CSV text file.
        /// Facilitates raw data integration and external reporting workflows.
        /// </summary>
        [HttpPost("ExportCsv")]
        public async Task<IActionResult> ExportCsv([FromBody] CustomerSalesRequest request)
        {
            if (request == null) return BadRequest("Invalid request.");
            int userId = GetUserId();
            var fileBytes = await _customerSalesDA.ExportToCsvAsync(request, userId);
            return File(fileBytes, "text/csv", $"CustomerSales-{request.CustGroup}-{DateTime.Now:yyyyMMdd}.csv");
        }

        /// <summary>
        /// Generates individual sales files per customer and bundles them into a ZIP archive.
        /// Used for distributing separated sales statements to multiple client accounts.
        /// </summary>
        [HttpPost("ExportPerCustomer")]
        public async Task<IActionResult> ExportPerCustomer([FromBody] CustomerSalesRequest request)
        {
            if (request == null) return BadRequest("Invalid request.");
            int userId = GetUserId();
            var fileBytes = await _customerSalesDA.ExportPerCustomerAsync(request, userId);
            return File(fileBytes, "application/zip", $"PerCustomer-{request.CustGroup}-{DateTime.Now:yyyyMMdd}.zip");
        }

        /// <summary>
        /// Generates customer sales lists filtered by Master Sales Distributor (MSD) classification.
        /// Aggregates sales performance for designated distributor networks.
        /// </summary>
        [HttpPost("GenerateByMSD")]
        public async Task<ActionResult<bool>> GenerateByMSD([FromBody] CustomerSalesRequest request)
        {
            if (request == null) return BadRequest("Invalid request.");
            int userId = GetUserId();
            var success = await _customerSalesDA.GenerateListByMSDAsync(request, userId);
            return Ok(success);
        }

        /// <summary>
        /// Generates customer sales data filtered by geographic territory assignments.
        /// Produces territory-focused revenue summaries for sales representatives.
        /// </summary>
        [HttpPost("GenerateByTerritory")]
        public async Task<ActionResult<bool>> GenerateByTerritory([FromBody] CustomerSalesRequest request)
        {
            if (request == null) return BadRequest("Invalid request.");
            int userId = GetUserId();
            var success = await _customerSalesDA.GenerateListByTerritoryAsync(request, userId);
            return Ok(success);
        }

        /// <summary>
        /// Automatically synchronizes and populates the Factory Direct (FD) Dealer reporting group.
        /// Discovers and assigns matching dealer customer codes into the group.
        /// </summary>
        [HttpPost("AddFDDealerGroup")]
        public async Task<ActionResult<bool>> AddFDDealerGroup()
        {
            int userId = GetUserId();
            var success = await _customerSalesDA.AddFDDealerGroupAsync(userId);
            return Ok(success);
        }

        /// <summary>
        /// Creates a new customer reporting group with custom name and initial settings.
        /// Allows operators to establish new corporate reporting clusters.
        /// </summary>
        [HttpPost("CreateGroup")]
        public async Task<ActionResult<bool>> CreateGroup([FromBody] CreateGroupRequest request)
        {
            if (request == null) return BadRequest("Invalid request.");
            int userId = GetUserId();
            var success = await _customerSalesDA.CreateCustomerGroupAsync(request, userId);
            return Ok(success);
        }

        /// <summary>
        /// Deletes a customer reporting group definition and its member mappings.
        /// Removes the group from available reporting selection dropdowns.
        /// </summary>
        [HttpDelete("DeleteGroup/{groupName}")]
        public async Task<ActionResult<bool>> DeleteGroup(string groupName)
        {
            int userId = GetUserId();
            var success = await _customerSalesDA.DeleteCustomerGroupAsync(groupName, userId);
            return Ok(success);
        }

        /// <summary>
        /// Retrieves custom export field definitions configured for a customer group.
        /// Used by the column customization editor to control exported field order.
        /// </summary>
        [HttpGet("GetFields/{groupName}")]
        public async Task<ActionResult<List<CustomerFieldBO>>> GetFields(string groupName)
        {
            var result = await _customerSalesDA.GetCustomerFieldsAsync(groupName);
            return Ok(result);
        }

        /// <summary>
        /// Updates the custom export field mapping and column layout for a group.
        /// Saves custom header labels and field visibility selections.
        /// </summary>
        [HttpPost("UpdateFields/{groupName}")]
        public async Task<ActionResult<bool>> UpdateFields(string groupName, [FromBody] List<CustomerFieldBO> fields)
        {
            var success = await _customerSalesDA.UpdateCustomerFieldsAsync(groupName, fields);
            return Ok(success);
        }

        /// <summary>
        /// Generates and exports the specialized SunLife financial sales report in Excel format.
        /// Formats data according to SunLife insurance partner specifications.
        /// </summary>
        [HttpPost("ExportSunLife")]
        public async Task<IActionResult> ExportSunLife([FromBody] CustomerSalesRequest request)
        {
            if (request == null) return BadRequest("Invalid request.");
            int userId = GetUserId();
            var fileBytes = await _customerSalesDA.GenerateSunLifeReportAsync(request, userId);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"SunLife-{request.CustGroup}.xlsx");
        }

        /// <summary>
        /// Generates and exports the Split Payment sales report in either CSV or Excel format.
        /// Breaks down multi-tender sales and split invoice remittances.
        /// </summary>
        [HttpPost("ExportSplitPayment/{format}")]
        public async Task<IActionResult> ExportSplitPayment(string format, [FromBody] CustomerSalesRequest request)
        {
            if (request == null) return BadRequest("Invalid request.");
            int userId = GetUserId();
            var fileBytes = await _customerSalesDA.GenerateSplitPaymentReportAsync(request, format, userId);
            string contentType = format == "CSV" ? "text/csv" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            string fileName = $"SplitPayment-{request.CustGroup}.{(format == "CSV" ? "csv" : "xlsx")}";
            return File(fileBytes, contentType, fileName);
        }

        /// <summary>
        /// Saves inline modifications to generated customer sales records.
        /// Allows operators to correct figures before exporting final statements.
        /// </summary>
        [HttpPost("UpdateGeneratedData")]
        public async Task<ActionResult<bool>> UpdateGeneratedData([FromBody] List<CustomerSalesRow> data)
        {
            if (data == null) return BadRequest("Invalid data.");
            int userId = GetUserId();
            var success = await _customerSalesDA.UpdateGeneratedDataAsync(data, userId);
            return Ok(success);
        }

        /// <summary>
        /// Associates a single customer account code with a reporting group.
        /// Adds new members to an existing reporting group.
        /// </summary>
        [HttpPost("AddCustomerToGroup/{groupCode}")]
        public async Task<ActionResult<bool>> AddCustomerToGroup(string groupCode, [FromBody] BVCustomerBO customer)
        {
            int userId = GetUserId();
            var success = await _customerSalesDA.AddCustomerToGroupAsync(groupCode, customer, userId);
            return Ok(success);
        }

        /// <summary>
        /// Updates a customer's code or details within a reporting group.
        /// Modifies customer membership associations.
        /// </summary>
        [HttpPut("UpdateCustomerInGroup/{groupCode}/{oldCustNo}")]
        public async Task<ActionResult<bool>> UpdateCustomerInGroup(string groupCode, string oldCustNo, [FromBody] BVCustomerBO customer)
        {
            int userId = GetUserId();
            var success = await _customerSalesDA.UpdateCustomerInGroupAsync(groupCode, oldCustNo, customer, userId);
            return Ok(success);
        }

        /// <summary>
        /// Removes a customer account from a specified reporting group.
        /// Excludes the customer from future group sales generation runs.
        /// </summary>
        [HttpDelete("RemoveCustomerFromGroup/{groupCode}/{custNo}")]
        public async Task<ActionResult<bool>> RemoveCustomerFromGroup(string groupCode, string custNo)
        {
            int userId = GetUserId();
            var success = await _customerSalesDA.RemoveCustomerFromGroupAsync(groupCode, custNo, userId);
            return Ok(success);
        }
    }
}
