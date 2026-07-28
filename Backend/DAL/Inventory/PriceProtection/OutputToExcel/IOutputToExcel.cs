using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Inventory.PriceProtection.OutputToExcel
{
    public interface IOutputToExcel
    {
        Task<byte[]> ExportPriceProtectionBatchAsync(int batchId);
        Task<byte[]> ExportRogersOverpaymentsAsync();
        Task<byte[]> ExportClaimsToCreditsAsync();
        Task<List<ClaimsToCreditsRow>> GetClaimsToCreditsDataAsync();
    }
}
