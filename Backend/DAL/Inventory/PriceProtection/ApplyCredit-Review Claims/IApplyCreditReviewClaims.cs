using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Inventory.PriceProtection.ApplyCredit_ReviewClaims
{
    public interface IApplyCreditReviewClaims
    {
        Task<List<ClaimsSummaryRow>> GetClaimsSummaryAsync();
        Task<List<CreditSummaryRow>> GetCreditSummaryAsync(int claimBatchID);
        Task<List<UnpaidClaimsDetailRow>> GetUnpaidClaimsDetailAsync(int claimBatchID, string? creditNoteNumber);
        Task<List<CreditDetailRow>> GetCreditDetailAsync(int ppClaimID);
        Task<bool> ModifyCreditNoteNumberAsync(string oldNumber, string newNumber, string user);
        Task<bool> ApplyCreditAsync(ApplyCreditRequest request, string user);
        Task<byte[]> ExportClaimsSummaryExcelAsync();
    }
}
