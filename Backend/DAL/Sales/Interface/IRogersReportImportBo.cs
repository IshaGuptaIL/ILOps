using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DAL.Sales.Interface
{
    public interface IRogersReportImportBo
    {
        Task<bool> ProcessAndImportFileAsync(Stream fileStream, string fileType, string fileName, CancellationToken cancellationToken);
        Task<bool> GenerateCmSummaryAsync(CancellationToken cancellationToken);
        Task<bool> ProcessManualRmaImportAsync(CancellationToken cancellationToken);
        Task<bool> DeleteBatchFilesAsync(string cmFile, string rmFile, string manualFile, CancellationToken cancellationToken);
        Task<byte[]> GenerateTemplateAsync(string fileType);
    }
}
