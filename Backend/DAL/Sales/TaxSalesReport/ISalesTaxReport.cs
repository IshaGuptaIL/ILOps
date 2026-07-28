using DAL.Models;
using DAL.Sales.TaxSalesReport;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Sales.BO
{
   

    public interface ISalesTaxReport
    {
        Task<SalesTaxReportResponse> GetSalesTaxReportAsync(SalesTaxReportRequest request, int userId);
        Task<byte[]> ExportToExcelAsync(SalesTaxReportRequest request, int userId);
        
        // New Load Methods
        Task<bool> LoadSalesTaxHistoryAsync(SalesTaxReportRequest request, int userId);
        Task<bool> LoadGLDataAsync(SalesTaxReportRequest request, int userId);
        Task<byte[]> ExportVendorActivityAsync(string vendor, System.DateTime start, System.DateTime end);
        Task<byte[]> ExportGLITCExcelAsync(SalesTaxReportRequest request, int userId);
        Task<byte[]> ExportGLDataExcelAsync(SalesTaxReportRequest request, int userId);

        Task<List<TaxCodeHistory>> GetTaxCodeHistoryAsync();
        Task<bool> SaveTaxCodeHistoryAsync(TaxCodeHistory history, int userId);
        Task<bool> DeleteTaxCodeHistoryAsync(int id);
        Task<List<VendorBO>> GetVendorsAsync();
    }
}
