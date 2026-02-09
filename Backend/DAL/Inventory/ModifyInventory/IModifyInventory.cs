using DAL.Common.Login;
using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.ModifyInventory
{
    public interface IModifyInventory
    {

        Task<ModifyInventoryBO> GetInventoryAsync(string search, int page, int size);

        Task<List<WarehousePriceBO>> GetAllWarehousesAsync(string partNo, string skipWhse);

        Task<ApiResposne> UpdatePriceAsync(PriceUpdateModel model, bool applyToAll);
    }
}
