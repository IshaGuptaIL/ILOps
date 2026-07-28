using Azure.Core;
using DAL.Models;
using DAL.Sales.ARCollections;
using Microsoft.AspNetCore.Mvc;


using DAL.Sales.ARCollections;
using DAL.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LegacyApp.Controllers.Sales
{
    [Route("api/[controller]")]
    [ApiController]
    public class ARCollectionsController : ControllerBase
    {
        private readonly IARCollectionsDA _arCollectionsDA;

        public ARCollectionsController(IARCollectionsDA arCollectionsDA)
        {
            _arCollectionsDA = arCollectionsDA;
        }

        private int GetUserId()
        {
            if (Request.Cookies.TryGetValue("userId", out string? userIdStr) && int.TryParse(userIdStr, out int userId))
            {
                return userId;
            }
            return 1; // Fallback for testing
        }

        private string GetUserInitials()
        {
            if (Request.Cookies.TryGetValue("userInitials", out string? initials) && !string.IsNullOrEmpty(initials))
            {
                return initials;
            }
            return "SYS"; // Fallback initials
        }

        [HttpGet("TerritoryGroups")]
        public async Task<ActionResult<List<TerritoryGroup>>> GetTerritoryGroups()
        {
            var result = await _arCollectionsDA.GetTerritoryGroupsAsync();
            return Ok(result);
        }

        [HttpGet("Customers")]
        public async Task<ActionResult<List<ARCustomerRow>>> LoadOpenCustomers(
            [FromQuery] int selectBy,
            [FromQuery] string? groupCriteria,
            [FromQuery] DateTime agingDate)
        {
            int userId = GetUserId();
            var result = await _arCollectionsDA.LoadOpenCustomersAsync(selectBy, groupCriteria ?? "", agingDate, userId);
            return Ok(result);
        }

        [HttpGet("RefreshARGrid")]
        public async Task<ActionResult<List<ARTransactionRow>>> RefreshARGrid(
            [FromQuery] string custNo,
            [FromQuery] int selectBy,
            [FromQuery] string? groupCriteria,
            [FromQuery] DateTime agingDate)
        {
            try
            {
                int userId = GetUserId();
                var result = await _arCollectionsDA.RefreshARGridAsync(custNo, selectBy, groupCriteria ?? "", agingDate, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                var errorText = $"=== Controller RefreshARGrid Error ({DateTime.Now}) ===\n" +
                                $"Error: {ex.Message}\n" +
                                $"Inner Exception: {innerMsg}\n" +
                                $"Stack Trace:\n{ex.StackTrace}\n\n";
                try { System.IO.File.AppendAllText(@"d:\LAPP\backend_error.txt", errorText); } catch { }
                throw;
            }
        }

        [HttpPost("UpdateARDetailRow")]
        public async Task<ActionResult<bool>> UpdateARDetailRow([FromBody] UpdateARDetailRequest request)
        {
            if (request == null) return BadRequest("Invalid request data.");
            int userId = GetUserId();
            var success = await _arCollectionsDA.UpdateARDetailRowAsync(request, userId);
            return Ok(success);
        }

        [HttpGet("Events")]
        public async Task<ActionResult<List<ARCommentEvent>>> GetEvents([FromQuery] string custNo, [FromQuery] int selectBy)
        {
            try
            {
                var result = await _arCollectionsDA.GetEventsAsync(custNo, selectBy);
                return Ok(result);
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                var errorText = $"=== Controller GetEvents Error ({DateTime.Now}) ===\n" +
                                $"Error: {ex.Message}\n" +
                                $"Inner Exception: {innerMsg}\n" +
                                $"Stack Trace:\n{ex.StackTrace}\n\n";
                try { System.IO.File.AppendAllText(@"d:\LAPP\backend_error.txt", errorText); } catch { }
                throw;
            }
        }

        [HttpPost("AddComment")]
        public async Task<ActionResult<int>> AddComment([FromBody] AddCommentRequest request)
        {
            try
            {
                if (request == null) return BadRequest("Invalid comment request.");
                int userId = GetUserId();
                string initials = GetUserInitials();
                var eventId = await _arCollectionsDA.AddCommentAsync(request, initials, userId);
                return Ok(eventId);
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                var errorText = $"=== Controller AddComment Error ({DateTime.Now}) ===\n" +
                                $"Error: {ex.Message}\n" +
                                $"Inner Exception: {innerMsg}\n" +
                                $"Stack Trace:\n{ex.StackTrace}\n\n";
                try { System.IO.File.AppendAllText(@"d:\LAPP\backend_error.txt", errorText); } catch { }
                throw;
            }
        }

        [HttpDelete("DeleteComment/{commentId}")]
        public async Task<ActionResult<bool>> DeleteComment(int commentId)
        {
            var success = await _arCollectionsDA.DeleteCommentAsync(commentId);
            return Ok(success);
        }

        [HttpPut("EditComment/{commentId}")]
        public async Task<ActionResult<bool>> EditComment(int commentId, [FromBody] string text)
        {
            string initials = GetUserInitials();
            var success = await _arCollectionsDA.EditCommentAsync(commentId, text, initials);
            return Ok(success);
        }

        [HttpDelete("RemoveCommentFromTrans/{eventTransId}")]
        public async Task<ActionResult<bool>> RemoveCommentFromTrans(int eventTransId)
        {
            var success = await _arCollectionsDA.RemoveCommentFromTransAsync(eventTransId);
            return Ok(success);
        }

        [HttpGet("CheckOpenPayments")]
        public async Task<ActionResult<bool>> CheckOpenPayments([FromQuery] string custNo)
        {
            var result = await _arCollectionsDA.CheckOpenPaymentsAsync(custNo);
            return Ok(result);
        }

        [HttpPost("GenerateOverdueNotice")]
        public async Task<IActionResult> GenerateOverdueNotice([FromBody] CreateNoticeRequest request)
        {
            if (request == null) return BadRequest("Invalid notice request.");
            int userId = GetUserId();
            string initials = GetUserInitials();

            string templatesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Templates");
            var fileBytes = await _arCollectionsDA.GenerateOverdueNoticeAsync(request, templatesPath, initials, userId);

            string fileName = request.Language == "French"
                ? (request.NoticeType == 1 ? "1er_Avis.docx" : "2ieme_Avis.docx")
                : (request.NoticeType == 1 ? "1st_Notice.docx" : "2nd_Notice.docx");

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
        }

        [HttpPost("OutputInvoicePdf")]
        public async Task<IActionResult> OutputInvoicePdf([FromBody] ExportInvoiceRequest request)
        {
            if (request == null) return BadRequest("Invalid invoice request.");
            int userId = GetUserId();
            var fileBytes = await _arCollectionsDA.OutputInvoicePdfAsync(request, userId);

            if (fileBytes == null || fileBytes.Length == 0)
            {
                return NotFound("Invoice data not found in Spire database.");
            }

            string contentType = request.InvoiceType == "Bulk" ? "application/zip" : "application/pdf";
            string fileName = request.InvoiceType == "Bulk"
                ? $"BulkInvoice-{request.InvoiceRef}.zip"
                : $"Invoice-{request.InvoiceRef}.pdf";

            return File(fileBytes, contentType, fileName);
        }

        [HttpPost("OutputCheckedDocuments")]
        public async Task<IActionResult> OutputCheckedDocuments([FromBody] OutputCheckedDocumentsRequest request)
        {
            if (request == null) return BadRequest("Invalid request.");
            int userId = GetUserId();
            var fileBytes = await _arCollectionsDA.OutputCheckedDocumentsAsync(request.CustNo, request.ChkSendBulk, request.CheckedTransNos, userId);

            if (fileBytes == null || fileBytes.Length == 0)
            {
                return NotFound("No checked documents generated.");
            }

            string fileName = $"Documents_{request.CustNo}_{DateTime.Now:yyyyMMdd}.zip";
            return File(fileBytes, "application/zip", fileName);
        }

        [HttpGet("OutputPaymentAdvicePdf")]
        public async Task<IActionResult> OutputPaymentAdvicePdf([FromQuery] string transNo)
        {
            if (string.IsNullOrEmpty(transNo)) return BadRequest("Invalid transaction number.");
            int userId = GetUserId();
            var fileBytes = await _arCollectionsDA.OutputPaymentAdvicePdfAsync(transNo, userId);

            if (fileBytes == null || fileBytes.Length == 0)
            {
                return NotFound("Payment transaction not found in Spire database.");
            }

            return File(fileBytes, "application/pdf", $"PaymentAdvice-{transNo}.pdf");
        }

        [HttpGet("Users")]
        public async Task<ActionResult<object>> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var users = await _arCollectionsDA.GetARUsersAsync(page, pageSize);
            var totalCount = await _arCollectionsDA.GetARUsersCountAsync();
            return Ok(new { data = users, total = totalCount });
        }

        [HttpPost("CreateUser")]
        public async Task<ActionResult<bool>> CreateUser([FromBody] ARCollectionUser user)
        {
            if (user == null) return BadRequest("Invalid user data.");
            int currentUserId = GetUserId();
            var success = await _arCollectionsDA.CreateARUserAsync(user, currentUserId);
            return Ok(success);
        }

        [HttpPost("UpdateUser")]
        public async Task<ActionResult<bool>> UpdateUser([FromBody] ARCollectionUser user)
        {
            if (user == null) return BadRequest("Invalid user data.");
            int currentUserId = GetUserId();
            var success = await _arCollectionsDA.UpdateARUserAsync(user, currentUserId);
            return Ok(success);
        }

        [HttpDelete("DeleteUser/{id}")]
        public async Task<ActionResult<bool>> DeleteUser(int id)
        {
            var success = await _arCollectionsDA.DeleteARUserAsync(id);
            return Ok(success);
        }

        // --- Customer Groups Management ---
        [HttpGet("CustomerGroups")]
        public async Task<ActionResult<object>> GetCustomerGroups([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var groups = await _arCollectionsDA.GetCustomerGroupsAsync(page, pageSize);
            var totalCount = await _arCollectionsDA.GetCustomerGroupsCountAsync();
            return Ok(new { data = groups, total = totalCount });
        }

        [HttpPost("CreateCustomerGroup")]
        public async Task<ActionResult<bool>> CreateCustomerGroup([FromBody] TblCustomerGroups group)
        {
            if (group == null) return BadRequest("Invalid customer group data.");
            int currentUserId = GetUserId();
            var success = await _arCollectionsDA.CreateCustomerGroupAsync(group, currentUserId);
            return Ok(success);
        }

        [HttpPost("UpdateCustomerGroup")]
        public async Task<ActionResult<bool>> UpdateCustomerGroup([FromBody] TblCustomerGroups group)
        {
            if (group == null) return BadRequest("Invalid customer group data.");
            int currentUserId = GetUserId();
            var success = await _arCollectionsDA.UpdateCustomerGroupAsync(group, currentUserId);
            return Ok(success);
        }

        [HttpDelete("DeleteCustomerGroup/{id}")]
        public async Task<ActionResult<bool>> DeleteCustomerGroup(int id)
        {
            var success = await _arCollectionsDA.DeleteCustomerGroupAsync(id);
            return Ok(success);
        }

        // --- Bulk Customers Management ---
        [HttpGet("BulkCustomers")]
        public async Task<ActionResult<object>> GetBulkCustomers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var bulks = await _arCollectionsDA.GetBulkCustomersAsync(page, pageSize);
            var totalCount = await _arCollectionsDA.GetBulkCustomersCountAsync();
            return Ok(new { data = bulks, total = totalCount });
        }

        [HttpPost("CreateBulkCustomer")]
        public async Task<ActionResult<bool>> CreateBulkCustomer([FromBody] TblBulkCustomers bulk)
        {
            if (bulk == null) return BadRequest("Invalid bulk customer data.");
            int currentUserId = GetUserId();
            var success = await _arCollectionsDA.CreateBulkCustomerAsync(bulk, currentUserId);
            return Ok(success);
        }

        [HttpDelete("DeleteBulkCustomer/{id}")]
        public async Task<ActionResult<bool>> DeleteBulkCustomer(int id)
        {
            var success = await _arCollectionsDA.DeleteBulkCustomerAsync(id);
            return Ok(success);
        }

        // --- Parity with Access Form frmCustGroupMaintain ---
        [HttpGet("GroupsSummary")]
        public async Task<ActionResult<List<CustomerGroupSummary>>> GetGroupsSummary([FromQuery] string groupType)
        {
            var result = await _arCollectionsDA.GetARGroupsSummaryAsync(groupType);
            return Ok(result);
        }

        [HttpGet("GroupCustomers")]
        public async Task<ActionResult<List<GroupCustomerRow>>> GetGroupCustomers([FromQuery] string groupType, [FromQuery] string custGroup)
        {
            var result = await _arCollectionsDA.GetARGroupCustomersAsync(groupType, custGroup);
            return Ok(result);
        }

        [HttpGet("LookupCustomerName")]
        public async Task<ActionResult<object>> LookupCustomerName([FromQuery] string custNo)
        {
            var (exists, name) = await _arCollectionsDA.LookupSpireCustomerNameAsync(custNo);
            return Ok(new { exists, name });
        }

        [HttpPost("AddCustomerToGroup")]
        public async Task<ActionResult<object>> AddCustomerToGroup([FromBody] AddCustomerToGroupRequest request)
        {
            if (request == null) return BadRequest("Invalid request");
            int userId = GetUserId();
            var message = await _arCollectionsDA.AddCustomerToGroupAsync(
                request.GroupType,
                request.CustNo,
                request.IsNewGroup,
                request.NewGroupName,
                request.SelectedCustGroup,
                userId
            );
            return Ok(new { message });
        }

        [HttpPost("RemoveCustomerFromGroup")]
        public async Task<ActionResult<bool>> RemoveCustomerFromGroup([FromBody] RemoveCustomerFromGroupRequest request)
        {
            if (request == null) return BadRequest("Invalid request");
            var success = await _arCollectionsDA.RemoveCustomerFromGroupAsync(request.GroupType, request.CustNo);
            return Ok(success);
        }

        [HttpPost("ModifyGroupName")]
        public async Task<ActionResult<bool>> ModifyGroupName([FromBody] ModifyGroupNameRequest request)
        {
            if (request == null) return BadRequest("Invalid request");
            var success = await _arCollectionsDA.ModifyGroupNameAsync(request.GroupType, request.CustGroup, request.NewGroupName);
            return Ok(success);
        }

        [HttpGet("BulkCustomersWithName")]
        public async Task<ActionResult<List<BulkCustomerRow>>> GetBulkCustomersWithName()
        {
            var result = await _arCollectionsDA.GetBulkCustomersWithNameAsync();
            return Ok(result);
        }

        [HttpPost("AddBulkCustomer")]
        public async Task<ActionResult<bool>> AddBulkCustomer([FromBody] string custNo)
        {
            int userId = GetUserId();
            var success = await _arCollectionsDA.AddBulkCustomerAsync(custNo, userId);
            return Ok(success);
        }

        [HttpDelete("RemoveBulkCustomer/{id}")]
        public async Task<ActionResult<bool>> RemoveBulkCustomer(int id)
        {
            var success = await _arCollectionsDA.RemoveBulkCustomerAsync(id);
            return Ok(success);
        }

        [HttpGet("GLAllowedAccounts")]
        public async Task<ActionResult<List<GLAllowedAccountDto>>> GetGLAllowedAccounts()
        {
            var result = await _arCollectionsDA.GetGLAllowedAccountsAsync();
            return Ok(result);
        }

        [HttpGet("GLActivity")]
        public async Task<ActionResult<List<GLActivityRow>>> GetGLActivity(
            [FromQuery] string accountNo,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            var result = await _arCollectionsDA.GetGLActivityAsync(accountNo, startDate, endDate);
            return Ok(result);
        }

        [HttpGet("ExportGLActivity")]
        public async Task<IActionResult> ExportGLActivity(
            [FromQuery] string accountNo,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            var fileBytes = await _arCollectionsDA.ExportGLActivityAsync(accountNo, startDate, endDate);
            string fileName = $"GLActivity-{accountNo} {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        #region Comment Review

        [HttpPost("GenerateCommentReviewData")]
        public async Task<ActionResult<bool>> GenerateCommentReviewData([FromBody] DateTime agingDate)
        {
            try
            {
                int userId = GetUserId();
                var result = await _arCollectionsDA.GenerateCommentReviewDataAsync(agingDate, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                var errorText = $"=== Controller GenerateCommentReviewData Error ({DateTime.Now}) ===\n" +
                                $"Error: {ex.Message}\n" +
                                $"Inner Exception: {innerMsg}\n" +
                                $"Stack Trace:\n{ex.StackTrace}\n\n";
                try { System.IO.File.AppendAllText(@"d:\LAPP\backend_error.txt", errorText); } catch { }
                return StatusCode(500, new { message = innerMsg });
            }
        }

        [HttpGet("CommentReviewSummary")]
        public async Task<ActionResult<List<CommentReviewSummaryRow>>> GetCommentReviewSummary(
            [FromQuery] int minDays,
            [FromQuery] string? groupCriteria)
        {
            try
            {
                int userId = GetUserId();
                var result = await _arCollectionsDA.GetCommentReviewSummaryAsync(minDays, groupCriteria ?? "", userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                var errorText = $"=== Controller GetCommentReviewSummary Error ({DateTime.Now}) ===\n" +
                                $"Error: {ex.Message}\n" +
                                $"Inner Exception: {innerMsg}\n" +
                                $"Stack Trace:\n{ex.StackTrace}\n\n";
                try { System.IO.File.AppendAllText(@"d:\LAPP\backend_error.txt", errorText); } catch { }
                return StatusCode(500, new { message = innerMsg });
            }
        }

        [HttpGet("SummaryComment")]
        public async Task<ActionResult<ARCommentEvent>> GetSummaryComment([FromQuery] string custNo)
        {
            try
            {
                var result = await _arCollectionsDA.GetSummaryCommentAsync(custNo);
                return Ok(result);
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                var errorText = $"=== Controller GetSummaryComment Error ({DateTime.Now}) ===\n" +
                                $"Error: {ex.Message}\n" +
                                $"Inner Exception: {innerMsg}\n" +
                                $"Stack Trace:\n{ex.StackTrace}\n\n";
                try { System.IO.File.AppendAllText(@"d:\LAPP\backend_error.txt", errorText); } catch { }
                return StatusCode(500, new { message = innerMsg });
            }
        }

        [HttpPost("SummaryComment")]
        public async Task<ActionResult<bool>> SaveSummaryComment([FromBody] SaveSummaryCommentRequest request)
        {
            try
            {
                if (request == null) return BadRequest("Invalid comment request.");
                int userId = GetUserId();
                string initials = GetUserInitials();
                var result = await _arCollectionsDA.SaveSummaryCommentAsync(request.CustNo, request.CustType, request.CommentText, initials, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                var errorText = $"=== Controller SaveSummaryComment Error ({DateTime.Now}) ===\n" +
                                $"Error: {ex.Message}\n" +
                                $"Inner Exception: {innerMsg}\n" +
                                $"Stack Trace:\n{ex.StackTrace}\n\n";
                try { System.IO.File.AppendAllText(@"d:\LAPP\backend_error.txt", errorText); } catch { }
                return StatusCode(500, new { message = innerMsg });
            }
        }

        [HttpGet("ExportSummaryComments")]
        public async Task<IActionResult> ExportSummaryComments(
            [FromQuery] int minDays,
            [FromQuery] string? groupCriteria)
        {
            try
            {
                int userId = GetUserId();
                var fileBytes = await _arCollectionsDA.ExportSummaryCommentsAsync(minDays, groupCriteria ?? "", userId);
                string fileName = $"SummaryComments-{DateTime.Now:yyyy-MM-dd}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                var errorText = $"=== Controller ExportSummaryComments Error ({DateTime.Now}) ===\n" +
                                $"Error: {ex.Message}\n" +
                                $"Inner Exception: {innerMsg}\n" +
                                $"Stack Trace:\n{ex.StackTrace}\n\n";
                try { System.IO.File.AppendAllText(@"d:\LAPP\backend_error.txt", errorText); } catch { }
                return StatusCode(500, new { message = innerMsg });
            }
        }

        #endregion

        #region AR Reporting

        [HttpPost("GenerateAgingData")]
        public async Task<ActionResult<bool>> GenerateAgingData([FromBody] GenerateAgingDataRequest request)
        {
            try
            {
                if (request == null) return BadRequest("Invalid request.");
                int userId = GetUserId();
                var result = await _arCollectionsDA.GenerateAgingDataAsync(request.LastReportDate, request.StartDate, request.EndDate, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                var errorText = $"=== Controller GenerateAgingData Error ({DateTime.Now}) ===\n" +
                                $"Error: {ex.Message}\n" +
                                $"Inner Exception: {innerMsg}\n" +
                                $"Stack Trace:\n{ex.StackTrace}\n\n";
                try { System.IO.File.AppendAllText(@"d:\LAPP\backend_error.txt", errorText); } catch { }
                return StatusCode(500, new { message = innerMsg });
            }
        }

        [HttpGet("ExportAgedSummary")]
        public async Task<IActionResult> ExportAgedSummary()
        {
            try
            {
                var currentUserId = 1;
                var fileBytes = await _arCollectionsDA.ExportAgedSummaryAsync(currentUserId);
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "PaymentsReceivedByChannel.xlsx");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("GetAgedSummaryData")]
        public async Task<ActionResult<IEnumerable<object>>> GetAgedSummaryData()
        {
            try
            {
                var currentUserId = 1;
                var data = await _arCollectionsDA.GetAgedSummaryDataAsync(currentUserId);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("GetPaymentDetailsData")]
        public async Task<ActionResult<IEnumerable<object>>> GetPaymentDetailsData()
        {
            try
            {
                var currentUserId = 1;
                var data = await _arCollectionsDA.GetPaymentDetailsDataAsync(currentUserId);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("ExportPaymentDetails")]
        public async Task<IActionResult> ExportPaymentDetails()
        {
            try
            {
                int userId = GetUserId();
                var fileBytes = await _arCollectionsDA.ExportPaymentDetailsAsync(userId);
                string fileName = $"PaymentDetails-{DateTime.Now:yyyy-MM-dd}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = innerMsg });
            }
        }

        [HttpGet("GetARMasterData")]
        public async Task<ActionResult<IEnumerable<object>>> GetARMasterData()
        {
            try
            {
                var currentUserId = 1;
                var data = await _arCollectionsDA.GetARMasterDataGridAsync(currentUserId);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("GenerateARMasterData")]
        public async Task<ActionResult<bool>> GenerateARMasterData([FromBody] DateTime agingDate)
        {
            try
            {
                int userId = GetUserId();
                var result = await _arCollectionsDA.GenerateARMasterDataAsync(agingDate, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                var errorText = $"=== Controller GenerateARMasterData Error ({DateTime.Now}) ===\n" +
                                $"Error: {ex.Message}\n" +
                                $"Inner Exception: {innerMsg}\n" +
                                $"Stack Trace:\n{ex.StackTrace}\n\n";
                try { System.IO.File.AppendAllText(@"d:\LAPP\backend_error.txt", errorText); } catch { }
                return StatusCode(500, new { message = innerMsg });
            }
        }

        [HttpGet("ExportARMaster")]
        public async Task<IActionResult> ExportARMaster()
        {
            try
            {
                int userId = GetUserId();
                var fileBytes = await _arCollectionsDA.ExportARMasterAsync(userId);
                string fileName = $"AR-Master-{DateTime.Now:yyyy-MM-dd}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                var errorText = $"=== Controller ExportARMaster Error ({DateTime.Now}) ===\n" +
                                $"Error: {ex.Message}\n" +
                                $"Inner Exception: {innerMsg}\n" +
                                $"Stack Trace:\n{ex.StackTrace}\n\n";
                try { System.IO.File.AppendAllText(@"d:\LAPP\backend_error.txt", errorText); } catch { }
                return StatusCode(500, new { message = innerMsg });
            }
        }

        [HttpGet("ExportARMasterAll")]
        public async Task<IActionResult> ExportARMasterAll()
        {
            try
            {
                int userId = GetUserId();
                var fileBytes = await _arCollectionsDA.ExportARMasterAllAsync(userId);
                string fileName = $"AR-Master-ALL-{DateTime.Now:yyyy-MM-dd}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                var errorText = $"=== Controller ExportARMasterAll Error ({DateTime.Now}) ===\n" +
                                $"Error: {ex.Message}\n" +
                                $"Inner Exception: {innerMsg}\n" +
                                $"Stack Trace:\n{ex.StackTrace}\n\n";
                try { System.IO.File.AppendAllText(@"d:\LAPP\backend_error.txt", errorText); } catch { }
                return StatusCode(500, new { message = innerMsg });
            }
        }

        [HttpGet("ExportARMasterSummary")]
        public async Task<IActionResult> ExportARMasterSummary()
        {
            try
            {
                int userId = GetUserId();
                var fileBytes = await _arCollectionsDA.ExportARMasterSummaryAsync(userId);
                string fileName = $"AR Summar-{DateTime.Now:yyyy-MM-dd}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                var errorText = $"=== Controller ExportARMasterSummary Error ({DateTime.Now}) ===\n" +
                                $"Error: {ex.Message}\n" +
                                $"Inner Exception: {innerMsg}\n" +
                                $"Stack Trace:\n{ex.StackTrace}\n\n";
                try { System.IO.File.AppendAllText(@"d:\LAPP\backend_error.txt", errorText); } catch { }
                return StatusCode(500, new { message = innerMsg });
            }
        }


        #endregion

        #region Batch Notice APIs

        [HttpPost("batch-notice/generate")]
        public async Task<ActionResult<bool>> GenerateBatchNoticeData([FromQuery] DateTime agingDate)
        {
            try
            {
                var userId = GetUserId();
                var result = await _arCollectionsDA.GenerateBatchNoticeDataAsync(agingDate, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = innerMsg });
            }
        }

        [HttpGet("batch-notice/summary")]
        public async Task<ActionResult<List<BatchNoticeSummaryRow>>> GetBatchNoticeSummary(
            [FromQuery] string? groupCriteria,
            [FromQuery] int startDays,
            [FromQuery] int endDays,
            [FromQuery] string? noticeType)
        {
            try
            {
                var userId = GetUserId();
                var result = await _arCollectionsDA.GetBatchNoticeSummaryAsync(groupCriteria ?? "", startDays, endDays, noticeType ?? "", userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = innerMsg });
            }
        }

        [HttpGet("batch-notice/detail")]
        public async Task<ActionResult<List<BatchNoticeDetailRow>>> GetBatchNoticeDetail(
            [FromQuery] string? groupCriteria,
            [FromQuery] int startDays,
            [FromQuery] int endDays,
            [FromQuery] string? noticeType)
        {
            try
            {
                var userId = GetUserId();
                var result = await _arCollectionsDA.GetBatchNoticeDetailAsync(groupCriteria ?? "", startDays, endDays, noticeType ?? "", userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = innerMsg });
            }
        }

        [HttpPost("batch-notice/output")]
        public async Task<IActionResult> OutputBatchNotices([FromBody] OutputBatchNoticeRequest request)
        {
            try
            {
                var userId = GetUserId();
                var initials = "XX"; // Or fetch from user config
                var templatesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Templates");

                var fileBytes = await _arCollectionsDA.OutputBatchNoticesAsync(
                    request.SelectedGroups, request.NoticeType, request.StartDays, request.EndDays, request.GroupCriteria ?? "", templatesPath, initials, userId);

                return File(fileBytes, "application/zip", "BatchNotices.zip");
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = innerMsg });
            }
        }

        #endregion
    }

    public class AddCustomerToGroupRequest
    {
        public string GroupType { get; set; } = string.Empty;
        public string CustNo { get; set; } = string.Empty;
        public bool IsNewGroup { get; set; }
        public string NewGroupName { get; set; } = string.Empty;
        public string SelectedCustGroup { get; set; } = string.Empty;
    }

    public class RemoveCustomerFromGroupRequest
    {
        public string GroupType { get; set; } = string.Empty;
        public string CustNo { get; set; } = string.Empty;
    }

    public class ModifyGroupNameRequest
    {
        public string GroupType { get; set; } = string.Empty;
        public string CustGroup { get; set; } = string.Empty;
        public string NewGroupName { get; set; } = string.Empty;
    }

    public class SaveSummaryCommentRequest
    {
        public string CustNo { get; set; } = string.Empty;
        public string CustType { get; set; } = string.Empty;
        public string CommentText { get; set; } = string.Empty;
    }

    public class GenerateAgingDataRequest
    {
        public DateTime LastReportDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class OutputBatchNoticeRequest
    {
        public List<string> SelectedGroups { get; set; } = new List<string>();
        public string NoticeType { get; set; } = string.Empty;
        public int StartDays { get; set; }
        public int EndDays { get; set; }
        public string GroupCriteria { get; set; } = string.Empty;
    }

    public class OutputCheckedDocumentsRequest
    {
        public string CustNo { get; set; } = string.Empty;
        public bool ChkSendBulk { get; set; }
        public List<string> CheckedTransNos { get; set; } = new List<string>();
    }
}