using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DAL.Models;

namespace DAL.Sales.ARCollections
{
    public interface IARCollectionsDA
    {
        Task<List<TerritoryGroup>> GetTerritoryGroupsAsync();

        Task<List<ARCustomerRow>> LoadOpenCustomersAsync(int selectBy, string groupCriteria, DateTime agingDate, int userId);

        Task<List<ARTransactionRow>> RefreshARGridAsync(string custNo, int selectBy, string groupCriteria, DateTime agingDate, int userId);

        Task<bool> UpdateARDetailRowAsync(UpdateARDetailRequest request, int userId);

        Task<List<ARCommentEvent>> GetEventsAsync(string custNo, int selectBy);

        Task<int> AddCommentAsync(AddCommentRequest request, string initials, int userId);

        Task<bool> DeleteCommentAsync(int commentId);

        Task<bool> EditCommentAsync(int commentId, string text, string initials);

        Task<bool> RemoveCommentFromTransAsync(int eventTransId);

        Task<bool> CheckOpenPaymentsAsync(string custNo);

        Task<byte[]> GenerateOverdueNoticeAsync(CreateNoticeRequest request, string templatesPath, string initials, int userId);

        Task<byte[]> OutputInvoicePdfAsync(ExportInvoiceRequest request, int userId);

        Task<byte[]> OutputPaymentAdvicePdfAsync(string transNo, int userId);

        Task<byte[]> OutputCheckedDocumentsAsync(string custNo, bool chkSendBulk, List<string> checkedTransNos, int userId);

        Task<List<ARCollectionUser>> GetARUsersAsync(int page, int pageSize);
        Task<int> GetARUsersCountAsync();
        Task<bool> CreateARUserAsync(ARCollectionUser user, int currentUserId);
        Task<bool> UpdateARUserAsync(ARCollectionUser user, int currentUserId);
        Task<bool> DeleteARUserAsync(int id);

        // --- Customer Groups Management ---
        Task<List<TblCustomerGroups>> GetCustomerGroupsAsync(int page, int pageSize);
        Task<int> GetCustomerGroupsCountAsync();
        Task<bool> CreateCustomerGroupAsync(TblCustomerGroups group, int currentUserId);
        Task<bool> UpdateCustomerGroupAsync(TblCustomerGroups group, int currentUserId);
        Task<bool> DeleteCustomerGroupAsync(int id);

        // --- Bulk Customers Management ---
        Task<List<TblBulkCustomers>> GetBulkCustomersAsync(int page, int pageSize);
        Task<int> GetBulkCustomersCountAsync();
        Task<bool> CreateBulkCustomerAsync(TblBulkCustomers bulk, int currentUserId);
        Task<bool> DeleteBulkCustomerAsync(int id);

        // --- Parity with Access Form frmCustGroupMaintain ---
        Task<List<CustomerGroupSummary>> GetARGroupsSummaryAsync(string groupType);
        Task<List<GroupCustomerRow>> GetARGroupCustomersAsync(string groupType, string custGroup);
        Task<(bool exists, string name)> LookupSpireCustomerNameAsync(string custNo);
        Task<string> AddCustomerToGroupAsync(string groupType, string custNo, bool isNewGroup, string newGroupName, string selectedCustGroup, int currentUserId);
        Task<bool> RemoveCustomerFromGroupAsync(string groupType, string custNo);
        Task<bool> ModifyGroupNameAsync(string groupType, string custGroup, string newGroupName);
        Task<List<BulkCustomerRow>> GetBulkCustomersWithNameAsync();
        Task<bool> AddBulkCustomerAsync(string custNo, int currentUserId);
        Task<bool> RemoveBulkCustomerAsync(int id);
        Task<List<GLAllowedAccountDto>> GetGLAllowedAccountsAsync();
        Task<List<GLActivityRow>> GetGLActivityAsync(string accountNo, DateTime startDate, DateTime endDate);
        Task<byte[]> ExportGLActivityAsync(string accountNo, DateTime startDate, DateTime endDate);

        // --- Comment Review Features ---
        Task<bool> GenerateCommentReviewDataAsync(DateTime agingDate, int userId);
        Task<List<CommentReviewSummaryRow>> GetCommentReviewSummaryAsync(int minDays, string groupCriteria, int userId);
        Task<ARCommentEvent?> GetSummaryCommentAsync(string custNo);
        Task<bool> SaveSummaryCommentAsync(string custNo, string custType, string commentText, string initials, int userId);
        Task<byte[]> ExportSummaryCommentsAsync(int minDays, string groupCriteria, int userId);

