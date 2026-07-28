using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Sales.RogersInvoiceSpire
{
    public interface IRogersInvoiceSpireDA
    {
        Task<ProcessDataResult> ProcessDataAsync(ProcessDataRequest request, int userId);
        Task<List<CostVerificationRow>> GetCostVerificationReportAsync(string startDate, string endDate);
        Task<List<DailySalesRow>> GetSalesSummaryByPaymentMethodAsync(string startDate, string endDate);
        Task<List<ReturnsVerificationRow>> GetReturnsVerificationReportAsync(string startDate, string endDate, string returnsStart, string returnsEnd, int userId);
        Task<List<CostVerificationRow>> GetHdwFeeReportAsync(int userId);
        Task<byte[]> GetRogersEstimateCsvAsync(int userId);
    }
}
