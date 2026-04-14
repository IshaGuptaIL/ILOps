using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.RunRate
{
    public interface IRunRate
    {

        Task<List<RunRateItem>> GetWFHInventoryAsync();
        Task<List<RunRateItemBO>> GetRunRateAsync(int minDays, int maxDays, int createdId);


        Task<List<HardwareRunRateItem>> GetHardwareAsync(int createdId);
        Task<byte[]> ExportHardwareExcel(Stream templateStream, int createdId);

        Task<int> LoadRunRateDataAsync(DateTime startDate, DateTime endDate, int createdId);
        Task<List<RunRateItemBO>> GetAccessoriesAsync(int createdId);
        Task<byte[]> ExportAccessoriesExcel(Stream templateStream, int createdId);
        Task<PagedResult<AccessoriesRunRateItem>> GetAccessoriesAsyncView(int pageNumber, int pageSize, int createdId);
        Task<byte[]> ExportAccessoriesRogersExcel(Stream templateStream, int createdId);
        Task<PagedResult<HardwareViewItem>> GetHardwareViewAsync(int pageNumber, int pageSize, int createdId);

    }
}
