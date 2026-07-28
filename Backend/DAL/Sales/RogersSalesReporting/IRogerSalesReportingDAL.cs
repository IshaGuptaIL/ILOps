using System.Data;
using System.Threading.Tasks;

namespace LegacyApp.DAL.Sales.RogersSalesReporting
{
    public interface IRogerSalesReportingDAL
    {
        Task<DataTable> ExecuteActionAsync(string endpoint, string actionType, string startDate, string endDate, string criteria, string territory, string userCreatedBy);
        Task<bool> UpdateSalesActivationRowAsync(SalesActivationUpdateModel row, string userModifiedBy);
    }
}
