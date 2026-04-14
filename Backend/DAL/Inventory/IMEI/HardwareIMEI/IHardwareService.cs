using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.IMEI.HardwareIMEI
{
    public interface IHardwareService
    {
        Task<ApiResponse<List<PurchaseOrderListItem>>> GetPurchaseOrdersAsync();
        Task<ApiResponse<List<string>>> ParseExcelImeisAsync(Stream fileStream);
        Task<ApiResponse<CheckErrorsResponse>> CheckErrorsAsync(CheckErrorsRequest request);
        Task<ApiResponse<string>> ReceiveImeiAsync(ReceiveImeiRequest request);
    }
}
