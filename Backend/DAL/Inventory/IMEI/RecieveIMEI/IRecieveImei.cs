using DAL.Common.Login;
using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.IMEI.RecieveIMEI
{
    public interface IRecieveImei
    {

        Task<ApiResposne> ClearPackingSlipAsync(int userId);
        Task<ApiResposne> InsertPackingSlipAsync(List<RecieveIMEIBO> items, int userId);
        Task<ApiResposne> InsertScanListAsync(List<RecieveIMEIBO> items, int userId);
        Task<ApiResposne> GetPurchaseOrdersAsync();
        Task<ApiResposne> GetIMEIGridsAsync(string poNumber, int userId);
        Task<ApiResposne> CheckErrorsAsync(long poId, long poItemId, bool isReversal, int userId);
        Task<ApiResposne> PostReceiptsAsync(long poId, long poItemId, string cmo, bool isReversal, int userId);
    }
}
