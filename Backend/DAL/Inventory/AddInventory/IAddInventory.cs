using DAL.Common.Login;
using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.AddInventory
{
    public interface IAddInventory
    {

      
        Task<ApiResposne> CheckPartNo(string partNo, string whse);

   
        Task<ApiResposne> AddInventoryItemAsync(AddInventoryBO model);
        Task<List<ManufacturerBO>> GetManufacturersAsync();
         Task<List<WarehouseBO>> GetWarehousesAsync(int? userRoleId);
    }
}
