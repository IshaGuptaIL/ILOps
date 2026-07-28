using DAL.Sales.CustomerSales;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LegacyApp.Controllers.Sales
{
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

        [HttpGet("GetCustomerGroups")]
        public async Task<ActionResult<List<CustomerGroupBO>>> GetCustomerGroups()
        {
            var result = await _customerSalesDA.GetCustomerGroupsAsync();
            return Ok(result);
        }

        [HttpGet("GetCustomersInGroup/{groupName}")]
        public async Task<ActionResult<List<BVCustomerBO>>> GetCustomersInGroup(string groupName)
        {
            var result = await _customerSalesDA.GetCustomersInGroupAsync(groupName);
            return Ok(result);
        }

        [HttpPost("GenerateData")]
        public async Task<ActionResult<bool>> GenerateData([FromBody] CustomerSalesRequest request)
        {
            if (request == null) return BadRequest("Invalid request.");
            int userId = GetUserId();
            var success = await _customerSalesDA.GenerateCustomerSalesDataAsync(request, userId);
            return Ok(success);
        }

        [HttpGet("GetGeneratedData/{groupName}")]
        public async Task<ActionResult<List<CustomerSalesRow>>> GetGeneratedData(string groupName)
        {
            var result = await _customerSalesDA.GetGeneratedDataAsync(groupName);
            return Ok(result);
        }

        [HttpPost("ExportExcel")]
        public async Task<IActionResult> ExportExcel([FromBody] CustomerSalesRequest request)
        {
            if (request == null) return BadRequest("Invalid request.");
            int userId = GetUserId();
            var fileBytes = await _customerSalesDA.ExportToExcelAsync(request, userId);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"CustomerSales-{request.CustGroup}-{DateTime.Now:yyyyMMdd}.xlsx");
        }

        [HttpPost("ExportCsv")]
        public async Task<IActionResult> ExportCsv([FromBody] CustomerSalesRequest request)
        {
            if (request == null) return BadRequest("Invalid request.");
            int userId = GetUserId();
            var fileBytes = await _customerSalesDA.ExportToCsvAsync(request, userId);
            return File(fileBytes, "text/csv", $"CustomerSales-{request.CustGroup}-{DateTime.Now:yyyyMMdd}.csv");
        }

        [HttpPost("ExportPerCustomer")]
        public async Task<IActionResult> ExportPerCustomer([FromBody] CustomerSalesRequest request)
        {
            if (request == null) return BadRequest("Invalid request.");
            int userId = GetUserId();
            var fileBytes = await _customerSalesDA.ExportPerCustomerAsync(request, userId);
            return File(fileBytes, "application/zip", $"PerCustomer-{request.CustGroup}-{DateTime.Now:yyyyMMdd}.zip");
        }

        [HttpPost("GenerateByMSD")]
        public async Task<ActionResult<bool>> GenerateByMSD([FromBody] CustomerSalesRequest request)
        {
            if (request == null) return BadRequest("Invalid request.");
            int userId = GetUserId();
            var success = await _customerSalesDA.GenerateListByMSDAsync(request, userId);
            return Ok(success);
        }

        [HttpPost("GenerateByTerritory")]
        public async Task<ActionResult<bool>> GenerateByTerritory([FromBody] CustomerSalesRequest request)
        {
            if (request == null) return BadRequest("Invalid request.");
            int userId = GetUserId();
            var success = await _customerSalesDA.GenerateListByTerritoryAsync(request, userId);
            return Ok(success);
        }

        [HttpPost("AddFDDealerGroup")]
        public async Task<ActionResult<bool>> AddFDDealerGroup()
        {
            int userId = GetUserId();
            var success = await _customerSalesDA.AddFDDealerGroupAsync(userId);
            return Ok(success);
        }

        [HttpPost("CreateGroup")]
        public async Task<ActionResult<bool>> CreateGroup([FromBody] CreateGroupRequest request)
        {
            if (request == null) return BadRequest("Invalid request.");
            int userId = GetUserId();
            var success = await _customerSalesDA.CreateCustomerGroupAsync(request, userId);
            return Ok(success);
        }

        [HttpDelete("DeleteGroup/{groupName}")]
        public async Task<ActionResult<bool>> DeleteGroup(string groupName)
        {
            int userId = GetUserId();
            var success = await _customerSalesDA.DeleteCustomerGroupAsync(groupName, userId);
            return Ok(success);
        }

        [HttpGet("GetFields/{groupName}")]
        public async Task<ActionResult<List<CustomerFieldBO>>> GetFields(string groupName)
        {
            var result = await _customerSalesDA.GetCustomerFieldsAsync(groupName);
            return Ok(result);
        }

        [HttpPost("UpdateFields/{groupName}")]
        public async Task<ActionResult<bool>> UpdateFields(string groupName, [FromBody] List<CustomerFieldBO> fields)
        {
            var success = await _customerSalesDA.UpdateCustomerFieldsAsync(groupName, fields);
            return Ok(success);
        }

        [HttpPost("ExportSunLife")]
        public async Task<IActionResult> ExportSunLife([FromBody] CustomerSalesRequest request)
        {
            if (request == null) return BadRequest("Invalid request.");
            int userId = GetUserId();
            var fileBytes = await _customerSalesDA.GenerateSunLifeReportAsync(request, userId);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"SunLife-{request.CustGroup}.xlsx");
        }

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

        [HttpPost("UpdateGeneratedData")]
        public async Task<ActionResult<bool>> UpdateGeneratedData([FromBody] List<CustomerSalesRow> data)
        {
            if (data == null) return BadRequest("Invalid data.");
            int userId = GetUserId();
            var success = await _customerSalesDA.UpdateGeneratedDataAsync(data, userId);
            return Ok(success);
        }

        [HttpPost("AddCustomerToGroup/{groupCode}")]
        public async Task<ActionResult<bool>> AddCustomerToGroup(string groupCode, [FromBody] BVCustomerBO customer)
        {
            int userId = GetUserId();
            var success = await _customerSalesDA.AddCustomerToGroupAsync(groupCode, customer, userId);
            return Ok(success);
        }

        [HttpPut("UpdateCustomerInGroup/{groupCode}/{oldCustNo}")]
        public async Task<ActionResult<bool>> UpdateCustomerInGroup(string groupCode, string oldCustNo, [FromBody] BVCustomerBO customer)
        {
            int userId = GetUserId();
            var success = await _customerSalesDA.UpdateCustomerInGroupAsync(groupCode, oldCustNo, customer, userId);
            return Ok(success);
        }

        [HttpDelete("RemoveCustomerFromGroup/{groupCode}/{custNo}")]
        public async Task<ActionResult<bool>> RemoveCustomerFromGroup(string groupCode, string custNo)
        {
            int userId = GetUserId();
            var success = await _customerSalesDA.RemoveCustomerFromGroupAsync(groupCode, custNo, userId);
            return Ok(success);
        }
    }
}
