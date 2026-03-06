using DAL.Common.Login;
using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.AddInventory
{
    public interface IAddInventory
    {

        // Check if part exists
        Task<ApiResposne> CheckPartNo(string partNo, string whse);

        // Add a new inventory item (EN + FR + Postgres)
        Task<ApiResposne> AddInventoryItemAsync(AddInventoryBO model);
        Task<List<ManufacturerBO>> GetManufacturersAsync();
         Task<List<WarehouseBO>> GetWarehousesAsync(int? userRoleId);
    }
}
