using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DAL.Sales.Interface
{
    public interface IRogersReportImportDa
    {
        Task<bool> BulkInsertExcelDataAsync(DataTable data, string destinationTableName, CancellationToken cancellationToken);
        Task<bool> ExecuteStoredProcedureOrQueryAsync(string query, CancellationToken cancellationToken);
        Task<bool> DeleteBatchFilesAsync(string cmFile, string rmFile, string manualFile, CancellationToken cancellationToken);
    }
}
