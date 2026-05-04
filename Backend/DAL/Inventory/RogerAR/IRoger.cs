using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.RogerAR
{
    public interface IRoger
    {
        Task<RogerarListResponse> GetARDataAsync(string searchTerm, int pageNumber, int pageSize);
        Task<RogerarListResponse> LoadARDataAsync(string userId, int pageNumber, int pageSize);
        Task<byte[]> ExportToExcelAsync();
        Task<bool> UpdateARDataAsync(RogerarBO item, string userId);



    }
}
