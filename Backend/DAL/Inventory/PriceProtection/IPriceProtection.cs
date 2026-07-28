using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Inventory.PriceProtection
{
    public interface IPriceProtection
    {
        Task<bool> LoadClaimDataAsync(string sku, DateTime onhandDate);
        Task<int> ProcessOnhandClaimAsync(string sku, DateTime onhandDate, decimal priceBefore, decimal priceAfter, string user);
        Task<ReceiptInfoBO?> FindReceiptAsync(string receiptNo);
        Task<int> ProcessReceiptClaimAsync(string receiptNo, DateTime dropDate, decimal priceBefore, decimal priceAfter, string user);
        Task<bool> ManualAddImeiAsync(string imei, decimal priceBefore, decimal priceAfter, DateTime onhandDate, string sku, string description, string user);
        Task<bool> ManualRemoveImeiAsync(string imei);
        Task<List<PriceProtectionBatchRow>> GetBatchDataAsync();
        Task<bool> AppendClaimAsync(string password, string user);
        Task<bool> RemoveBatchAsync(int batchNo);
        Task<List<PostedClaimSummaryBO>> GetPostedClaimsSummaryAsync();
        Task<byte[]> GetRawClaimDataExcelAsync(DateTime start, DateTime end);
        Task<int> GetNextBatchIDAsync();
    }
}
