using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Inventory.PriceProtection.ImeiSearch
{
    public interface IImeiSearch
    {
        Task<List<ImeiSearchClaimRow>> GetClaimsByImeiAsync(string imei);
        Task<List<ImeiSearchCreditRow>> GetCreditsByImeiAsync(string imei);
        Task<List<ImeiSearchOverpaymentRow>> GetOverpaymentsByImeiAsync(string imei);
    }
}
