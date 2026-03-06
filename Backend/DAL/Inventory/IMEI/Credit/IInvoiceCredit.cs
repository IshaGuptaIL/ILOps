using DAL.Common.Login;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.IMEI.Credit
{
    public interface IInvoiceCredit
    {
         Task<ApiResposne> FindReceiptByBVNoAsync(string bvReceiptNo);
        Task<ApiResposne> SaveInvoiceAsync(SaveInvoiceBO request);
        Task<ApiResposne> GetRogersInvoicesAsync(string receiptNo);
        Task<ApiResposne> GetAllReceiptsAsync();
       Task<ApiResposne> SearchReceiptsAsync(SearchReceiptsBO request);
        Task<ApiResposne> GetMissingReceiptsByPOAsync(string poNumber);
        Task<ApiResposne> GetReceiptsByTypeAsync(string type);
        
        Task<ApiResposne> LoadAccReceipts();

    }
}
