using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DAL.Inventory.PriceProtection.RogerOverPayments
{
    public interface IRogerOverPayments
    {
        Task<List<ImportedFileRow>> GetImportedFilesSummaryAsync();
        Task<bool> ImportRogersOverpaymentsAsync(Stream fileStream, string filename);
        Task<bool> RemoveRecordsByFileAsync(string filename);
        Task<byte[]> ExportAllOverpaymentsExcelAsync();
    }
}
