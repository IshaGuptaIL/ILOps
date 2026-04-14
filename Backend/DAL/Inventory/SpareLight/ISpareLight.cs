using DAL.Common.Login;
using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.SpareLight
{
    public interface ISpareLight
    {

        Task<List<HardwareTransferBO>> ParseHardwareExcelAsync(System.IO.Stream fileStream);
        Task<List<AccessoryTransferBO>> ParseAccessoryExcelAsync(System.IO.Stream fileStream);

        Task<ApiResposne> ValidateHardwareTransferAsync();
        Task<ApiResposne> DoHardwareTransferAsync(DateTime transferDate);

        Task<ApiResposne> ValidateAccessoryTransferAsync();
        Task<ApiResposne> DoAccessoryTransferAsync(DateTime transferDate);

         Task<ApiResposne> GetTransferLogAsync(DateTime? startDate, DateTime? endDate, string? type);
    }
}