        #region AR Reporting

        Task<bool> GenerateAgingDataAsync(DateTime lastReportDate, DateTime startDate, DateTime endDate, int userId);
        Task<byte[]> ExportAgedSummaryAsync(int userId);
        Task<IEnumerable<object>> GetAgedSummaryDataAsync(int userId);
        Task<IEnumerable<object>> GetPaymentDetailsDataAsync(int userId);

        Task<bool> GenerateARMasterDataAsync(DateTime agingDate, int userId);
        Task<byte[]> ExportARMasterAsync(int userId);
        Task<byte[]> ExportARMasterAllAsync(int userId);
        Task<byte[]> ExportARMasterSummaryAsync(int userId);
        Task<byte[]> ExportPaymentDetailsAsync(int userId);
        Task<IEnumerable<object>> GetARMasterDataGridAsync(int userId);

        #region Batch Notice Output

        Task<bool> GenerateBatchNoticeDataAsync(DateTime agingDate, int userId);
        Task<List<BatchNoticeSummaryRow>> GetBatchNoticeSummaryAsync(string groupCriteria, int startDays, int endDays, string noticeType, int userId);
        Task<List<BatchNoticeDetailRow>> GetBatchNoticeDetailAsync(string groupCriteria, int startDays, int endDays, string noticeType, int userId);
        Task<byte[]> OutputBatchNoticesAsync(List<string> selectedGroups, string noticeType, int startDays, int endDays, string groupCriteria, string templatesPath, string initials, int userId);

        #endregion
    }

    public class CustomerGroupSummary
    {
        public string CustGroup { get; set; } = string.Empty;
        public string MaxOfGroupName { get; set; } = string.Empty;
        public int CountOfCustGroup { get; set; }
    }

    public class GroupCustomerRow
    {
        public int Id { get; set; }
        public string CustGroup { get; set; } = string.Empty;
        public string BVCustNo { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string BVName { get; set; } = string.Empty;
    }

    public class BulkCustomerRow
    {
        public int ID { get; set; }
        public string CustNo { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class CommentReviewSummaryRow
    {
        public string GroupID { get; set; } = string.Empty;
        public string MaxOfSALES_TERR { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ARType { get; set; } = string.Empty;
        public int TransCount { get; set; }
        public int SumOfInvoiceCount { get; set; }
        public int SumOfPaymentCount { get; set; }
        public int SumOfFirstNoticeCount { get; set; }
        public int SumOfSecondNoticeCount { get; set; }
        public decimal SumOfBALANCE { get; set; }
        public bool BulkInvoice { get; set; }
    }

    public class BatchNoticeSummaryRow
    {
        public string GroupID { get; set; } = string.Empty;
        public string MaxOfSALES_TERR { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ARType { get; set; } = string.Empty;
        public int TransCount { get; set; }
        public int SumOfInvoiceCount { get; set; }
        public int SumOfPaymentCount { get; set; }
        public int SumOfFirstNoticeCount { get; set; }
        public int SumOfSecondNoticeCount { get; set; }
        public decimal SumOfBALANCE { get; set; }
        public bool BulkInvoice { get; set; }
    }

    public class BatchNoticeDetailRow
    {
        public string CUST { get; set; } = string.Empty;
        public string GroupID { get; set; } = string.Empty;
        public string SALES_TERR { get; set; } = string.Empty;
        public string CustType { get; set; } = string.Empty;
        public string CustName { get; set; } = string.Empty;
        public string CustGroup { get; set; } = string.Empty;
        public string FOLIO { get; set; } = string.Empty;
        public string TopItem { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string TRANS_NO { get; set; } = string.Empty;
        public string REF_NO { get; set; } = string.Empty;
        public DateTime? TranDate { get; set; }
        public decimal D_AMOUNT { get; set; }
        public decimal C_AMOUNT { get; set; }
        public decimal BALANCE { get; set; }
        public int DaysOld { get; set; }
        public bool Checked { get; set; }
        public DateTime? FirstNoticeDate { get; set; }
        public decimal? FirstNoticeBalance { get; set; }
        public DateTime? SecondNoticeDate { get; set; }
        public decimal? SecondNoticeBalance { get; set; }
        public int InvoiceCount { get; set; }
        public int PaymentCount { get; set; }
        public int FirstNoticeCount { get; set; }
        public int SecondNoticeCount { get; set; }
        public string BulkID { get; set; } = string.Empty;
        public bool BulkIDChecked { get; set; }
        public string Language { get; set; } = string.Empty;
        public bool SendBulk { get; set; }
    }
}


#endregion
