using DAL.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Inventory.AdvantageVoice
{
    public interface ISku
    {
        Task<List<tblSKU>> GetAllSkusAsync();
        Task<tblSKU> GetSkuByIdAsync(int id);
        Task<bool> AddSkuAsync(tblSKU sku);
        Task<bool> UpdateSkuAsync(tblSKU sku);
        Task<bool> DeleteSkuAsync(string skuName);
    }
}
