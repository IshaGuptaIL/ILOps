using DAL.Common.Login;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DAL.Inventory.CostValidation
{
    public interface ICostValidation
    {
        Task<List<HpcRecord>> GetHpcLatestAsync();
        Task<List<HpcRecord>> GetHpcDiscrepanciesAsync();
        Task<List<HardwareVsSpire>> GetRDHardwareVsSpireAsync();
        Task<ApiResposne> LoadHPC(Stream excelStream);
         Task<List<CostVarianceCurrentVsAvg>> GetCostVarianceCurrentVsAvgAsync();
        Task<List<CostVarianceAcrossWarehouses>> GetCostVarianceAcrossWarehousesAsync();
    }
}
