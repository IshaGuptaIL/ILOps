using System;
using System.Collections.Generic;

namespace DAL.Sales.ARCollections
{
    public class TerritoryGroup
    {
        public int ID { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string? GroupCriteria { get; set; }
        public int? SortOrder { get; set; }
        public string? Phone1 { get; set; }
        public string? Phone2 { get; set; }
        public bool RogersReporting { get; set; }
        public string? RogersReportingName { get; set; }
    }

    public class ARCustomerRow
    {
        public string CUST { get; set; } = string.Empty;
        public string? CustName { get; set; }
        public string? CustGroup { get; set; }
        public bool GroupAndSingle { get; set; }
        public string? SALES_TERR { get; set; }
        public string? PostalCode { get; set; }
        public string? BVADDRTELNO1 { get; set; }
        public string? BVADDREMAIL { get; set; }
        public string? BVCOCONTACT1NAME { get; set; }
        public string? BVCOCONTACT1TEL1 { get; set; }
        public string? BVCOCONTACT1EMAIL { get; set; }
        public string? BVCOCONTACT2NAME { get; set; }
        public string? BVCOCONTACT2TEL1 { get; set; }
        public string? BVCOCONTACT2EMAIL { get; set; }
        public string? BVCOCONTACT3NAME { get; set; }
        public string? BVCOCONTACT3TEL1 { get; set; }
        public string? BVCOCONTACT3EMAIL { get; set; }
        public string? Language { get; set; }
        public int? ChannelID { get; set; }
        public int? AddressID { get; set; }
        public bool SendBulk { get; set; }
    }

    public class ARTransactionRow
    {
        public int Id { get; set; }
        public bool Checked { get; set; }
        public string CUST { get; set; } = string.Empty;
        public string? FOLIO { get; set; }
        public string? TopItem { get; set; }
        public string? Type { get; set; }
        public string TRANS_NO { get; set; } = string.Empty;
        public string? REF_NO { get; set; }
        public DateTime? TranDate { get; set; }
        public decimal D_AMOUNT { get; set; }
        public decimal C_AMOUNT { get; set; }
        public decimal BALANCE { get; set; }
        public decimal Amount { get; set; } // Computed in query: IIf(debit_amt!=0, debit_amt, credit_amt*-1)
        public int? DaysOld { get; set; }

        // Aging Columns (Computed)
        public decimal Current { get; set; }
        public decimal ThirtyDays { get; set; }
        public decimal SixtyDays { get; set; }
        public decimal NinetyDays { get; set; }
        public decimal OneTwentyPlusDays { get; set; }

        // Activations details (from tblActivationsLookup)
        public string? ActivationsTerritory { get; set; }
        public string? MSD { get; set; }
        public string? WebOrderID { get; set; }
        public string? CostBudgetCode { get; set; }
        public string? CustomerPONo { get; set; }
        public string? UserName { get; set; }
        public string? CellPhoneNo { get; set; }
        public decimal? CountGovChannel { get; set; }
        public decimal? CountGovFee { get; set; }

        // Extra details (from tblARDetailExtra)
        public string? BAN { get; set; }
        public DateTime? FirstNoticeDate { get; set; }
        public decimal? FirstNoticeBalance { get; set; }
        public DateTime? SecondNoticeDate { get; set; }
        public decimal? SecondNoticeBalance { get; set; }
        public int? RootCauseID { get; set; }
        public string? RootCauseDescription { get; set; }
        public int? NextID { get; set; }
        public bool OPCResolved { get; set; }
        public string? OPCDescription { get; set; }
        public string? BulkID { get; set; }
        public bool IgnoreGroup { get; set; }
        public string? BillToCust { get; set; }
    }

    public class ARCommentEvent
    {
        public int ID { get; set; }
        public int EventType { get; set; }
        public string EventDescription { get; set; } = string.Empty;
        public string? CustNo { get; set; }
        public string? CustType { get; set; }
        public string? EventText { get; set; }
        public decimal? EventAmount { get; set; }
        public string? CommentKey { get; set; }
        public DateTime? AddDate { get; set; }
        public string? AddUser { get; set; }
        public DateTime? ModDate { get; set; }
        public string? ModUser { get; set; }
        public string? TransNo { get; set; }
        public int? EventTransID { get; set; }
    }

    public class UpdateARDetailRequest
    {
        public string TransNo { get; set; } = string.Empty;
        public string? BAN { get; set; }
        public int? RootCauseID { get; set; }
        public bool OPCResolved { get; set; }
        public string? OPCDescription { get; set; }
        public bool IgnoreGroup { get; set; }
        public string? BillToCust { get; set; }
    }

    public class AddCommentRequest
    {
        public string CustNo { get; set; } = string.Empty;
        public string CustType { get; set; } = string.Empty; // "Single" or "Group"
        public string CommentText { get; set; } = string.Empty;
        public List<string> CheckedTransNos { get; set; } = new List<string>();
        public int EventType { get; set; } = 1;
    }

    public class CreateNoticeRequest
    {
        public int NoticeType { get; set; } // 1 or 2
        public string CustNo { get; set; } = string.Empty;
        public string CustName { get; set; } = string.Empty;
        public string Language { get; set; } = "English";
        public decimal Amount { get; set; }
        public List<string> CheckedTransNos { get; set; } = new List<string>();
    }

    public class ExportInvoiceRequest
    {
        public string InvoiceRef { get; set; } = string.Empty; // Invoice or BulkID
        public string InvoiceType { get; set; } = "Normal"; // "Normal" or "Bulk"
        public string CustNo { get; set; } = string.Empty;
        public string CustName { get; set; } = string.Empty;

    }

    public class ARSummary
    {
        public decimal TotalCurrent { get; set; }
        public decimal Total30Days { get; set; }
        public decimal Total60Days { get; set; }
        public decimal Total90Days { get; set; }
        public decimal Total120PlusDays { get; set; }
        public decimal TotalOutstanding { get; set; }
    }

    public class ARCollectionUser
    {
        public int ID { get; set; }
        public string DomainUser { get; set; } = string.Empty;
        public string? Initials { get; set; }
        public int? DefaultChannel { get; set; }
        public string? ChannelName { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }

    public class GLAllowedAccountDto
    {
        public string Account { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class GLActivityRow
    {
        public string AccountNo { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public string TransNo { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string? GLMemo { get; set; }
        public string? Type { get; set; }
        public string? Entity { get; set; }
        public string? Document { get; set; }
        public decimal DebitAmt { get; set; }
        public decimal CreditAmt { get; set; }
        public decimal Balance { get; set; }
        public string? WebOrderID { get; set; }
        public DateTime? PostDate { get; set; }
    }
}

