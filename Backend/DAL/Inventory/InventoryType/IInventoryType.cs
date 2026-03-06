using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.InventoryType
{
    public interface IInventoryType
    {

        Task<(List<InventoryBO> data, int totalCount)> GetPagedDataAsync(string type, int page, int pageSize);

        Task<bool> AddGroupAsync(InventoryBO model);
        Task<bool> UpdateGroupAsync(InventoryBO model);
    }
}
