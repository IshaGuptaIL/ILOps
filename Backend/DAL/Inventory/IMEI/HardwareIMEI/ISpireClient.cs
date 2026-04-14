using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.IMEI.HardwareIMEI
{
    public interface ISpireClient
    {
        Task<string> GetPurchaseOrdersAsync();
        Task<string> GetPurchaseOrderAsync(long id);
        Task<bool> UpdatePurchaseOrderAsync(long id, string json);
         Task<string> PostReceiptAsync(long id, string sendJson = "");
        Task<string> GetLastReceiptIdAsync(long orderId, string guid);

        Task<string> GetSerialNumbersAsync(string whse, string partNo);

    }
}
