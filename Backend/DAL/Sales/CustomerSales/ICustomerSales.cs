using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Sales.CustomerSales
{
    public interface ICustomerSales
    {
        Task<List<CustomerGroupBO>> GetCustomerGroupsAsync();
        Task<List<BVCustomerBO>> GetCustomersInGroupAsync(string groupName);
        Task<bool> GenerateCustomerSalesDataAsync(CustomerSalesRequest request, int userId);
        Task<List<CustomerSalesRow>> GetGeneratedDataAsync(string groupName);
        Task<byte[]> ExportToExcelAsync(CustomerSalesRequest request, int userId);
        Task<byte[]> ExportToCsvAsync(CustomerSalesRequest request, int userId);
        Task<byte[]> ExportPerCustomerAsync(CustomerSalesRequest request, int userId);
        Task<bool> GenerateListByMSDAsync(CustomerSalesRequest request, int userId);
        Task<bool> GenerateListByTerritoryAsync(CustomerSalesRequest request, int userId);
        Task<bool> AddFDDealerGroupAsync(int userId);
        Task<bool> CreateCustomerGroupAsync(CreateGroupRequest request, int userId);
        Task<bool> DeleteCustomerGroupAsync(string groupName, int userId);
        Task<List<CustomerFieldBO>> GetCustomerFieldsAsync(string groupName);
        Task<bool> UpdateCustomerFieldsAsync(string groupName, List<CustomerFieldBO> fields);
        Task<byte[]> GenerateSunLifeReportAsync(CustomerSalesRequest request, int userId);
        Task<byte[]> GenerateSplitPaymentReportAsync(CustomerSalesRequest request, string format, int userId);
        Task<bool> UpdateGeneratedDataAsync(List<CustomerSalesRow> data, int userId);
        Task<bool> AddCustomerToGroupAsync(string groupCode, BVCustomerBO customer, int userId);
        Task<bool> UpdateCustomerInGroupAsync(string groupCode, string oldCustNo, BVCustomerBO customer, int userId);
        Task<bool> RemoveCustomerFromGroupAsync(string groupCode, string custNo, int userId);
    }
}
