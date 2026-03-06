using DAL.Common.Login;
using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.IMEI.RecieveIMEI
{
    public interface IRecieveImei
    {

        Task<ApiResposne> ClearPackingSlipAsync();
        Task<ApiResposne> InsertPackingSlipAsync(List<RecieveIMEIBO> items);
        Task<ApiResposne> InsertScanListAsync(List<RecieveIMEIBO> items);
        Task<ApiResposne> GetPurchaseOrdersAsync();
         Task<ApiResposne> GetIMEIGridsAsync(string poNumber);
        Task<ApiResposne> CheckErrorsAsync(long poId, long poItemId, bool isReversal);
        Task<ApiResposne> PostReceiptsAsync(long poId, long poItemId, string cmo, bool isReversal);
    }
}
